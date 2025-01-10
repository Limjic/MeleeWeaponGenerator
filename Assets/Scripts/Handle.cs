using System.Collections.Generic;
using UnityEngine;

public class Handle : WeaponPart
{
    public override WeaponPart GetFirstWeaponPartParent()
    {
        return null;
    }
}
