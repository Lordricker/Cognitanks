using UnityEngine;

/// <summary>
/// Quick setup script specifically for Arena1 scene to enable turret and projectile combat testing.
/// Add this script to any GameObject in Arena1 to automatically configure the combat system.
/// </summary>
public class Arena1Setup : MonoBehaviour
{
    [Header("Arena1 Combat Setup")]
    [Tooltip("Automatically setup combat system when scene loads")]
    public bool autoSetupOnStart = true;
    
    [Tooltip("Show setup logs in console")]
    public bool showLogs = true;
    
    [Tooltip("Enable debug visuals for turrets")]
    public bool enableDebugMode = true;
    
    [Header("Arena1 Configuration")]
    [Tooltip("Tank layer for target detection")]
    public LayerMask tankLayerMask = 1 << 6;
    
    [Tooltip("Obstacle layers that block projectiles")]
    public LayerMask obstacleLayerMask = 1 << 0;
    
    [Tooltip("Combined layers projectiles can hit")]
    public LayerMask projectileHitLayerMask = -1;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupArena1Combat();
        }
    }

    [ContextMenu("Setup Arena1 Combat")]
    public void SetupArena1Combat()
    {
        Log("=== ARENA1 COMBAT SETUP ===");
        
        // 1. Setup ProjectileManager
        SetupProjectileManager();
        
        // 2. Setup TurretAIMaster
        SetupTurretAIMaster();
        
        // 3. Configure any existing TurretControllers
        ConfigureExistingTurrets();
        
        // 4. Validate layer setup
        ValidateLayerConfiguration();
        
        Log("Arena1 combat setup complete! Tanks will have turret combat when spawned.");
        Log("Use the ArenaManager to spawn tanks, they will automatically have combat capabilities.");
    }

    void SetupProjectileManager()
    {
        if (ProjectileManager.Instance != null)
        {
            Log("ProjectileManager already exists");
            return;
        }

        Log("Creating ProjectileManager for Arena1...");
        
        GameObject managerObj = new GameObject("ProjectileManager");
        var manager = managerObj.AddComponent<ProjectileManager>();
        
        // Enable pooling for performance
        manager.usePooling = true;
        manager.poolSize = 50; // Smaller pool for Arena1 testing
        
        // Create default projectile prefabs
        manager.rifleBulletPrefab = CreateDefaultProjectile("RifleBullet", Color.yellow);
        manager.shotgunPelletPrefab = CreateDefaultProjectile("ShotgunPellet", new Color(1f, 0.5f, 0f));
        manager.artilleryShellPrefab = CreateDefaultProjectile("ArtilleryShell", Color.red);
        manager.sniperBulletPrefab = CreateDefaultProjectile("SniperBullet", Color.cyan);
        
        DontDestroyOnLoad(managerObj);
        Log("ProjectileManager created for Arena1");
    }

    void SetupTurretAIMaster()
    {
        if (TurretAIMaster.Instance != null)
        {
            Log("TurretAIMaster already exists");
            return;
        }

        var existingMaster = FindFirstObjectByType<TurretAIMaster>();
        if (existingMaster == null)
        {
            Log("Creating TurretAIMaster for Arena1...");
            GameObject masterObj = new GameObject("TurretAIMaster");
            masterObj.AddComponent<TurretAIMaster>();
            DontDestroyOnLoad(masterObj);
        }

        Log("TurretAIMaster ready for Arena1");
    }

    void ConfigureExistingTurrets()
    {
        var turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        Log($"Found {turretControllers.Length} existing TurretControllers to configure");

        foreach (var controller in turretControllers)
        {
            ConfigureTurretController(controller);
        }
    }

    void ConfigureTurretController(TurretController controller)
    {
        if (controller == null) return;

        // Set layer masks
        controller.tankLayerMask = tankLayerMask;
        controller.obstacleLayerMask = obstacleLayerMask;
        controller.projectileLayerMask = projectileHitLayerMask;

        // Ensure fire point exists
        if (controller.firePoint == null)
        {
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(controller.transform);
            firePoint.transform.localPosition = Vector3.forward * 0.5f;
            firePoint.transform.localRotation = Quaternion.identity;
            controller.firePoint = firePoint.transform;
        }

        // Enable debug visuals if requested
        if (enableDebugMode)
        {
            controller.showVisionCone = true;
        }

        Log($"Configured TurretController on {controller.name}");
    }

    GameObject CreateDefaultProjectile(string name, Color color)
    {
        GameObject projectile = new GameObject(name);
        
        // Add Projectile component
        var projectileComp = projectile.AddComponent<Projectile>();
        projectileComp.lifetime = 5f;
        
        // Create visual mesh
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(projectile.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.05f, 0.15f, 0.05f);
        visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        // Remove visual collider
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Set material
        var renderer = visual.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        material.SetFloat("_Metallic", 0.3f);
        material.SetFloat("_Smoothness", 0.7f);
        renderer.material = material;
        
        // Add trail
        var trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.03f;
        trail.endWidth = 0.005f;
        trail.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        trail.material.color = color;
        projectileComp.trailRenderer = trail;
        
        // Add collision
        var collider = projectile.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.05f;
        
        // Configure stats based on type
        ConfigureProjectileStats(projectileComp, name);
        
        return projectile;
    }

    void ConfigureProjectileStats(Projectile projectile, string typeName)
    {
        switch (typeName)
        {
            case "RifleBullet":
                projectile.speed = 75f;
                projectile.damage = 25f;
                projectile.knockbackType = "Low";
                break;
                
            case "ShotgunPellet":
                projectile.speed = 50f;
                projectile.damage = 15f;
                projectile.knockbackType = "Medium";
                break;
                
            case "ArtilleryShell":
                projectile.speed = 30f;
                projectile.damage = 60f;
                projectile.explosionRadius = 3f;
                projectile.knockbackType = "Extreme";
                break;
                
            case "SniperBullet":
                projectile.speed = 120f;
                projectile.damage = 45f;
                projectile.knockbackType = "High";
                break;
        }
    }

    void ValidateLayerConfiguration()
    {
        Log("Validating layer configuration...");
        
        if (tankLayerMask == 0)
        {
            Debug.LogWarning("Tank layer mask is empty! Turrets may not detect targets properly.");
        }
        
        if (obstacleLayerMask == 0)
        {
            Debug.LogWarning("Obstacle layer mask is empty! Projectiles may not collide with terrain.");
        }
        
        if (projectileHitLayerMask == 0)
        {
            Debug.LogWarning("Projectile hit layer mask is empty! Projectiles may not hit anything.");
        }
        
        Log("Layer validation complete");
    }

    void Log(string message)
    {
        if (showLogs)
        {
            Debug.Log($"[Arena1Setup] {message}");
        }
    }

    [ContextMenu("Test Fire All Turrets")]
    public void TestFireAllTurrets()
    {
        var turrets = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        foreach (var turret in turrets)
        {
            turret.FireAtTarget();
        }
        Log($"Test fired {turrets.Length} turrets");
    }

    [ContextMenu("Show System Status")]
    public void ShowSystemStatus()
    {
        Debug.Log("=== Arena1 Combat System Status ===");
        Debug.Log($"ProjectileManager: {(ProjectileManager.Instance != null ? "Ready" : "Missing")}");
        Debug.Log($"TurretAIMaster: {(TurretAIMaster.Instance != null ? "Ready" : "Missing")}");
        
        var turrets = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        Debug.Log($"TurretControllers: {turrets.Length} found");
        
        foreach (var turret in turrets)
        {
            Debug.Log($"  - {turret.name}: FirePoint={turret.firePoint != null}, TurretData={turret.turretData != null}");
        }
    }

    void OnGUI()
    {
        // Show status in top-right corner
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 100));
        
        if (ProjectileManager.Instance != null && TurretAIMaster.Instance != null)
        {
            GUI.backgroundColor = Color.green;
            GUILayout.BeginVertical("box");
            GUILayout.Label("✅ Arena1 Combat Ready!");
            if (GUILayout.Button("Test Fire All Turrets"))
            {
                TestFireAllTurrets();
            }
            GUILayout.EndVertical();
        }
        else
        {
            GUI.backgroundColor = Color.yellow;
            GUILayout.BeginVertical("box");
            GUILayout.Label("⚠️ Setting up combat...");
            if (GUILayout.Button("Setup Now"))
            {
                SetupArena1Combat();
            }
            GUILayout.EndVertical();
        }
        
        GUILayout.EndArea();
        GUI.backgroundColor = Color.white;
    }
}
