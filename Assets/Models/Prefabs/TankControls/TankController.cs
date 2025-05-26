using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 20f; // Base speed, can be scaled by engine frame
    public float rotationSpeed = 60f; // Base rotation speed, can be scaled by engine frame

    [Header("Clamp Settings")]
    public float maxXRotation = 10f; // Degrees, to prevent flipping
    public float maxZRotation = 10f; // Degrees, to prevent flipping

    [Header("Arena Bounds")]
    public Vector2 minXZ = new Vector2(-50, -50); // Min X,Z (arena floor)
    public Vector2 maxXZ = new Vector2(50, 50);   // Max X,Z (arena floor)

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
    }

    // Call this to set arena bounds based on the ArenaTerrain object in the scene
    public void SetArenaBoundsFromTerrain()
    {
        GameObject terrainObj = GameObject.Find("ArenaTerrain");
        if (terrainObj != null)
        {
            Terrain terrain = terrainObj.GetComponent<Terrain>();
            if (terrain != null)
            {
                Vector3 pos = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                float minX = pos.x + 15f;
                float maxX = pos.x + size.x - 15f;
                float minZ = pos.z + 15f;
                float maxZ = pos.z + size.z - 15f;
                minXZ = new Vector2(minX, minZ);
                maxXZ = new Vector2(maxX, maxZ);
            }
            else
            {
                // If not a Terrain, use bounds of the object
                Renderer rend = terrainObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    Bounds b = rend.bounds;
                    float minX = b.min.x + 15f;
                    float maxX = b.max.x - 15f;
                    float minZ = b.min.z + 15f;
                    float maxZ = b.max.z - 15f;
                    minXZ = new Vector2(minX, minZ);
                    maxXZ = new Vector2(maxX, maxZ);
                }
            }
        }
        else
        {
            Debug.LogWarning("ArenaTerrain object not found in scene. Using default bounds.");
        }
    }

    public bool IsFrozen() => freezeActions;
}
