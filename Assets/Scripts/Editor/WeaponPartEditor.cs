using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(WeaponPart), true)]
public class WeaponPartEditor : Editor
{
    string weaponName = "";
    bool isNamingWeapon = false;
    Vector2 scrollPosition;
    private bool isDeterministicGenerating = false;
    private List<(int categoryIndex, int partIndex)> selectedParts = new List<(int categoryIndex, int partIndex)>();

    private int selectedCategoryIndex = -1; // Persistent category index
    private int selectedPartIndex = -1; // Persistent part index

    public override void OnInspectorGUI()
    {
        WeaponPart weapon = (WeaponPart)target;
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate"))
        {
            weapon.Generate();
            EditorUtility.SetDirty(weapon);
        }

        if (GUILayout.Button("Deterministic Generate"))
        {
            DeterministicGenerateWindow.Open(weapon);
        }

        if (GUILayout.Button("Save"))
        {
            isNamingWeapon = true;
        }

        // Draw weapon naming interface if active
        if (isNamingWeapon)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Name your weapon:", EditorStyles.boldLabel);

            // Weapon name input field
            weaponName = EditorGUILayout.TextField("Weapon Name", weaponName);

            EditorGUILayout.BeginHorizontal();

            // Save button
            if (GUILayout.Button("Confirm Save"))
            {
                if (string.IsNullOrWhiteSpace(weaponName))
                {
                    EditorUtility.DisplayDialog("Invalid Name",
                        "Please enter a valid weapon name.", "OK");
                }
                else
                {
                    SavePartEditor(weapon);
                    isNamingWeapon = false;
                    weaponName = "";
                    GUIUtility.ExitGUI();
                }
            }

            // Cancel button
            if (GUILayout.Button("Cancel"))
            {
                isNamingWeapon = false;
                weaponName = "";
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Load"))
        {
            string path = Application.persistentDataPath;

            if (!string.IsNullOrEmpty(path))
            {
                // Get all JSON files in the directory
                string[] jsonFiles = Directory.GetFiles(path, "*.json");
                if (jsonFiles.Length > 0)
                {
                    // Convert absolute paths to file names
                    string[] fileNames = new string[jsonFiles.Length];
                    for (int i = 0; i < jsonFiles.Length; i++)
                    {
                        fileNames[i] = Path.GetFileName(jsonFiles[i]);
                    }

                    // Show a dropdown menu to select a file
                    int selectedFileIndex = EditorGUILayout.Popup("Select File", 0, fileNames);

                    // Get the selected file name
                    string selectedFileName = fileNames[selectedFileIndex];

                    // Combine the path to get the full path
                    string selectedFilePath = Path.Combine(path, "Test");

                    // Load the weapon using the selected file
                    LoadWeaponInEditor(weapon, selectedFilePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("No Files Found", "No JSON files found in the save folder.", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Path", "The Persistent Data Path is invalid.", "OK");
            }
        }

        if (GUILayout.Button("Open Save Folder"))
        {
            OpenPersistentDataPath();
        }
    }

    private void SavePartEditor(WeaponPart weaponPart)
    {
        // Ensure SaveSystem exists in editor
        SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();

        if (saveSystem != null)
        {
            saveSystem.Save(weaponPart, weaponName);
            EditorUtility.SetDirty(weaponPart);
        }
    }

    private void LoadWeaponInEditor(WeaponPart weaponPart, string path)
    {
        // Ensure SaveSystem exists in editor
        SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();

        if (saveSystem == null)
        {
            EditorUtility.DisplayDialog("Error", "SaveSystem not found in the scene.", "OK");
            return;
        }

        try
        {
            // Extract weapon name from the file path
            string weaponName = Path.GetFileNameWithoutExtension(path);

            // Load the weapon
            WeaponPart loadedWeapon = saveSystem.LoadWeapon(weaponName, weaponPart);

            if (loadedWeapon != null)
            {
                // Mark the object as dirty to save changes in editor
                EditorUtility.SetDirty(weaponPart);
                EditorUtility.SetDirty(loadedWeapon);

                // Show success message
                EditorUtility.DisplayDialog("Success", $"Weapon loaded from {Path.GetFileName(path)}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Load Error", "Failed to load weapon.", "OK");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Load Error", $"Failed to load weapon: {e.Message}", "OK");
            UnityEngine.Debug.LogError($"Weapon loading error: {e}");
        }
    }

    public void OpenPersistentDataPath()
    {
        string path = Application.persistentDataPath;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_STANDALONE_OSX
        Process.Start("open", path);
#elif UNITY_STANDALONE_LINUX
        Process.Start("xdg-open", path);
#else
        Debug.LogError("Opening file explorer is not supported on this platform.");
#endif
    }
}
