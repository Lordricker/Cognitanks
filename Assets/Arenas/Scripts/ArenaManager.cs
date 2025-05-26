using UnityEngine;

public class ArenaManager : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform[] spawnPoints = new Transform[10]; // Assign in Inspector
    public GameObject tankPrefab; // Assign modular tank prefab in Inspector
    public TankSlotData[] tankSlots = new TankSlotData[10]; // Assign ScriptableObjects in Inspector

    void Start()
    {
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
                TankAssembly assembly = tank.GetComponent<TankAssembly>();
                if (assembly != null)   
                {
                    assembly.Assemble(tankSlots[i]);
                }
            }
        }
    }
}

