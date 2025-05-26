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
    public string turretInstanceId;
    public string turretAIInstanceId;
    public string navAIInstanceId;
    // Add more component instanceIds as needed

    // Helper methods to resolve ScriptableObject references at runtime
    public TurretAIData GetTurretAIData()
    {
        return Resources.Load<TurretAIData>(turretAIInstanceId);
    }
    public NavAIData GetNavAIData()
    {
        return Resources.Load<NavAIData>(navAIInstanceId);
    }
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
    }

    public void SavePlayerData()
    {
        // Save all unique instanceIds for each owned component
        playerData.ownedComponents.Clear();
        var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
        if (workshopUI != null)
        {
            var grouped = new Dictionary<string, OwnedComponentEntry>();
            foreach (var comp in workshopUI.playerInventory)
            {
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
    }

    // Call this after loading player data and before using TankSlotData
    public void RestoreAIReferencesForAllSlots(List<TankSlotData> allSlots)
    {
        // No longer needed: AI SOs are now always loaded from playerInventory, not Resources.
        // This method can be left empty or removed.
    }
}
