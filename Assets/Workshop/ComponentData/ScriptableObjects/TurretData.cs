using UnityEngine;

[CreateAssetMenu(fileName = "NewTurret", menuName = "Components/Turret")]
public class TurretData : ComponentData
{
    [Header("Combat Stats")]
    public int damage;
    public float range;              // Maximum firing range
    public float shotspersec;
    public string knockback;
    
    [Header("Vision System")]
    public float visionRange = 60f;  // How far the turret can detect enemies (separate from firing range)
    [Range(10f, 180f)]
    public float visionCone = 45f;   // Field of view angle in degrees
}


