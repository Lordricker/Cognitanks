using UnityEngine;

/// <summary>
/// Complete setup script for turret and projectile systems in an arena.
/// Add this to any arena scene to automatically configure the combat system.
/// </summary>
public class ArenaCombatSystemSetup : MonoBehaviour
{
    [Header("System Configuration")]
    [Tooltip("Automatically setup systems when the scene starts")]
    public bool autoSetupOnStart = true;
    
    [Header("Layer Configuration")]
    [Tooltip("Layer that contains tank game objects")]
    public LayerMask tankLayerMask = 1 << 6;
    
    [Tooltip("Layers that can block projectiles (terrain, walls, etc.)")]
    public LayerMask obstacleLayerMask = 1 << 0;
    
    [Tooltip("Combined layers that projectiles can hit")]
    public LayerMask projectileHitLayerMask = -1;
    
    [Header("Projectile System")]
    [Tooltip("Use object pooling for better performance")]
    public bool useProjectilePooling = true;
    
    [Tooltip("Size of the projectile object pool")]
    public int projectilePoolSize = 100;
    
    [Header("Default Projectile Prefabs (Optional)")]
    public GameObject rifleBulletPrefab;
    public GameObject shotgunPelletPrefab;
    public GameObject artilleryShellPrefab;
    public GameObject sniperBulletPrefab;
    
    [Header("Debug & Testing")]
    public bool enableDebugMode = false;
    public bool showSetupLogs = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCombatSystem();
        }
    }
    
    [ContextMenu("Setup Combat System")]
    public void SetupCombatSystem()
    {
        LogSetup("Setting up Arena Combat System...");
        
        // 1. Setup ProjectileManager
        SetupProjectileManager();
        
        // 2. Configure all TurretControllers in scene
        ConfigureAllTurretControllers();
        
        // 3. Setup TurretAIMaster if needed
        SetupTurretAIMaster();
        
        // 4. Validate layer settings
        ValidateLayerConfiguration();
        
        LogSetup("Arena Combat System setup complete!");
    }
    
    void SetupProjectileManager()
    {
        if (ProjectileManager.Instance != null)
        {
            LogSetup("ProjectileManager already exists");
            return;
        }
        
        LogSetup("Creating ProjectileManager...");
        
        GameObject managerObj = new GameObject("ProjectileManager");
        var manager = managerObj.AddComponent<ProjectileManager>();
        
        // Configure pooling
        manager.usePooling = useProjectilePooling;
        manager.poolSize = projectilePoolSize;
        
        // Set prefabs or create defaults
        manager.rifleBulletPrefab = rifleBulletPrefab ?? CreateDefaultProjectilePrefab("RifleBullet", Color.yellow);
        manager.shotgunPelletPrefab = shotgunPelletPrefab ?? CreateDefaultProjectilePrefab("ShotgunPellet", new Color(1f, 0.5f, 0f));
        manager.artilleryShellPrefab = artilleryShellPrefab ?? CreateDefaultProjectilePrefab("ArtilleryShell", Color.red);
        manager.sniperBulletPrefab = sniperBulletPrefab ?? CreateDefaultProjectilePrefab("SniperBullet", Color.cyan);
        
        DontDestroyOnLoad(managerObj);
        LogSetup($"ProjectileManager created with {(useProjectilePooling ? "pooling enabled" : "pooling disabled")}");
    }
      void ConfigureAllTurretControllers()
    {
        var turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        LogSetup($"Configuring {turretControllers.Length} TurretControllers...");
        
        int configured = 0;
        foreach (var controller in turretControllers)
        {
            if (ConfigureTurretController(controller))
            {
                configured++;
            }
        }
        
        LogSetup($"Successfully configured {configured} TurretControllers");
    }
    
    bool ConfigureTurretController(TurretController controller)
    {
        if (controller == null) return false;
        
        try
        {
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
                
                LogSetup($"Created FirePoint for {controller.name}");
            }
            
            // Enable debug mode if requested
            if (enableDebugMode)
            {
                controller.showVisionCone = true;
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to configure TurretController {controller.name}: {e.Message}");
            return false;
        }
    }
    
    void SetupTurretAIMaster()
    {
        if (TurretAIMaster.Instance != null)
        {
            LogSetup("TurretAIMaster already exists");
            return;
        }
        
        // Check if we have a TurretAIMaster in the scene
        var existingMaster = FindFirstObjectByType<TurretAIMaster>();
        if (existingMaster == null)
        {
            LogSetup("Creating TurretAIMaster...");
            GameObject masterObj = new GameObject("TurretAIMaster");
            masterObj.AddComponent<TurretAIMaster>();
            DontDestroyOnLoad(masterObj);
        }
        
        LogSetup("TurretAIMaster ready");
    }
    
    void ValidateLayerConfiguration()
    {
        LogSetup("Validating layer configuration...");
        
        // Check if layers are properly set
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
        
        LogSetup("Layer validation complete");
    }
    
    GameObject CreateDefaultProjectilePrefab(string name, Color color)
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
        
        // Remove visual collider (projectile handles collision)
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
    
    void LogSetup(string message)
    {
        if (showSetupLogs)
        {
            Debug.Log($"[ArenaCombatSetup] {message}");
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
        Debug.Log($"Test fired {turrets.Length} turrets");
    }
    
    [ContextMenu("Show System Status")]
    public void ShowSystemStatus()
    {
        Debug.Log("=== Arena Combat System Status ===");
        Debug.Log($"ProjectileManager: {(ProjectileManager.Instance != null ? "Ready" : "Missing")}");
        Debug.Log($"TurretAIMaster: {(TurretAIMaster.Instance != null ? "Ready" : "Missing")}");
          var turrets = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        Debug.Log($"TurretControllers: {turrets.Length} found");
        
        foreach (var turret in turrets)
        {
            Debug.Log($"  - {turret.name}: FirePoint={turret.firePoint != null}, TurretData={turret.turretData != null}");
        }
    }
}
