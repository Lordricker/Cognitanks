using UnityEngine;
using AiEditor;

public class TankAssembly : MonoBehaviour
{
    public Transform basePivot; // Where engine frame and armor wont instantiated
    public Transform turretPivot; // Where turret will be instantiated    // Store component data for easy access by controllers
    private TurretData currentTurretData;
    private AiTreeAsset currentTurretAI;
    private AiTreeAsset currentNavAI;
    private Rigidbody tankRigidbody;
    private TankMan tankMan;
      public TurretData GetTurretData() => currentTurretData;
    public AiTreeAsset GetTurretAI() => currentTurretAI;
    public AiTreeAsset GetNavAI() => currentNavAI;

    public void Assemble(TankSlotData data)
    {
        Debug.Log($"TankAssembly.Assemble() called on {gameObject.name} with data: {(data != null ? data.name : "NULL")}");
        if (data == null) 
        {
            Debug.LogError($"TankAssembly.Assemble: data is null for {gameObject.name}!");
            return;
        }
        
        Debug.Log($"TankAssembly.Assemble: Tank data - turretPrefab: {(data.turretPrefab != null ? data.turretPrefab.name : "NULL")}, turretAIInstanceId: '{data.turretAIInstanceId}', isActive: {data.isActive}");          // Ensure a Rigidbody is present on the root tank object
        tankRigidbody = GetComponent<Rigidbody>();
        if (tankRigidbody == null)
        {
            tankRigidbody = gameObject.AddComponent<Rigidbody>();
        }        // Configure physics settings
        tankRigidbody.mass = Mathf.Max(50f, data.totalWeight * 10f); // Convert weight to reasonable mass
        tankRigidbody.linearDamping = 0.05f; // Almost no air resistance for natural falling
        tankRigidbody.angularDamping = 1f; // Minimal rotational resistance
        tankRigidbody.useGravity = true; // Ensure gravity is enabled
        // No rotation constraints - we'll handle rotation limits in TankMan to allow natural tilting
        
        Debug.Log($"[TankAssembly] Rigidbody configured - Mass: {tankRigidbody.mass}, LinearDamping: {tankRigidbody.linearDamping}, AngularDamping: {tankRigidbody.angularDamping}");        // Store component data for TankMan (if present)
        if (data.engineFramePrefab != null && data.engineFrameInstanceId != null)
        {
            // Find the EngineFrameData instance from WorkshopUIManager
            var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
            if (workshopUI != null)
            {
                data.engineFrameData = workshopUI.playerInventory.Find(c => c.instanceId == data.engineFrameInstanceId) as EngineFrameData;
                Debug.Log($"TankAssembly: Found EngineFrameData: {(data.engineFrameData != null ? data.engineFrameData.title : "NULL")}");
            }
        }
        
        if (data.armorPrefab != null && data.armorInstanceId != null)
        {
            // Find the ArmorData instance from WorkshopUIManager
            var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
            if (workshopUI != null)
            {
                data.armorData = workshopUI.playerInventory.Find(c => c.instanceId == data.armorInstanceId) as ArmorData;
                Debug.Log($"TankAssembly: Found ArmorData: {(data.armorData != null ? data.armorData.title : "NULL")}");
            }
        }
        
        // Note: turretData is already assigned in TankSlotData when turret is equipped
              // Ensure TankMan component is present and configured
        tankMan = GetComponent<TankMan>();
        if (tankMan == null)
            tankMan = gameObject.AddComponent<TankMan>();
        
        // Set the tank slot data so TankMan can calculate stats
        tankMan.SetTankSlotData(data);
        Debug.Log($"TankAssembly: Added and configured TankMan for {gameObject.name}");
        
        // Remove old children
        foreach (Transform child in basePivot) Destroy(child.gameObject);
        foreach (Transform child in turretPivot) Destroy(child.gameObject);
        
        // Instantiate engine frame and armor as children of basePivot
        if (data.engineFramePrefab != null)
        {
            GameObject engineFrame = Instantiate(data.engineFramePrefab, basePivot.position, basePivot.rotation, basePivot);
            ApplyColorToTreadMount(engineFrame, data.engineFrameColor);
        }
        if (data.armorPrefab != null)
        {
            GameObject armor = Instantiate(data.armorPrefab, basePivot.position, basePivot.rotation, basePivot);
            ApplyColorToModel(armor, data.armorColor);
        }        // Instantiate turret as child of turretPivot
        if (data.turretPrefab != null)
        {
            Debug.Log($"TankAssembly: Instantiating turret prefab: {data.turretPrefab.name}");
            GameObject turretInstance = Instantiate(data.turretPrefab, turretPivot.position, turretPivot.rotation, turretPivot);
            Debug.Log($"TankAssembly: Turret instantiated as: {turretInstance.name}");
            ApplyColorToModel(turretInstance, data.turretColor);
            
            // Find and assign turret transform and firePoint to TankMan
            Transform firePoint = FindFirePointRecursive(turretInstance.transform);
            tankMan.SetTurretComponents(turretInstance.transform, firePoint);
            
            // Get TurretData directly from TankSlotData (same pattern as NavAI)
            if (data.turretData != null)
            {
                currentTurretData = data.turretData;
                Debug.Log($"TankAssembly: Assigned TurretData from slot data: {data.turretData.name}");
            }
            else
            {
                Debug.Log("TankAssembly: No TurretData assigned to this tank slot");
            }
            
            // Get TurretAI directly from TankSlotData (same pattern as NavAI)
            if (data.turretAI != null)
            {
                currentTurretAI = data.turretAI;
                Debug.Log($"TankAssembly: Assigned TurretAI from slot data: {data.turretAI.title}");
            }
            else
            {
                Debug.Log("TankAssembly: No TurretAI assigned to this tank slot");
            }
        }

        // Store AI references from TankSlotData - now using AiTreeAsset
        if (data.navAI != null)
        {
            currentNavAI = data.navAI;
            Debug.Log($"TankAssembly: Assigned NavAI from slot data: {data.navAI.title}");
        }
        else
        {
            Debug.Log("TankAssembly: No NavAI assigned to this tank slot");
        }

        // Initialize TurretAI if present
        if (currentTurretAI != null)
        {
            Debug.Log($"TankAssembly: TurretAI assigned: {currentTurretAI.title}");
        }

        // Add CameraAnchor if not present
        Transform anchor = transform.Find("CameraAnchor");
        if (anchor == null)
        {
            GameObject anchorObj = new GameObject("CameraAnchor");
            anchorObj.transform.SetParent(transform);
            anchorObj.transform.localPosition = new Vector3(30f, 15f, 0f); // X=20, Y=10, Z=0
            anchorObj.transform.localRotation = Quaternion.identity;
        }
    }
      /// <summary>
    /// Recursively searches for a FirePoint transform in the hierarchy
    /// </summary>
    private Transform FindFirePointRecursive(Transform parent)
    {
        // Check if current transform is FirePoint
        if (parent.name == "FirePoint")
            return parent;
              // Search all children recursively
        foreach (Transform child in parent)
        {
            Transform result = FindFirePointRecursive(child);
            if (result != null)
                return result;
        }
        
        return null;
    }
    
    // Helper: Only color the TreadMount child
    private void ApplyColorToTreadMount(GameObject engineFrame, Color color)
    {
        var treadMount = engineFrame.transform.Find("TreadMount");
        if (treadMount != null)
        {
            var renderers = treadMount.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", color);
                    else if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", color);
                }
            }
        }
    }
    // Helper: Color all renderers in a model
    private void ApplyColorToModel(GameObject model, Color color)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color"))
                    mat.SetColor("_Color", color);
            }
        }
    }    
    // AI execution and gizmo drawing are now handled by TankMan component
}
