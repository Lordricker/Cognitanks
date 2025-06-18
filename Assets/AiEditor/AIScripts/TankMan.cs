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
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderRange = 100f;
    [SerializeField] private float wanderReachDistance = 3f;
    
    [Header("Tank Components")]
    [SerializeField] private Transform turretTransform;
    [SerializeField] private Transform firePoint;
    [SerializeField] private UnityEngine.AI.NavMeshAgent navAgent;
    
    [Header("Sensor Settings")]
    [SerializeField] private LayerMask enemyLayerMask = 1 << 10; // Layer 8: Enemy
    [SerializeField] private LayerMask allyLayerMask = 1 << 9;  // Layer 9: Ally
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
    
    [Header("Assigned AI Components")]
    [SerializeField] private AiTreeAsset assignedNavAI;
    [SerializeField] private AiTreeAsset assignedTurretAI;
    
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
    public AiTreeAsset AssignedNavAI => assignedNavAI;
    public AiTreeAsset AssignedTurretAI => assignedTurretAI;

    [Header("Terrain Following")]
    private Quaternion desiredRotation = Quaternion.identity;
    private bool hasValidTerrainRotation = false;
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
    
    // Wander State Management
    private Vector3 currentWanderTarget;
    private bool isWandering = false;
    private Vector3 wanderOrigin; // Reference point for wander range checking
    private float wanderStartTime; // Track when we started moving to current wander target
    private float wanderTimeout = 5f; // Timeout in seconds before picking new wander point
    
    void Start()
    {
        if (navAgent == null) navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        // Assign the AI components from tankSlotData for display/reference
        assignedNavAI = tankSlotData != null ? tankSlotData.navAI : null;
        assignedTurretAI = tankSlotData != null ? tankSlotData.turretAI : null;
        
        // Initialize wander origin point
        wanderOrigin = transform.position;
        
        CalculateStats();
        currentHealth = totalHP;
        
        // Start AI after a small delay to ensure NavMeshAgent is ready
        StartCoroutine(DelayedStartAI());
    }
    
    IEnumerator DelayedStartAI()
    {
        // Wait a frame for all components to initialize
        yield return null;
        
        // Try to place NavMeshAgent on NavMesh if it isn't already
        if (navAgent != null && navAgent.enabled && !navAgent.isOnNavMesh)
        {
            navAgent.Warp(transform.position);
        }
        
        StartAI();
    }
    
    void FixedUpdate()
    {
        // Keep FixedUpdate empty - we'll do terrain alignment in LateUpdate
    }
    
    void LateUpdate()
    {
        // LateUpdate runs after all other updates, so NavMeshAgent won't override our rotation
        AlignToTerrain();
    }
    
    void Update()
    {
        // Force apply stored rotation if we have one
        if (hasValidTerrainRotation)
        {
            transform.rotation = desiredRotation;
        }
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
        assignedNavAI = tankSlotData != null ? tankSlotData.navAI : null;
        assignedTurretAI = tankSlotData != null ? tankSlotData.turretAI : null;
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
        
        // Reset wander state
        isWandering = false;
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
            var nextNode = sortedConnections.FirstOrDefault();
            return nextNode;
        }
        else
        {
            // Condition failed - check if this node is connected directly from StartNavButton
            bool isTopLevelNode = tree.connections.Any(c => c.fromNodeId == "StartNavButton" && c.toNodeId == conditionNode.nodeId);
            if (isTopLevelNode)
            {
                return GetNextAlternativeFromStart(conditionNode, tree);
            }
            
            // Not a top-level node - find the parent node and try its next branch
            AiExecutableNode parentNode = FindParentNode(conditionNode, tree);
            if (parentNode != null && parentNode != conditionNode)
            {
                return GetNextAlternativeFromParent(parentNode, conditionNode, tree);
            }
            
            // No alternatives found - restart from beginning
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
            // Skip self detection
            if (collider.gameObject == gameObject) 
            {
                continue;
            }
            
            int objectLayer = collider.gameObject.layer;
            bool isEnemy = ((1 << objectLayer) & enemyLayerMask) != 0;
            bool isAlly = ((1 << objectLayer) & allyLayerMask) != 0;
            
            // Check layer masks
            if (isEnemy)
            {
                detectedEnemies.Add(collider.gameObject);
            }
            else if (isAlly)
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
        else
        {
            currentTarget = null;
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
                bool hasTarget = currentTarget != null;
                bool targetIsEnemy = hasTarget && detectedEnemies.Contains(currentTarget);
                bool result = hasTarget && targetIsEnemy;
                return result;
                
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
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.ResetPath(); // Stop NavMesh Agent movement
        }
        
        // No need to manually stop rigidbody since we're using NavMeshAgent only
    }
      IEnumerator WanderAction()
    {
        // Wait for NavMeshAgent to be properly initialized and placed on NavMesh
        float waitStartTime = Time.time;
        while (navAgent != null && (!navAgent.enabled || !navAgent.isOnNavMesh))
        {
            // Try to warp agent to current position to place it on NavMesh
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.Warp(transform.position);
            }
            
            // Timeout after 2 seconds of waiting
            if (Time.time - waitStartTime > 2f)
            {
                Debug.LogWarning($"[TankMan] NavMeshAgent failed to initialize after 2 seconds. Enabled: {navAgent?.enabled}, OnNavMesh: {navAgent?.isOnNavMesh}");
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        // Check if we need to set a new wander target
        if (!isWandering || ShouldPickNewWanderTarget())
        {
            SetNewWanderTarget();
            wanderStartTime = Time.time; // Reset timeout timer when setting new target
        }
        
        // Use NavMesh Agent to move to wander target
        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.SetDestination(currentWanderTarget);
        }
        else
        {
            Debug.LogWarning($"[TankMan] NavMeshAgent is not ready for SetDestination. Enabled: {navAgent?.enabled}, OnNavMesh: {navAgent?.isOnNavMesh}");
            yield break;
        }
        
        // Wait until we reach the target or timeout
        while (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            // Check if path is still pending
            if (navAgent.pathPending)
            {
                yield return null;
                continue;
            }
            
            // Check if we've reached the destination
            if (!navAgent.pathPending && navAgent.remainingDistance < wanderReachDistance)
            {
                break;
            }
            
            // Check for timeout - if we've been trying to reach this target for too long, pick a new one
            if (Time.time - wanderStartTime > wanderTimeout)
            {
                SetNewWanderTarget();
                wanderStartTime = Time.time;
                
                if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
                {
                    navAgent.SetDestination(currentWanderTarget);
                }
                continue;
            }
            
            yield return null;
        }
        
        // Mark as no longer wandering so a new target will be picked next time
        isWandering = false;
        
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
            Vector3 targetDirection = (currentTarget.transform.position - turretTransform.position);
            
            // Only rotate if there's a meaningful distance to the target
            if (targetDirection.magnitude > 0.1f)
            {
                targetDirection.Normalize();
                
                // Create rotation to look at target
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                
                // Smoothly rotate turret towards target
                turretTransform.rotation = Quaternion.RotateTowards(
                    turretTransform.rotation, 
                    targetRotation, 
                    TurnSpeed * 2f * Time.deltaTime // Turret rotates faster than tank body
                );
            }
            
            yield return null;
        }
        
        Debug.Log($"[TankMan] Lost target or no turret transform");
    }
      void MoveInDirection(Vector3 direction)
    {
        if (navAgent == null || !navAgent.enabled || !navAgent.isOnNavMesh) 
        {
            Debug.LogWarning($"[TankMan] MoveInDirection called but NavMeshAgent not ready. Enabled: {navAgent?.enabled}, OnNavMesh: {navAgent?.isOnNavMesh}");
            return;
        }
        
        if (direction == Vector3.zero)
        {
            // Stop smoothly
            navAgent.ResetPath();
            return;
        }
        
        // Calculate target position in the given direction
        Vector3 targetPosition = transform.position + direction.normalized * 10f; // Move 10 units in direction
        
        // Clamp target to map boundaries
        targetPosition.x = Mathf.Clamp(targetPosition.x, 30f, 770f);
        targetPosition.z = Mathf.Clamp(targetPosition.z, 30f, 770f);
        
        // Set NavMesh destination - NavMeshAgent will handle both movement and rotation
        navAgent.SetDestination(targetPosition);
    }
    
    /// <summary>
    /// Limits tank rotation to ±30 degrees on X and Z axes for natural terrain following
    /// </summary>
    void LimitTankRotation()
    {
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
        
        // Note: No need to dampen angular velocity since rigidbody is kinematic
    }
    
    /// <summary>
    /// Align tank to terrain by raycasting to detect ground slope and tilt accordingly
    /// </summary>
    void AlignToTerrain()
    {
        // Perform raycasts from tank to ground to detect terrain slope
        float rayDistance = 30f; // Distance to cast rays
        // Only detect layer 11 (Terrain)
        LayerMask terrainLayer = 1 << 11; // Only terrain layer
        
        // Cast rays from further out to get better slope detection
        Vector3 frontPoint = transform.position + transform.forward * 4f;
        Vector3 backPoint = transform.position - transform.forward * 4f;
        Vector3 leftPoint = transform.position - transform.right * 4f;
        Vector3 rightPoint = transform.position + transform.right * 4f;
        
        // Start rays from well above the tank
        Vector3 rayStart = Vector3.up * 15f;
        
        bool frontHit = Physics.Raycast(frontPoint + rayStart, Vector3.down, out RaycastHit frontHitInfo, rayDistance, terrainLayer);
        bool backHit = Physics.Raycast(backPoint + rayStart, Vector3.down, out RaycastHit backHitInfo, rayDistance, terrainLayer);
        bool leftHit = Physics.Raycast(leftPoint + rayStart, Vector3.down, out RaycastHit leftHitInfo, rayDistance, terrainLayer);
        bool rightHit = Physics.Raycast(rightPoint + rayStart, Vector3.down, out RaycastHit rightHitInfo, rayDistance, terrainLayer);
        
        // Visual debugging - draw the rays in Scene view
        Debug.DrawRay(frontPoint + rayStart, Vector3.down * rayDistance, frontHit ? Color.green : Color.red, 0.1f);
        Debug.DrawRay(backPoint + rayStart, Vector3.down * rayDistance, backHit ? Color.green : Color.red, 0.1f);
        Debug.DrawRay(leftPoint + rayStart, Vector3.down * rayDistance, leftHit ? Color.green : Color.red, 0.1f);
        Debug.DrawRay(rightPoint + rayStart, Vector3.down * rayDistance, rightHit ? Color.green : Color.red, 0.1f);
        
        // Debug what we're hitting - only show warnings if terrain detection fails
        if (Time.frameCount % 120 == 0) // Very infrequent logging
        {
            if (!frontHit || !backHit || !leftHit || !rightHit)
            {
                Debug.LogWarning($"[TankMan] Not all terrain raycasts hit layer 11 - make sure terrain objects are set to layer 11 (Terrain)");
            }
        }
        
        if (frontHit && backHit && leftHit && rightHit)
        {
            // Calculate pitch (X rotation) from front-back height difference
            float frontHeight = frontHitInfo.point.y;
            float backHeight = backHitInfo.point.y;
            float heightDifference = frontHeight - backHeight;
            float pitchAngle = Mathf.Atan2(heightDifference, 8f) * Mathf.Rad2Deg; // Using 8f for distance between points
            
            // Calculate roll (Z rotation) from left-right height difference
            float leftHeight = leftHitInfo.point.y;
            float rightHeight = rightHitInfo.point.y;
            float sideDifference = rightHeight - leftHeight;
            float rollAngle = Mathf.Atan2(sideDifference, 8f) * Mathf.Rad2Deg;
            
            // Get current Y rotation - since NavAgent updateRotation is false, we need to calculate turning manually
            float yRotation = transform.eulerAngles.y;
            
            // Manual Y-axis rotation towards movement direction if NavMeshAgent is moving
            if (navAgent != null && navAgent.velocity.magnitude > 0.1f)
            {
                Vector3 moveDirection = navAgent.velocity.normalized;
                float targetYRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                yRotation = Mathf.LerpAngle(yRotation, targetYRotation, Time.fixedDeltaTime * 3f);
            }
            
            // Debug the calculated angles - show them more frequently to see what's happening
            if (Time.frameCount % 30 == 0) // Show every 30 frames
            {
                Debug.Log($"[TankMan] Height values - Front: {frontHeight:F2}, Back: {backHeight:F2}, Left: {leftHeight:F2}, Right: {rightHeight:F2}");
                Debug.Log($"[TankMan] Calculated angles - Pitch: {pitchAngle:F2}°, Roll: {rollAngle:F2}°");
                Debug.Log($"[TankMan] Current tank rotation: {transform.eulerAngles}");
            }
        }
        else
        {
            // If we can't detect terrain properly
            if (Time.frameCount % 300 == 0) // Very infrequent logging
            {
                Debug.LogWarning($"[TankMan] Not all terrain raycasts hit layer 11 - make sure terrain objects are set to layer 11 (Terrain)");
            }
        }
    }

    /// <summary>
    /// Sets a new wander target within the allowed range
    /// </summary>
    private void SetNewWanderTarget()
    {
        // Update wander origin to current tank position for free roaming
        wanderOrigin = transform.position;
        Debug.Log($"[TankMan] Updated wander origin to current position: {wanderOrigin}");
        
        // Map boundaries (matching MoveInDirection clamp values)
        float minBoundary = 30f;
        float maxBoundary = 770f;
        
        Vector3 potentialTarget;
        int maxAttempts = 10; // Prevent infinite loops
        int attempts = 0;
        
        // Keep generating targets until we find one within map boundaries
        do
        {
            // Generate random point within wander range from new origin
            Vector2 randomCircle = Random.insideUnitCircle * wanderRange;
            potentialTarget = wanderOrigin + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Clamp target to map boundaries
            potentialTarget.x = Mathf.Clamp(potentialTarget.x, minBoundary, maxBoundary);
            potentialTarget.z = Mathf.Clamp(potentialTarget.z, minBoundary, maxBoundary);
            
            attempts++;
            
            // If we've tried many times and still getting clamped targets, 
            // generate a target closer to center of valid area
            if (attempts > 5)
            {
                // Find center of valid area relative to tank position
                float centerX = Mathf.Clamp(transform.position.x, minBoundary + 50f, maxBoundary - 50f);
                float centerZ = Mathf.Clamp(transform.position.z, minBoundary + 50f, maxBoundary - 50f);
                
                // Generate target in smaller range around the adjusted center
                Vector2 smallerCircle = Random.insideUnitCircle * Mathf.Min(wanderRange * 0.5f, 100f);
                potentialTarget = new Vector3(centerX + smallerCircle.x, wanderOrigin.y, centerZ + smallerCircle.y);
                
                // Final boundary clamp
                potentialTarget.x = Mathf.Clamp(potentialTarget.x, minBoundary, maxBoundary);
                potentialTarget.z = Mathf.Clamp(potentialTarget.z, minBoundary, maxBoundary);
                
                Debug.Log($"[TankMan] Generated boundary-safe wander target after {attempts} attempts");
                break;
            }
            
        } while ((potentialTarget.x <= minBoundary || potentialTarget.x >= maxBoundary || 
                  potentialTarget.z <= minBoundary || potentialTarget.z >= maxBoundary) && 
                 attempts < maxAttempts);
        
        // Check if we should prefer forward or backward movement based on tank orientation
        Vector3 tankForward = transform.forward; // Tank forward is now +Z direction
        Vector3 directionToTarget = (potentialTarget - transform.position).normalized;
        
        // Calculate dot product to determine if target is more forward or backward
        float forwardAlignment = Vector3.Dot(tankForward, directionToTarget);
        
        // If target is behind us (dot product < 0), consider generating a forward target instead
        if (forwardAlignment < -0.3f) // Allow some tolerance
        {
            // Generate a new target more in the forward direction, but keep it within boundaries
            Vector3 forwardDirection = tankForward + Random.insideUnitCircle.x * 0.5f * Vector3.forward + Random.insideUnitCircle.y * 0.5f * Vector3.back;
            forwardDirection.Normalize();
            Vector3 forwardTarget = transform.position + forwardDirection * Random.Range(wanderRange * 0.3f, wanderRange);
            
            // Clamp forward target to boundaries
            forwardTarget.x = Mathf.Clamp(forwardTarget.x, minBoundary, maxBoundary);
            forwardTarget.z = Mathf.Clamp(forwardTarget.z, minBoundary, maxBoundary);
            
            // Only use forward target if it's significantly different from original
            float distanceImprovement = Vector3.Distance(transform.position, forwardTarget) - Vector3.Distance(transform.position, potentialTarget);
            if (distanceImprovement > 10f) // Only if forward target is meaningfully better
            {
                potentialTarget = forwardTarget;
                Debug.Log($"[TankMan] Adjusted wander target to favor forward movement (within boundaries)");
            }
        }
        
        currentWanderTarget = potentialTarget;
        isWandering = true;
        
        // Calculate horizontal distance from new origin for logging
        Vector3 finalOriginPos = new Vector3(wanderOrigin.x, 0, wanderOrigin.z);
        Vector3 finalTargetPos = new Vector3(currentWanderTarget.x, 0, currentWanderTarget.z);
        float horizontalDistance = Vector3.Distance(finalOriginPos, finalTargetPos);
        
        Debug.Log($"[TankMan] New boundary-safe wander target set: {currentWanderTarget} (Distance from origin: {horizontalDistance:F1}u)");
    }
    
    /// <summary>
    /// Checks if we should pick a new wander target
    /// </summary>
    private bool ShouldPickNewWanderTarget()
    {
        // Always pick new target since we update origin each time - no range restrictions
        // This allows free roaming behavior
        return false; // Never force a new target based on range since origin moves with tank
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
        
        // Draw wander system visualization
        if (Application.isPlaying)
        {
            // Draw wander origin and range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wanderOrigin, wanderRange);
            
            // Draw current wander target if wandering
            if (isWandering)
            {
                Gizmos.color = Color.green;
                
                // Draw tall capsule for better waypoint visibility
                Vector3 bottom = currentWanderTarget - Vector3.up * 2f;
                Vector3 top = currentWanderTarget + Vector3.up * 2f;
                Gizmos.DrawWireCube(currentWanderTarget, new Vector3(wanderReachDistance * 2f, 4f, wanderReachDistance * 2f));
                Gizmos.DrawLine(bottom, top);
                
                Gizmos.DrawLine(transform.position, currentWanderTarget);
            }
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
            .ToList();
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
