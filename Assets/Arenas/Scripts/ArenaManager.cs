using UnityEngine;

public class ArenaManager : MonoBehaviour
{    
    [Header("Spawn Points")]
    public Transform[] spawnPoints = new Transform[10]; // Assign in Inspector
    public GameObject tankPrefab; // Assign modular tank prefab in Inspector
    public TankSlotData[] tankSlots = new TankSlotData[10]; // Assign ScriptableObjects in Inspector
      [Header("Team Layer Configuration")]
    [Tooltip("Unity layer for Team A tanks")]
    public int teamALayer = 10; // Layer 10 for allies
    [Tooltip("Unity layer for Team B tanks")]
    public int teamBLayer = 11; // Layer 11 for enemies

    void Start()
    {
        // Ensure time scale is reset to normal when arena starts (fixes pause bug)
        Time.timeScale = 1f;
        
        SpawnActiveTanks();
        // Refresh camera anchors after all tanks have spawned
        var camController = Object.FindFirstObjectByType<CameraController>();
        if (camController != null)
            camController.RefreshAnchors();
    }

    void SpawnActiveTanks()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (tankSlots[i] != null && tankSlots[i].isActive && tankSlots[i].engineFramePrefab != null)
            {
                GameObject tank = Instantiate(tankPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
                
                // Assign Unity layer based on team
                int targetLayer = GetLayerForTeam(tankSlots[i].teamId);
                AssignLayerRecursively(tank, targetLayer);
                  // Set the tank's name to include team information for debugging
                tank.name = $"Tank_{i}_Team{tankSlots[i].teamId}";
                
                TankAssembly assembly = tank.GetComponent<TankAssembly>();
                if (assembly != null)   
                {
                    assembly.Assemble(tankSlots[i]);
                    Debug.Log($"Tank {tank.name} (Team {tankSlots[i].teamId}) spawned and assigned to Layer {targetLayer}");
                }
            }
        }
    }
    
    /// <summary>
    /// Get the Unity layer for a specific team
    /// </summary>
    private int GetLayerForTeam(int teamId)
    {
        switch (teamId)
        {
            case 0: return teamALayer; // Team A
            case 1: return teamBLayer; // Team B
            default: 
                Debug.LogWarning($"Unknown team ID: {teamId}. Using Team A layer.");
                return teamALayer;
        }
    }
    
    /// <summary>
    /// Recursively assign a layer to a GameObject and all its children
    /// </summary>
    private void AssignLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
          foreach (Transform child in obj.transform)
        {
            AssignLayerRecursively(child.gameObject, layer);
        }
    }
}

