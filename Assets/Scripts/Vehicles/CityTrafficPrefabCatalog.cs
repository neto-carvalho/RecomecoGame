using System.Collections.Generic;
using UnityEngine;

public static class CityTrafficPrefabCatalog
{
    const string ResourcesFolder = "CityTraffic";
    const string NoInteriorFolder = "Assets/Flat_Style_Vehicles/Prefabs/Vehicles_no_interior";

    public static IReadOnlyList<GameObject> LoadSpawnPrefabs()
    {
        var list = new List<GameObject>();

        var fromResources = Resources.LoadAll<GameObject>(ResourcesFolder);
        if (fromResources != null && fromResources.Length > 0)
        {
            foreach (var p in fromResources)
            {
                if (p != null)
                    list.Add(p);
            }

            if (list.Count > 0)
                return list;
        }

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { NoInteriorFolder });
        foreach (var guid in guids)
        {
            if (list.Count >= 16)
                break;

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                list.Add(prefab);
        }
#endif

        return list;
    }
}
