#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class HouidiCanalWaterSetupMenu
{
    const string MenuApply = "Recomeco/Água/Aplicar água Houidi (automático)";
    const string HouidiSourceMatPath =
        "Assets/Houidisoft technology/One Click Add Water -Stylized Water Shader/Resources/water.mat";
    const string CanalMatPath = "Assets/Materials/CanalWater.mat";
    const string PcRpAssetPath = "Assets/Settings/PC_RPAsset.asset";
    const string MobileRpAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
    const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
    const string CidadeScenePath = "Assets/Scenes/Cidade.unity";
    const string ReflectionProbeName = "Recomeco_CanalWaterReflection";

    [MenuItem(MenuApply)]
    static void ApplyHouidiWaterAutomatic()
    {
        if (!System.IO.File.Exists(HouidiSourceMatPath.Replace('/', System.IO.Path.DirectorySeparatorChar)))
        {
            EditorUtility.DisplayDialog(
                "Água Houidi",
                "Pacote não encontrado.\n\nImporta «One Click Add Water» e tenta de novo.",
                "OK");
            return;
        }

        var urpNotes = ConfigureUrpForWater();
        var canalMat = EnsureCanalWaterMaterial();
        if (canalMat == null)
            return;

        var targets = CollectWaterTargets();
        if (targets.Count == 0 && TryOpenCidadeScene())
            targets = CollectWaterTargets();

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Água Houidi",
                "Nenhuma superfície de água encontrada.\n\n" +
                "Seleciona o mesh «Water» na Hierarchy ou nomeia o objeto com «Water» / «Lake_Water».",
                "OK");
            return;
        }

        var collidersRemoved = 0;
        var renderersUpdated = 0;
        var bounds = new Bounds(targets[0].transform.position, Vector3.one);

        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Aplicar água Houidi");

        foreach (var root in targets)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || ShouldSkipRenderer(renderer))
                    continue;

                Undo.RecordObject(renderer, "Material água");
                renderer.sharedMaterial = canalMat;
                renderersUpdated++;

                if (bounds.size.sqrMagnitude < 0.01f)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);

                collidersRemoved += RemoveSolidColliders(renderer.gameObject);
            }
        }

        EnsureReflectionProbe(bounds);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "Água Houidi",
            $"Concluído na cena «{SceneManager.GetActiveScene().name}».\n\n" +
            $"• Materiais: {renderersUpdated}\n" +
            $"• Colliders sólidos removidos: {collidersRemoved}\n" +
            $"• Material: {CanalMatPath}\n" +
            $"• Reflection Probe: {ReflectionProbeName}\n\n" +
            urpNotes +
            "\nGuarda a cena (Ctrl+S) e dá Play para ver ondas.\n" +
            "Coloca barreiras invisíveis na calçada se ainda não tiveres.",
            "OK");
    }

    static string ConfigureUrpForWater()
    {
        var notes = new List<string>();
        EnableDepthAndOpaque(PcRpAssetPath, "PC_RPAsset");
        EnableDepthAndOpaque(MobileRpAssetPath, "Mobile_RPAsset");
        notes.Add("URP: Depth + Opaque Texture ligados (PC e Mobile).");

        if (TryForceDepthPrepassOnPcRenderer())
            notes.Add("PC_Renderer: Depth Texture Mode → Force prepass (Deferred).");

        return string.Join("\n", notes);
    }

    static void EnableDepthAndOpaque(string assetPath, string label)
    {
        if (!System.IO.File.Exists(assetPath))
            return;

        var main = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (main == null)
            return;

        var so = new SerializedObject(main);
        var depth = so.FindProperty("m_RequireDepthTexture");
        var opaque = so.FindProperty("m_RequireOpaqueTexture");
        if (depth != null)
            depth.intValue = 1;
        if (opaque != null)
            opaque.intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(main);
    }

    static bool TryForceDepthPrepassOnPcRenderer()
    {
        if (!System.IO.File.Exists(PcRendererPath))
            return false;

        var main = AssetDatabase.LoadMainAssetAtPath(PcRendererPath);
        if (main == null)
            return false;

        var so = new SerializedObject(main);
        var renderingMode = so.FindProperty("m_RenderingMode");
        if (renderingMode == null || renderingMode.intValue != 2)
            return false;

        var copyDepth = so.FindProperty("m_CopyDepthMode");
        if (copyDepth == null)
            return false;

        if (copyDepth.intValue == 2)
            return false;

        copyDepth.intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(main);
        return true;
    }

    static Material EnsureCanalWaterMaterial()
    {
        EnsureFolder("Assets/Materials");

        var existing = AssetDatabase.LoadAssetAtPath<Material>(CanalMatPath);
        if (existing != null)
            return existing;

        if (!AssetDatabase.CopyAsset(HouidiSourceMatPath, CanalMatPath))
        {
            EditorUtility.DisplayDialog("Água Houidi", "Não foi possível criar " + CanalMatPath, "OK");
            return null;
        }

        AssetDatabase.SaveAssets();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(CanalMatPath);
        if (mat == null)
            return null;

        ApplyCanalMaterialTuning(mat);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void ApplyCanalMaterialTuning(Material mat)
    {
        if (mat.HasProperty("_Wave_ampllitude"))
            mat.SetFloat("_Wave_ampllitude", 0.04f);
        if (mat.HasProperty("_Waveampllitude"))
            mat.SetFloat("_Waveampllitude", 0.8f);
        if (mat.HasProperty("_WaveSpeed"))
            mat.SetFloat("_WaveSpeed", 0.008f);
        if (mat.HasProperty("_Waves_height"))
            mat.SetFloat("_Waves_height", 0.35f);
        if (mat.HasProperty("_shallow_water_color"))
            mat.SetColor("_shallow_water_color", new Color(0.35f, 0.62f, 0.82f, 0.55f));
        if (mat.HasProperty("_deep_water_color"))
            mat.SetColor("_deep_water_color", new Color(0.08f, 0.28f, 0.48f, 1f));
        if (mat.HasProperty("_foam_amaont"))
            mat.SetFloat("_foam_amaont", 0.45f);
    }

    static List<GameObject> CollectWaterTargets()
    {
        var result = new List<GameObject>();
        var seen = new HashSet<int>();

        void TryAdd(GameObject go)
        {
            if (go == null || !seen.Add(go.GetInstanceID()))
                return;
            result.Add(go);
        }

        foreach (var selected in Selection.gameObjects)
        {
            if (selected == null)
                continue;
            if (Selection.gameObjects.Length > 0)
                TryAdd(selected);
        }

        if (result.Count > 0)
            return result;

        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in all)
        {
            if (t == null || !IsWaterSurfaceName(t.name))
                continue;
            if (t.GetComponent<Renderer>() != null || t.GetComponentInChildren<Renderer>(true) != null)
                TryAdd(t.gameObject);
        }

        return result;
    }

    static bool TryOpenCidadeScene()
    {
        if (!System.IO.File.Exists(CidadeScenePath))
            return false;

        if (SceneManager.GetActiveScene().path == CidadeScenePath)
            return false;

        if (!EditorUtility.DisplayDialog(
                "Água Houidi",
                "Nenhum «Water» na cena atual.\n\nAbrir a cena Cidade e continuar?",
                "Abrir Cidade",
                "Cancelar"))
            return false;

        EditorSceneManager.OpenScene(CidadeScenePath, OpenSceneMode.Single);
        return true;
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

    static bool ShouldSkipRenderer(Renderer renderer)
    {
        var n = renderer.gameObject.name.ToLowerInvariant();
        return n.Contains("barricade") || n == "underwatertrigger";
    }

    static int RemoveSolidColliders(GameObject go)
    {
        var removed = 0;
        foreach (var col in go.GetComponents<Collider>())
        {
            if (col == null || col.isTrigger)
                continue;

            Undo.DestroyObjectImmediate(col);
            removed++;
        }

        return removed;
    }

    static void EnsureReflectionProbe(Bounds waterBounds)
    {
        if (waterBounds.size.sqrMagnitude < 0.01f)
            return;

        var probeGo = GameObject.Find(ReflectionProbeName);
        if (probeGo == null)
        {
            probeGo = new GameObject(ReflectionProbeName);
            Undo.RegisterCreatedObjectUndo(probeGo, "Reflection probe água");
            probeGo.AddComponent<ReflectionProbe>();
        }

        var center = waterBounds.center;
        center.y = waterBounds.max.y + 2f;
        probeGo.transform.position = center;

        var probe = probeGo.GetComponent<ReflectionProbe>();
        Undo.RecordObject(probe, "Reflection probe água");
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.resolution = 128;
        probe.boxProjection = true;
        probe.intensity = 1f;

        var size = waterBounds.size;
        size.y = Mathf.Max(size.y + 6f, 12f);
        size.x = Mathf.Max(size.x + 4f, 8f);
        size.z = Mathf.Max(size.z + 4f, 8f);
        probe.size = size;
        probe.center = Vector3.zero;

        EditorUtility.SetDirty(probeGo);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
