using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

public class AssetBrowser : EditorWindow
{
    private static string downloadFolderPath = "Assets/DownloadedAssets"; // Global variable for download path
    private string metadataFilePath = Path.Combine(downloadFolderPath, "downloaded_assets_meta.txt");

    private int currentPage = 1;
    private int maxPages = 10;
    private string selectedMainCategory = "All";
    private string selectedSubCategory = "All";
    private bool showOnlyDownloaded = false; // Toggle state for filtering downloaded files
    private List<Asset> assets = new List<Asset>();
    private List<Category> categories;
    private List<Asset> downloadedAssets = new List<Asset>(); // List of downloaded assets
    private VisualElement categoryContainer;
    private VisualElement subCategoryContainer;
    private ScrollView scrollView;
    private TextField searchField;
    private Toggle downloadedToggle;
    private VisualElement selectedCategoryButton;
    private VisualElement selectedSubCategoryButton;
    private Label noDownloadedAssetsLabel;

    private Color defaultColor = new Color(0.3f, 0.3f, 0.3f); // Darker default background color for readability
    private Color selectedColor = new Color(0.15f, 0.15f, 0.15f); // Darker selected background color

    [MenuItem("AO/AO Asset Browser")]
    public static void ShowWindow()
    {
        var window = GetWindow<AssetBrowser>("AO Asset Browser");
        window.minSize = new Vector2(800, 300); // Setting the minimum window size to 790px

        // Load the icon from the Resources or any path within your Assets
        Texture2D icon = (Texture2D)EditorGUIUtility.Load("Assets/AO/Editor/Icons/AO.png");

        // Set the title content with the icon
        window.titleContent = new GUIContent("Asset Browser", icon);
    }

