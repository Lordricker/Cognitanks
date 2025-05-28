using UnityEngine;

[CreateAssetMenu(menuName = "Tank/SlotData")]
public class TankSlotData : ScriptableObject
{
    public bool isActive;
    public GameObject turretPrefab;
    public GameObject armorPrefab;
    public TurretAIData turretAI;
    public NavAIData navAI;
    public GameObject engineFramePrefab;
    // Removed basePrefab field

    public float totalWeight; // Total weight of all equipped components

    public string engineFrameInstanceId;
    public string armorInstanceId;
    public string turretInstanceId;
    public string turretAIInstanceId;
    public string navAIInstanceId;
}