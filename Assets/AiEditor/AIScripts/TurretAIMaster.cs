using UnityEngine;
using System.Collections.Generic;

public class ArenaTurretAIMaster : MonoBehaviour
{
    public static ArenaTurretAIMaster Instance;
    
    [Header("Turret Settings")]
    public float turretRotationSpeed = 50f; // Degrees per second
    
    [Header("Map Settings")]
    public Vector3 mapCenter = Vector3.zero;
    
    // Dictionary to store tank tags
    private Dictionary<TankMan, int> tankTags = new Dictionary<TankMan, int>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // ========== TURRET ACTIONS ==========
    
    public void FireIfInAngle(TankMan tank, float angleThreshold = 5f)
    {
        if (tank.HasTarget() && IsTurretPointingAtEnemy(tank, angleThreshold))
        {
            // TODO: Implement projectile system
            Debug.Log($"{tank.name} fires at target!");
            // SpawnProjectile(tank.turretPivot.position, tank.turretPivot.forward);
        }
    }
    
    public void TrackTarget(TankMan tank)
    {
        if (tank.HasTarget() && tank.turretPivot != null)
        {
            PointTurretAtTarget(tank.turretPivot, tank.GetCurrentTarget());
        }
    }
    
    public void AlignFront(TankMan tank)
    {
        if (tank.turretPivot != null)
        {
            PointTurretAtDirection(tank, tank.transform.forward);
        }
    }
    
    public void AlignBack(TankMan tank)
    {
        if (tank.turretPivot != null)
        {
            PointTurretAtDirection(tank, -tank.transform.forward);
        }
    }
    
    public void AlignLeft(TankMan tank)
    {
        if (tank.turretPivot != null)
        {
            PointTurretAtDirection(tank, -tank.transform.right);
        }
    }
    
    public void AlignRight(TankMan tank)
    {
        if (tank.turretPivot != null)
        {
            PointTurretAtDirection(tank, tank.transform.right);
        }
    }
    
    public void RotateUp(TankMan tank, float degrees = 90f)
    {
        if (tank.turretPivot != null)
        {
            tank.turretPivot.Rotate(-degrees * Time.deltaTime, 0, 0);
        }
    }
    
    public void RotateDown(TankMan tank, float degrees = 90f)
    {
        if (tank.turretPivot != null)
        {
            tank.turretPivot.Rotate(degrees * Time.deltaTime, 0, 0);
        }
    }
    
    public void RotateLeft(TankMan tank, float degrees = 90f)
    {
        if (tank.turretPivot != null)
        {
            tank.turretPivot.Rotate(0, -degrees * Time.deltaTime, 0);
        }
    }
    
    public void RotateRight(TankMan tank, float degrees = 90f)
    {
        if (tank.turretPivot != null)
        {
            tank.turretPivot.Rotate(0, degrees * Time.deltaTime, 0);
        }
    }
    
    public void TagTarget(TankMan tank, int tagNumber)
    {
        if (tank.HasTarget())
        {
            TankMan targetTank = tank.GetCurrentTarget().GetComponent<TankMan>();
            if (targetTank != null)
            {
                tankTags[targetTank] = tagNumber;
                Debug.Log($"{tank.name} tagged {targetTank.name} with tag {tagNumber}");
            }
        }
    }
    
    // AI Action Blocks for Turret Control
    
    public void PointTurretAtEnemy(TankMan tank)
    {
        if (tank.HasTarget() && tank.turretPivot != null)
        {
            PointTurretAtTarget(tank.turretPivot, tank.GetCurrentTarget());
        }
    }
    
    public void PointTurretAtPosition(TankMan tank, Vector3 position)
    {
        if (tank.turretPivot != null)
        {
            // Calculate direction to target position
            Vector3 directionToTarget = position - tank.turretPivot.position;
            directionToTarget.y = 0; // Keep turret rotation on horizontal plane only
            
            if (directionToTarget.magnitude < 0.1f) return; // Too close to rotate
            
            // Calculate target rotation
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            // Smoothly rotate turret pivot
            tank.turretPivot.rotation = Quaternion.RotateTowards(
                tank.turretPivot.rotation, 
                targetRotation, 
                turretRotationSpeed * Time.deltaTime
            );
        }
    }
    
    public void StopTurretRotation(TankMan tank)
    {
        // Turret stops rotating by not calling any rotation methods
        // This is handled frame by frame in Update loops
    }
    
    // Core turret control method
    private void PointTurretAtTarget(Transform turretPivot, Transform target)
    {
        if (target == null || turretPivot == null) return;
        
        // Calculate direction to target
        Vector3 directionToTarget = target.position - turretPivot.position;
        directionToTarget.y = 0; // Keep turret rotation on horizontal plane only
        
        if (directionToTarget.magnitude < 0.1f) return; // Too close to rotate
        
        // Calculate target rotation
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        
        // Smoothly rotate turret pivot
        turretPivot.rotation = Quaternion.RotateTowards(
            turretPivot.rotation, 
            targetRotation, 
            turretRotationSpeed * Time.deltaTime
        );
    }
    
