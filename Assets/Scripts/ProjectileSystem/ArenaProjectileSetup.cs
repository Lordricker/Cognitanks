using UnityEngine;

public class ArenaProjectileSetup : MonoBehaviour
{
    [Header("Projectile System Setup")]
    public bool setupOnAwake = true;
    public LayerMask tankLayer = 1 << 6; // Assuming tanks are on layer 6
    public LayerMask obstacleLayer = 1 << 0; // Default layer for obstacles
    
    [Header("Projectile Prefabs")]
    public GameObject rifleBulletPrefab;
    public GameObject shotgunPelletPrefab;
    public GameObject artilleryShellPrefab;
    public GameObject sniperBulletPrefab;
    
    void Awake()
    {
        if (setupOnAwake)
        {
            SetupProjectileSystem();
        }
    }
    
    public void SetupProjectileSystem()
    {
        // Create ProjectileManager if it doesn't exist
        if (ProjectileManager.Instance == null)
        {
            GameObject managerObj = new GameObject("ProjectileManager");
            var manager = managerObj.AddComponent<ProjectileManager>();
            
            // Assign prefabs or create defaults
            manager.rifleBulletPrefab = rifleBulletPrefab ?? CreateDefaultProjectile("RifleBullet", Color.yellow, 0.8f);
            manager.shotgunPelletPrefab = shotgunPelletPrefab ?? CreateDefaultProjectile("ShotgunPellet", new Color(1f, 0.5f, 0f), 0.5f);
            manager.artilleryShellPrefab = artilleryShellPrefab ?? CreateDefaultProjectile("ArtilleryShell", Color.red, 2f);
            manager.sniperBulletPrefab = sniperBulletPrefab ?? CreateDefaultProjectile("SniperBullet", Color.cyan, 1.2f);
            
            DontDestroyOnLoad(managerObj);
            Debug.Log("ProjectileManager created for arena");
        }
        
        // Configure all turret controllers in the scene
        ConfigureTurretControllers();
    }
      void ConfigureTurretControllers()
    {
        var turretControllers = FindObjectsByType<TurretController>(FindObjectsSortMode.None);
        
        foreach (var controller in turretControllers)
        {
            // Set layer masks for projectiles
            controller.projectileLayerMask = tankLayer | obstacleLayer;
            
            // Ensure fire point is set
            if (controller.firePoint == null)
            {
                // Create a fire point if none exists
                GameObject firePoint = new GameObject("FirePoint");
                firePoint.transform.SetParent(controller.transform);
                firePoint.transform.localPosition = Vector3.forward * 0.5f;
                controller.firePoint = firePoint.transform;
            }
            
            Debug.Log($"Configured TurretController on {controller.name}");
        }
        
        Debug.Log($"Configured {turretControllers.Length} turret controllers");
    }
    
    GameObject CreateDefaultProjectile(string name, Color color, float scale = 1f)
    {
        // Create basic projectile
        GameObject projectile = new GameObject(name);
        
        // Add Projectile component
        var projectileComp = projectile.AddComponent<Projectile>();
        projectileComp.speed = 50f;
        projectileComp.damage = 25f;
        projectileComp.lifetime = 5f;
        
        // Create visual representation
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(projectile.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f) * scale;
        visual.transform.localRotation = Quaternion.Euler(90, 0, 0);
        
        // Remove the collider from visual (projectile will handle collision)
        DestroyImmediate(visual.GetComponent<Collider>());
        
        // Set material color
        var renderer = visual.GetComponent<Renderer>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        material.SetFloat("_Metallic", 0.3f);
        material.SetFloat("_Smoothness", 0.7f);
        renderer.material = material;
        
        // Add trail renderer
        var trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = 0.05f * scale;
        trail.endWidth = 0.01f * scale;
        trail.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        trail.material.color = color;
        projectileComp.trailRenderer = trail;
        
        // Add trigger collider for collision detection
        var collider = projectile.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 0.1f * scale;
        
        // Adjust projectile settings based on type
        if (name.Contains("Artillery"))
        {
            projectileComp.damage = 60f;
            projectileComp.explosionRadius = 3f;
            projectileComp.knockbackType = "Extreme";
            projectileComp.speed = 30f;
        }
        else if (name.Contains("Sniper"))
        {
            projectileComp.damage = 45f;
            projectileComp.knockbackType = "High";
            projectileComp.speed = 120f;
        }
        else if (name.Contains("Shotgun"))
        {
            projectileComp.damage = 15f;
            projectileComp.knockbackType = "Medium";
            projectileComp.speed = 50f;
        }
        else // Rifle
        {
            projectileComp.damage = 25f;
            projectileComp.knockbackType = "Low";
            projectileComp.speed = 75f;
        }
        
        return projectile;
    }
    
    [ContextMenu("Setup Projectile System")]
    public void SetupNow()
    {
        SetupProjectileSystem();
    }
    
    [ContextMenu("Test Fire Projectile")]
    public void TestFireProjectile()
    {
        if (ProjectileManager.Instance == null)
        {
            Debug.LogWarning("ProjectileManager not found!");
            return;
        }
        
        Vector3 firePos = transform.position + Vector3.up * 2f;
        Vector3 fireDir = transform.forward;
        
        ProjectileManager.Instance.SpawnProjectile(
            ProjectileManager.ProjectileType.Rifle,
            firePos,
            fireDir,
            gameObject,
            75f,
            25f,
            tankLayer | obstacleLayer
        );
        
        Debug.Log("Test projectile fired!");
    }
}
