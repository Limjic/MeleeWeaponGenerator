using UnityEngine;

public class Core : WeaponPart
{
    public override WeaponPart GetFirstWeaponPartParent()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            WeaponPart weaponPart = current.GetComponent<WeaponPart>();
            if (weaponPart != null)
            {
                return weaponPart;
            }

            current = current.parent;
        }

        return null;
    }
}
