using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIRenderer : MonoBehaviour
{
    public RectTransform canvas; // Reference to the Canvas where UI elements will be rendered
    public Transform entityTransform; // Entity's 3D position for world canvases

    // Method to render UI from schema data
    public void RenderUI(EntityUISchema uiSchema, bool isWorldCanvas = false)
    {
        foreach (UISchema schema in uiSchema.uiElements)
        {
            switch (schema.elementType)
            {
                case "Button":
                    CreateButtonFromSchema(schema, isWorldCanvas);
                    break;
                case "InputField":
                    CreateInputFieldFromSchema(schema, isWorldCanvas);
                    break;
                case "Dropdown":
                    CreateDropdownFromSchema(schema, isWorldCanvas);
                    break;
                case "ScrollView":
                    CreateScrollViewFromSchema(schema, isWorldCanvas);
                    break;
                // Add more UI element types as needed
            }
        }
    }

    // Set UI element position and size based on schema, handling world vs. screen canvas
    private void SetUIElementPosition(GameObject element, UISchema schema, bool isWorldCanvas)
    {
        RectTransform rect = element.GetComponent<RectTransform>();
        if (isWorldCanvas)
        {
            // For world canvas, use world space offset
            rect.position = entityTransform.position + schema.offsetFromEntity;
        }
        else
        {
            // For screen canvas, set anchored position and size
            rect.anchoredPosition = schema.position;
        }
        rect.sizeDelta = schema.size;
    }

    // Create a Button from schema with graphical and text properties
    private void CreateButtonFromSchema(UISchema schema, bool isWorldCanvas)
    {
        GameObject buttonObject = new GameObject(schema.id);
        buttonObject.AddComponent<RectTransform>().SetParent(canvas, false);
        Button button = buttonObject.AddComponent<Button>();

        // Set Button position, size, and background
        SetUIElementPosition(buttonObject, schema, isWorldCanvas);
        if (schema.backgroundImage != null)
        {
            Image backgroundImage = buttonObject.AddComponent<Image>();
            backgroundImage.sprite = schema.backgroundImage;
            backgroundImage.color = schema.backgroundColor;
        }

        // Add TMP text for button label
        TextMeshProUGUI buttonText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        buttonText.transform.SetParent(buttonObject.transform);
        buttonText.text = schema.labelText;
        buttonText.fontSize = schema.fontSize;
        buttonText.color = schema.fontColor;
        buttonText.font = Resources.GetBuiltinResource<TMP_FontAsset>(schema.fontName);

        // Add onClick event to send message to AO process
        button.onClick.AddListener(() =>
        {
            SendMessageToProcess(schema.onClickMessage);
        });
    }

    // Create a TMP_InputField from schema
    private void CreateInputFieldFromSchema(UISchema schema, bool isWorldCanvas)
    {
        GameObject inputObject = new GameObject(schema.id);
        inputObject.AddComponent<RectTransform>().SetParent(canvas, false);
        TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();

        // Set InputField position, size, and background
        SetUIElementPosition(inputObject, schema, isWorldCanvas);
        if (schema.backgroundImage != null)
        {
            Image backgroundImage = inputObject.AddComponent<Image>();
            backgroundImage.sprite = schema.backgroundImage;
            backgroundImage.color = schema.backgroundColor;
        }

        // Set InputField text properties
        TMP_Text textComponent = inputObject.AddComponent<TMP_Text>();
        textComponent.fontSize = schema.fontSize;
        textComponent.color = schema.fontColor;
        textComponent.font = Resources.GetBuiltinResource<TMP_FontAsset>(schema.fontName);
    }

    // Create a TMP_Dropdown from schema
    private void CreateDropdownFromSchema(UISchema schema, bool isWorldCanvas)
    {
        GameObject dropdownObject = new GameObject(schema.id);
        dropdownObject.AddComponent<RectTransform>().SetParent(canvas, false);
        TMP_Dropdown dropdown = dropdownObject.AddComponent<TMP_Dropdown>();

        // Set Dropdown position, size, and background
        SetUIElementPosition(dropdownObject, schema, isWorldCanvas);
        if (schema.backgroundImage != null)
        {
            Image backgroundImage = dropdownObject.AddComponent<Image>();
            backgroundImage.sprite = schema.backgroundImage;
            backgroundImage.color = schema.backgroundColor;
        }

        // Set Dropdown text properties
        TMP_Text textComponent = dropdown.captionText;
        textComponent.fontSize = schema.fontSize;
        textComponent.color = schema.fontColor;
        textComponent.font = Resources.GetBuiltinResource<TMP_FontAsset>(schema.fontName);

        // Populate dropdown options
        foreach (string option in schema.options)
        {
            TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(option);
            dropdown.options.Add(optionData);
        }
    }

    // Create a ScrollView from schema
    private void CreateScrollViewFromSchema(UISchema schema, bool isWorldCanvas)
    {
        GameObject scrollViewObject = new GameObject(schema.id);
        scrollViewObject.AddComponent<RectTransform>().SetParent(canvas, false);
        ScrollRect scrollView = scrollViewObject.AddComponent<ScrollRect>();

        // Set ScrollView position, size, and background
        SetUIElementPosition(scrollViewObject, schema, isWorldCanvas);
        if (schema.backgroundImage != null)
        {
            Image backgroundImage = scrollViewObject.AddComponent<Image>();
            backgroundImage.sprite = schema.backgroundImage;
            backgroundImage.color = schema.backgroundColor;
        }
    }

    // Example method to simulate sending a message to the AO process
    private void SendMessageToProcess(string message)
    {
        // Replace with actual logic to send a message to the AO process
        Debug.Log($"Message sent to process: {message}");
    }
}