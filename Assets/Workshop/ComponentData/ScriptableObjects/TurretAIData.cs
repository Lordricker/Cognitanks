using UnityEngine;

public enum TurretAIBehavior
{
    Aggressive,     // Fire at any target in range immediately
    Conservative,   // Wait for closer targets, prefer low HP enemies
    Defensive,      // Only fire when being fired upon
    Sniper,         // Prefer distant targets, wait for good shots
    Sweeper         // Constantly rotate and scan, rapid fire
}

[CreateAssetMenu(fileName = "TurretAIData", menuName = "Scriptable Objects/Turret AI Data")]
public class TurretAIData : ComponentData
{
    [Header("AI Behavior")]
    public TurretAIBehavior behavior = TurretAIBehavior.Aggressive;
    
    [Header("Targeting Preferences")]
    [Range(0f, 1f)]
    public float aggressiveness = 0.7f; // How eager to engage targets
    
    [Range(0f, 1f)]
    public float accuracy = 0.8f; // Wait time before firing for better accuracy
    
    [Range(0.1f, 5f)]
    public float reactionTime = 0.5f; // Time before responding to new targets
    
    [Header("Firing Control")]
    public bool conserveAmmo = false; // Wait for better shots
    
    [Range(0f, 1f)]
    public float leadTargetPrediction = 0.5f; // How much to lead targets (0-1)
    
    [Range(0f, 1f)]
    public float burstFireDelay = 0.1f; // Delay between shots in a burst
    
    [Header("Scanning Behavior")]
    public bool continuousScan = true; // Whether to continuously scan when no targets
    
    [Range(1f, 180f)]
    public float scanSpeed = 45f; // Degrees per second
    
    [Range(-180f, 0f)]
    public float scanRangeMin = -90f; // Minimum scan angle (local space)
    
    [Range(0f, 180f)]
    public float scanRangeMax = 90f; // Maximum scan angle (local space)
    
    // Runtime state tracking (non-serialized)
    [System.NonSerialized] public float lastFireTime;
    [System.NonSerialized] public float targetAcquiredTime;
    [System.NonSerialized] public float currentScanAngle;
    [System.NonSerialized] public bool scanDirection = true; // true = positive direction
    [System.NonSerialized] public bool isEngaged = false;
    
    public float GetFireDelay(TurretData turretData)
    {
        if (turretData == null) return 1f;
        float baseDelay = 1f / turretData.shotspersec;
        
        // Modify based on behavior
        switch (behavior)
        {
            case TurretAIBehavior.Aggressive:
                return baseDelay * 0.8f; // Faster firing
            case TurretAIBehavior.Conservative:
                return baseDelay * 1.2f; // Slower, more deliberate
            case TurretAIBehavior.Sniper:
                return baseDelay * 1.5f; // Much slower, precise shots
            case TurretAIBehavior.Sweeper:
                return baseDelay * 0.6f; // Rapid fire
            case TurretAIBehavior.Defensive:
                return baseDelay; // Normal rate
            default:
                return baseDelay;
        }
    }
    
    // This is a simplified implementation until we have a proper TurretController with TargetInfo
    public bool ShouldFire(TurretData turretData, float targetDistance) 
    {
        if (turretData == null) return false;
        
        // Check fire rate
        float fireDelay = GetFireDelay(turretData);
        if (Time.time - lastFireTime < fireDelay) return false;
        
        // Check reaction time
        if (Time.time - targetAcquiredTime < reactionTime) return false;
        
        // Simple behavior-specific logic based only on distance
        switch (behavior)
        {
            case TurretAIBehavior.Aggressive:
                return true; // Fire at anything in range
                
            case TurretAIBehavior.Conservative:
                // Only fire at closer targets
                float rangePercent = targetDistance / turretData.range;
                return rangePercent < 0.7f;
                
            case TurretAIBehavior.Defensive:
                // Only fire when being attacked (simplified)
                return isEngaged;
                
            case TurretAIBehavior.Sniper:
                // Prefer distant targets
                float distancePercent = targetDistance / turretData.range;
                return distancePercent > 0.5f;
                
            case TurretAIBehavior.Sweeper:
                return true; // Always fire when possible
                
            default:
                return true;
        }
    }
}