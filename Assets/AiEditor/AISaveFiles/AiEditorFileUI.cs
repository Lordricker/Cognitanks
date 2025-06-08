using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AiEditor;

public class AiEditorFileUI : MonoBehaviour
{
    public Button saveButton;
    public Button loadButton;
    public GameObject loadPanel;
    public Button turretBranchButton;
    public Button navBranchButton;
    public GameObject fileButtonPrefab;
    public Button startTurretButton;
    public Button startNavButton;
    public GameObject navFileScrollView; // Assign the ScrollView GameObject for Nav files
    public GameObject turretFileScrollView; // Assign the ScrollView GameObject for Turret files
    public Transform navFileContent; // Assign the Content transform of the Nav ScrollView
    public Transform turretFileContent; // Assign the Content transform of the Turret ScrollView

    private string navFolder = "NavFiles";
    private string turretFolder = "TurretFiles";

    // Track the currently loaded asset path for update-only saves
    private string currentAssetPath = null;

    // Prefabs for node types (assign in inspector)
    // public GameObject startNodePrefab; // No longer needed, always reuse StartNodePanel
    public GameObject EndNodePrefab;
    public GameObject MiddleNodePrefab;
    public GameObject SubAINodePrefab;
    public GameObject UILinePrefab;

    // New public field for tree name
    public TMPro.TMP_Text FileName;

    void Start()
    {
        saveButton.onClick.AddListener(OnSaveClicked);
        loadButton.onClick.AddListener(ToggleLoadPanel);
        
        turretBranchButton.onClick.AddListener(() => ShowFilePanel(turretFileScrollView, turretFileContent, turretFolder));
        navBranchButton.onClick.AddListener(() => ShowFilePanel(navFileScrollView, navFileContent, navFolder));
        loadPanel.SetActive(false);
        navFileScrollView.SetActive(false);
        turretFileScrollView.SetActive(false);
    }

    void ToggleLoadPanel()
    {
        loadPanel.SetActive(!loadPanel.activeSelf);
        if (!loadPanel.activeSelf)
        {
            navFileScrollView.SetActive(false);
            turretFileScrollView.SetActive(false);
        }
    }
    
