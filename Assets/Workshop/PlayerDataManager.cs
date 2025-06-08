using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class OwnedComponentEntry
{
    public string id;
    public List<string> instanceIds = new List<string>(); // unique instance ids for each owned copy
}

[Serializable]
public class PlayerData
{
    public List<OwnedComponentEntry> ownedComponents = new List<OwnedComponentEntry>(); // IDs and instanceIds of owned components
    public List<TankLoadoutSave> tankLoadouts = new List<TankLoadoutSave>(); // One per tank slot
}

[Serializable]
public class TankLoadoutSave
{
    public string tankName;
    public string engineFrameInstanceId;
    public string armorInstanceId;
    public string turretInstanceId;    public string turretAIInstanceId;
    public string navAIInstanceId;
    // Add more component instanceIds as needed

    // AI components are now handled as AiTreeAsset instances in player inventory
}

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;
    public PlayerData playerData = new PlayerData();
    private string saveFilePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
            LoadPlayerData();
        }
        else
        {
            Destroy(gameObject);
        }
    }    public void SavePlayerData()
    {
        // Save all unique instanceIds for each owned component (excluding AI components which are stored on disk)
        playerData.ownedComponents.Clear();
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (workshopUI != null)
        {
            var grouped = new Dictionary<string, OwnedComponentEntry>();            foreach (var comp in workshopUI.playerInventory)
            {
                // Skip AI components as they are stored as files on disk, not in inventory
                if (comp.category == ComponentCategory.AITree) continue;
                
                if (!grouped.ContainsKey(comp.id))
                    grouped[comp.id] = new OwnedComponentEntry { id = comp.id };
                grouped[comp.id].instanceIds.Add(comp.instanceId);
            }
            playerData.ownedComponents.AddRange(grouped.Values);
        }
        // Save tank slot assignments by instanceId
        playerData.tankLoadouts.Clear();
        if (workshopUI != null)
        {
            foreach (var slot in workshopUI.tankSlots)
            {
                var save = new TankLoadoutSave();
                save.tankName = slot.TankName;
                if (slot.slotData != null)
                {
                    save.engineFrameInstanceId = slot.slotData.engineFrameInstanceId;
                    save.armorInstanceId = slot.slotData.armorInstanceId;
                    save.turretInstanceId = slot.slotData.turretInstanceId;
                    save.turretAIInstanceId = slot.slotData.turretAIInstanceId;
                    save.navAIInstanceId = slot.slotData.navAIInstanceId;
                }
                playerData.tankLoadouts.Add(save);
            }
        }
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"Player data saved to {saveFilePath}");
    }

    public void LoadPlayerData()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("Player data loaded.");
        }
        else
        {
            playerData = new PlayerData();
            Debug.Log("No player data found. Created new data.");
        }
    }

    public void ErasePlayerData()
    {
        if (File.Exists(saveFilePath))
            File.Delete(saveFilePath);
        playerData = new PlayerData();
        // Also clear any runtime inventory if needed
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (workshopUI != null)
        {
            workshopUI.playerInventory.Clear();
            workshopUI.PopulateComponentList();
        }
        Debug.Log("Player data erased.");
    }    // Call this after loading player data and before using TankSlotData
    public void RestoreComponentDataReferences(List<TankSlotData> allSlots)
    {
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (workshopUI == null)
        {
            Debug.LogWarning("[PlayerDataManager] WorkshopUIManager not found - cannot restore component data references");
            return;
        }

        Debug.Log($"[PlayerDataManager] RestoreComponentDataReferences: Player inventory has {workshopUI.playerInventory.Count} components");
        foreach (var comp in workshopUI.playerInventory)
        {
            Debug.Log($"[PlayerDataManager] Inventory component: {comp.title} (id: {comp.id}, instanceId: {comp.instanceId}, category: {comp.category})");
        }

        bool saveNeeded = false;
        bool assetsModified = false;
        
        foreach (var slotData in allSlots)
        {
            if (slotData == null) continue;

            // Restore engine frame data reference
            if (!string.IsNullOrEmpty(slotData.engineFrameInstanceId))
            {
                var engineFrameComponent = workshopUI.playerInventory.Find(c => c.instanceId == slotData.engineFrameInstanceId);
                if (engineFrameComponent is EngineFrameData engineFrameData)
                {
                    slotData.engineFrameData = engineFrameData;
                    Debug.Log($"[PlayerDataManager] Restored EngineFrameData reference for {slotData.name}: {engineFrameData.title}");
                }                else
                {
                    Debug.LogWarning($"[PlayerDataManager] Could not find EngineFrameData with instanceId: {slotData.engineFrameInstanceId} - clearing reference");
                    slotData.engineFrameInstanceId = null;
                    slotData.engineFrameData = null;
                    saveNeeded = true;
                    assetsModified = true;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(slotData);
#endif
                }
            }

            // Restore armor data reference
            if (!string.IsNullOrEmpty(slotData.armorInstanceId))
            {
                var armorComponent = workshopUI.playerInventory.Find(c => c.instanceId == slotData.armorInstanceId);
                if (armorComponent is ArmorData armorData)
                {
                    slotData.armorData = armorData;
                    Debug.Log($"[PlayerDataManager] Restored ArmorData reference for {slotData.name}: {armorData.title}");
                }                else
                {
                    Debug.LogWarning($"[PlayerDataManager] Could not find ArmorData with instanceId: {slotData.armorInstanceId} - clearing reference");
                    slotData.armorInstanceId = null;
                    slotData.armorData = null;
                    saveNeeded = true;
                    assetsModified = true;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(slotData);
#endif
                }
            }            // Restore turret data reference
            if (!string.IsNullOrEmpty(slotData.turretInstanceId))
            {
                Debug.Log($"[PlayerDataManager] Looking for turret with instanceId: {slotData.turretInstanceId}");
                var turretComponent = workshopUI.playerInventory.Find(c => c.instanceId == slotData.turretInstanceId);
                if (turretComponent is TurretData turretData)
                {
                    slotData.turretData = turretData;
                    Debug.Log($"[PlayerDataManager] Restored TurretData reference for {slotData.name}: {turretData.title}");
                }                else
                {
                    Debug.LogWarning($"[PlayerDataManager] Could not find TurretData with instanceId: {slotData.turretInstanceId} - clearing reference");
                    if (turretComponent != null)
                        Debug.LogWarning($"[PlayerDataManager] Found component but it's not TurretData: {turretComponent.GetType().Name} - {turretComponent.title}");
                    slotData.turretInstanceId = null;
                    slotData.turretData = null;
                    slotData.turretPrefab = null; // Also clear the prefab reference for consistency
                    saveNeeded = true;
                    assetsModified = true;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(slotData);
#endif
                }
            }

            // Restore AI references from disk files (not from player inventory)
            if (!string.IsNullOrEmpty(slotData.turretAIInstanceId))
            {
                var turretAI = LoadAITreeAssetFromDisk(slotData.turretAIInstanceId, AiEditor.AiBranchType.Turret);
                if (turretAI != null)
                {
                    slotData.turretAI = turretAI;
                    Debug.Log($"[PlayerDataManager] Restored TurretAI reference from disk for {slotData.name}: {turretAI.title}");
                }                else
                {
                    Debug.LogWarning($"[PlayerDataManager] Could not find TurretAI file with instanceId: {slotData.turretAIInstanceId} - clearing reference");
                    slotData.turretAIInstanceId = null;
                    slotData.turretAI = null;
                    saveNeeded = true;
                    assetsModified = true;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(slotData);
#endif
                }
            }

            if (!string.IsNullOrEmpty(slotData.navAIInstanceId))
            {
                var navAI = LoadAITreeAssetFromDisk(slotData.navAIInstanceId, AiEditor.AiBranchType.Nav);
                if (navAI != null)
                {
                    slotData.navAI = navAI;
                    Debug.Log($"[PlayerDataManager] Restored NavAI reference from disk for {slotData.name}: {navAI.title}");
                }                else
                {
                    Debug.LogWarning($"[PlayerDataManager] Could not find NavAI file with instanceId: {slotData.navAIInstanceId} - clearing reference");
                    slotData.navAIInstanceId = null;
                    slotData.navAI = null;
                    saveNeeded = true;
                    assetsModified = true;
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(slotData);
#endif
                }
            }
        }        // If we cleared any invalid references, save the updated data
        if (saveNeeded)
        {
            Debug.Log("[PlayerDataManager] Cleaned up invalid component references - saving updated data");
            SavePlayerData();
        }

        // If we modified any ScriptableObject assets, save them
        if (assetsModified)
        {
            Debug.Log("[PlayerDataManager] Saving modified ScriptableObject assets");
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        Debug.Log("[PlayerDataManager] Component data reference restoration completed");
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
        
        Debug.LogWarning($"[PlayerDataManager] Could not load AI Tree asset from path: {assetPath}");
        return null;
#else
        Debug.LogWarning("[PlayerDataManager] AI Tree asset loading from disk is only supported in editor mode");
        return null;
#endif
    }
}
