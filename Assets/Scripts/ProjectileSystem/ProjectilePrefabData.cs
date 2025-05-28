using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile Prefab", menuName = "Tank System/Projectile Prefab Data")]
public class ProjectilePrefabData : ScriptableObject
{
    [Header("Visual Settings")]
    public GameObject meshPrefab;
    public Material material;
    public Color trailColor = Color.yellow;
    public float trailWidth = 0.1f;
    public float trailTime = 0.5f;
    
    [Header("Effects")]
    public GameObject hitEffectPrefab;
    public GameObject explosionEffectPrefab;
    public AudioClip fireSound;
    public AudioClip hitSound;
    public AudioClip explosionSound;
    
    [Header("Physics")]
    public float lifetime = 5f;
    public bool useGravity = false;
    public float gravityMultiplier = 1f;
    
    public GameObject CreateProjectilePrefab()
    {
        // Create main projectile object
        GameObject projectile = new GameObject("Projectile");
        
        // Add projectile component
        var projectileComponent = projectile.AddComponent<Projectile>();
        projectileComponent.lifetime = lifetime;
        projectileComponent.hitEffect = hitEffectPrefab;
        projectileComponent.explosionEffect = explosionEffectPrefab;
        projectileComponent.hitSound = hitSound;
        projectileComponent.explosionSound = explosionSound;
        
        // Add mesh if provided
        if (meshPrefab != null)
        {
            GameObject mesh = Instantiate(meshPrefab, projectile.transform);
            mesh.name = "Mesh";
        }
        else
        {
            // Create simple capsule mesh
            var meshRenderer = projectile.AddComponent<MeshRenderer>();
            var meshFilter = projectile.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateBulletMesh();
            meshRenderer.material = material;
        }
        
        // Add trail renderer
        var trailRenderer = projectile.AddComponent<TrailRenderer>();
        trailRenderer.time = trailTime;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = trailWidth * 0.2f;
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.material.color = trailColor;
        trailRenderer.minVertexDistance = 0.1f;
        projectileComponent.trailRenderer = trailRenderer;
        
        // Add collider for physics
        var collider = projectile.AddComponent<CapsuleCollider>();
        collider.radius = 0.05f;
        collider.height = 0.2f;
        collider.isTrigger = true;
          // Add rigidbody if using gravity
        if (useGravity)
        {
            var rb = projectile.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 0.1f;
            
            // Note: Unity 3D doesn't have gravityScale. 
            // Custom gravity scaling should be handled in Projectile.cs if needed.
        }
        
        return projectile;
    }
    
    private Mesh CreateBulletMesh()
    {
        // Create a simple bullet-shaped mesh
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0.1f),    // tip
            new Vector3(0.02f, 0, 0),   // body
            new Vector3(-0.02f, 0, 0),
            new Vector3(0, 0.02f, 0),
            new Vector3(0, -0.02f, 0),
            new Vector3(0, 0, -0.1f)    // tail
        };
        
        int[] triangles = new int[]
        {
            // Front
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 1,
            // Back
            5, 2, 1,
            5, 3, 2,
            5, 4, 3,
            5, 1, 4
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
}
