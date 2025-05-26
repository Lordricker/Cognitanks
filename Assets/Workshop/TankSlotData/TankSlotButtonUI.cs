using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    public TankSlotData slotData; // Assign in Inspector

    public void AssignComponent(ComponentData data)
    {
        if (slotData != null)
        {
            if (data.category == ComponentCategory.EngineFrame) {
                slotData.engineFramePrefab = data.modelPrefab;
                slotData.engineFrameInstanceId = data.instanceId;
            } else if (data.category == ComponentCategory.Armor) {
                slotData.armorPrefab = data.modelPrefab;
                slotData.armorInstanceId = data.instanceId;
            } else if (data.category == ComponentCategory.Turret) {
                slotData.turretPrefab = data.modelPrefab;
                slotData.turretInstanceId = data.instanceId;
            } else if (data.category == ComponentCategory.TurretAI) {
                Debug.Log($"AssignComponent: category=TurretAI, data type={data.GetType().FullName}, instanceId={data.instanceId}");
                if (data is TurretAIData turretAIData) {
                    slotData.turretAI = turretAIData;
                    slotData.turretAIInstanceId = data.instanceId;
                } else {
                    Debug.LogError($"Tried to assign a non-TurretAIData to turretAI slot! Actual type: {data.GetType().FullName}, instanceId: {data.instanceId}");
                }
            } else if (data.category == ComponentCategory.NavAI) {
                if (data is NavAIData navAIData) {
                    slotData.navAI = navAIData;
                    slotData.navAIInstanceId = data.instanceId;
                } else {
                    Debug.LogError("Tried to assign a non-NavAIData to navAI slot!");
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
        {
            if (data.category == ComponentCategory.EngineFrame && slotData.engineFrameInstanceId == data.instanceId)
            {
                slotData.engineFramePrefab = null;
                slotData.engineFrameInstanceId = null;
            }
            else if (data.category == ComponentCategory.Armor && slotData.armorInstanceId == data.instanceId)
            {
                slotData.armorPrefab = null;
                slotData.armorInstanceId = null;
            }
            else if (data.category == ComponentCategory.Turret && slotData.turretInstanceId == data.instanceId)
            {
                slotData.turretPrefab = null;
                slotData.turretInstanceId = null;
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
    }

    public void ClearAllAssignedComponents()
    {
        if (slotData != null)
        {
            slotData.engineFramePrefab = null;
            slotData.armorPrefab = null;
            slotData.turretPrefab = null;
            slotData.turretAI = null;
            slotData.navAI = null;
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
            }
            // TurretAI
            if (!string.IsNullOrEmpty(slotData.turretAIInstanceId)) {
                var comp = workshopUI.playerInventory.Find(c => c.instanceId == slotData.turretAIInstanceId);
                if (comp != null) assignedComponents[ComponentCategory.TurretAI] = comp;
            }
            // NavAI
            if (!string.IsNullOrEmpty(slotData.navAIInstanceId)) {
                var comp = workshopUI.playerInventory.Find(c => c.instanceId == slotData.navAIInstanceId);
                if (comp != null) assignedComponents[ComponentCategory.NavAI] = comp;
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
    }

    public bool HasCategory(ComponentCategory category)
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
        return false;
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
