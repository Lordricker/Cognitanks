using UnityEngine;

[CreateAssetMenu(fileName = "NewTurret", menuName = "Components/Turret")]
public class TurretData : ComponentData
{
    public int damage;
    public float range;
    public float shotspersec;
    public string knockback;
    
    [Header("Vision System")]
    [Range(10f, 90f)]
    public float visionCone = 45f; // Field of view angle in degrees
}


