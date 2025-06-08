using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using AiEditor;

public class WorkshopUIManager : MonoBehaviour
{
    public Toggle shopToggle;
    public Toggle inventoryToggle;    public Toggle turretToggle;
    public Toggle armorToggle;
    public Toggle turretAIToggle;
    public Toggle navAIToggle;
    public Toggle engineFrameToggle;

    public Transform scrollContentParent;
    public GameObject componentEntryPrefab;

    private bool isShopView = true;
    private ComponentCategory currentCategory = ComponentCategory.Turret;    [Header("Shop Components By Category")]
    public List<ComponentData> turretShopComponents;
    public List<ComponentData> armorShopComponents;
    public List<ComponentData> turretAIShopComponents;
    public List<ComponentData> navAIShopComponents;
    public List<ComponentData> aiTreeShopComponents;
    public List<ComponentData> engineFrameShopComponents;

    public List<ComponentData> playerInventory;

    [Header("Player Cash")]
    public int playerCash = 1000;
    public TMP_Text playerCashText;

    public List<TankSlotButtonUI> tankSlots;
    private TankSlotButtonUI selectedTankSlot;    public WorkshopModelPreview modelPreview;
    public WorkshopStatsPanel statsPanel;    [Header("Debug UI")]
    public TMP_Text debugText; // Assign in inspector

    private Coroutine debugTextCoroutine;
    private ComponentData selectedComponent;

    public TMP_Text itemStatsText;
    public TMP_Text descriptionText;

