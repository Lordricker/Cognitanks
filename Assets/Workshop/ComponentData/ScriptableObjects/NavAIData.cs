using UnityEngine;

[CreateAssetMenu(fileName = "NavAIData", menuName = "Scriptable Objects/Nav AI Data")]
public class NavAIData : ComponentData
{    // Wander settings (can be exposed in the Inspector if desired)
    public float minWanderTime = 10f;
    public float maxWanderTime = 20f;
    public float stuckSpeedThreshold = 0.2f; // Lowered from 0.5f to be less sensitive
    public float stuckTime = 5f; // Increased from 3f to give more time before triggering

    // State for wandering (should be managed per tank instance)
    [System.NonSerialized] public float wanderTimer;
    [System.NonSerialized] public float currentWanderDuration;
    [System.NonSerialized] public float stuckTimer;
    [System.NonSerialized] public Vector3 lastPosition;
    [System.NonSerialized] public float targetYaw; // target Y rotation for tank
    [System.NonSerialized] public bool turningToTarget; // are we currently turning to targetYaw?

    // Reference to NavAIMaster (set in scene or via code)
    [System.NonSerialized] private NavAIMaster _master;

    public void StartWander(GameObject tank)
    {
        wanderTimer = 0f;
        currentWanderDuration = Random.Range(minWanderTime, maxWanderTime);
        stuckTimer = 0f;
        lastPosition = tank.transform.position;
        targetYaw = Random.Range(0f, 360f);
        turningToTarget = true;
    }

    public void WanderTick(GameObject tank, Rigidbody rb)
    {
        // Move forward
        // Use NavAIMaster for logic
        if (_master == null)
            _master = Object.FindFirstObjectByType<NavAIMaster>();
        if (_master == null)
        {
            Debug.LogError("NavAIMaster not found in scene!");
            return;
        }
        _master.Wander(tank, rb, ref wanderTimer, ref currentWanderDuration, minWanderTime, maxWanderTime, ref lastPosition, stuckSpeedThreshold, ref stuckTimer, stuckTime, ref targetYaw, ref turningToTarget);
    }

    private void PickNewDirection(GameObject tank)
    {
        float randomAngle = Random.Range(0f, 360f);
        tank.transform.Rotate(0, randomAngle, 0);
        currentWanderDuration = Random.Range(minWanderTime, maxWanderTime);
        wanderTimer = 0f;
    }
}