    // AI Condition Blocks for Turret Control
    
    public bool IsEnemyVisible(TankMan tank)
    {
        return tank.IsEnemyVisible();
    }
    
    public bool IsTurretPointingAtEnemy(TankMan tank, float tolerance = 5f)
    {
        if (!tank.HasTarget() || tank.turretPivot == null) return false;
        return IsTurretPointingAtTarget(tank.turretPivot, tank.GetCurrentTarget(), tolerance);
    }
    
    public bool IsTurretPointingAtTarget(Transform turretPivot, Transform target, float toleranceAngle = 5f)
    {
        if (target == null || turretPivot == null) return false;
        
        Vector3 directionToTarget = target.position - turretPivot.position;
        directionToTarget.y = 0;
        
        float angle = Vector3.Angle(turretPivot.forward, directionToTarget);
        return angle <= toleranceAngle;
    }
    
    public bool IsEnemyInRange(TankMan tank, float range)
    {
        return tank.IsEnemyWithinDistance(range);
    }
    
    // Composite AI Behaviors
    
    public void TrackEnemyBehavior(TankMan tank)
    {
        if (IsEnemyVisible(tank))
        {
            PointTurretAtEnemy(tank);
        }
    }
    
    public void DefensiveBehavior(TankMan tank, float engagementRange = 30f)
    {
        if (IsEnemyVisible(tank) && IsEnemyInRange(tank, engagementRange))
        {
            PointTurretAtEnemy(tank);
        }
    }
    
    private void PointTurretAtDirection(TankMan tank, Vector3 direction)
    {
        if (tank.turretPivot != null)
        {
            direction.y = 0; // Keep on horizontal plane
            if (direction.magnitude < 0.1f) return;
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            tank.turretPivot.rotation = Quaternion.RotateTowards(
                tank.turretPivot.rotation,
                targetRotation,
                turretRotationSpeed * Time.deltaTime
            );
        }
    }
    
    // ========== CONDITION BLOCKS ==========
    
    // Target Conditions
    public bool HasTarget(TankMan tank)
    {
        return tank.HasTarget();
    }
    
    public bool HasMultipleTargets(TankMan tank, int minCount = 2)
    {
        // No GetVisibleEnemyCount in TankMan, so return false or implement your own logic
        return false; // Placeholder: always false
    }
    
    public bool TargetIsMoving(TankMan tank, float minSpeed = 0.1f)
    {
        if (!tank.HasTarget()) return false;
        
        TankMan targetTank = tank.GetCurrentTarget().GetComponent<TankMan>();
        if (targetTank == null) return false;
        
        // Check if target has moved significantly in recent frames
        // This would need velocity tracking in TankMan
        return true; // Placeholder - implement velocity tracking
    }
    
    public bool TargetIsStopped(TankMan tank, float maxSpeed = 0.1f)
    {
        return !TargetIsMoving(tank, maxSpeed);
    }
    
    // Range Conditions
    public bool IsTargetInRange(TankMan tank, float range)
    {
        return tank.IsEnemyWithinDistance(range);
    }
    
    public bool IsTargetTooClose(TankMan tank, float minRange = 10f)
    {
        if (!tank.HasTarget()) return false;
        float distance = Vector3.Distance(tank.transform.position, tank.GetCurrentTarget().position);
        return distance < minRange;
    }
    
    public bool IsTargetTooFar(TankMan tank, float maxRange = 80f)
    {
        if (!tank.HasTarget()) return false;
        float distance = Vector3.Distance(tank.transform.position, tank.GetCurrentTarget().position);
        return distance > maxRange;
    }
    
    public bool IsTargetAtOptimalRange(TankMan tank, float minRange = 20f, float maxRange = 60f)
    {
        if (!tank.HasTarget()) return false;
        float distance = Vector3.Distance(tank.transform.position, tank.GetCurrentTarget().position);
        return distance >= minRange && distance <= maxRange;
    }
    
    // Turret Type Conditions
    public bool HasTurretType(TankMan tank, string turretType)
    {
        // Hardcoded turret type for now (e.g., "Sniper")
        string defaultTurretType = "Sniper";
        return defaultTurretType.Contains(turretType);
    }
    public bool HasHeavyTurret(TankMan tank)
    {
        return HasTurretType(tank, "Heavy");
    }
    public bool HasLightTurret(TankMan tank)
    {
        return HasTurretType(tank, "Light");
    }
    public bool HasMediumTurret(TankMan tank)
    {
        return HasTurretType(tank, "Medium");
    }
    
