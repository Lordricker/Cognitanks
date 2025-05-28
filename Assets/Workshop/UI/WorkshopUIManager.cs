using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class WorkshopUIManager : MonoBehaviour
{
    public Toggle shopToggle;
    public Toggle inventoryToggle;

    public Toggle turretToggle;
    public Toggle armorToggle;
    public Toggle turretAIToggle;
    public Toggle navAIToggle;
    public Toggle engineFrameToggle;

    public Transform scrollContentParent;
    public GameObject componentEntryPrefab;

    private bool isShopView = true;
    private ComponentCategory currentCategory = ComponentCategory.Turret;

    [Header("Shop Components By Category")]
    public List<ComponentData> turretShopComponents;
    public List<ComponentData> armorShopComponents;
    public List<ComponentData> turretAIShopComponents;
    public List<ComponentData> navAIShopComponents;
    public List<ComponentData> engineFrameShopComponents;

    public List<ComponentData> playerInventory;

    [Header("Player Cash")]
    public int playerCash = 1000;
    public TMP_Text playerCashText;

    public List<TankSlotButtonUI> tankSlots;
    private TankSlotButtonUI selectedTankSlot;

    public WorkshopModelPreview modelPreview;
    public WorkshopStatsPanel statsPanel;

    [Header("Debug UI")]
    public TMP_Text debugText; // Assign in inspector

    private Coroutine debugTextCoroutine;
    private ComponentData selectedComponent;

    public TMP_Text itemStatsText;
    public TMP_Text descriptionText;

    private void Start()
    {
        // Ensure only one of Shop/Inventory is active
        shopToggle.isOn = true;
        inventoryToggle.isOn = false;

        // Ensure only one category is active
        turretToggle.isOn = true;
        armorToggle.isOn = false;
        turretAIToggle.isOn = false;
        navAIToggle.isOn = false;
        engineFrameToggle.isOn = false;

        SetViewShop(true);
        SetCategory(ComponentCategory.Turret);

        shopToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetViewShop(true); });
        inventoryToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetViewShop(false); });

        turretToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetCategory(ComponentCategory.Turret); });
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

        // Restore AI references for all tank slots after loading
        var allSlotData = new List<TankSlotData>();
        foreach (var slot in tankSlots)
        {
            if (slot.slotData != null)
                allSlotData.Add(slot.slotData);
        }
        PlayerDataManager.Instance.RestoreAIReferencesForAllSlots(allSlotData);
    }

    private void LoadPlayerInventoryFromSave()
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
    }

    private ComponentData FindComponentPrefabById(string id)
    {
        // Search all shop lists for a matching id
        foreach (var c in turretShopComponents) if (c.id == id) return c;
        foreach (var c in armorShopComponents) if (c.id == id) return c;
        foreach (var c in turretAIShopComponents) if (c.id == id) return c;
        foreach (var c in navAIShopComponents) if (c.id == id) return c;
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
    }

    public void PopulateComponentList()
    {
        foreach (Transform child in scrollContentParent)
            Destroy(child.gameObject);

        List<ComponentData> source;
        if (isShopView)
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
                    source = turretAIShopComponents;
                    break;
                case ComponentCategory.NavAI:
                    source = navAIShopComponents;
                    break;
                case ComponentCategory.EngineFrame:
                    source = engineFrameShopComponents;
                    break;
                default:
                    source = new List<ComponentData>();
                    break;
            }
        }
        else
        {
            // Inventory: filter by category at display time
            source = playerInventory.FindAll(c => c.category == currentCategory);
        }

        foreach (var component in source)
        {
            GameObject entryGO = Instantiate(componentEntryPrefab, scrollContentParent);
            ComponentEntryUI entryUI = entryGO.GetComponent<ComponentEntryUI>();

            entryUI.Setup(
                component,
                GetAssignedTankName(component),
                isShopView,
                OnBuyComponent,
                OnSellComponent,
                OnEquipComponent,
                OnComponentSelected // <-- pass selection callback
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
    }

    private string GetAssignedTankName(ComponentData component)
    {
        foreach (var slot in tankSlots)
        {
            if (slot.slotData != null)
            {
                if ((component.category == ComponentCategory.EngineFrame && slot.slotData.engineFramePrefab == component.modelPrefab && slot.slotData.engineFrameInstanceId == component.instanceId) ||
                    (component.category == ComponentCategory.Armor && slot.slotData.armorPrefab == component.modelPrefab && slot.slotData.armorInstanceId == component.instanceId) ||
                    (component.category == ComponentCategory.Turret && slot.slotData.turretPrefab == component.modelPrefab && slot.slotData.turretInstanceId == component.instanceId) ||
                    (component.category == ComponentCategory.TurretAI && slot.slotData.turretAI == component && slot.slotData.turretAIInstanceId == component.instanceId) ||
                    (component.category == ComponentCategory.NavAI && slot.slotData.navAI == component && slot.slotData.navAIInstanceId == component.instanceId))
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
        // If this is an AI SO, save the duplicate as an asset in the correct folder for later editing
        string aiFolder = null;
        if (newComp is NavAIData)
            aiFolder = "Assets/AiEditor/AIFiles/NavFiles/";
        else if (newComp is TurretAIData)
            aiFolder = "Assets/AiEditor/AIFiles/TurretFiles/";
        if (aiFolder != null)
        {
            string assetPath = aiFolder + newComp.instanceId + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(newComp, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"Created new AI SO asset at: {assetPath}");
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
            // If this is an AI SO, delete the asset from disk
            string aiFolder = null;
            if (component is NavAIData)
                aiFolder = "Assets/AiEditor/AIFiles/NavFiles/";
            else if (component is TurretAIData)
                aiFolder = "Assets/AiEditor/AIFiles/TurretFiles/";
            if (aiFolder != null)
            {
                string assetPath = aiFolder + component.instanceId + ".asset";
                if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                {
                    UnityEditor.AssetDatabase.DeleteAsset(assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    Debug.Log($"Deleted AI SO asset at: {assetPath}");
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
        {
            // Only allow if the selected slot doesn't already have this category
            if (selectedTankSlot.HasCategory(component.category))
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
        }

        // If this category is already assigned in the selected slot, do not allow
        if (selectedTankSlot.HasCategory(component.category))
        {
            ShowDebugMessage("Component already assigned!");
            Debug.Log("This tank slot already has a component of this category assigned.");
            return;
        }

        // Assign to selected slot
        ComponentData assignComponent = component;
        if (component.category == ComponentCategory.NavAI || component.category == ComponentCategory.TurretAI)
        {
            // Use the instance from playerInventory, not Resources
            assignComponent = playerInventory.Find(c => c.instanceId == component.instanceId);
        }
        if (assignComponent == null)
        {
            Debug.LogError($"Could not find AI asset in player inventory for {component.category} with instanceId={component.instanceId}. Assignment aborted.");
            ShowDebugMessage("AI asset not found in inventory! Check instanceId.");
            return;
        }
        selectedTankSlot.AssignComponent(assignComponent);
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
        var loadout = PlayerDataManager.Instance.playerData.tankLoadouts[slotIndex];
        if (remove)
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
                case ComponentCategory.TurretAI:
                    loadout.turretAIInstanceId = null;
                    break;
                case ComponentCategory.NavAI:
                    loadout.navAIInstanceId = null;
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
                case ComponentCategory.TurretAI:
                    loadout.turretAIInstanceId = component.instanceId;
                    break;
                case ComponentCategory.NavAI:
                    loadout.navAIInstanceId = component.instanceId;
                    break;
            }
            loadout.tankName = slot.TankName;
        }
    }

    private void OnComponentSelected(ComponentData component)
    {
        selectedComponent = component;
        // Show model
        if (modelPreview != null)
            modelPreview.ShowModel(component.modelPrefab);
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
    }
}