using System;
using UnityEngine;

public static class InteriorSurfaceFilter
{
    public static bool IsCeilingOrRoof(GameObject go)
    {
        if (go == null)
            return false;

        if (MatchesCeilingName(go.name))
            return true;

        var current = go.transform.parent;
        while (current != null)
        {
            if (MatchesCeilingName(current.name))
                return true;
            current = current.parent;
        }

        return false;
    }

    public static bool MatchesCeilingName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        return objectName.IndexOf("teto", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("ceiling", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("telhado", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("roof", StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("tecto", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsWalkableFloorHit(RaycastHit hit)
    {
        if (hit.collider == null || hit.collider.isTrigger)
            return false;

        if (IsCeilingOrRoof(hit.collider.gameObject))
            return false;

        return hit.normal.y >= 0.35f;
    }

    public static void DisableSolidCollidersOnCeilings(Transform root)
    {
        if (root == null)
            return;

        foreach (var col in root.GetComponentsInChildren<Collider>(true))
        {
            if (col == null || !IsCeilingOrRoof(col.gameObject))
                continue;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(col);
            else
                col.enabled = false;
        }
    }
}
