using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DeterministicGenerateWindow : EditorWindow
{
    private WeaponPart weapon;
    private SaveSystem saveSystem;

    private int selectedCategoryIndex = -1; // Tracks selected category
    private Vector2 partsScrollPosition; // Scroll position for part list
    private Vector2 selectedPartsScrollPosition; // Scroll position for selected parts list
    private List<(int categoryIndex, int partIndex)> selectedParts = new List<(int categoryIndex, int partIndex)>();

    private GameObject selectedPartPrefab; // Stores the currently selected part prefab for preview
    private int previewPartIndex = -1; // Tracks the part that was clicked for larger preview

    public static void Open(WeaponPart weaponPart)
    {
        DeterministicGenerateWindow window = GetWindow<DeterministicGenerateWindow>("Deterministic Generate");
        window.weapon = weaponPart;
        window.saveSystem = Object.FindObjectOfType<SaveSystem>();
        window.Show();
    }

    private void OnGUI()
    {
        if (weapon == null || saveSystem == null)
        {
            EditorGUILayout.HelpBox("Weapon or SaveSystem not found!", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Deterministic Generation", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Step 1: Category Selection
        EditorGUILayout.LabelField("Step 1: Select a Category", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        // Dynamically adjust the button width based on window size
        float buttonWidth = position.width / saveSystem.weaponCategories.Length - 10;
        for (int i = 0; i < saveSystem.weaponCategories.Length; i++)
        {
            if (GUILayout.Button(saveSystem.weaponCategories[i].name, GUILayout.Height(40), GUILayout.Width(buttonWidth)))
            {
                selectedCategoryIndex = i;
                selectedPartPrefab = null; // Reset preview when category changes
                previewPartIndex = -1; // Reset preview part when category changes
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // Part Selection (displaying items vertically in a scroll view)
        if (selectedCategoryIndex >= 0)
        {
            WeaponPartSO selectedCategory = saveSystem.weaponCategories[selectedCategoryIndex];
            EditorGUILayout.LabelField($"Parts in Category: {selectedCategory.name}", EditorStyles.boldLabel);

            // Make the height of the scroll view dynamic (it increases as window grows)
            partsScrollPosition = EditorGUILayout.BeginScrollView(partsScrollPosition, GUILayout.Height(position.height * 0.4f)); // Dynamic height based on window size
            if (selectedCategory.weaponPartArray != null && selectedCategory.weaponPartArray.Length > 0)
            {
                // Loop through the array and display parts in rows
                for (int j = 0; j < selectedCategory.weaponPartArray.Length; j++)
                {
                    DisplayPartPreview(selectedCategory.weaponPartArray, j);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No parts available in this category.", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        // Step 2: Selected Parts
        EditorGUILayout.LabelField("Step 2: Selected Parts", EditorStyles.boldLabel);

        selectedPartsScrollPosition = EditorGUILayout.BeginScrollView(selectedPartsScrollPosition, GUILayout.Height(position.height * 0.3f)); // Dynamic height based on window size
        int? partToRemove = null;

        for (int i = 0; i < selectedParts.Count; i++)
        {
            var (categoryIndex, partIndex) = selectedParts[i];
            string categoryName = saveSystem.weaponCategories[categoryIndex].name;
            string partName = saveSystem.weaponCategories[categoryIndex].weaponPartArray[partIndex].name;

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"{i + 1}. {categoryName} - {partName}");

            if (GUILayout.Button("❌", GUILayout.Width(80)))
            {
                partToRemove = i;
            }

            // Move Up Button
            GUI.enabled = i > 0;
            if (GUILayout.Button("↑", GUILayout.Width(50)) && i > 0)
            {
                (selectedParts[i], selectedParts[i - 1]) = (selectedParts[i - 1], selectedParts[i]);
            }
            GUI.enabled = true;

            // Move Down Button
            GUI.enabled = i < selectedParts.Count - 1;
            if (GUILayout.Button("↓", GUILayout.Width(50)) && i < selectedParts.Count - 1)
            {
                (selectedParts[i], selectedParts[i + 1]) = (selectedParts[i + 1], selectedParts[i]);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        if (partToRemove.HasValue)
        {
            selectedParts.RemoveAt(partToRemove.Value);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Generate Button
        if (GUILayout.Button("Generate", GUILayout.Height(40)))
        {
            if (selectedParts.Count > 0)
            {
                weapon.DeterministicGenerate(selectedParts);
                Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select at least one part.", "OK");
            }
        }

        // Cancel Button
        if (GUILayout.Button("Cancel", GUILayout.Height(20)))
        {
            Close();
        }

        // Show the larger preview if a part was selected
        if (previewPartIndex >= 0)
        {
            ShowLargePreview(selectedCategoryIndex, previewPartIndex);
        }
    }

    // Helper method to display part preview with name and button
    private void DisplayPartPreview(GameObject[] weaponPartArray, int partIndex)
    {
        float partHeight = 60f; // Fixed height for each part item
        EditorGUILayout.BeginHorizontal(GUILayout.Height(partHeight));

        // Display part name closer to the preview
        GUILayout.Label(weaponPartArray[partIndex].name, GUILayout.Width(150f)); // Adjust name width as needed

        // Display preview thumbnail
        Texture2D previewTexture = AssetPreview.GetAssetPreview(weaponPartArray[partIndex]);
        if (previewTexture != null)
        {
            if (GUILayout.Button(previewTexture, GUILayout.Width(50f), GUILayout.Height(50f)))
            {
                previewPartIndex = partIndex; // Set the selected part index for the large preview
                ShowLargePreviewWindow(weaponPartArray[partIndex]); // Show preview when clicked
            }
        }
        else
        {
            GUILayout.Label("No Preview", GUILayout.Width(50f), GUILayout.Height(50f));
        }

        // Display "+" button aligned to the right
        GUILayout.FlexibleSpace(); // Push the "+" button to the right
        if (GUILayout.Button("+", GUILayout.Width(40f)))
        {
            selectedParts.Add((selectedCategoryIndex, partIndex));
        }

        EditorGUILayout.EndHorizontal();
    }

    // Method to show the larger preview of the selected part
    private void ShowLargePreview(int categoryIndex, int partIndex)
    {
        WeaponPartSO selectedCategory = saveSystem.weaponCategories[categoryIndex];
        GameObject selectedPart = selectedCategory.weaponPartArray[partIndex];

        // Open a new window or display a larger preview inside the current window
        LargePreviewWindow window = EditorWindow.GetWindow<LargePreviewWindow>("Large Preview", true);
        window.SetPreview(selectedPart);
    }

    // Show the large preview window only when clicking on a part
    private void ShowLargePreviewWindow(GameObject part)
    {
        LargePreviewWindow window = EditorWindow.GetWindow<LargePreviewWindow>("Large Preview", true);
        window.SetPreview(part);
    }
}

public class LargePreviewWindow : EditorWindow
{
    private GameObject partToPreview;
    private Texture2D previewTexture;

    // Method to set the preview for the larger window
    public void SetPreview(GameObject part)
    {
        partToPreview = part;
        previewTexture = AssetPreview.GetAssetPreview(part);
    }

    private void OnGUI()
    {
        if (partToPreview != null && previewTexture != null)
        {
            // Maintain aspect ratio while scaling
            float aspectRatio = (float)previewTexture.width / previewTexture.height;
            float windowWidth = position.width - 20;
            float windowHeight = position.height - 20;

            float scaledWidth, scaledHeight;
            if (windowWidth / windowHeight > aspectRatio)
            {
                scaledHeight = windowHeight;
                scaledWidth = scaledHeight * aspectRatio;
            }
            else
            {
                scaledWidth = windowWidth;
                scaledHeight = scaledWidth / aspectRatio;
            }

            Rect imageRect = new Rect((position.width - scaledWidth) / 2, 10, scaledWidth, scaledHeight);
            GUI.DrawTexture(imageRect, previewTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            GUILayout.Label("No Preview Available");
        }
    }
}
