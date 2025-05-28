using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 20f; // Base speed, can be scaled by engine frame
    public float rotationSpeed = 60f; // Base rotation speed, can be scaled by engine frame

    [Header("Clamp Settings")]
    public float maxXRotation = 45f; // Degrees, to prevent flipping
    public float maxZRotation = 45f; // Degrees, to prevent flipping    [Header("Arena Bounds")]
    public Vector2 minXZ = new Vector2(-300, -300); // Min X,Z (arena floor) - 600x600 default
    public Vector2 maxXZ = new Vector2(300, 300);   // Max X,Z (arena floor) - 600x600 default

    private Rigidbody rb;
    private EngineFrameData engineFrame; // Reference to equipped engine frame

    private float currentSpeed = 0f;
    public float acceleration = 10f; // Units per second squared
    public float deceleration = 15f; // Units per second squared

    private bool freezeActions = true;
    private float freezeTimer = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        freezeActions = true;
        freezeTimer = 2f;
    }

    public void SetEngineFrame(EngineFrameData frame)
    {
        engineFrame = frame;
        // No need to set moveSpeed or rotationSpeed from engineFrame
    }

    // Call this from AI or player input
    public void Move(float forwardInput, float turnInput)
    {
        if (freezeActions) return;

        float engineMultiplier = engineFrame != null ? engineFrame.enginePower : 1f;
        float speed = moveSpeed * engineMultiplier;
        float rotSpeed = rotationSpeed * engineMultiplier;
        float targetSpeed = speed * forwardInput;

        // Acceleration/deceleration
        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.fixedDeltaTime);
        }

        // --- FIX: If tank model's forward is -X, use -transform.right for forward
        Vector3 modelForward = -transform.right;
        Vector3 move = modelForward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // Rotate around Y axis (no change needed)
        Quaternion turn = Quaternion.Euler(0f, turnInput * rotSpeed * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    public void SetTankMass(float mass)
    {
        if (rb != null)
            rb.mass = mass;
    }

    void FixedUpdate()
    {
        if (freezeActions)
        {
            freezeTimer -= Time.fixedDeltaTime;
            if (freezeTimer <= 0f)
                freezeActions = false;
            // Still clamp rotation and position to arena bounds
            Vector3 euler = rb.rotation.eulerAngles;
            float clampedX = ClampAngle(euler.x, -maxXRotation, maxXRotation);
            float clampedZ = ClampAngle(euler.z, -maxZRotation, maxZRotation);
            rb.rotation = Quaternion.Euler(clampedX, euler.y, clampedZ);
            Vector3 pos = rb.position;
            pos.x = Mathf.Clamp(pos.x, minXZ.x, maxXZ.x);
            pos.z = Mathf.Clamp(pos.z, minXZ.y, maxXZ.y);
            rb.position = pos;
            return;
        }

        // Clamp X and Z rotation to keep tank upright
        Vector3 eulerAngles = rb.rotation.eulerAngles;
        float clampedXRotation = ClampAngle(eulerAngles.x, -maxXRotation, maxXRotation);
        float clampedZRotation = ClampAngle(eulerAngles.z, -maxZRotation, maxZRotation);
        rb.rotation = Quaternion.Euler(clampedXRotation, eulerAngles.y, clampedZRotation);

        // Clamp position to arena bounds
        Vector3 position = rb.position;
        position.x = Mathf.Clamp(position.x, minXZ.x, maxXZ.x);
        position.z = Mathf.Clamp(position.z, minXZ.y, maxXZ.y);
        rb.position = position;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        angle = Mathf.Repeat(angle + 180f, 360f) - 180f;
        return Mathf.Clamp(angle, min, max);
    }    // Call this to set arena bounds based on the ArenaTerrain object in the scene
    public void SetArenaBoundsFromTerrain()
    {
        // Try multiple terrain names in case it's named differently
        string[] terrainNames = { "ArenaTerrain", "Terrain", "New Terrain", "Arena" };
        GameObject terrainObj = null;
        
        foreach (string name in terrainNames)
        {
            terrainObj = GameObject.Find(name);
            if (terrainObj != null)
            {
                Debug.Log($"Found terrain object: {name}");
                break;
            }
        }
        
        if (terrainObj != null)
        {
            Terrain terrain = terrainObj.GetComponent<Terrain>();
            if (terrain != null)
            {
                Vector3 pos = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                float margin = 35f; // Increased margin to prevent wall climbing
                float minX = pos.x + margin;
                float maxX = pos.x + size.x - margin;
                float minZ = pos.z + margin;
                float maxZ = pos.z + size.z - margin;
                minXZ = new Vector2(minX, minZ);
                maxXZ = new Vector2(maxX, maxZ);
                Debug.Log($"Arena bounds set from terrain: MinXZ({minX:F1}, {minZ:F1}) MaxXZ({maxX:F1}, {maxZ:F1})");
                Debug.Log($"Terrain size: {size}, Position: {pos}, Margin: {margin}");
            }
            else
            {
                // If not a Terrain, use bounds of the object
                Renderer rend = terrainObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    Bounds b = rend.bounds;
                    float margin = 35f; // Increased margin
                    float minX = b.min.x + margin;
                    float maxX = b.max.x - margin;
                    float minZ = b.min.z + margin;
                    float maxZ = b.max.z - margin;
                    minXZ = new Vector2(minX, minZ);
                    maxXZ = new Vector2(maxX, maxZ);
                    Debug.Log($"Arena bounds set from renderer: MinXZ({minX:F1}, {minZ:F1}) MaxXZ({maxX:F1}, {maxZ:F1})");
                }
            }
        }
        else
        {
            Debug.LogWarning("No terrain object found in scene (tried: " + string.Join(", ", terrainNames) + "). Using default bounds.");
        }
    }

    public bool IsFrozen() => freezeActions;

    // Debug methods for testing arena bounds
    public void DebugCurrentBounds()
    {
        Debug.Log($"Current Arena Bounds: MinXZ({minXZ.x:F1}, {minXZ.y:F1}) MaxXZ({maxXZ.x:F1}, {maxXZ.y:F1})");
        Debug.Log($"Current Tank Position: ({transform.position.x:F1}, {transform.position.z:F1})");
    }

    public void SetArenaBoundsManually(float minX, float maxX, float minZ, float maxZ)
    {
        minXZ = new Vector2(minX, minZ);
        maxXZ = new Vector2(maxX, maxZ);
        Debug.Log($"Arena bounds manually set: MinXZ({minX:F1}, {minZ:F1}) MaxXZ({maxX:F1}, {maxZ:F1})");
    }
}
