using UnityEngine;
using System.Collections;

public class TurretProjectileTestController : MonoBehaviour
{
    [Header("Test Settings")]
    public bool autoTest = false;
    public float testInterval = 3f;
    public Transform testTarget;
    
    [Header("Manual Test Controls")]
    public KeyCode fireTestKey = KeyCode.Space;
    public KeyCode cycleTurretTypeKey = KeyCode.T;
    public KeyCode toggleAIKey = KeyCode.A;
    
    private TurretController[] turretControllers;
    private int currentTurretIndex = 0;
    private bool aiEnabled = true;
    
    void Start()
    {        // Find all turret controllers in scene
        turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        Debug.Log($"Found {turretControllers.Length} turret controllers for testing");
        
        // Ensure ProjectileManager exists
        if (ProjectileManager.Instance == null)
        {
            var setup = FindFirstObjectByType<ArenaProjectileSetup>();
            if (setup != null)
            {
                setup.SetupProjectileSystem();
            }
            else
            {
                Debug.LogWarning("No ArenaProjectileSetup found! Creating basic ProjectileManager...");
                CreateBasicProjectileManager();
            }
        }
        
        if (autoTest)
        {
            StartCoroutine(AutoTestFiring());
        }
        
        // Create test target if none exists
        if (testTarget == null)
        {
            CreateTestTarget();
        }
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        // Manual fire test
        if (Input.GetKeyDown(fireTestKey))
        {
            FireCurrentTurret();
        }
        
        // Cycle through turret types
        if (Input.GetKeyDown(cycleTurretTypeKey))
        {
            CycleTurretType();
        }
        
        // Toggle AI
        if (Input.GetKeyDown(toggleAIKey))
        {
            ToggleAI();
        }
    }
    
    void FireCurrentTurret()
    {
        if (turretControllers.Length == 0) return;
        
        var turret = turretControllers[currentTurretIndex % turretControllers.Length];
        if (turret != null)
        {
            // Create a test target info
            if (testTarget != null)
            {
                var targetInfo = new TargetInfo(testTarget.gameObject, 
                    Vector3.Distance(turret.transform.position, testTarget.position),
                    (testTarget.position - turret.transform.position).normalized);
                
                turret.FireAtTarget(targetInfo);
                Debug.Log($"Manual fire from {turret.name}");
            }
            else
            {
                turret.FireAtTarget(); // Fire at current target
                Debug.Log($"Manual fire from {turret.name} at current target");
            }
        }
    }
    
    void CycleTurretType()
    {
        currentTurretIndex = (currentTurretIndex + 1) % Mathf.Max(1, turretControllers.Length);
        if (turretControllers.Length > 0)
        {
            Debug.Log($"Selected turret: {turretControllers[currentTurretIndex].name}");
        }
    }
    
    void ToggleAI()
    {
        aiEnabled = !aiEnabled;
        
        foreach (var turret in turretControllers)
        {
            turret.enabled = aiEnabled;
        }
        
        Debug.Log($"Turret AI {(aiEnabled ? "enabled" : "disabled")}");
    }
    
    IEnumerator AutoTestFiring()
    {
        while (autoTest)
        {
            yield return new WaitForSeconds(testInterval);
            
            if (turretControllers.Length > 0 && testTarget != null)
            {
                // Fire from a random turret
                var turret = turretControllers[Random.Range(0, turretControllers.Length)];
                if (turret != null)
                {
                    var targetInfo = new TargetInfo(testTarget.gameObject,
                        Vector3.Distance(turret.transform.position, testTarget.position),
                        (testTarget.position - turret.transform.position).normalized);
                    
                    turret.FireAtTarget(targetInfo);
                    Debug.Log($"Auto test fire from {turret.name}");
                }
            }
        }
    }
    
    void CreateTestTarget()
    {
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "TestTarget";
        target.transform.position = transform.position + Vector3.forward * 10f + Vector3.up * 1f;
        target.transform.localScale = Vector3.one * 2f;
        
        // Make it red so it's visible
        var renderer = target.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = Color.red;
        renderer.material = material;
        
        // Add some simple movement
        var movement = target.AddComponent<SimpleTargetMovement>();
        
        testTarget = target.transform;
        Debug.Log("Created test target");
    }
    
    void CreateBasicProjectileManager()
    {
        GameObject managerObj = new GameObject("ProjectileManager");
        var manager = managerObj.AddComponent<ProjectileManager>();
        
        // Create very basic projectile prefab
        GameObject basicBullet = new GameObject("BasicBullet");
        basicBullet.AddComponent<Projectile>();
        
        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.transform.SetParent(basicBullet.transform);
        visual.transform.localScale = Vector3.one * 0.1f;
        DestroyImmediate(visual.GetComponent<Collider>());
        
        var collider = basicBullet.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.1f;
        
        // Assign to all types for basic testing
        manager.rifleBulletPrefab = basicBullet;
        manager.shotgunPelletPrefab = basicBullet;
        manager.artilleryShellPrefab = basicBullet;
        manager.sniperBulletPrefab = basicBullet;
        
        DontDestroyOnLoad(managerObj);
        Debug.Log("Created basic ProjectileManager for testing");
    }
    
    void OnGUI()
    {
        if (turretControllers.Length == 0) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Turret & Projectile Test Controls:");
        GUILayout.Label($"Current Turret: {turretControllers[currentTurretIndex % turretControllers.Length].name}");
        GUILayout.Label($"AI Enabled: {aiEnabled}");
        GUILayout.Label($"Auto Test: {autoTest}");
        GUILayout.Space(10);
        GUILayout.Label($"[{fireTestKey}] - Manual Fire");
        GUILayout.Label($"[{cycleTurretTypeKey}] - Cycle Turret");
        GUILayout.Label($"[{toggleAIKey}] - Toggle AI");
        
        if (GUILayout.Button("Fire Current Turret"))
        {
            FireCurrentTurret();
        }
        
        if (GUILayout.Button(autoTest ? "Stop Auto Test" : "Start Auto Test"))
        {
            autoTest = !autoTest;
            if (autoTest)
            {
                StartCoroutine(AutoTestFiring());
            }
        }
        
        GUILayout.EndArea();
    }
}

// Simple component to make test targets move
public class SimpleTargetMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float moveRadius = 5f;
    
    private Vector3 centerPoint;
    private float angle = 0f;
    
    void Start()
    {
        centerPoint = transform.position;
    }
    
    void Update()
    {
        angle += moveSpeed * Time.deltaTime;
        Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * moveRadius;
        transform.position = centerPoint + offset;
    }
}
