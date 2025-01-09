using UnityEngine;

[CreateAssetMenu(fileName = "WeaponPartSO", menuName = "Weapons/Weapon Part Category")]
public class WeaponPartSO : ScriptableObject
{
    public string categoryName;
    public GameObject[] weaponPartArray;

    private void OnValidate()
    {
        // Ensure category name isn't empty
        if (string.IsNullOrEmpty(categoryName))
        {
            categoryName = name;
        }

        // Check for null entries in the weapon part array
        if (weaponPartArray != null)
        {
            for (int i = 0; i < weaponPartArray.Length; i++)
            {
                if (weaponPartArray[i] == null)
                {
                    Debug.LogWarning($"Null entry found in weapon part array at index {i} in category {categoryName}");
                }
            }
        }
    }
}