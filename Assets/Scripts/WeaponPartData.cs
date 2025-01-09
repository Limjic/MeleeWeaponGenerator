using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponPartData
{
    public List<WeaponPartData> weaponParts = new List<WeaponPartData>();


    public int categoryIndex;
    public int partIndex;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
    public int parentIndex;
    public int snapPointIndex;

    public WeaponPartData(int catIndex, int pIndex, Vector3 pos, Quaternion rot, int parent = -1, int snapIndex = -1)
    {
        categoryIndex = catIndex;
        partIndex = pIndex;
        position = new SerializableVector3(pos);
        rotation = new SerializableQuaternion(rot);
        parentIndex = parent;
        snapPointIndex = snapIndex;
    }
}

[System.Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[System.Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}