    private void Start()
    {
        // Ensure only one of Shop/Inventory is active
        shopToggle.isOn = true;
        inventoryToggle.isOn = false;        // Ensure only one category is active
        turretToggle.isOn = true;
        armorToggle.isOn = false;
        turretAIToggle.isOn = false;
        navAIToggle.isOn = false;
        engineFrameToggle.isOn = false;

        SetViewShop(true);
        SetCategory(ComponentCategory.Turret);

        shopToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetViewShop(true); });
        inventoryToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetViewShop(false); });        turretToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetCategory(ComponentCategory.Turret); });
        armorToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetCategory(ComponentCategory.Armor); });
        turretAIToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetCategory(ComponentCategory.TurretAI); });
        navAIToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetCategory(ComponentCategory.NavAI); });
        engineFrameToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetCategory(ComponentCategory.EngineFrame); });

        UpdatePlayerCashUI();
        UpdateToggleColors();

        // Setup tank slot button listeners
        for (int i = 0; i < tankSlots.Count; i++)
        {
            int idx = i;
            tankSlots[i].button.onClick.AddListener(() => OnTankSlotSelected(idx));
            tankSlots[i].SetSelected(false);
        }
        selectedTankSlot = null;

        LoadPlayerInventoryFromSave();
        LoadTankSlotsFromScriptableObjects();
        
        // Load AI components from AI Editor folders
        LoadAIComponentsFromFolders();        // Restore component data references for all tank slots after loading
        var allSlotData = new List<TankSlotData>();
        foreach (var slot in tankSlots)
        {
            if (slot.slotData != null)
                allSlotData.Add(slot.slotData);
        }        PlayerDataManager.Instance.RestoreComponentDataReferences(allSlotData);
    }    private void LoadPlayerInventoryFromSave()
    {
        playerInventory.Clear();
        foreach (var entry in PlayerDataManager.Instance.playerData.ownedComponents)
        {
            ComponentData prefab = FindComponentPrefabById(entry.id);
            if (prefab != null)
            {
                foreach (var instanceId in entry.instanceIds)
                {
                    ComponentData newComp = Instantiate(prefab);
                    newComp.instanceId = instanceId;
                    playerInventory.Add(newComp);
                }
            }
        }
        
        // Add default components for new players if inventory is empty
        if (playerInventory.Count == 0)
        {
            Debug.Log("No saved inventory found. Adding default components for new player.");
            AddDefaultComponentsToInventory();
        }
    }      private void AddDefaultComponentsToInventory()
    {
        // Add default AITree components that tank slots expect - now stored on disk
        var defaultTurretAI = aiTreeShopComponents.Find(c => c.id == "Aggressive Hunter" && 
            c is AiTreeAsset tree && tree.branchType == AiEditor.AiBranchType.Turret);
        if (defaultTurretAI != null)
        {
#if UNITY_EDITOR
            // Create AI assets on disk instead of adding to inventory
            
            // Add TurretAI for Tank Slot 0
            ComponentData turretAI1 = Instantiate(defaultTurretAI);
            turretAI1.instanceId = "Aggressive Hunter_ce9255c7-8383-4919-a8ba-d1686373d471";
            string assetPath1 = "Assets/AiEditor/AISaveFiles/TurretFiles/" + turretAI1.instanceId + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(turretAI1, assetPath1);
            
            // Add TurretAI for Tank Slot 9
            ComponentData turretAI2 = Instantiate(defaultTurretAI);
            turretAI2.instanceId = "Aggressive Hunter_c082be34-e8c2-4937-8aaf-e9f11fced160";
            string assetPath2 = "Assets/AiEditor/AISaveFiles/TurretFiles/" + turretAI2.instanceId + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(turretAI2, assetPath2);
            
            UnityEditor.AssetDatabase.SaveAssets();
            
            Debug.Log($"Created default AITree Turret components on disk: {turretAI1.instanceId}, {turretAI2.instanceId}");
            
            // Note: AI components are no longer tracked in PlayerData.ownedComponents since they're stored on disk
#endif
        }
        else
        {
            Debug.LogError("Could not find 'Aggressive Hunter' AITree Turret component in shop components!");
        }
    }private ComponentData FindComponentPrefabById(string id)
    {
        // Search all shop lists for a matching id
        foreach (var c in turretShopComponents) if (c.id == id) return c;
        foreach (var c in armorShopComponents) if (c.id == id) return c;
        foreach (var c in aiTreeShopComponents) if (c.id == id) return c;
        foreach (var c in engineFrameShopComponents) if (c.id == id) return c;
        return null;
    }

    private void UpdatePlayerCashUI()
    {
        if (playerCashText != null)
            playerCashText.text = $"${playerCash}";
    }

    public static void UpdateSelectableColor(Selectable selectable, bool isSelected)
    {
        var colors = selectable.colors;
        Color prefabSelected = colors.selectedColor;
        Color prefabPressed = colors.pressedColor;
        colors.normalColor = isSelected ? prefabSelected : prefabPressed;
        selectable.colors = colors;
    }

    private void SetViewShop(bool isShop)
    {
        isShopView = isShop;
        UpdateToggleColors();
        PopulateComponentList();
    }

    private void SetCategory(ComponentCategory category)
    {
        currentCategory = category;
        UpdateToggleColors();
        PopulateComponentList();
    }

    private void UpdateToggleColors()
    {
        UpdateSelectableColor(shopToggle, shopToggle.isOn);
        UpdateSelectableColor(inventoryToggle, inventoryToggle.isOn);
        UpdateSelectableColor(turretToggle, turretToggle.isOn);
        UpdateSelectableColor(armorToggle, armorToggle.isOn);
        UpdateSelectableColor(turretAIToggle, turretAIToggle.isOn);
        UpdateSelectableColor(navAIToggle, navAIToggle.isOn);
        UpdateSelectableColor(engineFrameToggle, engineFrameToggle.isOn);

        // Also update tank slot buttons
        foreach (var slot in tankSlots)
            UpdateSelectableColor(slot.button, slot.IsSelected);
    }    public void PopulateComponentList()
    {
        foreach (Transform child in scrollContentParent)
            Destroy(child.gameObject);

        List<ComponentData> source;        if (isShopView)
        {
            // Use the selected category's shop list
            switch (currentCategory)
            {
                case ComponentCategory.Turret:
                    source = turretShopComponents;
                    break;
                case ComponentCategory.Armor:
                    source = armorShopComponents;
                    break;
                case ComponentCategory.TurretAI:
                    // Filter AITree components for Turret branch type
                    source = aiTreeShopComponents.FindAll(c => 
                        c is AiTreeAsset tree && tree.branchType == AiEditor.AiBranchType.Turret);
                    break;
                case ComponentCategory.NavAI:
                    // Filter AITree components for Nav branch type  
                    source = aiTreeShopComponents.FindAll(c => 
                        c is AiTreeAsset tree && tree.branchType == AiEditor.AiBranchType.Nav);
                    break;
                case ComponentCategory.EngineFrame:
                    source = engineFrameShopComponents;
                    break;
                default:
                    source = new List<ComponentData>();
                    break;            }
        }
        else
        {
            // Inventory: handle AI categories by branch type
            if (currentCategory == ComponentCategory.TurretAI)
            {
                // Show Turret AI trees from inventory
                var aiTrees = LoadAITreeInventoryFromFolders();
                source = aiTrees.FindAll(c => c is AiTreeAsset tree && tree.branchType == AiEditor.AiBranchType.Turret);
            }
            else if (currentCategory == ComponentCategory.NavAI)
            {
                // Show Nav AI trees from inventory
                var aiTrees = LoadAITreeInventoryFromFolders();
                source = aiTrees.FindAll(c => c is AiTreeAsset tree && tree.branchType == AiEditor.AiBranchType.Nav);
            }
            else
            {
                // Filter standard inventory by category
                source = playerInventory.FindAll(c => c.category == currentCategory);
            }
        }

        foreach (var component in source)
        {
            GameObject entryGO = Instantiate(componentEntryPrefab, scrollContentParent);
            ComponentEntryUI entryUI = entryGO.GetComponent<ComponentEntryUI>();            entryUI.Setup(
                component,
                GetAssignedTankName(component),
                isShopView,
                OnBuyComponent,
                OnSellComponent,
                OnEquipComponent,
                OnComponentSelected, // selection callback
                (changedComponent) => {
                    // Handle color changes for components
                    OnComponentColorChanged(changedComponent);
                }
            );
        }
    }

    private void OnTankSlotSelected(int index)
    {
        // If the clicked slot is already selected, unselect all
        if (selectedTankSlot == tankSlots[index])
        {
            selectedTankSlot.SetSelected(false);
            UpdateSelectableColor(selectedTankSlot.button, false);
            selectedTankSlot = null;
            Debug.Log("All tank slots unselected");
            // Unselect in EventSystem so button is not visually selected
            EventSystem.current.SetSelectedGameObject(null);
            UpdateToggleColors();
            PopulateComponentList();
            // Clear tank preview when no slot is selected
            if (modelPreview != null)
                modelPreview.ClearPreview();
            // Clear stats and description when no slot is selected
            itemStatsText.text = "";
            descriptionText.text = "";
            return;
        }

        if (selectedTankSlot != null)
            selectedTankSlot.SetSelected(false);

        selectedTankSlot = tankSlots[index];
        selectedTankSlot.SetSelected(true);
        Debug.Log($"tankslot{index} selected");
        UpdateToggleColors();
        PopulateComponentList();

        // Show tank preview for selected slot
        if (modelPreview != null)
            modelPreview.ShowTank(GetEquippedComponentsForSlot(selectedTankSlot));        // Sum weights of all equipped components and update ItemStats text, clear description
        float totalWeight = CalculateAndSaveTotalWeight(selectedTankSlot);
        itemStatsText.text = $"Total Weight: {totalWeight}";
        descriptionText.text = "";
    }

    // Helper to get equipped components for a tank slot
    private Dictionary<ComponentCategory, ComponentData> GetEquippedComponentsForSlot(TankSlotButtonUI slot)
    {
        var equipped = new Dictionary<ComponentCategory, ComponentData>();
        foreach (ComponentCategory cat in System.Enum.GetValues(typeof(ComponentCategory)))
        {
            var comp = slot.GetComponentByCategory(cat);
            if (comp != null)
                equipped[cat] = comp;
        }
        return equipped;
    }    private string GetAssignedTankName(ComponentData component)
    {
        foreach (var slot in tankSlots)
        {
            if (slot.slotData != null)
            {
                if ((component.category == ComponentCategory.EngineFrame && slot.slotData.engineFramePrefab == component.modelPrefab && slot.slotData.engineFrameInstanceId == component.instanceId) ||
                    (component.category == ComponentCategory.Armor && slot.slotData.armorPrefab == component.modelPrefab && slot.slotData.armorInstanceId == component.instanceId) ||
                    (component.category == ComponentCategory.Turret && slot.slotData.turretPrefab == component.modelPrefab && slot.slotData.turretInstanceId == component.instanceId) ||
                    (component.category == ComponentCategory.AITree && component is AiTreeAsset aiAsset && 
                     ((aiAsset.branchType == AiEditor.AiBranchType.Turret && slot.slotData.turretAI == component && slot.slotData.turretAIInstanceId == component.instanceId) ||
                      (aiAsset.branchType == AiEditor.AiBranchType.Nav && slot.slotData.navAI == component && slot.slotData.navAIInstanceId == component.instanceId))))
                {
                    return slot.TankName;
                }
            }
        }
        return "";
    }

    private void OnBuyComponent(ComponentData component)
    {
        if (playerCash < component.cost)
        {
            ShowDebugMessage("Not enough cash!");
            Debug.Log("Not enough cash!");
            return;
        }
        playerCash -= component.cost;

        // Always instantiate a new copy for all component types (including AI SOs)
        ComponentData newComp = Instantiate(component);
        newComp.instanceId = component.id + "_" + System.Guid.NewGuid().ToString();

#if UNITY_EDITOR
        // If this is an AiTreeAsset, save the duplicate as an asset in the correct folder for later editing
        if (newComp is AiTreeAsset aiTree)
        {
            string aiFolder = null;
            if (aiTree.branchType == AiEditor.AiBranchType.Nav)
                aiFolder = "Assets/AiEditor/AISaveFiles/NavFiles/";
            else if (aiTree.branchType == AiEditor.AiBranchType.Turret)
                aiFolder = "Assets/AiEditor/AISaveFiles/TurretFiles/";
            
            if (aiFolder != null)
            {
                string assetPath = aiFolder + newComp.instanceId + ".asset";
                UnityEditor.AssetDatabase.CreateAsset(newComp, assetPath);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"Created new AI Tree asset at: {assetPath}");
            }
        }
