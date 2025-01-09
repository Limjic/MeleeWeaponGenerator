using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static SaveSystem;

public class WeaponPart : MonoBehaviour
{

    public int parentIndex;
    public int snapPointIndex;

    [Header("Generation Range")]
    [SerializeField, Range(0, 20)] public int partMinRange = 3;
    [SerializeField, Range(0, 20)] public int partMaxRange = 5;

    [Header("References")]
    public List<GameObject> SnapPointList = new List<GameObject>();
    public WeaponPartSO[] soWeaponParts;
    public List<WeaponPart> weaponParts = new List<WeaponPart>();

    SaveSystem saveSystem;
    public virtual void Generate()
    {
        weaponParts.Clear();
        FindSnapObjectsInChildren();
        SpawnWeaponPart();

        foreach (var part in weaponParts)
        {
            part.Generate();
        }

    }

    public void DeterministicGenerate(List<(int categoryIndex, int partIndex)> selectedParts)
    {
        weaponParts.Clear();
        FindSnapObjectsInChildren();

        // Ensure we don't exceed available snap points
        int partsToGenerate = Mathf.Min(selectedParts.Count, SnapPointList.Count);

        // Get the save system
        SaveSystem saveSystem = FindFirstObjectByType<SaveSystem>();
        if (saveSystem == null)
        {
            Debug.LogError("SaveSystem not found in the scene!");
            return;
        }

        // Iterate through selected parts
        for (int i = 0; i < partsToGenerate; i++)
        {
            var (categoryIndex, partIndex) = selectedParts[i];

            // Check category and part indices
            if (categoryIndex < 0 || categoryIndex >= saveSystem.weaponCategories.Length)
            {
                Debug.LogError($"Invalid category index: {categoryIndex}");
                continue;
            }

            WeaponPartSO category = saveSystem.weaponCategories[categoryIndex];

            if (partIndex < 0 || partIndex >= category.weaponPartArray.Length)
            {
                Debug.LogError($"Invalid part index: {partIndex}");
                continue;
            }

            GameObject partPrefab = category.weaponPartArray[partIndex];

            // Instantiate at the corresponding snap point
            GameObject instantiatedPart = Instantiate(
                partPrefab,
                SnapPointList[i].transform
            );

            // Check the instantiated part has the correct references
            WeaponPart tempPart = instantiatedPart.GetComponent<WeaponPart>();
            if (tempPart != null)
            {
                tempPart.soWeaponParts = soWeaponParts;
                weaponParts.Add(tempPart);

                // Do it for the next part
                var remainingParts = selectedParts.Skip(partsToGenerate).ToList();
                if (remainingParts.Count > 0)
                {
                    tempPart.DeterministicGenerate(remainingParts);
                }
            }
        }
    }

    // Will look for the SnapPoints Gameobject (use to know the location of where to spawn the weapon parts)
    public void FindSnapObjectsInChildren()
    {
        SnapPointList.Clear();
        List<Transform> children = GetComponentsInChildren<Transform>(true).ToList();

        foreach (Transform child in children)
        {
            if (child != null && child != transform && child.gameObject.name.Contains("Snap"))
            {
                ClearGameObject(child);
                SnapPointList.Add(child.gameObject);
            }
        }
    }

    // The function that delete every generated object before generating again
    void ClearGameObject(Transform _child)
    {
        List<Transform> children = new List<Transform>();

        foreach (Transform child2 in _child)
        {
            children.Add(child2);
        }

        foreach (Transform child2 in children)
        {
            if (child2 != null && child2.gameObject != null)
            {
                DestroyImmediate(child2.gameObject);
            }
        }
    }

    protected virtual void SpawnWeaponPart()
    {
        if (GetFirstWeaponPartParent() != null)
        {
            int nbparentMax = GetFirstWeaponPartParent().partMaxRange;
            nbparentMax--;
            partMaxRange = nbparentMax;

            int nbparentMin = GetFirstWeaponPartParent().partMinRange;
            nbparentMin--;
            partMinRange = nbparentMin;
        }

        for (int i = 0; i < SnapPointList.Count; i++)
        {
            int partType = DeterminePartTypeToSpawn();
            int range = Random.Range(0, soWeaponParts[partType].weaponPartArray.Length);

            GameObject instantiatedPart = Instantiate(soWeaponParts[partType].weaponPartArray[range], SnapPointList[i].transform);
            WeaponPart tempPart = instantiatedPart.GetComponent<WeaponPart>();
            tempPart.soWeaponParts = soWeaponParts; // Ensure the reference is passed down
            weaponParts.Add(tempPart);
        }
    }

    // Will determine if I'm going to spawn a blade,a core, etc...
    private int DeterminePartTypeToSpawn()
    {
        if (partMaxRange <= 0)
        {
            return soWeaponParts.Length - 1;
        }
        else if (partMinRange > 0)
        {
            return Random.Range(0, soWeaponParts.Length - 1);
        }
        else
        {
            return Random.Range(0, soWeaponParts.Length);
        }
    }

    public int GetCategoryIndex()
    {
        saveSystem = FindFirstObjectByType<SaveSystem>();
        WeaponPartSO[] weaponPartSOArray = saveSystem.weaponCategories;

        if (weaponPartSOArray == null) return -1;

        for (int categoryIndex = 0; categoryIndex < weaponPartSOArray.Length; categoryIndex++)
        {
            WeaponPartSO category = weaponPartSOArray[categoryIndex];

            if (category.weaponPartArray != null)
            {
                foreach (GameObject prefab in category.weaponPartArray)
                {
                    // Compare prefab tags instead of direct object reference
                    if (prefab.CompareTag(gameObject.tag))
                    {
                        return categoryIndex;
                    }
                }
            }
        }

        return -1; // Return -1 if no category is found
    }

    public int GetPartType()
    {
        int categoryIndex = GetCategoryIndex();
        saveSystem = FindFirstObjectByType<SaveSystem>();
        WeaponPartSO weaponPartSO = saveSystem.weaponCategories[categoryIndex];

        if (weaponPartSO == null) return -1;


        if (weaponPartSO.weaponPartArray != null)
        {
            for (int i = 0; i < weaponPartSO.weaponPartArray.Length; i++)
            {
                GameObject prefab = weaponPartSO.weaponPartArray[i];

                // Compare prefab names instead of direct object reference. DON'T DO THAAAAT !
                string sanitizedObjectName = gameObject.name.Replace("(Clone)", "").Trim();
                if (prefab.name == sanitizedObjectName)
                {
                    return i;
                }
            }
        }
        else
        {
            Debug.Log("PB GetPartType");
        }

        return -1; // Return -1 if part type is not found
    }


    // Will look for the next WeaponPart in the hierarchy
    public virtual WeaponPart GetFirstWeaponPartParent()
    {
        return null;
    }


}