    void OnSaveClicked()
    {
        // Determine branch by which start button is active
        string folder = "";
        if (startTurretButton.gameObject.activeSelf)
            folder = turretFolder;
        else if (startNavButton.gameObject.activeSelf)
            folder = navFolder;
        else
            return; // No branch selected
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        // Get the filename from the starting node's label (replace spaces with _)
        // Always get the tree name from the FileName field if set, otherwise fallback to FileNameText under StartNodePanel
        string treeName = "NewAI";
        if (FileName != null && !string.IsNullOrEmpty(FileName.text))
        {
            treeName = FileName.text;
        }
        else
        {
            var startNodePanel = GameObject.Find("StartNodePanel");
            if (startNodePanel != null)
            {
                var fileNameText = startNodePanel.transform.Find("FileNameText");
                if (fileNameText != null)
                {
                    var tmp = fileNameText.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                        treeName = tmp.text;
                }
            }
        }
        string assetName = treeName.Replace(' ', '_');
        // Only allow updating an existing file
        if (!string.IsNullOrEmpty(currentAssetPath) && File.Exists(currentAssetPath))
        {
            #if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiTreeAsset>(currentAssetPath);
            if (asset != null)
            {
                asset.TreeName = treeName;
                asset.branchType = (folder == navFolder) ? AiEditor.AiBranchType.Nav : AiEditor.AiBranchType.Turret;
                // --- Serialize nodes and connections ---
                var content = GameObject.Find("Content");
                var nodeDraggables = content.GetComponentsInChildren<NodeDraggable>();
                var nodeList = new List<AiEditor.AiNodeData>();
                var nodeIdToDraggable = new Dictionary<string, NodeDraggable>();
                foreach (var node in nodeDraggables)
                {
                    // Ensure nodeId is set
                    if (string.IsNullOrEmpty(node.nodeId))
                        node.nodeId = System.Guid.NewGuid().ToString();
                    // Find the child named "NodeText" with TMP_Text
                    string label = node.name;
                    var textChild = node.transform.Find("NodeText");
                    if (textChild != null)
                    {
                        var tmp = textChild.GetComponent<TMPro.TMP_Text>();
                        if (tmp != null)
                            label = tmp.text;
                    }
                    // Save the node type as the GameObject name (e.g., EndNode(Clone), MiddleNode(Clone), SubAINode(Clone))
                    string nodeType = node.name;
                    var nodeData = new AiEditor.AiNodeData
                    {
                        nodeId = node.nodeId,
                        nodeType = nodeType, // e.g., EndNode(Clone)
                        nodeLabel = label,   // NodeText
                        position = node.GetComponent<RectTransform>().anchoredPosition,
                        properties = new Dictionary<string, string>()
                    };
                    nodeList.Add(nodeData);
                    nodeIdToDraggable[node.nodeId] = node;
                }
                // Save all output connections for each node
                var lineConnectors = content.GetComponentsInChildren<UILineConnector>();
                var connectionList = new List<AiEditor.AiConnectionData>();
                foreach (var line in lineConnectors)
                {
                    // Check if outputRect is StartNavButton or StartTurretButton under StartNodePanel
                    var outputButton = line.outputRect != null ? line.outputRect.GetComponent<Button>() : null;
                    string fromNodeId = null;
                    string fromPortId = null;
                    if (outputButton != null)
                    {
                        if (outputButton.gameObject.name == "StartNavButton")
                        {
                            fromNodeId = "StartNavButton";
                            fromPortId = "NavOrigin";
                        }
                        else if (outputButton.gameObject.name == "StartTurretButton")
                        {
                            fromNodeId = "StartTurretButton";
                            fromPortId = "TurretOrigin";
                        }
                    }
                    var fromNode = line.outputRect != null ? line.outputRect.GetComponentInParent<NodeDraggable>() : null;
                    var toNode = line.inputRect != null ? line.inputRect.GetComponentInParent<NodeDraggable>() : null;
                    if (fromNodeId != null && toNode != null)
                    {
                        string toPortId = line.inputRect != null ? line.inputRect.gameObject.name : "InputPort";
                        string toNodeId = toNode.nodeId;
                        connectionList.Add(new AiEditor.AiConnectionData
                        {
                            fromNodeId = fromNodeId,
                            fromPortId = fromPortId,
                            toNodeId = toNodeId,
                            toPortId = toPortId
                        });
                    }
                    else if (fromNode != null && toNode != null)
                    {
                        // Store the tag for the output port if it's an origin (NavOrigin, TurretOrigin), otherwise use OutputPort
                        string portId = "OutputPort";
                        if (line.outputRect != null)
                        {
                            var tag = line.outputRect.gameObject.tag;
                            if (tag == "NavOrigin" || tag == "TurretOrigin")
                                portId = tag;
                            else
                                portId = line.outputRect.gameObject.name;
                        }
                        string toPortId = line.inputRect != null ? line.inputRect.gameObject.name : "InputPort";
                        connectionList.Add(new AiEditor.AiConnectionData
                        {
                            fromNodeId = fromNode.nodeId,
                            fromPortId = portId,
                            toNodeId = toNode.nodeId,
                            toPortId = toPortId
                        });
                    }                }
                asset.nodes = nodeList;
                asset.connections = connectionList;
                
                // Generate execution data
                GenerateExecutionData(asset, nodeList, connectionList);
                
                UnityEditor.EditorUtility.SetDirty(asset);
                // If the name has changed, rename the asset file
                string newFileName = assetName + ".asset";
                string newPath = Path.Combine("Assets", "AiEditor", "AISaveFiles", folder, newFileName);
                if (!currentAssetPath.EndsWith(newFileName))
                {
                    UnityEditor.AssetDatabase.RenameAsset(currentAssetPath, assetName);
                    currentAssetPath = newPath;
                }
                UnityEditor.AssetDatabase.SaveAssets();
            }
            #endif
        }
        else
        {
            // TEMP: Allow new file creation if no file is loaded
            #if UNITY_EDITOR
            var asset = ScriptableObject.CreateInstance<AiTreeAsset>();
            asset.name = assetName;
            asset.TreeName = treeName;
            asset.branchType = (folder == navFolder) ? AiEditor.AiBranchType.Nav : AiEditor.AiBranchType.Turret;
            // --- Serialize nodes and connections ---
            var content = GameObject.Find("Content");
            var nodeDraggables = content.GetComponentsInChildren<NodeDraggable>();
            var nodeList = new List<AiEditor.AiNodeData>();
            var nodeIdToDraggable = new Dictionary<string, NodeDraggable>();
            foreach (var node in nodeDraggables)
            {
                if (string.IsNullOrEmpty(node.nodeId))
                    node.nodeId = System.Guid.NewGuid().ToString();
                var title = node.GetComponentInChildren<TitleName>();
                string label = title != null ? title.titleText.text : node.name;
                string type = label;
                var nodeData = new AiEditor.AiNodeData
                {
                    nodeId = node.nodeId,
                    nodeType = type,
                    nodeLabel = label,
                    position = node.GetComponent<RectTransform>().anchoredPosition,
                    properties = new Dictionary<string, string>()
                };
                nodeList.Add(nodeData);
                nodeIdToDraggable[node.nodeId] = node;
            }
            var lineConnectors = content.GetComponentsInChildren<UILineConnector>();
            var connectionList = new List<AiEditor.AiConnectionData>();
            foreach (var line in lineConnectors)
            {
                var fromNode = line.outputRect != null ? line.outputRect.GetComponentInParent<NodeDraggable>() : null;
                var toNode = line.inputRect != null ? line.inputRect.GetComponentInParent<NodeDraggable>() : null;
                if (fromNode != null && toNode != null)
                {
                    connectionList.Add(new AiEditor.AiConnectionData
                    {
                        fromNodeId = fromNode.nodeId,
                        fromPortId = "OutputPort",
                        toNodeId = toNode.nodeId,
                        toPortId = "InputPort"
                    });
                }
            }            asset.nodes = nodeList;
            asset.connections = connectionList;
            
            // Generate execution data
            GenerateExecutionData(asset, nodeList, connectionList);
            
            string path = Path.Combine("Assets", "AiEditor", "AISaveFiles", folder, assetName + ".asset");
            UnityEditor.AssetDatabase.CreateAsset(asset, path);
            UnityEditor.AssetDatabase.SaveAssets();
            currentAssetPath = path;
            #endif
        }
    }

