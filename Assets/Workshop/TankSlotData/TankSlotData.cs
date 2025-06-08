using UnityEngine;
using AiEditor;

[CreateAssetMenu(menuName = "Tank/SlotData")]
public class TankSlotData : ScriptableObject
{
    public bool isActive;
    public int teamId;
    
    // Prefab references
    public GameObject turretPrefab;
    public GameObject armorPrefab;
    public GameObject engineFramePrefab;    // Component data references - direct references for efficient lookup
    public TurretData turretData;
    public ArmorData armorData;
    public EngineFrameData engineFrameData;
    public AiTreeAsset turretAI;
    public AiTreeAsset navAI;
    
    // Stats
    public float totalWeight;
    
    // Instance IDs for saving/loading
    public string engineFrameInstanceId;
    public string armorInstanceId;
    public string turretInstanceId;
    public string turretAIInstanceId;
    public string navAIInstanceId;
    
    // Custom colors for visual customization
    public Color engineFrameColor = Color.white;
    public Color armorColor = Color.white;
    public Color turretColor = Color.white;
}