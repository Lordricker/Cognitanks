using UnityEngine;
using System.Collections.Generic;

public class ArenaNavAIMaster : MonoBehaviour
{
    public static ArenaNavAIMaster Instance;
    
    [Header("Movement Settings")]
    public float moveSpeed = 25f; // Units per second
    public float rotateSpeed = 50f; // Degrees per second
    
    [Header("Map Settings")]
    public Vector3 mapCenter = Vector3.zero;
    public float mapRadius = 100f;
    
    [Header("Wander Settings")]
    public float wanderRadius = 20f;
    public float wanderDistance = 10f;
    public float wanderJitter = 1f;
    
    // Dictionary to store tank home positions and wander targets
    private Dictionary<TankMan, Vector3> tankHomePositions = new Dictionary<TankMan, Vector3>();
    private Dictionary<TankMan, Vector3> tankWanderTargets = new Dictionary<TankMan, Vector3>();
    private Dictionary<TankMan, float> tankWanderTimers = new Dictionary<TankMan, float>();
    
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
    
    // Core Movement Methods
      public void MoveForward(Transform tankTransform, float speedMultiplier = 1f)
    {
        Rigidbody rb = tankTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use physics-based movement
            Vector3 force = tankTransform.forward * moveSpeed * speedMultiplier * rb.mass;
            rb.AddForce(force, ForceMode.Force);
            
            // Limit maximum velocity
            if (rb.linearVelocity.magnitude > moveSpeed * speedMultiplier)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed * speedMultiplier;
            }
        }
        else
        {
            // Fallback to direct transform manipulation
            Vector3 movement = tankTransform.forward * moveSpeed * speedMultiplier * Time.deltaTime;
            tankTransform.position += movement;
        }
    }
    
    public void MoveBackward(Transform tankTransform, float speedMultiplier = 1f)
    {
        Rigidbody rb = tankTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use physics-based movement
            Vector3 force = -tankTransform.forward * moveSpeed * speedMultiplier * rb.mass;
            rb.AddForce(force, ForceMode.Force);
            
            // Limit maximum velocity
            if (rb.linearVelocity.magnitude > moveSpeed * speedMultiplier)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed * speedMultiplier;
            }
        }
        else
        {
            // Fallback to direct transform manipulation
            Vector3 movement = -tankTransform.forward * moveSpeed * speedMultiplier * Time.deltaTime;
            tankTransform.position += movement;
        }
    }
    
    public void RotateLeft(Transform tankTransform, float speedMultiplier = 1f)
    {
        Rigidbody rb = tankTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use physics-based rotation
            float torque = -rotateSpeed * speedMultiplier * Mathf.Deg2Rad * rb.mass;
            rb.AddTorque(Vector3.up * torque, ForceMode.Force);
        }
        else
        {
            // Fallback to direct transform manipulation
            float rotation = -rotateSpeed * speedMultiplier * Time.deltaTime;
            tankTransform.Rotate(0, rotation, 0);
        }
    }
    
    public void RotateRight(Transform tankTransform, float speedMultiplier = 1f)
    {
        Rigidbody rb = tankTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Use physics-based rotation
            float torque = rotateSpeed * speedMultiplier * Mathf.Deg2Rad * rb.mass;
            rb.AddTorque(Vector3.up * torque, ForceMode.Force);
        }
        else
        {
            // Fallback to direct transform manipulation
            float rotation = rotateSpeed * speedMultiplier * Time.deltaTime;
            tankTransform.Rotate(0, rotation, 0);
        }
    }
    
    public void MoveTo(Transform tankTransform, Vector3 targetPosition, float speedMultiplier = 1f)
    {
        Vector3 direction = (targetPosition - tankTransform.position);
        direction.y = 0; // Keep movement on horizontal plane
        
        if (direction.magnitude < 0.5f) return; // Close enough
        
        direction = direction.normalized;
        
        // Calculate if we need to turn towards target
        float angle = Vector3.SignedAngle(tankTransform.forward, direction, Vector3.up);
        
        // Rotate towards target if not facing it
        if (Mathf.Abs(angle) > 5f)
        {
            if (angle > 0)
                RotateRight(tankTransform, speedMultiplier);
            else
                RotateLeft(tankTransform, speedMultiplier);
        }
        else
        {
            // Move forward when facing the right direction
            MoveForward(tankTransform, speedMultiplier);
        }
    }
    
    public void FaceTarget(Transform tankTransform, Transform target)
    {
        if (target == null) return;
        
        Vector3 direction = (target.position - tankTransform.position);
        direction.y = 0;
        
        if (direction.magnitude < 0.1f) return;
        
        float angle = Vector3.SignedAngle(tankTransform.forward, direction, Vector3.up);
        
        if (Mathf.Abs(angle) > 2f)
        {
            if (angle > 0)
                RotateRight(tankTransform);
            else
                RotateLeft(tankTransform);
        }
    }
    
    public bool IsFacingTarget(Transform tankTransform, Transform target, float toleranceAngle = 10f)
    {
        if (target == null) return false;
        
        Vector3 direction = (target.position - tankTransform.position);
        direction.y = 0;
        
        float angle = Vector3.Angle(tankTransform.forward, direction);
        return angle <= toleranceAngle;
    }
      // ========== NAVIGATION ACTIONS ==========
    
    public void Wander(TankMan tank)
    {
        // Initialize wander target if needed
        if (!tankWanderTargets.ContainsKey(tank))
        {
            SetNewWanderTarget(tank);
        }
        
        Vector3 wanderTarget = tankWanderTargets[tank];
        
        // Check if we've reached the wander target or need a new one
        float distanceToTarget = Vector3.Distance(tank.transform.position, wanderTarget);
        if (distanceToTarget < 2f || ShouldGetNewWanderTarget(tank))
        {
            SetNewWanderTarget(tank);
            wanderTarget = tankWanderTargets[tank];
        }
        
        // Move towards wander target
        MoveTo(tank.transform, wanderTarget, GetTankMoveSpeed(tank));
    }
    
    public void Chase(TankMan tank)
    {
        if (tank.HasTarget())
        {
            Vector3 targetPosition = tank.GetCurrentTarget().position;
            MoveTo(tank.transform, targetPosition, GetTankMoveSpeed(tank));
        }
    }
    
    public void Flee(TankMan tank)
    {
        if (tank.HasTarget())
        {
            Vector3 fleeDirection = (tank.transform.position - tank.GetCurrentTarget().position).normalized;
            Vector3 fleeTarget = tank.transform.position + fleeDirection * 20f;
            
            // Clamp flee target to map bounds
            fleeTarget = ClampToMapBounds(fleeTarget);
            
            MoveTo(tank.transform, fleeTarget, GetTankMoveSpeed(tank));
        }
    }
    
    public void MoveToMapCenter(TankMan tank)
    {
        MoveTo(tank.transform, mapCenter, GetTankMoveSpeed(tank));
    }
    
    public void MoveToHome(TankMan tank)
    {
        if (!tankHomePositions.ContainsKey(tank))
        {
            // Set current position as home if not set
            tankHomePositions[tank] = tank.transform.position;
        }
        
        Vector3 homePosition = tankHomePositions[tank];
        MoveTo(tank.transform, homePosition, GetTankMoveSpeed(tank));
    }
    
    public void Wait(TankMan tank)
    {
        // Stop all movement
        StopMovement(tank);
    }
    
    // Helper method to set home position
    public void SetHome(TankMan tank, Vector3 homePosition)
    {
        tankHomePositions[tank] = homePosition;
    }
    
    public void SetHome(TankMan tank)
    {
        tankHomePositions[tank] = tank.transform.position;
    }
    
    // Wander system implementation
    private void SetNewWanderTarget(TankMan tank)
    {
        Vector3 currentPosition = tank.transform.position;
        
        // Generate random point in circle around current position
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 wanderTarget = currentPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Clamp to map bounds
        wanderTarget = ClampToMapBounds(wanderTarget);
        
        tankWanderTargets[tank] = wanderTarget;
        tankWanderTimers[tank] = Time.time;
    }
    
    private bool ShouldGetNewWanderTarget(TankMan tank)
    {
        if (!tankWanderTimers.ContainsKey(tank)) return true;
        
        // Get new target every 5-10 seconds
        float timeThreshold = Random.Range(5f, 10f);
        return Time.time - tankWanderTimers[tank] > timeThreshold;
    }
    
    private Vector3 ClampToMapBounds(Vector3 position)
    {
        Vector3 fromCenter = position - mapCenter;
        if (fromCenter.magnitude > mapRadius)
        {
            fromCenter = fromCenter.normalized * mapRadius;
            position = mapCenter + fromCenter;
        }
        return position;
    }
    
    private float GetTankMoveSpeed(TankMan tank)
    {
        // Use the default moveSpeed for all tanks (no equipment dependency)
        return moveSpeed;
    }
    
    public void MoveTowardsEnemy(TankMan tank, float speed = 5f)
    {
        if (tank.HasTarget())
        {
            Vector3 direction = (tank.GetCurrentTarget().position - tank.transform.position).normalized;
            tank.transform.position += direction * speed * Time.deltaTime;
            
            // Rotate tank to face movement direction
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                tank.transform.rotation = Quaternion.Slerp(tank.transform.rotation, targetRotation, 2f * Time.deltaTime);
            }
        }
    }
    
    public void MoveToPosition(TankMan tank, Vector3 targetPosition, float speed = 5f)
    {
        Vector3 direction = (targetPosition - tank.transform.position).normalized;
        tank.transform.position += direction * speed * Time.deltaTime;
        
        // Rotate tank to face movement direction
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            tank.transform.rotation = Quaternion.Slerp(tank.transform.rotation, targetRotation, 2f * Time.deltaTime);
        }
    }
    
    public void StopMovement(TankMan tank)
    {
        // Tank stops by not calling movement methods
        // Movement is handled frame by frame
    }
    
    public void Retreat(TankMan tank, float speed = 3f)
    {
        if (tank.HasTarget())
        {
            Vector3 direction = (tank.transform.position - tank.GetCurrentTarget().position).normalized;
            tank.transform.position += direction * speed * Time.deltaTime;
        }
    }
    
    public void CircleEnemy(TankMan tank, float radius = 20f, float speed = 3f)
    {
        if (tank.HasTarget())
        {
            Vector3 enemyPosition = tank.GetCurrentTarget().position;
            Vector3 tankPosition = tank.transform.position;
            
            // Calculate perpendicular direction for circling
            Vector3 toEnemy = (enemyPosition - tankPosition).normalized;
            Vector3 circleDirection = Vector3.Cross(toEnemy, Vector3.up).normalized;
            
            // Move in circle
            tank.transform.position += circleDirection * speed * Time.deltaTime;
            
            // Maintain distance
            float currentDistance = Vector3.Distance(tankPosition, enemyPosition);
            if (currentDistance < radius - 2f)
            {
                tank.transform.position += -toEnemy * speed * 0.5f * Time.deltaTime;
            }
            else if (currentDistance > radius + 2f)
            {
                tank.transform.position += toEnemy * speed * 0.5f * Time.deltaTime;
            }
        }
    }
    
    // AI Condition Blocks for Navigation
    
    public bool IsEnemyVisible(TankMan tank)
    {
        return tank.IsEnemyVisible();
    }
    
    public bool IsEnemyWithinDistance(TankMan tank, float distance)
    {
        return tank.IsEnemyWithinDistance(distance);
    }
    
    public bool IsEnemyFurtherThan(TankMan tank, float distance)
    {
        return tank.GetDistanceToTarget() > distance;
    }
    
    public bool IsAtPosition(TankMan tank, Vector3 position, float tolerance = 2f)
    {
        return Vector3.Distance(tank.transform.position, position) <= tolerance;
    }
    
    // Composite AI Behaviors
    
    public void ChaseAndEngageBehavior(TankMan tank, float engageDistance = 50f, float speed = 5f)
    {
        if (IsEnemyVisible(tank))
        {
            if (IsEnemyFurtherThan(tank, engageDistance))
            {
                MoveTowardsEnemy(tank, speed);
            }
            else
            {
                StopMovement(tank);
            }
        }
    }
    
    public void HitAndRunBehavior(TankMan tank, float minDistance = 30f, float maxDistance = 60f)
    {
        if (IsEnemyVisible(tank))
        {
            if (IsEnemyWithinDistance(tank, minDistance))
            {
                Retreat(tank);
            }
            else if (IsEnemyFurtherThan(tank, maxDistance))
            {
                MoveTowardsEnemy(tank);
            }
            else
            {
                CircleEnemy(tank);
            }
        }
    }
}
