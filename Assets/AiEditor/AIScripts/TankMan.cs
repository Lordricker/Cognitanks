using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using AiEditor;

/// <summary>
/// Unified tank management system that handles:
/// 1. Tank parameter calculations from component data
/// 2. AI execution for both navigation and turret control
/// 3. Sensor-based decision making and combat systems
/// 4. All movement and combat operations (consolidated from former Master scripts)
/// </summary>
public class TankMan : MonoBehaviour
{
    [Header("Tank Slot Data")]
    [SerializeField] private TankSlotData tankSlotData;
    
    [Header("AI Configuration")]
    [SerializeField] private bool enableNavAI = true;
    [SerializeField] private bool enableTurretAI = true;
    [SerializeField] private float aiUpdateInterval = 0.1f;
      [Header("Tank Components")]
    [SerializeField] private Rigidbody tankRigidbody;
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform firePoint;
    
    [Header("Sensor Settings")]
    [SerializeField] private LayerMask enemyLayerMask = 1;
    [SerializeField] private LayerMask allyLayerMask = 1;
    [SerializeField] private string tankTag = "Tank";
    
    [Header("Projectile Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    
    [Header("Calculated Stats - Read Only")]
    [SerializeField] private float totalWeight;
    [SerializeField] private int totalHP;
    [SerializeField] private int enginePower;
    [SerializeField] private int damage;
    [SerializeField] private float range;
    [SerializeField] private float shotsPerSec;
    [SerializeField] private string knockback;
    [SerializeField] private float visionCone;
    [SerializeField] private float visionRange;
    [SerializeField] private float currentHealth;
    [SerializeField] private float armor;
    
    // Public properties for external access
    public float TotalWeight => totalWeight;
    public int TotalHP => totalHP;
    public int EnginePower => enginePower;
    public int Damage => damage;
    public float Range => range;
    public float ShotsPerSec => shotsPerSec;
    public string Knockback => knockback;
    public float VisionCone => visionCone;
    public float VisionRange => visionRange;
    public float CurrentHealth => currentHealth;
    public float Armor => armor;
    
    // Movement calculations based on weight and engine power
    public float MoveSpeed => Mathf.Max(1f, enginePower - (totalWeight * 0.1f));
    public float TurnSpeed => Mathf.Max(30f, 90f - (totalWeight * 0.5f));
    
    // Public properties for AI Master scripts
    public Transform turretPivot => turretTransform;
    
    // AI interface methods expected by NavAIMaster and TurretAIMaster
    public bool HasTarget() => currentTarget != null;
    public Transform GetCurrentTarget() => currentTarget?.transform;
    public bool IsEnemyVisible() => currentTarget != null && detectedEnemies.Contains(currentTarget);
    public bool IsEnemyWithinDistance(float distance) => currentTarget != null && Vector3.Distance(transform.position, currentTarget.transform.position) <= distance;
    public float GetDistanceToTarget() => currentTarget != null ? Vector3.Distance(transform.position, currentTarget.transform.position) : float.MaxValue;
    
    // AI execution state
    private AiExecutableNode currentNavNode;
    private AiExecutableNode currentTurretNode;
    private Coroutine navAiCoroutine;
    private Coroutine turretAiCoroutine;
    private Coroutine currentActionCoroutine;
    
    // Sensor data
    private GameObject currentTarget;
    private List<GameObject> detectedEnemies = new List<GameObject>();
    private List<GameObject> detectedAllies = new List<GameObject>();
    private float lastFireTime;
      void Start()
    {
        if (tankRigidbody == null) tankRigidbody = GetComponent<Rigidbody>();
        
        CalculateStats();
        currentHealth = totalHP;
        StartAI();
    }
    
    void FixedUpdate()
    {
        // Limit tank rotation to prevent flipping while allowing natural tilting
        LimitTankRotation();
    }
    
    #region Tank Parameters System
      /// <summary>
    /// Calculates all tank stats from component data stored in TankSlotData
    /// Call this when tank components change
    /// </summary>
    public void CalculateStats()
    {
        if (tankSlotData == null)
        {
            Debug.LogError($"[TankMan] No TankSlotData assigned to {gameObject.name}");
            return;
        }
        
        // Get total weight from TankSlotData (it calculates this)
        totalWeight = tankSlotData.totalWeight;
        
        // Get armor stats from TankSlotData stat fields
        totalHP = 100; // Base HP
        if (tankSlotData.armorHP > 0)
        {
            totalHP += tankSlotData.armorHP;
            armor = tankSlotData.armorHP * 0.25f; // Convert HP to armor value
            Debug.Log($"[TankMan] Added {tankSlotData.armorHP} HP from armor. Total HP: {totalHP}");
        }
        else
        {
            armor = 0f;
        }
        
        // Get engine stats from TankSlotData stat fields
        enginePower = tankSlotData.enginePower > 0 ? tankSlotData.enginePower : 1; // Base engine power
        Debug.Log($"[TankMan] Engine power: {enginePower}");
        
        // Get turret stats from TankSlotData stat fields
        damage = tankSlotData.turretDamage;
        range = tankSlotData.turretRange;
        shotsPerSec = tankSlotData.turretShotsPerSec;
        knockback = tankSlotData.turretKnockback;
        visionCone = tankSlotData.turretVisionCone;
        visionRange = tankSlotData.turretVisionRange;
        
        Debug.Log($"[TankMan] Turret stats - Damage: {damage}, Range: {range}, Vision: {visionRange}u/{visionCone}°");
        
        Debug.Log($"[TankMan] Final stats for {gameObject.name}:");
        Debug.Log($"  Weight: {totalWeight}, HP: {totalHP}, Engine: {enginePower}");
        Debug.Log($"  Move Speed: {MoveSpeed}, Turn Speed: {TurnSpeed}");
        Debug.Log($"  Combat: {damage} dmg, {range}u range, {shotsPerSec} shots/sec");
        Debug.Log($"  Vision: {visionRange}u range, {visionCone}° cone");
    }    /// <summary>
    /// Set the tank slot data reference (called by TankAssembly)
    /// </summary>
    public void SetTankSlotData(TankSlotData slotData)
    {
        tankSlotData = slotData;
        CalculateStats();
    }
    
    /// <summary>
    /// Set the turret and fire point transforms (called by TankAssembly)
    /// </summary>
    public void SetTurretComponents(Transform turret, Transform firePointTransform)
    {
        turretTransform = turret;
        firePoint = firePointTransform;
        Debug.Log($"[TankMan] Turret components set - Turret: {turret?.name}, FirePoint: {firePointTransform?.name}");
    }
    
    #endregion
    
    #region AI System
      public void StartAI()
    {
        StopAI();
        
        Debug.Log($"[TankMan] StartAI called for {gameObject.name}");
        Debug.Log($"[TankMan] tankSlotData: {(tankSlotData != null ? "present" : "null")}");
        Debug.Log($"[TankMan] navAI: {(tankSlotData?.navAI != null ? tankSlotData.navAI.name : "null")}");
        Debug.Log($"[TankMan] turretAI: {(tankSlotData?.turretAI != null ? tankSlotData.turretAI.name : "null")}");
        Debug.Log($"[TankMan] enableNavAI: {enableNavAI}, enableTurretAI: {enableTurretAI}");
        
        if (enableNavAI && tankSlotData?.navAI != null)
        {
            Debug.Log($"[TankMan] Starting NavAI coroutine for {gameObject.name}");
            navAiCoroutine = StartCoroutine(ExecuteNavAI());
        }
        else
        {
            Debug.Log($"[TankMan] NavAI not started - enableNavAI: {enableNavAI}, navAI present: {tankSlotData?.navAI != null}");
        }
        
        if (enableTurretAI && tankSlotData?.turretAI != null)
        {
            Debug.Log($"[TankMan] Starting TurretAI coroutine for {gameObject.name}");
            turretAiCoroutine = StartCoroutine(ExecuteTurretAI());
        }
        else
        {
            Debug.Log($"[TankMan] TurretAI not started - enableTurretAI: {enableTurretAI}, turretAI present: {tankSlotData?.turretAI != null}");
        }
    }
    
    public void StopAI()
    {
        if (navAiCoroutine != null)
        {
            StopCoroutine(navAiCoroutine);
            navAiCoroutine = null;
        }
        
        if (turretAiCoroutine != null)
        {
            StopCoroutine(turretAiCoroutine);
            turretAiCoroutine = null;
        }
        
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }
    }
      /// <summary>
    /// Main navigation AI execution loop
    /// </summary>
    IEnumerator ExecuteNavAI()
    {
        var navAiTree = tankSlotData.navAI;
        if (string.IsNullOrEmpty(navAiTree.startNodeId))
        {
            Debug.LogWarning($"[TankMan] Nav AI tree has no start node: {navAiTree.name}");
            yield break;
        }

        // Handle StartNavButton case - find nodes connected from StartNavButton
        currentNavNode = GetFirstNodeFromStart(navAiTree);
        
        while (currentNavNode != null)
        {
            yield return new WaitForSeconds(aiUpdateInterval);
            
            // Update sensor data
            UpdateSensorData();
            
            // Execute current node and get next node
            currentNavNode = ExecuteNode(currentNavNode, navAiTree);
        }
    }
      /// <summary>
    /// Main turret AI execution loop  
    /// </summary>
    IEnumerator ExecuteTurretAI()
    {
        var turretAiTree = tankSlotData.turretAI;
        if (string.IsNullOrEmpty(turretAiTree.startNodeId))
        {
            Debug.LogWarning($"[TankMan] Turret AI tree has no start node: {turretAiTree.name}");
            yield break;
        }

        // Handle StartNavButton case - find nodes connected from StartNavButton (same as NavAI)
        currentTurretNode = GetFirstNodeFromStart(turretAiTree);
        
        while (currentTurretNode != null)
        {
            yield return new WaitForSeconds(aiUpdateInterval);
            
            // Update sensor data
            UpdateSensorData();
            
            // Execute current node and get next node
            currentTurretNode = ExecuteNode(currentTurretNode, turretAiTree);
        }
    }
    
    /// <summary>
    /// Executes a single AI node and returns the next node to execute
    /// Implements the top-down, backtrack-on-false, Y-position priority pattern
    /// </summary>
    AiExecutableNode ExecuteNode(AiExecutableNode node, AiTreeAsset tree)
    {
        if (node == null) return null;
          // ...existing code...
        switch (node.nodeType)
        {
            case AiNodeType.Condition:
                bool conditionResult = ExecuteCondition(node);
                return GetNextNodeFromCondition(node, tree, conditionResult);
                
            case AiNodeType.Action:
                ExecuteAction(node);
                return GetNextNodeFromAction(node, tree);
                
            case AiNodeType.SubAI:
                ExecuteSubAI(node);
                return GetNextNodeFromAction(node, tree);
                
            default:
                // Move to first connected node
                if (node.connectedNodeIds.Count > 0)
                {
                    return tree.executableNodes.Find(n => n.nodeId == node.connectedNodeIds[0]);
                }
                return null;
        }
    }    /// <summary>
    /// Gets the next node after a condition based on the result and Y-position priority
    /// </summary>
    AiExecutableNode GetNextNodeFromCondition(AiExecutableNode conditionNode, AiTreeAsset tree, bool conditionResult)
    {
        if (conditionNode.connectedNodeIds.Count == 0)
            return null;
        
        // Sort connected nodes by Y position (highest first)
        var sortedConnections = conditionNode.connectedNodeIds
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();
        
        if (conditionResult)
        {
            // Condition passed - follow to first connected node (highest Y-position)
            var nextNode = sortedConnections.FirstOrDefault();            // ...existing code...
            return nextNode;
        }
        else
        {            // ...existing code...
            // Condition failed - try alternative paths from this node first
            if (sortedConnections.Count > 1)
            {
                // Try next connection (lower Y-position)
                var nextNode = sortedConnections[1];                // ...existing code...
                return nextNode;
            }
            
            // No direct alternatives - find the parent node and try its next branch
            AiExecutableNode parentNode = FindParentNode(conditionNode, tree);
            if (parentNode != null && parentNode != conditionNode)
            {            // ...existing code...
            return GetNextAlternativeFromParent(parentNode, conditionNode, tree);
            }
            
            // Check if this node is connected directly from StartNavButton
            bool isTopLevelNode = tree.connections.Any(c => c.fromNodeId == "StartNavButton" && c.toNodeId == conditionNode.nodeId);
            if (isTopLevelNode)
            {                // ...existing code...
                return GetNextAlternativeFromStart(conditionNode, tree);
            }
            
            // No alternatives found - restart from beginning            // ...existing code...
            return GetFirstNodeFromStart(tree);
        }
    }
    
    /// <summary>
    /// Find the parent node that connects to the given node
    /// </summary>
    AiExecutableNode FindParentNode(AiExecutableNode childNode, AiTreeAsset tree)
    {
        foreach (var node in tree.executableNodes)
        {
            if (node.connectedNodeIds.Contains(childNode.nodeId))
            {
                return node;
            }
        }
        return null;
    }
      /// <summary>
    /// Get the next alternative branch from a parent node
    /// </summary>
    AiExecutableNode GetNextAlternativeFromParent(AiExecutableNode parentNode, AiExecutableNode failedChild, AiTreeAsset tree)
    {
        // Sort parent's connections by Y position (highest first)
        var sortedConnections = parentNode.connectedNodeIds
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();

        // Find the failed child and try the next one
        int failedIndex = sortedConnections.FindIndex(n => n.nodeId == failedChild.nodeId);
        if (failedIndex >= 0 && failedIndex + 1 < sortedConnections.Count)
        {
            var nextNode = sortedConnections[failedIndex + 1];            // ...existing code...
            return nextNode;
        }
        
        // No more alternatives from this parent, continue backtracking
        AiExecutableNode grandParent = FindParentNode(parentNode, tree);
        if (grandParent != null && grandParent != parentNode)
        {
            return GetNextAlternativeFromParent(grandParent, parentNode, tree);
        }
        
        return null;
    }
      /// <summary>
    /// Gets the next node after an action
    /// </summary>
    AiExecutableNode GetNextNodeFromAction(AiExecutableNode actionNode, AiTreeAsset tree)
    {
        if (actionNode.connectedNodeIds.Count > 0)
        {
            string nextNodeId = actionNode.connectedNodeIds[0];
            return tree.executableNodes.Find(n => n.nodeId == nextNodeId);
        }
        
        // No connections - restart from beginning
        return GetFirstNodeFromStart(tree);
    }
    
    /// <summary>
    /// Updates sensor data for decision making
    /// </summary>
    void UpdateSensorData()
    {
        detectedEnemies.Clear();
        detectedAllies.Clear();
        currentTarget = null;
        
        // Detect enemies and allies in range
        Collider[] detected = Physics.OverlapSphere(transform.position, visionRange);
        
        foreach (var collider in detected)
        {
            if (collider.gameObject == gameObject) continue;
            
            // Check layer masks
            if (((1 << collider.gameObject.layer) & enemyLayerMask) != 0)
            {
                detectedEnemies.Add(collider.gameObject);
            }
            else if (((1 << collider.gameObject.layer) & allyLayerMask) != 0)
            {
                detectedAllies.Add(collider.gameObject);
            }
        }
        
        // Set current target to closest enemy
        if (detectedEnemies.Count > 0)
        {
            currentTarget = detectedEnemies
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .FirstOrDefault();
        }
    }
    
    #endregion
    
    #region Condition Execution
    
    /// <summary>
    /// Executes condition nodes and returns true/false result
    /// </summary>
    bool ExecuteCondition(AiExecutableNode conditionNode)
    {
        switch (conditionNode.methodName)
        {
            case "IfSelf":
                return currentTarget == gameObject;
                
            case "IfEnemy":
                return currentTarget != null && detectedEnemies.Contains(currentTarget);
                
            case "IfAlly":
                return currentTarget != null && detectedAllies.Contains(currentTarget);
                
            case "IfAny":
                return currentTarget != null;
                
            case "IfRifle":
                return currentTarget != null && 
                       Vector3.Distance(transform.position, currentTarget.transform.position) <= range;
                
            case "IfHP":
                // Check if current health meets the condition (e.g., "If HP > 50%" -> numericValue = 50)
                float healthPercent = (currentHealth / totalHP) * 100f;
                if (conditionNode.originalLabel.Contains(">"))
                    return healthPercent > conditionNode.numericValue;
                else if (conditionNode.originalLabel.Contains("<"))
                    return healthPercent < conditionNode.numericValue;
                else
                    return healthPercent >= conditionNode.numericValue;
                
            case "IfArmor":
                // Check armor condition
                if (conditionNode.originalLabel.Contains(">"))
                    return armor > conditionNode.numericValue;
                else if (conditionNode.originalLabel.Contains("<"))
                    return armor < conditionNode.numericValue;
                else
                    return armor >= conditionNode.numericValue;
                
            case "IfRange":
                // Check if target is within specified range
                if (currentTarget == null) return false;
                float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
                if (conditionNode.originalLabel.Contains(">"))
                    return distance > conditionNode.numericValue;
                else if (conditionNode.originalLabel.Contains("<"))
                    return distance < conditionNode.numericValue;
                else
                    return distance <= conditionNode.numericValue;
                    
            case "IfTag":
                return currentTarget != null && currentTarget.CompareTag(tankTag);
                
            default:
                Debug.LogWarning($"[TankMan] Unknown condition: {conditionNode.methodName}");
                return false;
        }
    }
    
    #endregion
    
    #region Action Execution
    
    /// <summary>
    /// Executes action nodes
    /// </summary>
    void ExecuteAction(AiExecutableNode actionNode)
    {
        // Stop any current action
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }
        
        switch (actionNode.methodName)
        {
            case "Fire":
                if (CanFire())
                {
                    Fire();
                }
                break;
                
            case "Wander":
                currentActionCoroutine = StartCoroutine(WanderAction());
                break;
                
            case "Move":
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(MoveToTarget());
                }
                else
                {
                    currentActionCoroutine = StartCoroutine(WanderAction());
                }
                break;
                
            case "Stop":
                StopMovement();
                break;
                
            case "Chase":
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(ChaseTarget());
                }
                break;
                
