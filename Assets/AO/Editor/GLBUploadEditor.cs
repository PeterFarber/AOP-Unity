using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

public class GLBUploadEditor : EditorWindow
{
    private GameObject selectedGLBModel; 
    private string modelName = "Model Name";
    private string category = "Category";
    private string subcategory = "Subcategory";
    private string selectedImagePath; 
    private List<Texture2D> capturedFrames = new List<Texture2D>(); 
    private int selectedFrameIndex = 0; 
    private bool isFrameSelected = false;
    private Color previewBackgroundColor = Color.gray; 
    private List<Category> categories; 
    private int selectedCategoryIndex = 0; 
    private int selectedSubcategoryIndex = 0; 
    private float modelSizeMB;
    private int vertexCount;
    private Vector2 scrollPos; // For scroll position

    [MenuItem("AO/GLB Model Uploader")]
    public static void ShowWindow()
    {
        var window = GetWindow<GLBUploadEditor>("GLB Model Uploader");

        Texture2D icon = (Texture2D)EditorGUIUtility.Load("Assets/AO/Editor/Icons/AO.png");
        window.titleContent = new GUIContent("GLB Model Uploader", icon);
    }

    private void OnEnable()
    {
        LoadCategoriesFromJSON();
    }

    private void OnGUI()
    {
        GUILayout.Label("Upload GLB Model", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Model Name:", GUILayout.Width(120));  // Adjusting label size
        modelName = EditorGUILayout.TextField(modelName);
        EditorGUILayout.EndHorizontal();

        // Category Dropdown
        if (categories != null && categories.Count > 0)
        {
            string[] categoryNames = new string[categories.Count + 1]; 
            categoryNames[0] = "All";
            for (int i = 0; i < categories.Count; i++)
            {
                categoryNames[i + 1] = categories[i].name;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Category:", GUILayout.Width(120));  // Adjusting label size
            selectedCategoryIndex = EditorGUILayout.Popup(selectedCategoryIndex, categoryNames);
            EditorGUILayout.EndHorizontal();
            category = categoryNames[selectedCategoryIndex];

            // Subcategory Dropdown
            if (selectedCategoryIndex > 0 && categories[selectedCategoryIndex - 1].subcategories.Count > 0)
            {
                string[] subcategoryNames = categories[selectedCategoryIndex - 1].subcategories.ToArray();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Subcategory:", GUILayout.Width(120));  // Adjusting label size
                selectedSubcategoryIndex = EditorGUILayout.Popup(selectedSubcategoryIndex, subcategoryNames);
                EditorGUILayout.EndHorizontal();
                subcategory = subcategoryNames[selectedSubcategoryIndex];
            }
        }

        GUILayout.Space(10);

        // Model reference label and object field on the same line
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Model Reference:", GUILayout.Width(120));
        selectedGLBModel = (GameObject)EditorGUILayout.ObjectField(selectedGLBModel, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

        if (selectedGLBModel != null)
        {
            // Calculate model size and vertex count
            CalculateModelInfo(selectedGLBModel);

            // Show selected model info in a single line
            GUILayout.Label($"Model Size: {modelSizeMB:F2} MB | Vertex Count: {vertexCount}", GUILayout.Width(300));
        }

        if (selectedGLBModel != null)
        {
            GUILayout.Space(10);

            // Color Picker for background on the same line as the label
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview Background Color:", GUILayout.Width(160));
            previewBackgroundColor = EditorGUILayout.ColorField(previewBackgroundColor);
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);

        if (selectedGLBModel != null && GUILayout.Button("Capture Preview Frames"))
        {
            LoadAndCaptureGLB();
        }

        // Horizontal ScrollView for frames
        if (capturedFrames.Count > 0)
        {
            GUILayout.Label("Select a Frame for Preview:", EditorStyles.boldLabel);
            
            // Restrict the scroll view to horizontal only
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Height(110), GUILayout.ExpandHeight(false));

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < capturedFrames.Count; i++)
            {
                GUIStyle frameStyle = new GUIStyle(GUI.skin.button);
                frameStyle.margin = new RectOffset(4, 4, 4, 4);
                
                // Highlight the selected frame with a thicker border and distinct color
                if (i == selectedFrameIndex)
                {
                    frameStyle.normal.background = MakeTex(4, 4, Color.yellow); // Thicker yellow border
                }

                if (GUILayout.Button(capturedFrames[i], frameStyle, GUILayout.Width(80), GUILayout.Height(80)))
                {
                    selectedFrameIndex = i;
                    selectedImagePath = SaveSelectedFrame(capturedFrames[i]);
                    isFrameSelected = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            // Display the selected frame
            if (selectedFrameIndex >= 0)
            {
                GUILayout.Label("Selected Preview:");
                GUILayout.Label(capturedFrames[selectedFrameIndex], GUILayout.Width(256), GUILayout.Height(256));
            }
        }

        if (isFrameSelected)
        {
            if (GUILayout.Button("Upload Selected Frame and Metadata"))
            {
                UploadToAPI(modelName, category, subcategory, selectedImagePath);
            }
        }
    }

    private void LoadCategoriesFromJSON()
    {
        var jsonFile = Resources.Load<TextAsset>("categories");
        if (jsonFile != null)
        {
            var data = JsonUtility.FromJson<CategoryList>(jsonFile.text);
            categories = data.categories;
        }
        else
        {
            Debug.LogError("Category JSON file not found!");
        }
    }

    private void LoadAndCaptureGLB()
    {
        if (selectedGLBModel != null)
        {
            string modelPath = AssetDatabase.GetAssetPath(selectedGLBModel);
            string glbFileName = Path.GetFileNameWithoutExtension(modelPath);

            Camera previewCamera = CreatePreviewCamera(previewBackgroundColor); 
            GameObject model = Instantiate(selectedGLBModel);
            PositionCameraToFitModel(previewCamera, model);

            EditorCoroutineUtility.StartCoroutineOwnerless(CaptureFrames(previewCamera, model));
        }
        else
        {
            Debug.LogError("No GLB model selected!");
        }
    }

    private Camera CreatePreviewCamera(Color backgroundColor)
    {
        Camera previewCamera = new GameObject("PreviewCamera").AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor; 
        previewCamera.backgroundColor = backgroundColor; 

        previewCamera.cullingMask = 1 << 8; 

        return previewCamera;
    }

    private void PositionCameraToFitModel(Camera camera, GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            float distance = bounds.size.magnitude * 2f;
            camera.transform.position = bounds.center - camera.transform.forward * distance;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = distance * 4f;

            model.layer = 8;
            foreach (Transform child in model.transform)
            {
                child.gameObject.layer = 8;
            }
        }
        else
        {
            Debug.LogWarning("No renderers found in the model.");
        }
    }

    private IEnumerator CaptureFrames(Camera camera, GameObject model)
    {
        capturedFrames.Clear(); 
        
        for (int i = 0; i < 36; i++)
        {
            model.transform.Rotate(0, 10, 0);
            RenderTexture renderTexture = new RenderTexture(256, 256, 24);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            Texture2D frame = new Texture2D(256, 256, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            frame.Apply();
            capturedFrames.Add(frame);

            camera.targetTexture = null;
            RenderTexture.active = null;
            renderTexture.Release();

            yield return null;
        }

        DestroyImmediate(model);
        DestroyImmediate(camera.gameObject);
    }

    private string SaveSelectedFrame(Texture2D selectedFrame)
    {
        string previewFilePath = Path.Combine(Application.dataPath, $"{modelName}_Preview.png");
        byte[] pngData = selectedFrame.EncodeToPNG();
        File.WriteAllBytes(previewFilePath, pngData);
        AssetDatabase.Refresh();
        Debug.Log("Preview image saved at: " + previewFilePath);
        return previewFilePath;
    }

    private void UploadToAPI(string modelName, string category, string subcategory, string previewFilePath)
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(UploadGLB(modelName, category, subcategory, previewFilePath));
    }

    private IEnumerator UploadGLB(string modelName, string category, string subcategory, string previewFilePath)
    {
        var metadata = new
        {
            modelName = modelName,
            category = category,
            subcategory = subcategory
        };

        string metadataJson = JsonUtility.ToJson(metadata);
        Debug.Log($"Uploading GLB and Preview: {metadataJson}");

        yield return null;
    }

    private void CalculateModelInfo(GameObject model)
    {
        string path = AssetDatabase.GetAssetPath(model);
        FileInfo fileInfo = new FileInfo(path);
        modelSizeMB = (float)fileInfo.Length / (1024 * 1024);

        MeshFilter[] meshFilters = model.GetComponentsInChildren<MeshFilter>();
        vertexCount = 0;
        foreach (var meshFilter in meshFilters)
        {
            vertexCount += meshFilter.sharedMesh.vertexCount;
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}