    // Armor Type Conditions
    public bool HasArmorType(TankMan tank, string armorType)
    {
        // Hardcoded armor type for now (e.g., "Medium")
        string defaultArmorType = "Medium";
        return defaultArmorType.Contains(armorType);
    }
    public bool HasHeavyArmor(TankMan tank)
    {
        return HasArmorType(tank, "Heavy");
    }
    public bool HasLightArmor(TankMan tank)
    {
        return HasArmorType(tank, "Light");
    }
    public bool HasMediumArmor(TankMan tank)
    {
        return HasArmorType(tank, "Medium");
    }
    
    // HP Conditions
    public bool IsLowHealth(TankMan tank, float threshold = 0.3f)
    {
        // TODO: Implement HP system in TankMan
        // return tank.GetHealthPercentage() < threshold;
        return false; // Placeholder
    }
    
    public bool IsHighHealth(TankMan tank, float threshold = 0.8f)
    {
        // TODO: Implement HP system in TankMan
        // return tank.GetHealthPercentage() > threshold;
        return true; // Placeholder
    }
    
    public bool IsCriticalHealth(TankMan tank, float threshold = 0.1f)
    {
        return IsLowHealth(tank, threshold);
    }
    
    public bool IsFullHealth(TankMan tank)
    {
        // TODO: Implement HP system in TankMan
        // return tank.GetHealthPercentage() >= 1.0f;
        return true; // Placeholder
    }
    
    // Tag Conditions
    public bool HasTag(TankMan tank, int tagNumber)
    {
        return tankTags.ContainsKey(tank) && tankTags[tank] == tagNumber;
    }
    
    public bool TargetHasTag(TankMan tank, int tagNumber)
    {
        if (!tank.HasTarget()) return false;
        
        TankMan targetTank = tank.GetCurrentTarget().GetComponent<TankMan>();
        if (targetTank == null) return false;
        
        return HasTag(targetTank, tagNumber);
    }
    
    public bool HasAnyTag(TankMan tank)
    {
        return tankTags.ContainsKey(tank);
    }
    
    public int GetTag(TankMan tank)
    {
        return tankTags.ContainsKey(tank) ? tankTags[tank] : -1;
    }
    
    public void ClearTag(TankMan tank)
    {
        tankTags.Remove(tank);
    }
    
    public void ClearAllTags()
    {
        tankTags.Clear();
    }

    // ===== SPECIFIC CONDITION BLOCKS =====

    // Target Conditions
    public bool IfSelf(TankMan tank) { return true; }
    public bool IfEnemy(TankMan tank) { return tank.HasTarget() && tank.GetCurrentTarget().CompareTag("Enemy"); }
    public bool IfAlly(TankMan tank) { return tank.HasTarget() && tank.GetCurrentTarget().CompareTag("Ally"); }
    public bool IfAny(TankMan tank) { return tank.HasTarget(); }

    // Range Conditions
    public bool IfRangeLessThan(TankMan tank, float value) { return tank.HasTarget() && Vector3.Distance(tank.transform.position, tank.GetCurrentTarget().position) < value; }
    public bool IfRangeGreaterThan(TankMan tank, float value) { return tank.HasTarget() && Vector3.Distance(tank.transform.position, tank.GetCurrentTarget().position) > value; }

    // Turret Type Conditions
    public bool IfSniper(TankMan tank) { return HasTurretType(tank, "Sniper"); }
    public bool IfShotgun(TankMan tank) { return HasTurretType(tank, "Shotgun"); }
    public bool IfArtillery(TankMan tank) { return HasTurretType(tank, "Artillery"); }
    public bool IfGatlinGun(TankMan tank) { return HasTurretType(tank, "GatlinGun"); }
    public bool IfHammer(TankMan tank) { return HasTurretType(tank, "Hammer"); }
    public bool IfLaser(TankMan tank) { return HasTurretType(tank, "Laser"); }

    // Armor Conditions
    public bool IfLight(TankMan tank) { return HasArmorType(tank, "Light"); }
    public bool IfHeavy(TankMan tank) { return HasArmorType(tank, "Heavy"); }

    // HP Conditions
    public bool IfHPLessThan(TankMan tank, float value) { /* TODO: Implement HP system */ return false; }
    public bool IfHPGreaterThan(TankMan tank, float value) { /* TODO: Implement HP system */ return true; }

    // Tag Conditions
    public bool IfTagEquals(TankMan tank, int tag) { return HasTag(tank, tag); }
    public bool IfTagLessThan(TankMan tank, int tag) { return GetTag(tank) < tag; }
    public bool IfTagGreaterThan(TankMan tank, int tag) { return GetTag(tank) > tag; }
}
