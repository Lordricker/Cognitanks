using UnityEngine;

public class NavAIMaster : MonoBehaviour
{
    // Wander with tank-style movement: pick a random Y rotation, rotate toward it, then move forward
    public void Wander(GameObject tank, Rigidbody rb, ref float wanderTimer, ref float currentWanderDuration, float minWanderTime, float maxWanderTime, ref Vector3 lastPosition, float stuckSpeedThreshold, ref float stuckTimer, float stuckTime, ref float targetYaw, ref bool turningToTarget)
    {
        wanderTimer += Time.fixedDeltaTime;

        // If not currently turning to a target, pick a new random yaw
        if (!turningToTarget || wanderTimer >= currentWanderDuration)
        {
            targetYaw = Random.Range(0f, 360f);
            turningToTarget = true;
            currentWanderDuration = Random.Range(minWanderTime, maxWanderTime);
            wanderTimer = 0f;
        }

        // Get current yaw
        float currentYaw = tank.transform.eulerAngles.y;
        float deltaYaw = Mathf.DeltaAngle(currentYaw, targetYaw);

        // --- FIX: Always use Z axis as forward for tank movement (Unity default)
        // (No need to swap axes, just ensure tank model is oriented with +Z as forward)
        float forwardDelta = Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw));
        float backwardDelta = Mathf.Abs(Mathf.DeltaAngle((currentYaw + 180f), targetYaw));
        float forwardInput = 0f;
        float turnInput = 0f;

        if (backwardDelta < forwardDelta)
        {
            // Move backward, rotate to face away from targetYaw
            float reverseDeltaYaw = Mathf.DeltaAngle((currentYaw + 180f), targetYaw);
            turnInput = Mathf.Clamp(reverseDeltaYaw / 45f, -1f, 1f);
            forwardInput = -1f;
            if (Mathf.Abs(reverseDeltaYaw) < 2f)
            {
                turningToTarget = false;
                turnInput = 0f;
            }
        }
        else
        {
            // Move forward, rotate to face targetYaw
            turnInput = Mathf.Clamp(deltaYaw / 45f, -1f, 1f);
            forwardInput = 1f;
            if (Mathf.Abs(deltaYaw) < 2f)
            {
                turningToTarget = false;
                turnInput = 0f;
            }
        }

        // Prevent AI from moving the tank if TankController is frozen
        TankController controller = tank.GetComponent<TankController>();
        if (controller != null && controller.IsFrozen())
            return;

        // Use TankController if present
        if (controller != null)
        {
            controller.Move(forwardInput, turnInput);
        }        else
        {
            // Fallback: direct Rigidbody manipulation with proper speed
            float moveSpeed = 20f; // Default tank speed
            float rotationSpeed = 60f; // Default rotation speed
            
            // Move forward/backward using transform.forward
            rb.MovePosition(rb.position + tank.transform.forward * forwardInput * moveSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(Quaternion.Euler(0f, currentYaw + turnInput * rotationSpeed * Time.fixedDeltaTime, 0f));
        }

        // Stuck detection
        float speed = ((tank.transform.position - lastPosition) / Time.fixedDeltaTime).magnitude;
        if (speed < stuckSpeedThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > stuckTime)
            {
                targetYaw = Random.Range(0f, 360f);
                turningToTarget = true;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = tank.transform.position;
    }
}