    private void CreateGUI()
    {
        // Apply global padding to the root element
        var root = this.rootVisualElement;
        root.style.paddingLeft = 20;
        root.style.paddingRight = 20;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;

        // Load Categories from JSON
        LoadCategoriesFromJSON();

        // Load downloaded assets metadata into a list for performance
        LoadDownloadedAssetsMetadata();

        // Title Label for Description
        var titleLabel = new Label("Use this window to browse, search, manage and load 3D assets.");
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.marginBottom = 10;

        // Search Bar
        var searchBarContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 10 } };
        searchField = new TextField() { style = { flexGrow = 1, marginRight = 10 } }; // Adds right margin for spacing
        var searchButton = new Button(() => RefreshSearch()) { text = "Search", style = { paddingLeft = 10, paddingRight = 10 } };
        searchBarContainer.Add(searchField);
        searchBarContainer.Add(searchButton);

        // Toggle for "Only Downloaded"
        downloadedToggle = new Toggle { text = " Only Downloaded" }; // Added space before "Only Downloaded"
        downloadedToggle.RegisterValueChangedCallback(evt =>
        {
            showOnlyDownloaded = evt.newValue;
            RefreshSearch(); // Refresh search when toggled
        });

        // Ensure toggle is set to false by default and showOnlyDownloaded follows the same state
        showOnlyDownloaded = false;
        downloadedToggle.value = false; // Set the toggle's default value to false

        searchBarContainer.Add(downloadedToggle);

        // Container for Categories
        categoryContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10 } };

        // Add "All" Category by default
        Button allCategoryButton = new Button(() => SelectCategory("All"))
        {
            text = "All",
            style = {
                paddingLeft = 15, paddingRight = 15,
                marginRight = 5,
                backgroundColor = selectedColor // Set selected by default
            }
        };
        categoryContainer.Add(allCategoryButton);
        selectedCategoryButton = allCategoryButton;  // "All" is selected by default

        // Add other categories from JSON to the category container
        foreach (var category in categories)
        {
            Button categoryButton = new Button(() => SelectCategory(category.name))
            {
                text = category.name,
                style = {
                    paddingLeft = 15, paddingRight = 15,
                    marginLeft = 5, marginRight = 5,
                    backgroundColor = defaultColor
                }
            };

            categoryContainer.Add(categoryButton);
        }

        // Container for Subcategories
        subCategoryContainer = new VisualElement
        {
            style = { flexDirection = FlexDirection.Row, marginTop = 10 }
        };

        // ScrollView to display assets
        scrollView = new ScrollView();
        scrollView.style.flexGrow = 1;
        scrollView.style.marginTop = 20;
        scrollView.style.marginBottom = 10;
        scrollView.style.overflow = Overflow.Hidden; // Hide overflow for better layout
        scrollView.style.marginLeft = 5;
        scrollView.style.marginRight = 5;

        // Create "No downloaded assets found" label and hide it initially
        noDownloadedAssetsLabel = new Label("No downloaded assets found.");
        noDownloadedAssetsLabel.style.display = DisplayStyle.None; // Hidden by default
        scrollView.Add(noDownloadedAssetsLabel); // Add it to the scrollView

        // Pagination controls
        var paginationContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, marginTop = 10, paddingLeft = 15, paddingRight = 15 } };
        var previousButton = new Button(() => ChangePage(-1)) { text = "Previous", style = { marginRight = 5 } };
        var paginationLabel = new Label($"Page {currentPage} of {maxPages}")
        {
            style = { unityTextAlign = TextAnchor.MiddleCenter, flexGrow = 0 }
        };
        var nextButton = new Button(() => ChangePage(1)) { text = "Next", style = { marginLeft = 5 } };

        var paginationWrapper = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center } };
        paginationWrapper.Add(previousButton);
        paginationWrapper.Add(paginationLabel);
        paginationWrapper.Add(nextButton);

        paginationContainer.Add(paginationWrapper);

        // Layout elements in the window
        root.Add(titleLabel);           // Title label for window description
        root.Add(searchBarContainer);   // Search bar and search button
        root.Add(downloadedToggle);     // Toggle for downloaded files (new line)
        root.Add(categoryContainer);    // Category container
        root.Add(subCategoryContainer); // Subcategory container (dynamically updated)
        root.Add(scrollView);           // Asset list
        root.Add(paginationContainer);  // Pagination controls

        SelectCategory("All"); // Select "All" category by default
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

    private void LoadDownloadedAssetsMetadata()
    {
        downloadedAssets.Clear();
        if (File.Exists(metadataFilePath))
        {
            var lines = File.ReadAllLines(metadataFilePath);
            foreach (var line in lines)
            {
                var parts = line.Split('|');
                if (parts.Length >= 4)
                {
                    downloadedAssets.Add(new Asset
                    {
                        id = parts[0],
                        name = parts[1],
                        thumbnailUrl = parts[2],
                        tag = parts[3],
                        subtag = parts.Length > 4 ? parts[4] : null
                    });
                }
            }
        }
    }

    private void SelectCategory(string category)
    {
        selectedMainCategory = category;

        // Change background color for selected category
        if (selectedCategoryButton != null)
        {
            selectedCategoryButton.style.backgroundColor = defaultColor; // Reset previous button
        }

        foreach (VisualElement child in categoryContainer.Children())
        {
            var button = child as Button;
            if (button != null && button.text == category)
            {
                button.style.backgroundColor = selectedColor; // Set selected button's background
                selectedCategoryButton = button;
            }
        }

        // Update subcategories only if a category other than "All" is selected
        subCategoryContainer.Clear();
        if (category != "All")
        {
            subCategoryContainer.style.display = DisplayStyle.Flex; // Show subcategory container

            // Add "All" subcategory
            Button allSubCategoryButton = new Button(() => SelectSubCategory("All"))
            {
                text = "All",
                style = {
                paddingLeft = 15, paddingRight = 15,
                marginRight = 5,
                backgroundColor = selectedColor // Pre-selected subcategory
            }
            };
            subCategoryContainer.Add(allSubCategoryButton);
            selectedSubCategoryButton = allSubCategoryButton;

            // Add other subcategories
            var subcategories = categories.Find(c => c.name == selectedMainCategory)?.subcategories ?? new List<string>();

            foreach (var subcategory in subcategories)
            {
                Button subCategoryButton = new Button(() => SelectSubCategory(subcategory))
                {
                    text = subcategory,
                    style = {
                    paddingLeft = 15, paddingRight = 15,
                    marginLeft = 5, marginRight = 5,
                    backgroundColor = defaultColor
                }
                };

                subCategoryContainer.Add(subCategoryButton);
            }
        }
        else
        {
            subCategoryContainer.style.display = DisplayStyle.None; // Hide subcategory container for "All" category
        }

        RefreshSearch();  // Refresh the asset list based on the selected category
    }

    private void SelectSubCategory(string subcategory)
    {
        selectedSubCategory = subcategory;

        // Change background color for selected subcategory
        if (selectedSubCategoryButton != null)
        {
            selectedSubCategoryButton.style.backgroundColor = defaultColor; // Reset previous button
        }

        foreach (VisualElement child in subCategoryContainer.Children())
        {
            var button = child as Button;
            if (button != null && button.text == subcategory)
            {
                button.style.backgroundColor = selectedColor; // Set selected button's background
                selectedSubCategoryButton = button;
            }
        }

        RefreshSearch();  // Refresh the asset list based on the selected subcategory
    }

    private void RefreshSearch()
    {
        // Clear the scrollView content except for the "No downloaded assets" label
        foreach (var child in scrollView.Children())
        {
            if (child != noDownloadedAssetsLabel)
            {
                scrollView.Remove(child);
            }
        }

        // Combine search text, main category, and subcategory to refresh the asset list
        List<Asset> filteredAssets;

        if (showOnlyDownloaded)
        {
            // If "Only Downloaded" is toggled, query the parsed downloaded assets list
            filteredAssets = QueryDownloadedAssets(searchField.value.ToLower());

            if (filteredAssets.Count == 0)
            {
                Debug.Log("No downloaded assets found, displaying label.");
                noDownloadedAssetsLabel.style.display = DisplayStyle.Flex; // Show label
            }
            else
            {
                Debug.Log("Downloaded assets found, hiding label.");
                noDownloadedAssetsLabel.style.display = DisplayStyle.None; // Hide label
                UpdateAssetsList(filteredAssets);
            }
        }
        else
        {
            // Normal filtering for assets from online or non-downloaded
            filteredAssets = assets.FindAll(asset =>
                (selectedMainCategory == "All" || asset.tag == selectedMainCategory) &&
                (selectedSubCategory == "All" || asset.subtag == selectedSubCategory) &&
                asset.name.ToLower().Contains(searchField.value.ToLower()));

            Debug.Log("Searching non-downloaded assets.");
            noDownloadedAssetsLabel.style.display = DisplayStyle.None; // Hide label when not filtering by downloads

            if (filteredAssets.Count > 0)
            {
                UpdateAssetsList(filteredAssets);
            }
        }

        // If assets are found, update the list
        if (filteredAssets.Count > 0)
        {
            UpdateAssetsList(filteredAssets);
        }
    }

    private List<Asset> QueryDownloadedAssets(string searchTerm)
    {
        var downloadedMatches = new List<Asset>();
        foreach (var asset in downloadedAssets)
        {
            if (asset.name.ToLower().Contains(searchTerm))
            {
                downloadedMatches.Add(asset);
            }
        }

        return downloadedMatches;
    }

    private void ChangePage(int direction)
    {
        currentPage = Mathf.Clamp(currentPage + direction, 1, maxPages);
        RefreshSearch();
    }

    private void UpdateAssetsList(List<Asset> filteredAssets)
    {
        scrollView.Clear();

        foreach (var asset in filteredAssets)
        {
            var assetCard = new VisualElement();
            var thumbnail = new Image { image = LoadThumbnail(asset.thumbnailUrl) };
            var nameLabel = new Label(asset.name);

            // Action buttons (Download, Load, and Delete for downloaded assets)
            var actionButton = new Button();
            var deleteButton = new Button();
            if (IsAssetDownloaded(asset.id))
            {
                actionButton.text = "Load";
                actionButton.clicked += () => LoadAsset(asset.id);
                deleteButton.text = "Delete";
                deleteButton.clicked += () => DeleteAsset(asset.id);
            }
            else
            {
                actionButton.text = "Download";
                actionButton.clicked += () => DownloadAsset(asset.id);
            }

            assetCard.Add(thumbnail);
            assetCard.Add(nameLabel);
            assetCard.Add(actionButton);
            if (IsAssetDownloaded(asset.id))
            {
                assetCard.Add(deleteButton); // Add delete button if the asset is downloaded
            }

            scrollView.Add(assetCard);
        }
    }

    private bool IsAssetDownloaded(string assetId)
    {
        foreach (var asset in downloadedAssets)
        {
            if (asset.id == assetId)
            {
                return true;
            }
        }
        return false;
    }

    private Texture2D LoadThumbnail(string url)
    {
        // Implement logic to load thumbnail from URL
        return null;
    }

    private void DownloadAsset(string assetId)
    {
        // Simulate downloading the asset and update the metadata
        string assetFile = Path.Combine(downloadFolderPath, assetId + ".glb");
        if (!Directory.Exists(downloadFolderPath))
        {
            Directory.CreateDirectory(downloadFolderPath);
        }
        File.WriteAllText(assetFile, "Simulated GLB content for " + assetId);

        // Update the metadata file with asset info (storing all fields)
        string thumbnailUrl = "https://example.com/thumbnail.png"; // Placeholder, assume you get this from the API
        string tag = selectedMainCategory;
        string subtag = selectedSubCategory ?? "null"; // Some can be null
        File.AppendAllText(metadataFilePath, $"{assetId}|{assetId}|{thumbnailUrl}|{tag}|{subtag}\n");

        // Update the downloaded assets list
        downloadedAssets.Add(new Asset { id = assetId, name = assetId, thumbnailUrl = thumbnailUrl, tag = tag, subtag = subtag });

        RefreshSearch(); // Refresh search after downloading
    }

    private void DeleteAsset(string assetId)
    {
        // Delete the asset file and update the metadata
        string assetFile = Path.Combine(downloadFolderPath, assetId + ".glb");
        if (File.Exists(assetFile))
        {
            File.Delete(assetFile);
        }

        // Remove the asset from the metadata file
        var lines = new List<string>(File.ReadAllLines(metadataFilePath));
        lines.RemoveAll(line => line.StartsWith(assetId + "|"));
        File.WriteAllLines(metadataFilePath, lines.ToArray());

        // Remove the asset from the downloaded assets list
        downloadedAssets.RemoveAll(asset => asset.id == assetId);

        RefreshSearch(); // Refresh search after deletion
    }

    private void LoadAsset(string assetId)
    {
        Debug.Log($"Loading asset {assetId} into the scene...");
    }

    // Asset structure with all possible fields
    private class Asset
    {
        public string id;
        public string name;
        public string thumbnailUrl;
        public string tag;
        public string subtag;
    }
}

    // Category structure for loading from JSON
    [System.Serializable]
    public class Category
    {
        public string name;
        public List<string> subcategories;
    }

    [System.Serializable]
    public class CategoryList
    {
        public List<Category> categories;
    }