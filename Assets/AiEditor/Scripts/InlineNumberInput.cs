using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

public class InlineNumberInput : MonoBehaviour, IPointerClickHandler
{
    [Header("Components")]
    public TMP_Text buttonText;
    public TMP_InputField inputField; // Assign in inspector, overlay on top of buttonText
    
    [Header("Settings")]
    public string originalTemplate = ""; // Store the original template like "HP > #%"
    
    private string currentNumber = "0";    void Start()
    {
        if (inputField != null)
        {
            inputField.gameObject.SetActive(false);
            inputField.onEndEdit.AddListener(OnInputEndEdit);
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            inputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        }
        
        // Store the original template from the button text
        if (buttonText != null && string.IsNullOrEmpty(originalTemplate))
        {
            originalTemplate = buttonText.text;
        }
        
        // Don't initialize with "0" here - let loading system set the correct value
        // The loading system will call SetCurrentNumber() with the saved value
    }    public void SetTemplate(string template)
    {
        originalTemplate = template;
        if (template.Contains("#"))
        {
            // If currentNumber is still default "0" and we're setting a template,
            // only update display if no specific number has been set yet
            if (currentNumber == "0")
            {
                UpdateButtonText(currentNumber);
            }
            else
            {
                // Update display with current number
                UpdateButtonText(currentNumber);
            }
        }
        else
        {
            if (buttonText != null)
                buttonText.text = template;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only allow editing if the template contains #
        if (!originalTemplate.Contains("#"))
            return;
            
        if (inputField != null && buttonText != null)
        {
            inputField.text = currentNumber;
            inputField.gameObject.SetActive(true);
            inputField.Select();
            inputField.ActivateInputField();
            buttonText.gameObject.SetActive(false);
        }
    }

    private void OnInputEndEdit(string newText)
    {
        // Validate the input is a valid number
        float result;
        if (!string.IsNullOrEmpty(newText) && float.TryParse(newText, out result))
        {
            currentNumber = newText;
        }
        else
        {
            // If invalid, keep the previous value
            Debug.Log("Invalid number input, keeping previous value: " + currentNumber);
        }
        
        UpdateButtonText(currentNumber);
        
        if (inputField != null)
            inputField.gameObject.SetActive(false);
        if (buttonText != null)
            buttonText.gameObject.SetActive(true);
    }    private void UpdateButtonText(string number)
    {
        if (buttonText != null)
        {
            // Always show just the number entered by the user
            buttonText.text = number;
        }
    }
    
    public string GetCurrentNumber()
    {
        return currentNumber;
    }
      public void SetCurrentNumber(string number)
    {
        float result;
        if (float.TryParse(number, out result))
        {
            currentNumber = number;
            // Always update the display when setting a number programmatically
            UpdateButtonText(currentNumber);
            Debug.Log($"[InlineNumberInput] Set number to {currentNumber}, display updated");
        }
        else
        {
            Debug.LogWarning($"[InlineNumberInput] Invalid number: {number}, keeping current value: {currentNumber}");
        }
    }
}
