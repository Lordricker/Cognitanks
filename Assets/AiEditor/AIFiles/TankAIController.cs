using UnityEngine;

public class TankAIController : MonoBehaviour
{
    public NavAIData navAIData; // Assign this at runtime based on tank loadout

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (navAIData != null)
            navAIData.StartWander(gameObject);
    }

    void FixedUpdate()
    {
        if (navAIData != null)
            navAIData.WanderTick(gameObject, rb);
    }
}
