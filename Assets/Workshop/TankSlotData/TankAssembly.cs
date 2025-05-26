using UnityEngine;

public class TankAssembly : MonoBehaviour
{
    public Transform basePivot; // Where engine frame and armor will be instantiated
    public Transform turretPivot; // Where turret will be instantiated

    public void Assemble(TankSlotData data)
    {
        // Ensure a Rigidbody is present on the root tank object
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 100f; // Set a default mass, adjust as needed
        }

        // Ensure a TankController is present and assign engine frame
        TankController tankController = GetComponent<TankController>();
        if (tankController == null)
            tankController = gameObject.AddComponent<TankController>();
        if (data.engineFramePrefab != null && data.engineFrameInstanceId != null)
        {
            // Find the EngineFrameData instance from player inventory
            var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
            if (workshopUI != null)
            {
                var engineFrame = workshopUI.playerInventory.Find(c => c.instanceId == data.engineFrameInstanceId) as EngineFrameData;
                if (engineFrame != null)
                    tankController.SetEngineFrame(engineFrame);
            }
        }

        // After setting up TankController, set arena bounds from terrain
        if (tankController != null)
            tankController.SetArenaBoundsFromTerrain();

        // Calculate total tank weight from equipped components
        float totalWeight = 0f;
        if (data.engineFramePrefab != null)
        {
            var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
            if (workshopUI != null)
            {
                var engineFrame = workshopUI.playerInventory.Find(c => c.instanceId == data.engineFrameInstanceId);
                if (engineFrame != null)
                    totalWeight += engineFrame.weight;
            }
        }
        if (data.armorPrefab != null)
        {
            var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
            if (workshopUI != null)
            {
                var armor = workshopUI.playerInventory.Find(c => c.instanceId == data.armorInstanceId);
                if (armor != null)
                    totalWeight += armor.weight;
            }
        }
        if (data.turretPrefab != null)
        {
            var workshopUI = FindFirstObjectByType<WorkshopUIManager>();
            if (workshopUI != null)
            {
                var turret = workshopUI.playerInventory.Find(c => c.instanceId == data.turretInstanceId);
                if (turret != null)
                    totalWeight += turret.weight;
            }
        }
        // Set tank mass based on total weight
        if (tankController != null)
            tankController.SetTankMass(Mathf.Max(1f, totalWeight));

        // Ensure a TankAIController is present and assign NavAIData from slot
        TankAIController ai = GetComponent<TankAIController>();
        if (ai == null)
            ai = gameObject.AddComponent<TankAIController>();
        ai.navAIData = data.navAI;

        // Remove old children
        foreach (Transform child in basePivot) Destroy(child.gameObject);
        foreach (Transform child in turretPivot) Destroy(child.gameObject);

        // Instantiate engine frame and armor as children of basePivot
        if (data.engineFramePrefab != null)
            Instantiate(data.engineFramePrefab, basePivot.position, basePivot.rotation, basePivot);
        if (data.armorPrefab != null)
            Instantiate(data.armorPrefab, basePivot.position, basePivot.rotation, basePivot);

        // Instantiate turret as child of turretPivot
        if (data.turretPrefab != null)
            Instantiate(data.turretPrefab, turretPivot.position, turretPivot.rotation, turretPivot);

        // Add CameraAnchor if not present
        Transform anchor = transform.Find("CameraAnchor");
        if (anchor == null)
        {
            GameObject anchorObj = new GameObject("CameraAnchor");
            anchorObj.transform.SetParent(transform);
            anchorObj.transform.localPosition = new Vector3(30f, 15f, 0f); // X=20, Y=10, Z=0
            anchorObj.transform.localRotation = Quaternion.identity;
        }
    }
}
