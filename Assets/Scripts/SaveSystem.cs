using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    public WeaponPartData weaponData;

    [Header("Weapon Categories")]
    [Tooltip("Array of WeaponPartSO containing categorized weapon parts")]
    public WeaponPartSO[] weaponCategories;

    string saveDestination;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Initialize(string _weaponName)
    {
        _weaponName += ".json";
        saveDestination = Path.Combine(Application.persistentDataPath, _weaponName);
    }

    public void Save(WeaponPart _weapon, string _weaponName)
    {
        Initialize(_weaponName);
        weaponData = ConvertWeaponPartToData(_weapon);
        string weaponSaveString = JsonConvert.SerializeObject(weaponData, Formatting.Indented);
        File.WriteAllText(saveDestination, weaponSaveString);
        Debug.Log(_weaponName + "Saved In " + saveDestination);
    }

    WeaponPartData ConvertWeaponPartToData(WeaponPart _weaponToConvert)
    {
        WeaponPartData weaponData = new WeaponPartData(
            _weaponToConvert.GetCategoryIndex(),
            _weaponToConvert.GetPartType(),
            _weaponToConvert.transform.position,
            _weaponToConvert.transform.rotation
            );

        for (int i = 0; i < _weaponToConvert.weaponParts.Count; i++)
        {
            weaponData.weaponParts.Add(ConvertWeaponPartToData(_weaponToConvert.weaponParts[i]));
        }


        return weaponData;
    }

    public WeaponPart LoadWeapon(string _weaponName, WeaponPart parentWeaponPart)
    {
        Initialize(_weaponName);

        // Check if the file exists
        if (!File.Exists(saveDestination))
        {
            Debug.LogError($"Save file not found: {_weaponName}");
            return null;
        }

        try
        {
            // Read the JSON file
            string loadedJson = File.ReadAllText(saveDestination);
            WeaponPartData loadedWeaponData = JsonConvert.DeserializeObject<WeaponPartData>(loadedJson);

            // Clear existing child objects
            foreach (Transform child in parentWeaponPart.transform)
            {
                DestroyImmediate(child.gameObject);
            }

            // Reinitialize the snap point list
            parentWeaponPart.SnapPointList.Clear();
            parentWeaponPart.FindSnapObjectsInChildren();

            // Reconstruct the weapon
            return ReconstructWeaponPart(loadedWeaponData, parentWeaponPart);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load weapon: {e.Message}");
            return null;
        }
    }

    WeaponPart ReconstructWeaponPart(WeaponPartData partData, WeaponPart parentWeaponPart, Transform parentTransform = null)
    {
        // Validate the category and part indices
        if (partData.categoryIndex < 0 || partData.categoryIndex >= weaponCategories.Length)
        {
            Debug.LogError($"Invalid category index: {partData.categoryIndex}");
            return null;
        }

        WeaponPartSO category = weaponCategories[partData.categoryIndex];

        if (partData.partIndex < 0 || partData.partIndex >= category.weaponPartArray.Length)
        {
            Debug.LogError($"Invalid part index: {partData.partIndex}");
            return null;
        }

        // Determine the parent transform
        Transform spawnTransform = parentTransform ??
            (parentWeaponPart.SnapPointList.Count > 0 ?
                parentWeaponPart.SnapPointList[0].transform :
                parentWeaponPart.transform);

        // Instantiate the weapon part
        GameObject partPrefab = category.weaponPartArray[partData.partIndex];
        GameObject instantiatedPart = Instantiate(partPrefab, spawnTransform);

        // Set position and rotation
        instantiatedPart.transform.localPosition = partData.position.ToVector3();
        instantiatedPart.transform.localRotation = partData.rotation.ToQuaternion();

        // Get the WeaponPart component
        WeaponPart weaponPart = instantiatedPart.GetComponent<WeaponPart>();
        weaponPart.soWeaponParts = parentWeaponPart.soWeaponParts;

        // Recursively reconstruct child parts
        for (int i = 0; i < partData.weaponParts.Count; i++)
        {
            // Find the corresponding snap point for this child part
            if (i < weaponPart.SnapPointList.Count)
            {
                ReconstructWeaponPart(partData.weaponParts[i], weaponPart, weaponPart.SnapPointList[i].transform);
            }
            else
            {
                Debug.LogWarning($"Not enough snap points for child part {i}");
            }
        }

        return weaponPart;
    }
}