#endif

        playerInventory.Add(newComp);
        var entry = PlayerDataManager.Instance.playerData.ownedComponents.Find(e => e.id == component.id);
        if (entry == null)
        {
            entry = new OwnedComponentEntry { id = component.id, instanceIds = new List<string>() };
            PlayerDataManager.Instance.playerData.ownedComponents.Add(entry);
        }
        entry.instanceIds.Add(newComp.instanceId);

        PlayerDataManager.Instance.SavePlayerData();
        UpdatePlayerCashUI();
        PopulateComponentList();
    }

    private void OnSellComponent(ComponentData component)
    {
        // Unassign from any tank slot before selling
        foreach (var slot in tankSlots)
        {
            if (slot.HasComponent(component))
            {
                slot.UnassignComponent(component);
            }
        }

        if (playerInventory.Contains(component))
        {
            // Remove only the exact instance (by instanceId)
            var toRemove = playerInventory.Find(c => c.instanceId == component.instanceId);
            if (toRemove != null)
                playerInventory.Remove(toRemove);
            playerCash += component.cost / 2;
            // Remove from player save data (remove instanceId from OwnedComponentEntry)
            var entry = PlayerDataManager.Instance.playerData.ownedComponents.Find(e => e.id == component.id);
            if (entry != null)
            {
                entry.instanceIds.Remove(component.instanceId);
                if (entry.instanceIds.Count == 0)
                    PlayerDataManager.Instance.playerData.ownedComponents.Remove(entry);
            }

#if UNITY_EDITOR
            // If this is an AiTreeAsset, delete the asset from disk
            if (component is AiTreeAsset aiTree)
            {
                string aiFolder = null;
                if (aiTree.branchType == AiEditor.AiBranchType.Nav)
                    aiFolder = "Assets/AiEditor/AISaveFiles/NavFiles/";
                else if (aiTree.branchType == AiEditor.AiBranchType.Turret)
                    aiFolder = "Assets/AiEditor/AISaveFiles/TurretFiles/";
                
                if (aiFolder != null)
                {
                    string assetPath = aiFolder + component.instanceId + ".asset";
                    if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                    {
                        UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"Deleted AI Tree asset at: {assetPath}");
                    }
                }
            }
#endif

            PlayerDataManager.Instance.SavePlayerData();
            UpdatePlayerCashUI();
            PopulateComponentList();
        }
        if (selectedTankSlot != null && modelPreview != null)
            modelPreview.ShowTank(GetEquippedComponentsForSlot(selectedTankSlot));
    }

    private void OnEquipComponent(ComponentData component)
    {
        // Find which tank slot (if any) this component is currently assigned to
        TankSlotButtonUI assignedSlot = null;
        foreach (var slot in tankSlots)
        {
            if (slot.HasComponent(component))
            {
                assignedSlot = slot;
                break;
            }
        }

        // If no tank slot is selected and the component is assigned, unassign it
        if (selectedTankSlot == null)
        {
            if (assignedSlot != null)
            {
                assignedSlot.UnassignComponent(component);
                // Remove from loadout save data
                UpdateTankLoadoutSave(assignedSlot, component, remove:true);
                PlayerDataManager.Instance.SavePlayerData();
                PopulateComponentList();
            }
            else
            {
                ShowDebugMessage("No tank slot selected!");
                Debug.Log("No tank slot selected!");
            }
            return;
        }

        // If assigned and selected slot is different, move to selected slot (if allowed)
        if (assignedSlot != null && assignedSlot != selectedTankSlot)
        {            // Only allow if the selected slot doesn't already have this category
            if (selectedTankSlot.HasConflictingComponent(component))
            {
                ShowDebugMessage("Component already assigned!");
                Debug.Log("This tank slot already has a component of this category assigned.");
                return;
            }
            assignedSlot.UnassignComponent(component);
            UpdateTankLoadoutSave(assignedSlot, component, remove:true);
            selectedTankSlot.AssignComponent(component);
            UpdateTankLoadoutSave(selectedTankSlot, component);
            PlayerDataManager.Instance.SavePlayerData();            PopulateComponentList();
            // Refresh tank preview
            if (selectedTankSlot != null && modelPreview != null)
                modelPreview.ShowTank(GetEquippedComponentsForSlot(selectedTankSlot));
            // Refresh total weight in itemStatsText if a tank slot is selected
            if (selectedTankSlot != null)
            {
                float totalWeight = CalculateAndSaveTotalWeight(selectedTankSlot);
                itemStatsText.text = $"Total Weight: {totalWeight}";
                descriptionText.text = "";
            }
            return;
        }

        // If already assigned to selected slot, unequip it
        if (assignedSlot == selectedTankSlot)
        {
            selectedTankSlot.UnassignComponent(component);
            UpdateTankLoadoutSave(selectedTankSlot, component, remove:true);
            PlayerDataManager.Instance.SavePlayerData();
            PopulateComponentList();
            // Refresh tank preview
            if (selectedTankSlot != null && modelPreview != null)
                modelPreview.ShowTank(GetEquippedComponentsForSlot(selectedTankSlot));            // Refresh total weight in itemStatsText if a tank slot is selected
            if (selectedTankSlot != null)
            {
                float totalWeight = CalculateAndSaveTotalWeight(selectedTankSlot);
                itemStatsText.text = $"Total Weight: {totalWeight}";
                descriptionText.text = "";
            }
            return;
        }        // If this category is already assigned in the selected slot, do not allow
        if (selectedTankSlot.HasConflictingComponent(component))
        {
            ShowDebugMessage("Component already assigned!");
            Debug.Log("This tank slot already has a component of this category assigned.");
            return;
        }        // Assign to selected slot
        ComponentData assignComponent = component;
        if (component.category == ComponentCategory.AITree)
        {
            // For AI trees, load from disk since they're not stored in playerInventory
            if (component is AiTreeAsset aiAsset)
            {
                assignComponent = LoadAITreeAssetFromDisk(component.instanceId, aiAsset.branchType);
            }
        }
        if (assignComponent == null)
        {
            Debug.LogError($"Could not find AI asset on disk for {component.category} with instanceId={component.instanceId}. Assignment aborted.");
            ShowDebugMessage("AI asset not found on disk! Check instanceId.");
            return;
        }selectedTankSlot.AssignComponent(assignComponent);
        // Color copying is now handled automatically by OnComponentColorChanged when the component was set up
        UpdateTankLoadoutSave(selectedTankSlot, assignComponent);
        PlayerDataManager.Instance.SavePlayerData();
        PopulateComponentList();
        // Refresh tank preview
        if (selectedTankSlot != null && modelPreview != null)
            modelPreview.ShowTank(GetEquippedComponentsForSlot(selectedTankSlot));        // Refresh total weight in itemStatsText if a tank slot is selected
        if (selectedTankSlot != null)
        {
            float totalWeight = CalculateAndSaveTotalWeight(selectedTankSlot);
            itemStatsText.text = $"Total Weight: {totalWeight}";
            descriptionText.text = "";
        }
    }

    // Helper to update the save data for tank loadouts
    private void UpdateTankLoadoutSave(TankSlotButtonUI slot, ComponentData component, bool remove = false)
    {
        int slotIndex = tankSlots.IndexOf(slot);
        if (slotIndex < 0)
            return;
        // Ensure the list is large enough
        while (PlayerDataManager.Instance.playerData.tankLoadouts.Count <= slotIndex)
            PlayerDataManager.Instance.playerData.tankLoadouts.Add(new TankLoadoutSave());
        var loadout = PlayerDataManager.Instance.playerData.tankLoadouts[slotIndex];        if (remove)
        {
            // Remove the component from the loadout
            switch (component.category)
            {
                case ComponentCategory.EngineFrame:
                    loadout.engineFrameInstanceId = null;
                    break;
                case ComponentCategory.Armor:
                    loadout.armorInstanceId = null;
                    break;
                case ComponentCategory.Turret:
                    loadout.turretInstanceId = null;
                    break;
                case ComponentCategory.AITree:
                    if (component is AiTreeAsset aiAsset)
                    {
                        if (aiAsset.branchType == AiEditor.AiBranchType.Turret)
                            loadout.turretAIInstanceId = null;
                        else if (aiAsset.branchType == AiEditor.AiBranchType.Nav)
                            loadout.navAIInstanceId = null;
                    }
                    break;
            }
        }
        else
        {
            // Assign the component to the loadout
            switch (component.category)
            {
                case ComponentCategory.EngineFrame:
                    loadout.engineFrameInstanceId = component.instanceId;
                    break;
                case ComponentCategory.Armor:
                    loadout.armorInstanceId = component.instanceId;
                    break;
                case ComponentCategory.Turret:
                    loadout.turretInstanceId = component.instanceId;
                    break;
                case ComponentCategory.AITree:
                    if (component is AiTreeAsset aiAsset)
                    {
                        if (aiAsset.branchType == AiEditor.AiBranchType.Turret)
                            loadout.turretAIInstanceId = component.instanceId;
                        else if (aiAsset.branchType == AiEditor.AiBranchType.Nav)
                            loadout.navAIInstanceId = component.instanceId;
                    }
                    break;
            }loadout.tankName = slot.TankName;
        }
    }

    private void OnComponentSelected(ComponentData component)
    {
        selectedComponent = component;
        // Show model        if (modelPreview != null)
            modelPreview.ShowModel(component);
        // Show stats
        if (statsPanel != null)
            statsPanel.ShowStats(component);
    }

    // Debug message system
    public void ShowDebugMessage(string message, float duration = 1.5f)
    {
        if (debugTextCoroutine != null)
            StopCoroutine(debugTextCoroutine);
        debugTextCoroutine = StartCoroutine(ShowDebugMessageRoutine(message, duration));
    }

    private IEnumerator ShowDebugMessageRoutine(string message, float duration)
    {
        debugText.text = message;
        debugText.color = Color.red;
        debugText.gameObject.SetActive(true);

        // Optional: flash effect
        float elapsed = 0f;
        while (elapsed < duration)
        {
            debugText.alpha = Mathf.PingPong(Time.time * 2f, 1f); // Flash
            elapsed += Time.deltaTime;
            yield return null;
        }
        debugText.gameObject.SetActive(false);
        debugText.text = "";
    }

    private void LoadTankSlotsFromScriptableObjects()
    {
        foreach (var slot in tankSlots)
        {
            if (slot.slotData != null)
            {
                slot.SetSelected(false); // Deselect by default
                slot.SetActive(slot.slotData.isActive);
                slot.UpdateAssignedComponentsFromSlotData();
            }
        }
    }

    // Make this method public so it can be accessed from TankSlotButtonUI
    public ComponentData FindComponentByPrefab(GameObject prefab, List<ComponentData> list)
    {
        foreach (var c in list)
            if (c.modelPrefab == prefab)
                return c;
        return null;
    }

    // Add this method to allow external refresh of the selected slot's preview and stats
    public void RefreshSelectedSlotUI()
    {
        if (selectedTankSlot != null)
        {
            // Show tank preview for selected slot
            if (modelPreview != null)
                modelPreview.ShowTank(GetEquippedComponentsForSlot(selectedTankSlot));

            // Sum weights of all equipped components and update ItemStats text, clear description
            float totalWeight = CalculateAndSaveTotalWeight(selectedTankSlot);
            itemStatsText.text = $"Total Weight: {totalWeight}";
            descriptionText.text = "";
        }
    }

    // Calculate total weight of equipped components and save it to TankSlotData
    public float CalculateAndSaveTotalWeight(TankSlotButtonUI slot)
    {
        float totalWeight = 0f;
        var equipped = GetEquippedComponentsForSlot(slot);
        foreach (var comp in equipped.Values)
        {
            if (comp != null)
                totalWeight += comp.weight;
        }
        
        // Save to TankSlotData
        if (slot.slotData != null)
        {
            slot.slotData.totalWeight = totalWeight;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(slot.slotData);
            #endif
        }
        
        return totalWeight;
    }    // Handle component color changes
    private void OnComponentColorChanged(ComponentData changedComponent)
    {
        // Update the color in TankSlotData if this component is assigned to a tank slot
        bool componentUpdated = false;
        foreach (var slot in tankSlots)
        {
            if (slot.HasComponent(changedComponent))
            {
                // Update the color in TankSlotData
                if (slot.slotData != null)
                {
                    switch (changedComponent.category)
                    {
                        case ComponentCategory.EngineFrame:
                            slot.slotData.engineFrameColor = changedComponent.customColor;
                            break;
                        case ComponentCategory.Armor:
                            slot.slotData.armorColor = changedComponent.customColor;
                            break;
                        case ComponentCategory.Turret:
                            slot.slotData.turretColor = changedComponent.customColor;
                            break;
                    }
                    
                    // Mark the ScriptableObject as dirty for saving
                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(slot.slotData);
                    #endif
                    
                    componentUpdated = true;
                }
                break;
            }
        }
        
        // Update the preview based on current selection state
        if (selectedTankSlot != null && componentUpdated && modelPreview != null)
        {
            // If a tank slot is selected and this component is part of it, refresh the tank preview
            modelPreview.ShowTank(GetEquippedComponentsForSlot(selectedTankSlot));
        }
        else if (selectedComponent == changedComponent && modelPreview != null)
        {
            // If this component is currently selected for individual preview, refresh it
            modelPreview.ShowModel(changedComponent);
        }
    }    private void LoadAIComponentsFromFolders()
    {
#if UNITY_EDITOR
        // Load AiTreeAssets directly from the AISaveFiles folder and subfolders
        string aiSaveFilesPath = "Assets/AiEditor/AISaveFiles";
        
        // Load from main AISaveFiles directory
        LoadAiTreeAssetsFromPath(aiSaveFilesPath, true);
        
        // Load from NavFiles subfolder for inventory categorization
        string navFilesPath = "Assets/AiEditor/AISaveFiles/NavFiles";
        LoadAiTreeAssetsFromPath(navFilesPath, false);
        
        // Load from TurretFiles subfolder for inventory categorization  
        string turretFilesPath = "Assets/AiEditor/AISaveFiles/TurretFiles";
        LoadAiTreeAssetsFromPath(turretFilesPath, false);
#endif
    }    private void LoadAiTreeAssetsFromPath(string path, bool addToShop)
    {
#if UNITY_EDITOR
        if (!System.IO.Directory.Exists(path))
            return;
            
        string[] files = System.IO.Directory.GetFiles(path, "*.asset");
        foreach (string filePath in files)
        {
            var aiTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiTreeAsset>(filePath);
            if (aiTreeAsset != null)
            {
                // Ensure the tree asset has proper category set
                if (aiTreeAsset.category != ComponentCategory.AITree)
                {
                    aiTreeAsset.category = ComponentCategory.AITree;
                    UnityEditor.EditorUtility.SetDirty(aiTreeAsset);
                }
                
                if (addToShop)
                {
                    // Add to shop if not already present
                    if (!aiTreeShopComponents.Exists(c => c.title == aiTreeAsset.title))
                    {
                        aiTreeShopComponents.Add(aiTreeAsset);
                        Debug.Log($"[WorkshopUIManager] Added AITree component to shop: {aiTreeAsset.title} (Branch: {aiTreeAsset.branchType})");
                    }
                }
            }
        }
#endif
    }

    private List<ComponentData> LoadAITreeInventoryFromFolders()
    {
        var inventoryItems = new List<ComponentData>();
        
#if UNITY_EDITOR
        // Load from NavFiles subfolder
        string navFilesPath = "Assets/AiEditor/AISaveFiles/NavFiles";
        if (System.IO.Directory.Exists(navFilesPath))
        {
            string[] navFiles = System.IO.Directory.GetFiles(navFilesPath, "*.asset");
            foreach (string filePath in navFiles)
            {
                var aiTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiTreeAsset>(filePath);
                if (aiTreeAsset != null && aiTreeAsset.branchType == AiEditor.AiBranchType.Nav)
                {
                    inventoryItems.Add(aiTreeAsset);
                }
            }
        }
        
        // Load from TurretFiles subfolder
        string turretFilesPath = "Assets/AiEditor/AISaveFiles/TurretFiles";
        if (System.IO.Directory.Exists(turretFilesPath))
        {
            string[] turretFiles = System.IO.Directory.GetFiles(turretFilesPath, "*.asset");
            foreach (string filePath in turretFiles)
            {
                var aiTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiTreeAsset>(filePath);
                if (aiTreeAsset != null && aiTreeAsset.branchType == AiEditor.AiBranchType.Turret)
                {
                    inventoryItems.Add(aiTreeAsset);
                }
            }
        }
#endif
        
        return inventoryItems;
    }

    /// <summary>
    /// Loads an AI Tree Asset from disk based on instanceId and branch type
    /// </summary>
    private AiEditor.AiTreeAsset LoadAITreeAssetFromDisk(string instanceId, AiEditor.AiBranchType branchType)
    {
#if UNITY_EDITOR
        string folderPath = branchType == AiEditor.AiBranchType.Turret 
            ? "Assets/AiEditor/AISaveFiles/TurretFiles/" 
            : "Assets/AiEditor/AISaveFiles/NavFiles/";
        
        string assetPath = folderPath + instanceId + ".asset";
        
        var aiTreeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<AiEditor.AiTreeAsset>(assetPath);
        if (aiTreeAsset != null && aiTreeAsset.branchType == branchType)
        {
            return aiTreeAsset;
        }
        
        Debug.LogWarning($"[WorkshopUIManager] Could not load AI Tree asset from path: {assetPath}");
        return null;
#else
        Debug.LogWarning("[WorkshopUIManager] AI Tree asset loading from disk is only supported in editor mode");
        return null;
#endif
    }
}