    // Helper to get the label from the starting node (FileNameText under StartNodePanel)
    string GetStartNodeLabel()
    {
        // Find the StartNodePanel in the scene
        var startNodePanel = GameObject.Find("StartNodePanel");
        if (startNodePanel != null)
        {
            var fileNameText = startNodePanel.GetComponentInChildren<TMPro.TMP_Text>();
            if (fileNameText != null)
                return fileNameText.text;
        }
        return "";
    }

    void ShowFilePanel(GameObject scrollView, Transform contentPanel, string folder)
    {
        navFileScrollView.SetActive(false);
        turretFileScrollView.SetActive(false);
        scrollView.SetActive(true);
        // Clear previous
        foreach (Transform child in contentPanel) Destroy(child.gameObject);
        if (!Directory.Exists(Path.Combine("Assets", "AiEditor", "AISaveFiles", folder))) return;
        var files = Directory.GetFiles(Path.Combine("Assets", "AiEditor", "AISaveFiles", folder), "*.asset").OrderBy(f => f).ToArray();
        foreach (var file in files)
        {
            var btnObj = Instantiate(fileButtonPrefab, contentPanel);
            var btn = btnObj.GetComponent<Button>();
            var txt = btnObj.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt != null) txt.text = Path.GetFileNameWithoutExtension(file);
            btn.onClick.AddListener(() => OnFileSelected(file));
        }
    }

    void OnFileSelected(string filePath)
    {
        // Load AiTreeAsset and reconstruct node graph
        currentAssetPath = filePath.Replace("\\", "/"); // Track the loaded file for update-only saves
#if UNITY_EDITOR
    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiTreeAsset>(currentAssetPath);
    if (asset != null)
    {
        // Set FileName field if present (ALWAYS, regardless of node loop)
        if (FileName != null)
        {
            FileName.text = asset.TreeName;
            Debug.Log($"[AiEditorFileUI] (OnFileSelected) Set FileName.text to loaded TreeName: {asset.TreeName}");
        }
        
        // Clear existing nodes and lines from the Content panel, except StartNodePanel
        var content = GameObject.Find("Content");
        var startPanel = GameObject.Find("StartNodePanel");
        // Explicitly destroy all node and line prefabs before loading
        var toDestroy = new List<GameObject>();
        foreach (Transform child in content.transform)
        {
            if (child.gameObject == startPanel) continue;
            string n = child.gameObject.name;
            if (n == "UILine(Clone)" || n == "MiddleNode(Clone)" || n == "SubAINode(Clone)" || n == "EndNode(Clone)")
                toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
            DestroyImmediate(go);

        // --- Always run branch label/button hiding logic FIRST ---
        if (asset.branchType == AiEditor.AiBranchType.Nav)
        {
            var turretLabel = GameObject.Find("TurretLabel");
            if (turretLabel != null) turretLabel.SetActive(false);
            
            var navLabel = GameObject.Find("NavLabel");
            if (navLabel != null) navLabel.SetActive(true);
            
            // Find buttons under StartNodePanel (consistent with connection code)
            if (startPanel != null)
            {
                var turretBtn = startPanel.transform.Find("StartTurretButton");
                if (turretBtn != null) turretBtn.gameObject.SetActive(false);
                
                var navBtn = startPanel.transform.Find("StartNavButton");
                if (navBtn != null) navBtn.gameObject.SetActive(true);
                
                Debug.Log("[AiEditorFileUI] Nav file loaded - activated StartNavButton, deactivated StartTurretButton");
            }
        }
        else if (asset.branchType == AiEditor.AiBranchType.Turret)
        {
            var navLabel = GameObject.Find("NavLabel");
            if (navLabel != null) navLabel.SetActive(false);
            
            var turretLabel = GameObject.Find("TurretLabel");
            if (turretLabel != null) turretLabel.SetActive(true);
            
            // Find buttons under StartNodePanel (consistent with connection code)
            if (startPanel != null)
            {
                var navBtn = startPanel.transform.Find("StartNavButton");
                if (navBtn != null) navBtn.gameObject.SetActive(false);
                
                var turretBtn = startPanel.transform.Find("StartTurretButton");
                if (turretBtn != null) turretBtn.gameObject.SetActive(true);
                
                Debug.Log("[AiEditorFileUI] Turret file loaded - activated StartTurretButton, deactivated StartNavButton");
            }
        }

        // Load nodes and connections
        LoadNodesFromAsset(asset, content, startPanel);
        LoadConnectionsFromAsset(asset, content, startPanel);
        
        loadPanel.SetActive(false);
    }
    
    void LoadNodesFromAsset(AiTreeAsset asset, GameObject content, GameObject startPanel)
    {
        // --- NodeId-based mapping ---
        var nodeIdToGameObject = new Dictionary<string, GameObject>();
        // --- Handle StartNodePanel and all nodes ---
        foreach (var nodeData in asset.nodes)
        {
            Debug.Log($"Loading node: type={nodeData.nodeType}, label={nodeData.nodeLabel}, nodeId={nodeData.nodeId}");
            GameObject nodeGO = null;            if ((nodeData.nodeType == "Start" || nodeData.nodeLabel == asset.TreeName) && startPanel != null)
            {
                // Update StartNodePanel label and position
                // Set FileNameText to asset.TreeName
                var fileNameTextObj = startPanel.transform.Find("FileNameText");
                if (fileNameTextObj != null)
                {
                    var tmp = fileNameTextObj.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.text = asset.TreeName;
                        Debug.Log($"[AiEditorFileUI] Set FileNameText to loaded TreeName: {asset.TreeName}");
                    }
                }
                var rect = startPanel.GetComponent<RectTransform>();
                rect.anchoredPosition = nodeData.position;
                var title = startPanel.GetComponentInChildren<TitleName>();
                if (title != null)
                    title.SetTitle(nodeData.nodeLabel);                // Set nodeId if possible
                var nd = startPanel.GetComponent<NodeDraggable>();
                if (nd != null) nd.nodeId = nodeData.nodeId;
                // Set TMP_Text child named "Text" to node label
                var textChild = startPanel.transform.Find("Text");
                if (textChild != null)
                {
                    var tmp = textChild.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null)
                        tmp.text = nodeData.nodeLabel;
                }
                nodeGO = startPanel;
            }
            else
            {
                // Instantiate other nodes based on type
                if (nodeData.nodeType.StartsWith("EndNode"))
                {
                    nodeGO = Instantiate(EndNodePrefab, content.transform);
                }
                else if (nodeData.nodeType.StartsWith("MiddleNode"))
                {
                    nodeGO = Instantiate(MiddleNodePrefab, content.transform);
                }
                else if (nodeData.nodeType.StartsWith("SubAINode"))
                {
                    nodeGO = Instantiate(SubAINodePrefab, content.transform);
                }                
                else
                {
                    // Fallback: use EndNodePrefab
                    nodeGO = Instantiate(EndNodePrefab, content.transform);
                }
                var rect = nodeGO.GetComponent<RectTransform>();
                rect.anchoredPosition = nodeData.position;
                var title = nodeGO.GetComponentInChildren<TitleName>();
                if (title != null)
                    title.SetTitle(nodeData.nodeLabel);
                // Also set TMP_Text directly if present (for action nodes)
                var labelText = nodeGO.GetComponentInChildren<TMPro.TMP_Text>();
                if (labelText != null)
                    labelText.text = nodeData.nodeLabel;
                // Set nodeId
                var nd = nodeGO.GetComponent<NodeDraggable>();
                if (nd != null) nd.nodeId = nodeData.nodeId;
                // Set TMP_Text child named "Text" to node label
                var textChild = nodeGO.transform.Find("Text");
                if (textChild != null)
                {
                    var tmp = textChild.GetComponent<TMPro.TMP_Text>();
                    if (tmp != null)
                        tmp.text = nodeData.nodeLabel;                }
            }
              // Setup NumberInputButton if the label contains # and we have execution data
            if (nodeData.nodeLabel.Contains("#") && nodeGO != null)
            {
                var numberInputButton = nodeGO.transform.Find("NumberInputButton");
                if (numberInputButton != null)
                {
                    numberInputButton.gameObject.SetActive(true);
                    var inlineNumberInput = numberInputButton.GetComponent<InlineNumberInput>();
                    if (inlineNumberInput != null)
                    {
                        // Set template first
                        inlineNumberInput.SetTemplate(nodeData.nodeLabel);
                          // Find the corresponding executable node and set the numeric value
                        var executableNode = asset.executableNodes.Find(n => n.nodeId == nodeData.nodeId);
                        if (executableNode != null)
                        {
                            // Always set the value from the save file, even if it's 0
                            string valueToSet = executableNode.numericValue.ToString();
                            inlineNumberInput.SetCurrentNumber(valueToSet);
                            Debug.Log($"[AiEditorFileUI] Loaded number {executableNode.numericValue} for node {nodeData.nodeLabel}");
                            
                            // Ensure the value sticks by setting it again after a frame delay
                            StartCoroutine(VerifyNumberDisplayAfterDelay(inlineNumberInput, valueToSet, nodeData.nodeLabel));
                        }
                        else
                        {
                            Debug.LogWarning($"[AiEditorFileUI] No executable node found for {nodeData.nodeId}");
                        }
                    }
                }
            }
            
            // Register in map
            if (!string.IsNullOrEmpty(nodeData.nodeId) && nodeGO != null)
                nodeIdToGameObject[nodeData.nodeId] = nodeGO;        }
    }
    
    void LoadConnectionsFromAsset(AiTreeAsset asset, GameObject content, GameObject startPanel)
    {
        // --- Create nodeId mapping for connections ---
        var nodeIdToGameObject = new Dictionary<string, GameObject>();
        
        // Add StartNodePanel to mapping
        if (startPanel != null)
        {
            var startDraggable = startPanel.GetComponent<NodeDraggable>();
            if (startDraggable != null && !string.IsNullOrEmpty(startDraggable.nodeId))
            {
                nodeIdToGameObject[startDraggable.nodeId] = startPanel;
            }
        }
        
        // Add all other nodes to mapping
        var nodeDraggables = content.GetComponentsInChildren<NodeDraggable>();
        foreach (var node in nodeDraggables)
        {
            if (!string.IsNullOrEmpty(node.nodeId) && node.gameObject != startPanel)
            {
                nodeIdToGameObject[node.nodeId] = node.gameObject;
            }
        }
        
        // --- Recreate connections using nodeId mapping ---
        foreach (var conn in asset.connections)
        {
            // Special handling for StartNavButton/StartTurretButton as origin
            GameObject fromNode = null;
            Button outputButton = null;            
            
            if (conn.fromNodeId == "StartNavButton")
            {
                if (startPanel != null)
                {
                    var navBtn = startPanel.transform.Find("StartNavButton");
                    if (navBtn != null && navBtn.gameObject.activeSelf)
                        outputButton = navBtn.GetComponent<Button>();
                }
                fromNode = startPanel;
            }
            else if (conn.fromNodeId == "StartTurretButton")
            {
                if (startPanel != null)
                {
                    var turretBtn = startPanel.transform.Find("StartTurretButton");
                    if (turretBtn != null && turretBtn.gameObject.activeSelf)
                        outputButton = turretBtn.GetComponent<Button>();
                }
                fromNode = startPanel;
            }
            else if (!string.IsNullOrEmpty(conn.fromNodeId) && nodeIdToGameObject.ContainsKey(conn.fromNodeId))
            {
                fromNode = nodeIdToGameObject[conn.fromNodeId];
            }
            
            if (string.IsNullOrEmpty(conn.toNodeId) || !nodeIdToGameObject.ContainsKey(conn.toNodeId)) continue;
            var toNode = nodeIdToGameObject[conn.toNodeId];
            
            // Find input port/button
            Button inputButton = null;
            foreach (var btn in toNode.GetComponentsInChildren<Button>())
                if (btn.CompareTag("InputPort")) { inputButton = btn; break; }
            
            // For non-origin, find output port/button
            if (outputButton == null && fromNode != null)
            {
                if (conn.fromPortId == "NavOrigin" || conn.fromPortId == "TurretOrigin")
                {
                    foreach (var btn in fromNode.GetComponentsInChildren<Button>())
                        if (btn.CompareTag(conn.fromPortId)) { outputButton = btn; break; }
                }
                else
                {
                    foreach (var btn in fromNode.GetComponentsInChildren<Button>())
                        if (btn.CompareTag("OutputPort")) { outputButton = btn; break; }
                }
            }
            
            if (outputButton == null || inputButton == null) continue;
            
            // Instantiate line using UILinePrefab
            var lineGO = Instantiate(UILinePrefab, content.transform);
            var lineRect = lineGO.GetComponent<RectTransform>();
            
            // Set up UILineConnector
            var connector = lineGO.GetComponent<UILineConnector>();
            if (connector == null) connector = lineGO.AddComponent<UILineConnector>();
            connector.outputRect = outputButton.GetComponent<RectTransform>();
            connector.inputRect = inputButton.GetComponent<RectTransform>();
            connector.canvas = content.GetComponentInParent<Canvas>();
            connector.UpdateLine();
            
            // Add click-to-delete functionality
            if (lineGO.GetComponent<UILineClickDeleter>() == null)
                lineGO.AddComponent<UILineClickDeleter>();
            
            // Register with NodeDraggable for drag updates
            var fromDraggable = fromNode != null ? fromNode.GetComponent<NodeDraggable>() : null;
            var toDraggable = toNode.GetComponent<NodeDraggable>();            
            if (fromDraggable != null) fromDraggable.RegisterConnectedLine(connector);
            if (toDraggable != null) toDraggable.RegisterConnectedLine(connector);        
        }
    }
