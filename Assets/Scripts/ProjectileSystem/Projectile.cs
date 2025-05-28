using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 50f;
    public float damage = 25f;
    public float lifetime = 5f;
    public float explosionRadius = 2f;
    public string knockbackType = "Medium";
    
    [Header("Visual Effects")]
    public GameObject hitEffect;
    public GameObject explosionEffect;
    public TrailRenderer trailRenderer;
    
    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip explosionSound;
    
    // Private fields
    private Vector3 direction;
    private GameObject shooter;
    private LayerMask hitLayerMask = -1;
    private bool hasExploded = false;
    private float travelTime = 0f;
    
    void Start()
    {
        // Destroy projectile after lifetime
        Destroy(gameObject, lifetime);
        
        // Set up trail renderer if present
        if (trailRenderer != null)
        {
            trailRenderer.time = 0.5f;
            trailRenderer.startWidth = 0.1f;
            trailRenderer.endWidth = 0.05f;
        }
    }
    
    void Update()
    {
        // Move projectile
        float deltaDistance = speed * Time.deltaTime;
        Vector3 newPosition = transform.position + direction * deltaDistance;
        
        // Raycast for collision detection
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, deltaDistance, hitLayerMask))
        {
            // Hit something
            HandleHit(hit);
        }
        else
        {
            // Update position
            transform.position = newPosition;
            travelTime += Time.deltaTime;
        }
    }
      public void Initialize(Vector3 shootDirection, GameObject shooterObj, float projectileSpeed, float projectileDamage, LayerMask layerMask)
    {
        direction = shootDirection.normalized;
        shooter = shooterObj;
        speed = projectileSpeed;
        damage = projectileDamage;
        hitLayerMask = layerMask;
        
        // Reset state for pooled projectiles
        hasExploded = false;
        travelTime = 0f;
        
        // Orient projectile towards direction
        transform.LookAt(transform.position + direction);
        
        // Reset trail renderer if present
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
    }
      void HandleHit(RaycastHit hit)
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Move to hit point
        transform.position = hit.point;
        
        // Check if we hit a tank
        TankController hitTank = hit.collider.GetComponentInParent<TankController>();
        if (hitTank != null && hitTank.gameObject != shooter)
        {
            // Apply damage to tank
            ApplyDamageToTank(hitTank, hit);
        }
        
        // Create hit effects
        CreateHitEffect(hit);
        
        // Apply area damage if explosion radius > 0
        if (explosionRadius > 0f)
        {
            ApplyAreaDamage(hit.point);
        }
        
        // Play hit sound
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, hit.point);
        }
        
        // Handle pooling or destruction
        var pooledProjectile = GetComponent<PooledProjectile>();
        if (pooledProjectile != null)
        {
            pooledProjectile.OnProjectileHit();
        }
        else
        {
            Destroy(gameObject, 0.1f); // Small delay to allow effects to spawn
        }
    }
    
    void ApplyDamageToTank(TankController tank, RaycastHit hit)
    {
        // Get tank assembly for component data
        var assembly = tank.GetComponent<TankAssembly>();
        if (assembly == null) return;
        
        // Apply damage (simplified - would integrate with health system)
        Debug.Log($"Projectile hit {tank.name} for {damage} damage at {hit.collider.name}");
        
        // Apply knockback based on knockback type
        ApplyKnockback(tank, hit);
        
        // Notify defensive turrets that this tank is under attack
        var aiMaster = TurretAIMaster.Instance;
        if (aiMaster != null)
        {
            aiMaster.NotifyTankUnderAttack(tank.gameObject);
        }
    }
    
    void ApplyKnockback(TankController tank, RaycastHit hit)
    {
        var rb = tank.GetComponent<Rigidbody>();
        if (rb == null) return;
        
        // Calculate knockback force based on type
        float knockbackForce = GetKnockbackForce(knockbackType);
        Vector3 knockbackDirection = (hit.point - transform.position).normalized;
        
        // Apply the force
        rb.AddForceAtPosition(knockbackDirection * knockbackForce, hit.point, ForceMode.Impulse);
        
        Debug.Log($"Applied {knockbackType} knockback ({knockbackForce}) to {tank.name}");
    }
    
    float GetKnockbackForce(string knockbackType)
    {
        switch (knockbackType.ToLower())
        {
            case "low": return 500f;
            case "medium": return 1000f;
            case "high": return 2000f;
            case "extreme": return 3500f;
            default: return 1000f;
        }
    }
    
    void ApplyAreaDamage(Vector3 explosionCenter)
    {
        Collider[] hitColliders = Physics.OverlapSphere(explosionCenter, explosionRadius);
        
        foreach (var collider in hitColliders)
        {
            TankController tank = collider.GetComponentInParent<TankController>();
            if (tank != null && tank.gameObject != shooter)
            {
                // Calculate distance-based damage reduction
                float distance = Vector3.Distance(explosionCenter, tank.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                float areaDamage = damage * damageMultiplier * 0.7f; // Area damage is 70% of direct hit
                
                Debug.Log($"Area damage to {tank.name}: {areaDamage:F1} (distance: {distance:F1})");
                
                // Apply reduced knockback for area damage
                var rb = tank.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 knockbackDir = (tank.transform.position - explosionCenter).normalized;
                    float areaKnockback = GetKnockbackForce(knockbackType) * damageMultiplier * 0.5f;
                    rb.AddForce(knockbackDir * areaKnockback, ForceMode.Impulse);
                }
            }
        }
    }
    
    void CreateHitEffect(RaycastHit hit)
    {
        // Create hit particle effect
        if (hitEffect != null)
        {
            var effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2f);
        }
        
        // Create explosion effect for area damage projectiles
        if (explosionRadius > 0f && explosionEffect != null)
        {
            var explosion = Instantiate(explosionEffect, hit.point, Quaternion.identity);
            Destroy(explosion, 3f);
            
            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, hit.point);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Show explosion radius in editor
        if (explosionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
        
        // Show projectile direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}