            case "Flee":
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(FleeFromTarget());
                }
                break;
                
            case "Patrol":
                currentActionCoroutine = StartCoroutine(PatrolAction());
                break;
                  case "Guard":
                currentActionCoroutine = StartCoroutine(GuardAction());
                break;
                  case "Wait":
                currentActionCoroutine = StartCoroutine(WaitAction());
                break;
                
            case "TrackTarget":
            case "CenterTarget": // Alias for TrackTarget
                if (currentTarget != null)
                {
                    currentActionCoroutine = StartCoroutine(TrackTargetAction());
                }
                break;
                
            default:
                Debug.LogWarning($"[TankMan] Unknown action: {actionNode.methodName}");
                break;
        }
    }
    
    /// <summary>
    /// Executes SubAI nodes (placeholder for now)
    /// </summary>
    void ExecuteSubAI(AiExecutableNode subAiNode)
    {
        Debug.Log($"[TankMan] Executing SubAI: {subAiNode.originalLabel}");
        // TODO: Implement SubAI execution by loading and running another AI tree
    }
    
    #endregion
    
    #region Combat System
    
    bool CanFire()
    {
        return currentTarget != null && 
               Time.time - lastFireTime >= (1f / shotsPerSec) &&
               Vector3.Distance(transform.position, currentTarget.transform.position) <= range;
    }
    
    void Fire()
    {
        if (currentTarget == null || firePoint == null) return;
        
        lastFireTime = Time.time;
        
        // Simple firing - instantiate projectile if prefab exists
        if (projectilePrefab != null)
        {
            Vector3 direction = (currentTarget.transform.position - firePoint.position).normalized;
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
            
            // Give projectile some velocity if it has a Rigidbody
            Rigidbody projRb = projectile.GetComponent<Rigidbody>();
            if (projRb != null)
            {
                projRb.linearVelocity = direction * projectileSpeed;
            }
        }
        
        Debug.Log($"[TankMan] Fired at {currentTarget.name}");
    }
    
    public void TakeDamage(float damageAmount)
    {
        // Apply armor reduction
        float finalDamage = Mathf.Max(0, damageAmount - armor);
        currentHealth -= finalDamage;
        
        Debug.Log($"[TankMan] {gameObject.name} took {finalDamage} damage (original: {damageAmount}, armor: {armor}). Health: {currentHealth}/{totalHP}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log($"[TankMan] {gameObject.name} destroyed!");
        StopAI();
        // TODO: Add death effects, cleanup, etc.
    }
    
    #endregion
    
    #region Movement Actions
      void StopMovement()
    {
        if (tankRigidbody != null)
        {
            // Apply braking force instead of directly stopping
            tankRigidbody.linearVelocity = Vector3.Lerp(tankRigidbody.linearVelocity, Vector3.zero, 5f * Time.deltaTime);
            tankRigidbody.angularVelocity = Vector3.Lerp(tankRigidbody.angularVelocity, Vector3.zero, 5f * Time.deltaTime);
        }
    }
      IEnumerator WanderAction()
    {
        // Pick a random point within 100 units on X and Z plane
        Vector2 randomPoint = Random.insideUnitCircle * 100f;
        Vector3 wanderTarget = transform.position + new Vector3(randomPoint.x, 0, randomPoint.y);
        
        Debug.Log($"[TankMan] Wandering to point: {wanderTarget}");
        
        // Move towards the wander target
        while (Vector3.Distance(transform.position, wanderTarget) > 3f)
        {
            Vector3 direction = (wanderTarget - transform.position).normalized;
            direction.y = 0f; // Keep on horizontal plane
            MoveInDirection(direction);
            yield return null;
        }
        
        Debug.Log($"[TankMan] Reached wander target, waiting briefly");
        // Wait briefly at the destination
        yield return new WaitForSeconds(1f);
    }
    
    IEnumerator MoveToTarget()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            MoveInDirection(direction);
            yield return null;
        }
    }
    
    IEnumerator ChaseTarget()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            MoveInDirection(direction);
            yield return null;
        }
    }
    
    IEnumerator FleeFromTarget()
    {
        while (currentTarget != null)
        {
            Vector3 direction = (transform.position - currentTarget.transform.position).normalized;
            MoveInDirection(direction);
            yield return null;
        }
    }
    
    IEnumerator PatrolAction()
    {
        // Simple back-and-forth patrol
        Vector3[] patrolPoints = {
            transform.position + transform.forward * 10f,
            transform.position + transform.forward * -10f
        };
        
        int currentPoint = 0;
        
        while (true)
        {
            Vector3 targetPoint = patrolPoints[currentPoint];
            Vector3 direction = (targetPoint - transform.position).normalized;
            
            while (Vector3.Distance(transform.position, targetPoint) > 2f)
            {
                MoveInDirection(direction);
                yield return null;
            }
            
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
            yield return new WaitForSeconds(1f); // Pause at patrol point
        }
    }
    
    IEnumerator GuardAction()
    {
        Vector3 guardPosition = transform.position;
        
        while (true)
        {
            // Return to guard position if too far away
            if (Vector3.Distance(transform.position, guardPosition) > 5f)
            {
                Vector3 direction = (guardPosition - transform.position).normalized;
                MoveInDirection(direction);
            }
            else
            {
                // Stop and look around
                StopMovement();
                
                // Slowly rotate to scan area
                if (tankRigidbody != null)
                {
                    tankRigidbody.angularVelocity = Vector3.up * TurnSpeed * 0.5f * Mathf.Deg2Rad;
                }
            }
              yield return null;
        }
    }
      IEnumerator WaitAction()
    {
        Debug.Log($"[TankMan] Waiting in place");
        
        // Stop all movement
        StopMovement();
        
        // Wait for a specified time (default 2 seconds)
        float waitTime = 2f;
        yield return new WaitForSeconds(waitTime);
        
        Debug.Log($"[TankMan] Finished waiting");
    }
    
    IEnumerator TrackTargetAction()
    {
        Debug.Log($"[TankMan] Tracking target with turret");
        
        while (currentTarget != null && turretTransform != null)
        {
            // Calculate direction to target
            Vector3 targetDirection = (currentTarget.transform.position - turretTransform.position).normalized;
            
            // Create rotation to look at target
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            
            // Smoothly rotate turret towards target
            turretTransform.rotation = Quaternion.RotateTowards(
                turretTransform.rotation, 
                targetRotation, 
                TurnSpeed * 2f * Time.deltaTime // Turret rotates faster than tank body
            );
            
            yield return null;
        }
        
        Debug.Log($"[TankMan] Lost target or no turret transform");
    }
      void MoveInDirection(Vector3 direction)
    {
        if (tankRigidbody == null) return;
        
        // Apply force instead of directly setting velocity for more realistic physics
        Vector3 force = direction * MoveSpeed * tankRigidbody.mass;
        tankRigidbody.AddForce(force, ForceMode.Force);
          // Limit maximum horizontal velocity to prevent unrealistic speeds, but allow natural falling
        Vector3 horizontalVelocity = new Vector3(tankRigidbody.linearVelocity.x, 0, tankRigidbody.linearVelocity.z);
        if (horizontalVelocity.magnitude > MoveSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * MoveSpeed;
            tankRigidbody.linearVelocity = new Vector3(horizontalVelocity.x, tankRigidbody.linearVelocity.y, horizontalVelocity.z);
        }
        
        // Rotate towards movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, TurnSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Limits tank rotation to ±30 degrees on X and Z axes for natural terrain following
    /// </summary>
    void LimitTankRotation()
    {
        if (tankRigidbody == null) return;
        
        Vector3 eulerAngles = transform.eulerAngles;
        
        // Convert angles to -180 to 180 range for easier clamping
        float xAngle = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;
        float zAngle = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
        
        // Clamp X and Z rotation to ±30 degrees
        float maxTilt = 30f;
        xAngle = Mathf.Clamp(xAngle, -maxTilt, maxTilt);
        zAngle = Mathf.Clamp(zAngle, -maxTilt, maxTilt);
        
        // Keep Y rotation unchanged (tank can rotate freely horizontally)
        Vector3 clampedRotation = new Vector3(xAngle, eulerAngles.y, zAngle);
        
        // Apply the clamped rotation
        transform.eulerAngles = clampedRotation;
        
        // If we hit the rotation limits, reduce angular velocity to prevent fighting
        if (Mathf.Abs(xAngle) >= maxTilt - 1f || Mathf.Abs(zAngle) >= maxTilt - 1f)
        {
            Vector3 angularVel = tankRigidbody.angularVelocity;
            angularVel.x *= 0.5f; // Dampen X rotation when near limit
            angularVel.z *= 0.5f; // Dampen Z rotation when near limit
            tankRigidbody.angularVelocity = angularVel;        }
    }
    
    #endregion
    
    #region Debug Visualization
    
    void OnDrawGizmosSelected()
    {
        // Draw sensor range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        
        // Draw weapon range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
        
        // Draw current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
    
    #endregion

    /// <summary>
    /// Gets the first node to execute from StartNavButton based on Y-position priority
    /// </summary>
    AiExecutableNode GetFirstNodeFromStart(AiTreeAsset tree)
    {
        // Find all connections from StartNavButton
        var startConnections = tree.nodes
            .Where(n => n.nodeId == "StartNavButton")
            .SelectMany(n => tree.connections
                .Where(c => c.fromNodeId == "StartNavButton")
                .Select(c => c.toNodeId))
            .ToList();

        if (startConnections.Count == 0)
        {
            // Fallback to old method if no StartNavButton connections found
            return tree.executableNodes.Find(n => n.nodeId == tree.startNodeId);
        }

        // Get connected nodes and sort by Y position (highest first)
        var connectedNodes = startConnections
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();        // ...existing code...
        return connectedNodes.FirstOrDefault();
    }

    /// <summary>
    /// Gets alternative nodes from StartNavButton when backtracking from a failed top-level node
    /// </summary>
    AiExecutableNode GetNextAlternativeFromStart(AiExecutableNode failedNode, AiTreeAsset tree)
    {
        // Find all connections from StartNavButton
        var startConnections = tree.connections
            .Where(c => c.fromNodeId == "StartNavButton")
            .Select(c => c.toNodeId)
            .ToList();

        // Get connected nodes and sort by Y position (highest first)
        var connectedNodes = startConnections
            .Select(nodeId => tree.executableNodes.Find(n => n.nodeId == nodeId))
            .Where(n => n != null)
            .OrderByDescending(n => n.position.y)
            .ToList();

        // Find the failed node and try the next one
        int failedIndex = connectedNodes.FindIndex(n => n.nodeId == failedNode.nodeId);
        if (failedIndex >= 0 && failedIndex + 1 < connectedNodes.Count)
        {
            var nextNode = connectedNodes[failedIndex + 1];            // ...existing code...
            return nextNode;
        }

        Debug.Log($"[TankMan] No more alternatives from StartNavButton, restarting");
        return connectedNodes.FirstOrDefault(); // Restart from first node
    }}