#endif
        loadPanel.SetActive(false);
    }

    /// <summary>
    /// Generates execution data from the visual node graph for AI runtime execution
    /// </summary>
    private void GenerateExecutionData(AiEditor.AiTreeAsset asset, List<AiEditor.AiNodeData> nodeList, List<AiEditor.AiConnectionData> connectionList)
    {
        asset.executableNodes.Clear();
        
        // Find the start node ID (either StartNavButton or StartTurretButton connections)
        asset.startNodeId = null;
        foreach (var conn in connectionList)
        {
            if (conn.fromNodeId == "StartNavButton" || conn.fromNodeId == "StartTurretButton")
            {
                asset.startNodeId = conn.toNodeId;
                break;
            }
        }
          // Convert each node to executable format
        foreach (var nodeData in nodeList)
        {
            float numericValue = 0f;
            string methodName = AiEditor.AiMethodConverter.ConvertToMethodName(nodeData.nodeLabel, out numericValue);
            AiEditor.AiNodeType nodeType = AiEditor.AiMethodConverter.DetermineNodeType(nodeData.nodeLabel);
            
            // Check if this node has a NumberInputButton with InlineNumberInput - extract the actual number
            var nodeGameObject = FindNodeGameObjectById(nodeData.nodeId);
            if (nodeGameObject != null)
            {
                var numberInputButton = nodeGameObject.transform.Find("NumberInputButton");
                if (numberInputButton != null && numberInputButton.gameObject.activeSelf)
                {
                    var inlineNumberInput = numberInputButton.GetComponent<InlineNumberInput>();
                    if (inlineNumberInput != null)
                    {
                        string currentNumberStr = inlineNumberInput.GetCurrentNumber();
                        if (float.TryParse(currentNumberStr, out float parsedNumber))
                        {
                            numericValue = parsedNumber;
                            Debug.Log($"[AiEditorFileUI] Extracted number {numericValue} from node {nodeData.nodeLabel}");
                        }
                    }
                }
            }
            
            var executableNode = new AiEditor.AiExecutableNode
            {
                nodeId = nodeData.nodeId,
                methodName = methodName,
                originalLabel = nodeData.nodeLabel,
                nodeType = nodeType,
                numericValue = numericValue,
                position = nodeData.position,
                connectedNodeIds = new List<string>()
            };
            
            // Find all nodes this one connects to
            foreach (var conn in connectionList)
            {
                if (conn.fromNodeId == nodeData.nodeId)
                {
                    executableNode.connectedNodeIds.Add(conn.toNodeId);
                }
            }
            
            // Sort connected nodes by Y-position for priority handling
            executableNode.connectedNodeIds.Sort((id1, id2) => {
                var node1 = nodeList.Find(n => n.nodeId == id1);
                var node2 = nodeList.Find(n => n.nodeId == id2);
                if (node1 == null || node2 == null) return 0;
                return node1.position.y.CompareTo(node2.position.y); // Higher Y = higher priority
            });
            
            asset.executableNodes.Add(executableNode);
        }
          Debug.Log($"[AiEditorFileUI] Generated execution data: {asset.executableNodes.Count} executable nodes, start: {asset.startNodeId}");
    }
    
    /// <summary>
    /// Helper method to find a node GameObject by its nodeId
    /// </summary>
    private GameObject FindNodeGameObjectById(string nodeId)
    {
        var content = GameObject.Find("Content");
        if (content == null) return null;
        
        var nodeDraggables = content.GetComponentsInChildren<NodeDraggable>();
        foreach (var node in nodeDraggables)
        {
            if (node.nodeId == nodeId)
                return node.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Verifies that the number input displays the correct value after a delay
    /// This ensures the value persists after Unity's initialization
    /// </summary>
    private System.Collections.IEnumerator VerifyNumberDisplayAfterDelay(InlineNumberInput input, string expectedValue, string nodeLabel)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Wait two frames to be sure
        
        if (input != null)
        {
            string currentValue = input.GetCurrentNumber();
            if (currentValue != expectedValue)
            {
                Debug.LogWarning($"[AiEditorFileUI] Number value mismatch for {nodeLabel}: expected {expectedValue}, got {currentValue}. Re-setting...");
                input.SetCurrentNumber(expectedValue);
            }
            else
            {
                Debug.Log($"[AiEditorFileUI] Number display verified for {nodeLabel}: {currentValue}");
            }
        }
    }
}
