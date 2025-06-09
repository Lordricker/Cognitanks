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
    public GameObject engineFramePrefab;    // AI references
    public AiTreeAsset turretAI;
    public AiTreeAsset navAI;
    
    // Component stats - stored directly to avoid ScriptableObject reference issues
    [Header("Turret Stats")]
    public int turretDamage;
    public float turretRange;
    public float turretShotsPerSec;
    public string turretKnockback;
    public float turretVisionRange = 60f;
    public float turretVisionCone = 45f;
    
    [Header("Armor Stats")]
    public int armorHP;
    
    [Header("Engine Stats")]
    public int engineWeightCapacity;
    public int enginePower;
    
    [Header("Calculated Stats")]
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