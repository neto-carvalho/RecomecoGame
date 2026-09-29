#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WaterEdgeBarrierSetupMenu
{
    const string MenuAuto = "Recomeco/Água/Barreiras invisíveis (borda da água — automático)";
    const string MenuManual = "Recomeco/Água/Barreira invisível (objeto selecionado)";
    const string BlockersRootName = "Recomeco_WaterWalkBlockers";
    const float BarrierThickness = 0.45f;
    const float BarrierHeight = 3.2f;
    const float EdgePadding = 1.25f;
    const float WallDetectDistance = 2.5f;

    enum WaterEdge
    {
        PosX,
        NegX,
        PosZ,
        NegZ
    }

    [MenuItem(MenuAuto)]
    static void CreateBarriersAutomatic()
    {
        var waterRoots = CollectWaterRootsFromSelectionOrScene();
        if (waterRoots.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Barreiras",
                "Nenhuma água encontrada.\n\nSeleciona o mesh «Water» ou deixa a cena com um objeto «Water» / «Lake_Water».",
                "OK");
            return;
        }

        if (!TryGetCombinedWaterBounds(waterRoots, out var waterBounds))
        {
            EditorUtility.DisplayDialog("Barreiras", "Não foi possível calcular os limites da água.", "OK");
            return;
        }

        var walkableRef = FindWalkableReference(waterBounds);
        var edgesToBlock = ResolveEdgesToBlock(waterBounds, walkableRef, blockAllOpenEdges: walkableRef == null);

        if (edgesToBlock.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Barreiras",
                "Todas as bordas já parecem ter muro/cais (Channel_wall, Fence, etc.) ou nenhuma borda aberta foi detectada.\n\n" +
                "Usa «Barreira invisível (objeto selecionado)» num Empty na calçada.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Barreiras invisíveis água");

        var root = EnsureBlockersRoot();
        var created = 0;
        foreach (var edge in edgesToBlock)
        {
            if (TryCreateBarrierForEdge(root.transform, waterBounds, edge, out _))
                created++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        var refNote = walkableRef != null
            ? $"Referência calçada: «{walkableRef.name}»."
            : "Sem referência: barreiras só em lados sem muro próximo.";

        EditorUtility.DisplayDialog(
            "Barreiras",
            $"Criadas {created} barreira(s) em «{BlockersRootName}».\n\n{refNote}\n\n" +
            "Ajusta posição/espessura se precisares (seleciona o filho InvisibleBarrier_*).\nGuarda a cena (Ctrl+S).",
            "OK");

        if (root != null)
            Selection.activeGameObject = root;
    }

    [MenuItem(MenuManual)]
    static void CreateBarrierOnSelection()
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Barreira",
                "Seleciona um Empty (ou qualquer objeto) na beira da calçada, alinhado ao canal.",
                "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Barreira invisível manual");

        var root = EnsureBlockersRoot();
        var count = 0;
        foreach (var go in Selection.gameObjects)
        {
            if (go == null || go.transform.IsChildOf(root.transform))
                continue;

            CreateManualBarrier(root.transform, go.transform);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "Barreira",
            count > 0
                ? $"{count} barreira(s) criadas. Escala o Box Collider no Inspector se estiver curta/larga."
                : "Nada criado.",
            "OK");
    }

    static List<GameObject> CollectWaterRootsFromSelectionOrScene()
    {
        var result = new List<GameObject>();
        var seen = new HashSet<int>();

        void TryAdd(GameObject go)
        {
            if (go == null || !seen.Add(go.GetInstanceID()))
                return;
            if (!IsWaterSurfaceName(go.name))
                return;
            result.Add(go);
        }

        foreach (var go in Selection.gameObjects)
            TryAdd(go);

        if (result.Count > 0)
            return result;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || !IsWaterSurfaceName(t.name))
                continue;
            if (t.GetComponent<Renderer>() != null || t.GetComponentInChildren<Renderer>(true) != null)
                TryAdd(t.gameObject);
        }

        return result;
    }

    static bool IsWaterSurfaceName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var n = name.ToLowerInvariant();
        if (n.Contains("barricade") || n.Contains("underwatertrigger"))
            return false;

        return n == "water"
               || n.StartsWith("water(")
               || n.Contains("lake_water")
               || n.Contains("lake water");
    }

    static bool TryGetCombinedWaterBounds(List<GameObject> roots, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var root in roots)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r.gameObject.name.ToLowerInvariant().Contains("barricade"))
                    continue;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(r.bounds);
            }
        }

        return hasBounds;
    }

    static Transform FindWalkableReference(Bounds waterBounds)
    {
        foreach (var go in Selection.gameObjects)
        {
            if (go == null || IsWaterSurfaceName(go.name))
                continue;
            return go.transform;
        }

        Transform best = null;
        var bestDist = float.MaxValue;
        var center = waterBounds.center;
        center.y = waterBounds.min.y;

        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || !IsWalkableReferenceName(t.name))
                continue;

            var p = t.position;
            p.y = center.y;
            var d = Vector3.SqrMagnitude(p - center);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    static bool IsWalkableReferenceName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var n = name.ToLowerInvariant();
        return n.Contains("asphalt")
               || n.Contains("channel_wall")
               || n.Contains("channel wall")
               || n.Contains("sidewalk")
               || n.Contains("calçada")
               || n.Contains("calcada")
               || n.Contains("dock")
               || n.Contains("pier")
               || n.Contains("cais");
    }

    static List<WaterEdge> ResolveEdgesToBlock(Bounds waterBounds, Transform walkableRef, bool blockAllOpenEdges)
    {
        var edges = new List<WaterEdge>();

        if (walkableRef != null && !blockAllOpenEdges)
        {
            var edge = PickEdgeFromWalkableSide(waterBounds, walkableRef.position);
            if (!EdgeHasNearbyWall(waterBounds, edge))
                edges.Add(edge);
            return edges;
        }

        foreach (WaterEdge e in System.Enum.GetValues(typeof(WaterEdge)))
        {
            if (!EdgeHasNearbyWall(waterBounds, e))
                edges.Add(e);
        }

        return edges;
    }

    static WaterEdge PickEdgeFromWalkableSide(Bounds waterBounds, Vector3 walkablePos)
    {
        var c = waterBounds.center;
        var dx = walkablePos.x - c.x;
        var dz = walkablePos.z - c.z;

        if (Mathf.Abs(dz) >= Mathf.Abs(dx))
            return dz > 0f ? WaterEdge.PosZ : WaterEdge.NegZ;

        return dx > 0f ? WaterEdge.PosX : WaterEdge.NegX;
    }

    static bool EdgeHasNearbyWall(Bounds waterBounds, WaterEdge edge)
    {
        GetEdgePlacement(waterBounds, edge, out var center, out _, out _);
        var half = new Vector3(WallDetectDistance, BarrierHeight * 0.5f, WallDetectDistance);

        foreach (var col in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (col == null || col.isTrigger)
                continue;

            var go = col.gameObject;
            if (go.name.StartsWith("InvisibleBarrier_") || go.transform.root.name == BlockersRootName)
                continue;

            if (!IsLikelyPhysicalEdge(go.name))
                continue;

            if (col.bounds.SqrDistance(center) <= half.x * half.x)
                return true;
        }

        return false;
    }

    static bool IsLikelyPhysicalEdge(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("channel_wall")
               || n.Contains("channel wall")
               || n.Contains("fence")
               || n.Contains("wall")
               || n.Contains("barricade")
               || n.Contains("muro")
               || n.Contains("grade");
    }

    static bool TryCreateBarrierForEdge(Transform parent, Bounds waterBounds, WaterEdge edge, out GameObject barrierGo)
    {
        barrierGo = null;
        GetEdgePlacement(waterBounds, edge, out var center, out var size, out var rotation);

        var name = $"InvisibleBarrier_{edge}";
        var existing = parent.Find(name);
        if (existing != null)
        {
            barrierGo = existing.gameObject;
            Undo.RecordObject(barrierGo.transform, "Atualizar barreira");
            barrierGo.transform.SetPositionAndRotation(center, rotation);
            ConfigureBoxCollider(barrierGo, size);
            return true;
        }

        barrierGo = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(barrierGo, "Criar barreira");
        barrierGo.transform.SetParent(parent, true);
        barrierGo.transform.SetPositionAndRotation(center, rotation);
        barrierGo.tag = "Untagged";
        ConfigureBoxCollider(barrierGo, size);
        return true;
    }

    static void GetEdgePlacement(Bounds waterBounds, WaterEdge edge, out Vector3 center, out Vector3 size, out Quaternion rotation)
    {
        var y = waterBounds.min.y + BarrierHeight * 0.5f;
        var ext = waterBounds.extents;
        var c = waterBounds.center;
        var t = BarrierThickness;
        var pad = EdgePadding;

        switch (edge)
        {
            case WaterEdge.PosZ:
                center = new Vector3(c.x, y, waterBounds.max.z - t * 0.5f);
                size = new Vector3(ext.x * 2f + pad * 2f, BarrierHeight, t);
                rotation = Quaternion.identity;
                break;
            case WaterEdge.NegZ:
                center = new Vector3(c.x, y, waterBounds.min.z + t * 0.5f);
                size = new Vector3(ext.x * 2f + pad * 2f, BarrierHeight, t);
                rotation = Quaternion.identity;
                break;
            case WaterEdge.PosX:
                center = new Vector3(waterBounds.max.x - t * 0.5f, y, c.z);
                size = new Vector3(t, BarrierHeight, ext.z * 2f + pad * 2f);
                rotation = Quaternion.identity;
                break;
            default:
                center = new Vector3(waterBounds.min.x + t * 0.5f, y, c.z);
                size = new Vector3(t, BarrierHeight, ext.z * 2f + pad * 2f);
                rotation = Quaternion.identity;
                break;
        }
    }

    static void ConfigureBoxCollider(GameObject go, Vector3 size)
    {
        var box = go.GetComponent<BoxCollider>();
        if (box == null)
            box = Undo.AddComponent<BoxCollider>(go);

        Undo.RecordObject(box, "Box barreira");
        box.isTrigger = false;
        box.center = Vector3.zero;
        box.size = size;
    }

    static void CreateManualBarrier(Transform parent, Transform anchor)
    {
        var go = new GameObject("InvisibleBarrier_Manual");
        Undo.RegisterCreatedObjectUndo(go, "Barreira manual");
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(anchor.position, anchor.rotation);

        var box = Undo.AddComponent<BoxCollider>(go);
        box.isTrigger = false;
        box.center = Vector3.zero;
        box.size = new Vector3(8f, BarrierHeight, BarrierThickness);
    }

    static GameObject EnsureBlockersRoot()
    {
        var existing = GameObject.Find(BlockersRootName);
        if (existing != null)
            return existing;

        var root = new GameObject(BlockersRootName);
        Undo.RegisterCreatedObjectUndo(root, "Root barreiras");
        return root;
    }
}
#endif
