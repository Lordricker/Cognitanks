using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class NodeDeleteUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Button deleteButton;
    public Image nodeImage;

    private NodeDraggable nodeDraggable;
    private Canvas parentCanvas;

    private Vector2 pointerDownPos;
    private float pointerDownTime;
    private const float clickThreshold = 10f; // pixels
    private const float clickTime = 0.25f; // seconds

    void Awake()
    {
        nodeDraggable = GetComponent<NodeDraggable>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    void Update()
    {
        // No longer hide the delete button on click outside; only hide if node image is clicked again
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        pointerDownTime = Time.unscaledTime;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        float dist = Vector2.Distance(pointerDownPos, eventData.position);
        float t = Time.unscaledTime - pointerDownTime;
        if (dist < clickThreshold && t < clickTime && eventData.pointerPress == nodeImage.gameObject)
        {
            if (deleteButton != null && deleteButton.gameObject.activeSelf)
                HideDeleteButton();
            else
                ShowDeleteButton();
        }
        else
        {
            // Do nothing
        }
    }

    public void OnPointerClick(PointerEventData eventData) { /* No-op, handled by up/down */ }

    public void ShowDeleteButton()
    {
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(true);
    }

    public void HideDeleteButton()
    {
        Debug.Log($"hiding button");
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);
    }

    public void OnDeleteClicked()
    {
        Debug.Log($"OnDeleteClicked called on {gameObject.name}");
        var nd = GetComponent<NodeDraggable>();
        if (nd != null)
        {
            Debug.Log($"Deleting all connected lines for {gameObject.name}");
            nd.DeleteAllConnectedLines();
        }
        else
        {
            Debug.Log($"No NodeDraggable found on {gameObject.name}");
        }
        Destroy(gameObject);
    }
}
