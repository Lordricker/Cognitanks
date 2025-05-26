using UnityEngine;
using System.Collections.Generic;

public class WorkshopModelPreview : MonoBehaviour
{
    public Transform previewAnchor; // Assign in inspector
    public int previewLayer = 8; // Set to your "ModelPreview" layer number
    public float spinSpeed = 50f;

    private List<GameObject> currentModels = new List<GameObject>();

    void Update()
    {
        if (previewAnchor.childCount > 0)
            previewAnchor.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    public void ShowModel(GameObject prefab)
    {
        ClearPreview();
        if (prefab != null)
        {
            var model = Instantiate(prefab, previewAnchor);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(model, previewLayer);
            currentModels.Add(model);
        }
    }

    public void ShowTank(Dictionary<ComponentCategory, ComponentData> equipped)
    {
        ClearPreview();

        // Engine Frame (base)
        if (equipped.TryGetValue(ComponentCategory.EngineFrame, out var engineFrame) && engineFrame.modelPrefab != null)
        {
            var model = Instantiate(engineFrame.modelPrefab, previewAnchor);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(model, previewLayer);
            currentModels.Add(model);
        }

        // Turret
        if (equipped.TryGetValue(ComponentCategory.Turret, out var turret) && turret.modelPrefab != null)
        {
            var model = Instantiate(turret.modelPrefab, previewAnchor);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(model, previewLayer);
            currentModels.Add(model);
        }

        // Armor
        if (equipped.TryGetValue(ComponentCategory.Armor, out var armor) && armor.modelPrefab != null)
        {
            var model = Instantiate(armor.modelPrefab, previewAnchor);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(model, previewLayer);
            currentModels.Add(model);
        }

        // Add more categories as needed (e.g., AIModule)
    }

    public void ClearPreview()
    {
        foreach (Transform child in previewAnchor)
            Destroy(child.gameObject);
        currentModels.Clear();
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
