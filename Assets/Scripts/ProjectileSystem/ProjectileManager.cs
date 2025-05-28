using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }
    
    [Header("Projectile Prefabs")]
    public GameObject rifleBulletPrefab;
    public GameObject shotgunPelletPrefab;
    public GameObject artilleryShellPrefab;
    public GameObject sniperBulletPrefab;
    
    [Header("Pool Settings")]
    public int poolSize = 100;
    public bool usePooling = true;
    
    // Object pools for different projectile types
    private Dictionary<ProjectileType, Queue<GameObject>> projectilePools;
    private Dictionary<ProjectileType, GameObject> projectilePrefabs;
    
    public enum ProjectileType
    {
        Rifle,
        Shotgun,
        Artillery,
        Sniper
    }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializePools()
    {
        projectilePools = new Dictionary<ProjectileType, Queue<GameObject>>();
        projectilePrefabs = new Dictionary<ProjectileType, GameObject>
        {
            { ProjectileType.Rifle, rifleBulletPrefab },
            { ProjectileType.Shotgun, shotgunPelletPrefab },
            { ProjectileType.Artillery, artilleryShellPrefab },
            { ProjectileType.Sniper, sniperBulletPrefab }
        };
        
        if (!usePooling) return;
        
        // Initialize pools for each projectile type
        foreach (var kvp in projectilePrefabs)
        {
            if (kvp.Value == null) continue;
            
            Queue<GameObject> pool = new Queue<GameObject>();
            
            for (int i = 0; i < poolSize / 4; i++) // Divide pool size among projectile types
            {
                GameObject obj = Instantiate(kvp.Value);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                pool.Enqueue(obj);
            }
            
            projectilePools[kvp.Key] = pool;
        }
        
        Debug.Log($"ProjectileManager initialized with {projectilePools.Count} pools");
    }
    
    public GameObject SpawnProjectile(ProjectileType type, Vector3 position, Vector3 direction, 
                                    GameObject shooter, float speed, float damage, LayerMask hitLayerMask)
    {
        GameObject projectileObj = null;
        
        if (usePooling && projectilePools.ContainsKey(type) && projectilePools[type].Count > 0)
        {
            // Get from pool
            projectileObj = projectilePools[type].Dequeue();
            projectileObj.transform.position = position;
            projectileObj.transform.rotation = Quaternion.LookRotation(direction);
            projectileObj.SetActive(true);
        }
        else
        {
            // Create new instance
            if (projectilePrefabs.ContainsKey(type) && projectilePrefabs[type] != null)
            {
                projectileObj = Instantiate(projectilePrefabs[type], position, Quaternion.LookRotation(direction));
            }
        }
        
        if (projectileObj != null)
        {
            // Initialize the projectile
            var projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(direction, shooter, speed, damage, hitLayerMask);
                
                // Set up pooling return callback if using pools
                if (usePooling)
                {
                    var pooledProjectile = projectileObj.GetComponent<PooledProjectile>();
                    if (pooledProjectile == null)
                    {
                        pooledProjectile = projectileObj.AddComponent<PooledProjectile>();
                    }
                    pooledProjectile.SetPool(this, type);
                }
            }
        }
        
        return projectileObj;
    }
    
    public void ReturnToPool(GameObject projectile, ProjectileType type)
    {
        if (!usePooling || !projectilePools.ContainsKey(type)) 
        {
            Destroy(projectile);
            return;
        }
        
        // Reset projectile state
        projectile.SetActive(false);
        projectile.transform.SetParent(transform);
        
        // Reset projectile component
        var projectileComponent = projectile.GetComponent<Projectile>();
        if (projectileComponent != null)
        {
            // Reset any necessary state here
        }
        
        // Return to pool
        projectilePools[type].Enqueue(projectile);
    }
    
    public ProjectileType GetProjectileTypeFromTurret(string turretName)
    {
        string lowerName = turretName.ToLower();
        
        if (lowerName.Contains("rifle")) return ProjectileType.Rifle;
        if (lowerName.Contains("shotgun")) return ProjectileType.Shotgun;
        if (lowerName.Contains("artillery")) return ProjectileType.Artillery;
        if (lowerName.Contains("sniper")) return ProjectileType.Sniper;
        
        // Default to rifle
        return ProjectileType.Rifle;
    }
    
    public ProjectileStats GetProjectileStats(ProjectileType type)
    {
        switch (type)
        {
            case ProjectileType.Rifle:
                return new ProjectileStats
                {
                    speed = 75f,
                    damage = 25f,
                    explosionRadius = 0f,
                    knockbackType = "Low"
                };
                
            case ProjectileType.Shotgun:
                return new ProjectileStats
                {
                    speed = 50f,
                    damage = 15f,
                    explosionRadius = 0f,
                    knockbackType = "Medium"
                };
                
            case ProjectileType.Artillery:
                return new ProjectileStats
                {
                    speed = 30f,
                    damage = 60f,
                    explosionRadius = 3f,
                    knockbackType = "Extreme"
                };
                
            case ProjectileType.Sniper:
                return new ProjectileStats
                {
                    speed = 120f,
                    damage = 45f,
                    explosionRadius = 0f,
                    knockbackType = "High"
                };
                
            default:
                return new ProjectileStats();
        }
    }
}

[System.Serializable]
public struct ProjectileStats
{
    public float speed;
    public float damage;
    public float explosionRadius;
    public string knockbackType;
}

// Helper component for pooled projectiles
public class PooledProjectile : MonoBehaviour
{
    private ProjectileManager manager;
    private ProjectileManager.ProjectileType type;
    private float lifetime;
    private float timer;
    
    public void SetPool(ProjectileManager poolManager, ProjectileManager.ProjectileType projectileType)
    {
        manager = poolManager;
        type = projectileType;
        
        // Get lifetime from projectile component
        var projectile = GetComponent<Projectile>();
        if (projectile != null)
        {
            lifetime = projectile.lifetime;
        }
        
        timer = 0f;
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            ReturnToPool();
        }
    }
    
    void OnDisable()
    {
        timer = 0f;
    }
    
    public void ReturnToPool()
    {
        if (manager != null)
        {
            manager.ReturnToPool(gameObject, type);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Called when projectile hits something
    public void OnProjectileHit()
    {
        ReturnToPool();
    }
}
