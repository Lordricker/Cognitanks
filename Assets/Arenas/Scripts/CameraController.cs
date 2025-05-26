using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public float lerpSpeed = 5f;
    public List<Transform> cameraAnchors = new List<Transform>();
    private int currentAnchorIndex = 0;
    private Transform targetAnchor;

    [Header("UI Buttons")]
    public Button globalCamButton;
    public Button cycleTankCamButton;

    private Vector3 dragOrigin;
    private bool isDragging = false;
    public float orbitSensitivity = 2f;

    public Transform globalAnchor; // Assign in inspector for global view

    private Vector3 smoothVelocity = Vector3.zero;
    private float smoothTime = 0.15f; // Smoothing time for camera follow
    private Quaternion targetRotation;

    void Start()
    {
        RefreshAnchors();
        if (cameraAnchors.Count > 0)
            SetTargetAnchor(cameraAnchors[0]);
        if (globalCamButton != null)
            globalCamButton.onClick.AddListener(MoveToGlobalAnchor);
        if (cycleTankCamButton != null)
            cycleTankCamButton.onClick.AddListener(CycleTankAnchor);
    }

    public void RefreshAnchors()
    {
        cameraAnchors.Clear();
        // Only add tank anchors (not global) to the cycle list
        foreach (var anchor in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (anchor.name == "CameraAnchor")
                cameraAnchors.Add(anchor);
        }
        currentAnchorIndex = 0;
        if (globalAnchor != null)
            SetTargetAnchor(globalAnchor);
        else if (cameraAnchors.Count > 0)
            SetTargetAnchor(cameraAnchors[0]);
    }

    void LateUpdate()
    {
        if (cameraAnchors.Count == 0) return;

        // Cycle camera anchors with Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            CycleTankAnchor();
        }

        // Smooth follow position and rotation
        if (targetAnchor != null)
        {
            // SmoothDamp for position
            transform.position = Vector3.SmoothDamp(transform.position, targetAnchor.position, ref smoothVelocity, smoothTime);
            // Slerp for rotation
            targetRotation = targetAnchor.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lerpSpeed);
        }

        // Mouse drag to orbit
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = Input.mousePosition;
            isDragging = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        if (isDragging && targetAnchor != null)
        {
            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;
            float yaw = delta.x * orbitSensitivity * Time.deltaTime;
            float pitch = -delta.y * orbitSensitivity * Time.deltaTime;
            targetAnchor.Rotate(Vector3.up, yaw, Space.World);
            targetAnchor.Rotate(Vector3.right, pitch, Space.Self);
        }
    }

    public void SetTargetAnchor(Transform anchor)
    {
        targetAnchor = anchor;
    }

    public void MoveToGlobalAnchor()
    {
        SetTargetAnchor(globalAnchor);
    }

    public void CycleTankAnchor()
    {
        if (cameraAnchors.Count == 0) return;
        currentAnchorIndex = (currentAnchorIndex + 1) % cameraAnchors.Count;
        SetTargetAnchor(cameraAnchors[currentAnchorIndex]);

        // Orbit anchor to -180 Y so camera faces tank's forward direction
        Transform anchor = cameraAnchors[currentAnchorIndex];
        Transform tank = anchor.parent;
        if (tank != null)
        {
            // Set anchor local rotation so camera looks forward relative to tank
            anchor.localRotation = Quaternion.Euler(10f, -90f, 0f);
        }
    }
}
