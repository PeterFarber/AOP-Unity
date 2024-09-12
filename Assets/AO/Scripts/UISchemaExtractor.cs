using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class UISchema
{
    public string elementType;   // E.g., "Button", "InputField", "Dropdown"
    public string id;            // Unique ID for the UI element
    public string labelText;     // Text for Button, Label, or Dropdown options
    public Vector2 position;     // Position in the UI canvas
    public Vector2 size;         // Size of the UI element
    public Vector3 offsetFromEntity; // Offset from a 3D entity for world canvas
    public string fontName;      // Name of the font used
    public Color fontColor;      // Color of the text
    public float fontSize;         // Font size for text elements
    public Color backgroundColor; // Background color for buttons or input fields
    public Sprite backgroundImage; // Background image for buttons, dropdowns, etc.
    public List<string> options; // Dropdown options (for Dropdowns)
    public string onClickMessage; // Message to send to process on click
}

[System.Serializable]
public class EntityUISchema
{
    public List<UISchema> uiElements = new List<UISchema>();
}

public class UISchemaExtractor : MonoBehaviour
{
    public RectTransform canvas; // Reference to the Canvas where UI elements are created

    // Safe area definition for screen canvas (example values, customize as needed)
    public Rect safeArea = new Rect(10, 10, 1920 - 20, 1080 - 20); // Example safe area in screen units

    public Vector2 maxElementSize = new Vector2(500, 500); // Maximum size for any UI element

    // Extract schema from root UI object, supporting both world and screen canvas
    public EntityUISchema ExtractSchemaFromRoot(GameObject root, bool isWorldCanvas = false, Transform entityTransform = null)
    {
        EntityUISchema schema = new EntityUISchema();
        TraverseAndExtract(root.transform, schema, isWorldCanvas, entityTransform);
        return schema;
    }

    // Recursive function to traverse the hierarchy and extract UI components with validation
    private void TraverseAndExtract(Transform parent, EntityUISchema schema, bool isWorldCanvas, Transform entityTransform)
    {
        foreach (Transform child in parent)
        {
            RectTransform rect = child.GetComponent<RectTransform>();

            if (rect == null)
                continue; // Skip if no RectTransform

            // Check if the element scale is (1,1,1)
            if (rect.localScale != Vector3.one)
            {
                Debug.LogError($"UI element '{child.name}' does not have scale (1, 1, 1). Stopping extraction.");
                return; // Stop processing further elements
            }

            // Check if the element is too big
            if (rect.sizeDelta.x > maxElementSize.x || rect.sizeDelta.y > maxElementSize.y)
            {
                Debug.LogError($"UI element '{child.name}' exceeds the maximum allowed size {maxElementSize}. Please fix.");
                continue;
            }

            // Check screen canvas safe area if not world canvas
            if (!isWorldCanvas && !IsWithinSafeArea(rect))
            {
                Debug.LogError($"UI element '{child.name}' is outside the safe area. Please adjust its position.");
                continue;
            }

            UISchema elementSchema = new UISchema
            {
                id = child.name,
                position = rect.anchoredPosition,
                size = rect.sizeDelta
            };

            // If it's a world canvas, we need the offset from the associated 3D entity
            if (isWorldCanvas && entityTransform != null)
            {
                elementSchema.offsetFromEntity = child.position - entityTransform.position;
            }

            // Detect and handle different UI components and extract relevant info
            if (child.GetComponent<Button>() != null)
            {
                elementSchema.elementType = "Button";
                TMP_Text tmpText = child.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    elementSchema.labelText = tmpText.text;
                    elementSchema.fontName = tmpText.font.name;
                    elementSchema.fontSize = tmpText.fontSize;
                    elementSchema.fontColor = tmpText.color;
                }
                Image buttonBackground = child.GetComponent<Image>();
                if (buttonBackground != null)
                {
                    elementSchema.backgroundColor = buttonBackground.color;
                    elementSchema.backgroundImage = buttonBackground.sprite;
                }
                elementSchema.onClickMessage = $"Message for {elementSchema.id}";
                schema.uiElements.Add(elementSchema);
            }
            else if (child.GetComponent<TMP_InputField>() != null)
            {
                elementSchema.elementType = "InputField";
                TMP_InputField tmpInputField = child.GetComponent<TMP_InputField>();
                elementSchema.fontSize = tmpInputField.textComponent.fontSize;
                elementSchema.fontColor = tmpInputField.textComponent.color;
                elementSchema.fontName = tmpInputField.textComponent.font.name;
                Image inputBackground = child.GetComponent<Image>();
                if (inputBackground != null)
                {
                    elementSchema.backgroundColor = inputBackground.color;
                    elementSchema.backgroundImage = inputBackground.sprite;
                }
                schema.uiElements.Add(elementSchema);
            }
            else if (child.GetComponent<TMP_Dropdown>() != null)
            {
                elementSchema.elementType = "Dropdown";
                TMP_Dropdown tmpDropdown = child.GetComponent<TMP_Dropdown>();
                elementSchema.options = new List<string>();
                foreach (TMP_Dropdown.OptionData option in tmpDropdown.options)
                {
                    elementSchema.options.Add(option.text);
                }
                elementSchema.fontName = tmpDropdown.captionText.font.name;
                elementSchema.fontSize = tmpDropdown.captionText.fontSize;
                elementSchema.fontColor = tmpDropdown.captionText.color;
                Image dropdownBackground = child.GetComponent<Image>();
                if (dropdownBackground != null)
                {
                    elementSchema.backgroundColor = dropdownBackground.color;
                    elementSchema.backgroundImage = dropdownBackground.sprite;
                }
                schema.uiElements.Add(elementSchema);
            }
            else if (child.GetComponent<ScrollRect>() != null)
            {
                elementSchema.elementType = "ScrollView";
                Image scrollViewBackground = child.GetComponent<Image>();
                if (scrollViewBackground != null)
                {
                    elementSchema.backgroundColor = scrollViewBackground.color;
                    elementSchema.backgroundImage = scrollViewBackground.sprite;
                }
                schema.uiElements.Add(elementSchema);
            }

            // Recursively process the child objects
            if (child.childCount > 0)
            {
                TraverseAndExtract(child, schema, isWorldCanvas, entityTransform);
            }
        }
    }

    // Check if the RectTransform is within the safe area for screen canvas
    private bool IsWithinSafeArea(RectTransform rect)
    {
        Vector2 min = rect.anchoredPosition - (rect.sizeDelta / 2);
        Vector2 max = rect.anchoredPosition + (rect.sizeDelta / 2);

        // Check if the element is within the safe area bounds
        return min.x >= safeArea.xMin && max.x <= safeArea.xMax && min.y >= safeArea.yMin && max.y <= safeArea.yMax;
    }

    // Export the schema to a format that can be sent to the on-chain process
    public EntityUISchema ExportUISchema(GameObject rootUI, bool isWorldCanvas = false, Transform entityTransform = null)
    {
        return ExtractSchemaFromRoot(rootUI, isWorldCanvas, entityTransform);
    }
}