using UnityEngine;

public class ProjectileSystemSetup : MonoBehaviour
{
    [Header("Setup Projectile Manager")]
    [SerializeField] private bool autoSetupProjectileManager = true;
    [SerializeField] private ProjectileManager projectileManagerPrefab;
    
    [Header("Default Projectile Prefabs")]
    public GameObject defaultRifleBullet;
    public GameObject defaultShotgunPellet;
    public GameObject defaultArtilleryShell;
    public GameObject defaultSniperBullet;
    
    void Awake()
    {
        if (autoSetupProjectileManager && ProjectileManager.Instance == null)
        {
            SetupProjectileManager();
        }
    }
    
    void SetupProjectileManager()
    {
        GameObject managerObj;
        
        if (projectileManagerPrefab != null)
        {
            managerObj = Instantiate(projectileManagerPrefab.gameObject);
        }
        else
        {
            managerObj = new GameObject("ProjectileManager");
            var manager = managerObj.AddComponent<ProjectileManager>();
            
            // Assign default prefabs
            manager.rifleBulletPrefab = defaultRifleBullet ?? CreateDefaultBulletPrefab("RifleBullet", Color.yellow);
            manager.shotgunPelletPrefab = defaultShotgunPellet ?? CreateDefaultBulletPrefab("ShotgunPellet", new Color(1f, 0.5f, 0f));
            manager.artilleryShellPrefab = defaultArtilleryShell ?? CreateDefaultBulletPrefab("ArtilleryShell", Color.red);
            manager.sniperBulletPrefab = defaultSniperBullet ?? CreateDefaultBulletPrefab("SniperBullet", Color.blue);
        }
        
        managerObj.name = "ProjectileManager";
        DontDestroyOnLoad(managerObj);
        
        Debug.Log("ProjectileManager created and configured");
    }
    
    GameObject CreateDefaultBulletPrefab(string name, Color color)
    {
        // Create basic bullet prefab
        GameObject bullet = new GameObject(name);
        
        // Add projectile component
        var projectile = bullet.AddComponent<Projectile>();
        projectile.lifetime = 5f;
        
        // Add visual mesh
        var meshRenderer = bullet.AddComponent<MeshRenderer>();
        var meshFilter = bullet.AddComponent<MeshFilter>();
        
        // Create simple bullet mesh
        meshFilter.mesh = CreateSimpleBulletMesh();
        
        // Create material
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Metallic", 0.5f);
        material.SetFloat("_Smoothness", 0.8f);
        meshRenderer.material = material;
        
        // Add trail renderer
        var trailRenderer = bullet.AddComponent<TrailRenderer>();
        trailRenderer.time = 0.5f;
        trailRenderer.startWidth = 0.1f;
        trailRenderer.endWidth = 0.02f;
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.material.color = color;
        trailRenderer.minVertexDistance = 0.1f;
        projectile.trailRenderer = trailRenderer;
        
        // Add collider
        var collider = bullet.AddComponent<CapsuleCollider>();
        collider.radius = 0.05f;
        collider.height = 0.2f;
        collider.isTrigger = true;
        
        // Scale down for different bullet types
        if (name.Contains("Pellet"))
        {
            bullet.transform.localScale = Vector3.one * 0.5f;
        }
        else if (name.Contains("Artillery"))
        {
            bullet.transform.localScale = Vector3.one * 2f;
        }
        else if (name.Contains("Sniper"))
        {
            bullet.transform.localScale = new Vector3(0.7f, 0.7f, 1.5f);
        }
        
        return bullet;
    }
    
    Mesh CreateSimpleBulletMesh()
    {
        // Create a simple elongated bullet shape
        Mesh mesh = new Mesh();
        
        // Simple capsule-like bullet
        Vector3[] vertices = new Vector3[]
        {
            // Front tip
            new Vector3(0, 0, 0.1f),
            
            // Middle ring
            new Vector3(0.03f, 0, 0),
            new Vector3(0, 0.03f, 0),
            new Vector3(-0.03f, 0, 0),
            new Vector3(0, -0.03f, 0),
            
            // Back ring
            new Vector3(0.025f, 0, -0.08f),
            new Vector3(0, 0.025f, -0.08f),
            new Vector3(-0.025f, 0, -0.08f),
            new Vector3(0, -0.025f, -0.08f),
            
            // Back tip
            new Vector3(0, 0, -0.1f)
        };
        
        int[] triangles = new int[]
        {
            // Front cone
            0, 2, 1,
            0, 3, 2,
            0, 4, 3,
            0, 1, 4,
            
            // Body sides
            1, 2, 5, 2, 6, 5,
            2, 3, 6, 3, 7, 6,
            3, 4, 7, 4, 8, 7,
            4, 1, 8, 1, 5, 8,
            
            // Back cone
            5, 6, 9,
            6, 7, 9,
            7, 8, 9,
            8, 5, 9
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    [ContextMenu("Setup Projectile Manager Now")]
    void SetupNow()
    {
        SetupProjectileManager();
    }
}
