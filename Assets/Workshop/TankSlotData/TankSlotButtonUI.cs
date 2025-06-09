using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using AiEditor;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TankSlotButtonUI : MonoBehaviour
{
    public Button button;
    public TMP_Text label;
    public Image highlightImage;

    public int slotIndex;
    public string TankName => label != null ? label.text : $"Tank {slotIndex + 1}";

    // Assigned components by category
    private Dictionary<ComponentCategory, object> assignedComponents = new Dictionary<ComponentCategory, object>();

    public bool IsSelected { get; private set; }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        // No color logic here; handled by WorkshopUIManager.UpdateSelectableColor
    }

    public TankSlotData slotData; // Assign in Inspector    /// <summary>
    /// Finds and loads permanent component data from Assets/Workshop/ComponentData/
    /// </summary>
#if UNITY_EDITOR
    private T FindPermanentComponentData<T>(string componentName) where T : ComponentData
    {
        string searchPath = "Assets/Workshop/ComponentData/";
        
        // Load all assets of the specified type from the ComponentData folder recursively
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { searchPath });
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            T componentData = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            
            if (componentData != null && componentData.title.Contains(componentName))
            {
                return componentData;
            }
        }
        
        return null;
    }
#endif

    public void AssignComponent(ComponentData data)
    {
        if (slotData != null)
        {            if (data.category == ComponentCategory.EngineFrame) {
                slotData.engineFramePrefab = data.modelPrefab;
                slotData.engineFrameInstanceId = data.instanceId;
                // Copy stats from permanent ScriptableObject data
#if UNITY_EDITOR
                var permanentEngineData = FindPermanentComponentData<EngineFrameData>(data.title);
                if (permanentEngineData != null)
                {
                    slotData.engineWeightCapacity = permanentEngineData.weightCapacity;
                    slotData.enginePower = permanentEngineData.enginePower;
                    Debug.Log($"[TankSlotButtonUI] Copied engine stats: WeightCapacity={permanentEngineData.weightCapacity}, Power={permanentEngineData.enginePower}");
                }
#endif
            } else if (data.category == ComponentCategory.Armor) {
                slotData.armorPrefab = data.modelPrefab;
                slotData.armorInstanceId = data.instanceId;
                // Copy stats from permanent ScriptableObject data
#if UNITY_EDITOR
                var permanentArmorData = FindPermanentComponentData<ArmorData>(data.title);
                if (permanentArmorData != null)
                {
                    slotData.armorHP = permanentArmorData.HP;
                    Debug.Log($"[TankSlotButtonUI] Copied armor stats: HP={permanentArmorData.HP}");
                }
#endif
            } else if (data.category == ComponentCategory.Turret) {
                slotData.turretPrefab = data.modelPrefab;
                slotData.turretInstanceId = data.instanceId;
                // Copy stats from permanent ScriptableObject data
#if UNITY_EDITOR
                var permanentTurretData = FindPermanentComponentData<TurretData>(data.title);
                if (permanentTurretData != null)
                {
                    slotData.turretDamage = permanentTurretData.damage;
                    slotData.turretRange = permanentTurretData.range;
                    slotData.turretShotsPerSec = permanentTurretData.shotspersec;
                    slotData.turretKnockback = permanentTurretData.knockback;
                    slotData.turretVisionRange = permanentTurretData.visionRange;
                    slotData.turretVisionCone = permanentTurretData.visionCone;
                    Debug.Log($"[TankSlotButtonUI] Copied turret stats: Damage={permanentTurretData.damage}, Range={permanentTurretData.range}, ShotsPerSec={permanentTurretData.shotspersec}");
                }
#endif
            } else if (data.category == ComponentCategory.AITree) {
                Debug.Log($"AssignComponent: category=AITree, data type={data.GetType().FullName}, instanceId={data.instanceId}");
                if (data is AiTreeAsset aiTreeAsset) {
                    if (aiTreeAsset.branchType == AiBranchType.Turret) {
                        slotData.turretAI = aiTreeAsset;
                        slotData.turretAIInstanceId = data.instanceId;
                    } else if (aiTreeAsset.branchType == AiBranchType.Nav) {
                        slotData.navAI = aiTreeAsset;
                        slotData.navAIInstanceId = data.instanceId;
                    } else {
                        Debug.LogError($"Tried to assign AiTreeAsset with unsupported branch type: {aiTreeAsset.branchType}");
                    }
                } else {
                    Debug.LogError($"Tried to assign a non-AiTreeAsset to AITree slot! Actual type: {data.GetType().FullName}, instanceId: {data.instanceId}");
                }
            } else if (data.category == ComponentCategory.TurretAI) {
                // Legacy compatibility: treat as AITree with Turret branch
                Debug.Log($"AssignComponent: legacy category=TurretAI, data type={data.GetType().FullName}, instanceId={data.instanceId}");
                if (data is AiTreeAsset turretAI) {
                    slotData.turretAI = turretAI;
                    slotData.turretAIInstanceId = data.instanceId;
                } else {
                    Debug.LogError($"Tried to assign a non-AiTreeAsset to turretAI slot! Actual type: {data.GetType().FullName}, instanceId: {data.instanceId}");
                }
            } else if (data.category == ComponentCategory.NavAI) {
                // Legacy compatibility: treat as AITree with Nav branch
                Debug.Log($"AssignComponent: legacy category=NavAI, data type={data.GetType().FullName}, instanceId={data.instanceId}");
                if (data is AiTreeAsset navAI) {
                    slotData.navAI = navAI;
                    slotData.navAIInstanceId = data.instanceId;
                } else {
                    Debug.LogError($"Tried to assign a non-AiTreeAsset to navAI slot! Actual type: {data.GetType().FullName}, instanceId: {data.instanceId}");
                }
            }
            // Add more categories as needed
        }
        UpdateAssignedComponentsFromSlotData();
        // Refresh preview and stats if this slot is selected
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (IsSelected && workshopUI != null)
            workshopUI.RefreshSelectedSlotUI();
    }

    public void SetActive(bool active)
    {
        if (slotData != null)
            slotData.isActive = active;
    }

    public void UnassignComponent(ComponentData data)
    {
        if (slotData != null)
        {            if (data.category == ComponentCategory.EngineFrame && slotData.engineFrameInstanceId == data.instanceId)
            {
                slotData.engineFramePrefab = null;
                slotData.engineFrameInstanceId = null;
                // Clear engine stats
                slotData.engineWeightCapacity = 0;
                slotData.enginePower = 0;
            }
            else if (data.category == ComponentCategory.Armor && slotData.armorInstanceId == data.instanceId)
            {
                slotData.armorPrefab = null;
                slotData.armorInstanceId = null;
                // Clear armor stats
                slotData.armorHP = 0;
            }
            else if (data.category == ComponentCategory.Turret && slotData.turretInstanceId == data.instanceId)
            {
                slotData.turretPrefab = null;
                slotData.turretInstanceId = null;
                // Clear turret stats
                slotData.turretDamage = 0;
                slotData.turretRange = 0f;
                slotData.turretShotsPerSec = 0f;
                slotData.turretKnockback = "";
                slotData.turretVisionRange = 60f; // Reset to default
                slotData.turretVisionCone = 45f; // Reset to default
            }else if (data.category == ComponentCategory.AITree) {
                // Handle AITree unassignment based on branch type
                if (data is AiTreeAsset aiTreeAsset) {
                    if (aiTreeAsset.branchType == AiBranchType.Turret && slotData.turretAIInstanceId == data.instanceId) {
                        slotData.turretAI = null;
                        slotData.turretAIInstanceId = null;
                    } else if (aiTreeAsset.branchType == AiBranchType.Nav && slotData.navAIInstanceId == data.instanceId) {
                        slotData.navAI = null;
                        slotData.navAIInstanceId = null;
                    }
                }
            }
            else if (data.category == ComponentCategory.TurretAI && slotData.turretAIInstanceId == data.instanceId)
            {
                slotData.turretAI = null;
                slotData.turretAIInstanceId = null;
            }
            else if (data.category == ComponentCategory.NavAI && slotData.navAIInstanceId == data.instanceId)
            {
                slotData.navAI = null;
                slotData.navAIInstanceId = null;
            }
            // Add more categories as needed
        }
        if (assignedComponents.ContainsKey(data.category)) {
            var comp = assignedComponents[data.category] as ComponentData;
            if (comp != null && comp.instanceId == data.instanceId)
                assignedComponents.Remove(data.category);
        }
        UpdateAssignedComponentsFromSlotData();
        // Refresh preview and stats if this slot is selected
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (IsSelected && workshopUI != null)
            workshopUI.RefreshSelectedSlotUI();
    }    public void ClearAllAssignedComponents()
    {        if (slotData != null)
        {
            slotData.engineFramePrefab = null;
            slotData.armorPrefab = null;
            slotData.turretPrefab = null;
            slotData.turretAI = null;
            slotData.navAI = null;
            
            // Clear all component stats
            slotData.turretDamage = 0;
            slotData.turretRange = 0f;
            slotData.turretShotsPerSec = 0f;
            slotData.turretKnockback = "";
            slotData.turretVisionRange = 60f; // Reset to default
            slotData.turretVisionCone = 45f; // Reset to default
            slotData.armorHP = 0;
            slotData.engineWeightCapacity = 0;
            slotData.enginePower = 0;
            // Add more categories as needed
        }
        UpdateAssignedComponentsFromSlotData();
        // Refresh preview and stats if this slot is selected
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (IsSelected && workshopUI != null)
            workshopUI.RefreshSelectedSlotUI();
    }

    public void UpdateAssignedComponentsFromSlotData()
    {
        assignedComponents.Clear();
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (slotData != null && workshopUI != null)
        {
            // EngineFrame
            if (!string.IsNullOrEmpty(slotData.engineFrameInstanceId))
            {
                var comp = workshopUI.playerInventory.Find(c => c.instanceId == slotData.engineFrameInstanceId);
                if (comp != null) assignedComponents[ComponentCategory.EngineFrame] = comp;
            }
            // Armor
            if (!string.IsNullOrEmpty(slotData.armorInstanceId))
            {
                var comp = workshopUI.playerInventory.Find(c => c.instanceId == slotData.armorInstanceId);
                if (comp != null) assignedComponents[ComponentCategory.Armor] = comp;
            }
            // Turret
            if (!string.IsNullOrEmpty(slotData.turretInstanceId))
            {
                var comp = workshopUI.playerInventory.Find(c => c.instanceId == slotData.turretInstanceId);
                if (comp != null) assignedComponents[ComponentCategory.Turret] = comp;
            }            // AITree components (both legacy TurretAI/NavAI and new AITree category)
            if (!string.IsNullOrEmpty(slotData.turretAIInstanceId)) {
                // Try to find in playerInventory first
                var comp = workshopUI.playerInventory.Find(c => c.instanceId == slotData.turretAIInstanceId);
                if (comp != null) {
                    assignedComponents[ComponentCategory.TurretAI] = comp;
                    // Also add to AITree category for unified handling
                    if (comp is AiTreeAsset) assignedComponents[ComponentCategory.AITree] = comp;
                }
            }
            // NavAI
            if (!string.IsNullOrEmpty(slotData.navAIInstanceId)) {
                // Try to find in playerInventory first
                var comp = workshopUI.playerInventory.Find(c => c.instanceId == slotData.navAIInstanceId);
                if (comp != null) {
                    assignedComponents[ComponentCategory.NavAI] = comp;
                    // Also add to AITree category for unified handling
                    if (comp is AiTreeAsset) assignedComponents[ComponentCategory.AITree] = comp;
                }
            }
            // Add more categories as needed
        }
    }

    public bool HasComponent(ComponentData data)
    {
        if (slotData == null) return false;
        if (assignedComponents.ContainsKey(data.category)) {
            var comp = assignedComponents[data.category] as ComponentData;
            return comp != null && comp.instanceId == data.instanceId;
        }
        return false;
    }    public bool HasCategory(ComponentCategory category)
    {
        if (slotData == null) return false;
        if (category == ComponentCategory.EngineFrame)
            return slotData.engineFramePrefab != null;
        if (category == ComponentCategory.Armor)
            return slotData.armorPrefab != null;
        if (category == ComponentCategory.Turret)
            return slotData.turretPrefab != null;
        if (category == ComponentCategory.TurretAI)
            return slotData.turretAI != null;
        if (category == ComponentCategory.NavAI)
            return slotData.navAI != null;
        if (category == ComponentCategory.AITree)
            return slotData.turretAI != null || slotData.navAI != null;
        return false;
    }

    /// <summary>
    /// Gets an AI component by its branch type (used for AITree category handling)
    /// </summary>
    public ComponentData GetAIComponentByBranchType(AiBranchType branchType)
    {
        if (slotData == null) return null;
        
        if (branchType == AiBranchType.Turret)
            return slotData.turretAI;
        else if (branchType == AiBranchType.Nav)
            return slotData.navAI;
            
        return null;
    }

    /// <summary>
    /// Checks if assigning this component would conflict with existing assignments
    /// </summary>
    public bool HasConflictingComponent(ComponentData component)
    {
        if (slotData == null) return false;
        
        // For AITree components, check branch type conflicts
        if (component.category == ComponentCategory.AITree && component is AiTreeAsset aiAsset)
        {
            if (aiAsset.branchType == AiBranchType.Turret)
                return slotData.turretAI != null;
            else if (aiAsset.branchType == AiBranchType.Nav)
                return slotData.navAI != null;
        }
        
        // For other categories, use the standard category check
        return HasCategory(component.category);
    }

    public ComponentData GetComponentByCategory(ComponentCategory category)
    {
        if (slotData == null) return null;
        if (assignedComponents.ContainsKey(category))
            return assignedComponents[category] as ComponentData;
        return null;
    }

    public string GetAssignedTankName(ComponentData data)
    {
        return HasComponent(data) ? TankName : "";
    }

    // Remove this method from TankSlotButtonUI, as prefab lookup should be centralized in WorkshopUIManager
    // public ComponentData FindComponentByPrefab(GameObject prefab, List<ComponentData> list)
    // {
    //     foreach (var c in list)
    //         if (c.modelPrefab == prefab)
    //             return c;
    //     return null;
    // }
}
