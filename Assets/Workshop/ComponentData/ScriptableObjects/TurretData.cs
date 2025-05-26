using UnityEngine;

[CreateAssetMenu(fileName = "NewTurret", menuName = "Components/Turret")]
public class TurretData : ComponentData
{
    public int damage;
    public float range;
    public float shotspersec;
    public string knockback;
}


