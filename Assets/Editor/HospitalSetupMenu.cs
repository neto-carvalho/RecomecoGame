#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HospitalSetupMenu
{
    const string MenuRoot = "Recomeco/Cidade/";
    const string CidadeScenePath = "Assets/Scenes/Cidade.unity";
    const string HospitalPrefabPath =
        "Assets/Studio Horizon/Simple Building Generic Free/Prefabs/Hospital.prefab";
    const float SpawnForwardMeters = 5f;

    [MenuItem(MenuRoot + "Spawn pós-desmaio — na frente do hospital")]
    static void SetupSpawnInFrontOfHospital()
    {
        if (!OpenCidadeScene())
            return;

        if (!HospitalSpawnUtility.TryFindHospitalRoot(out var hospital))
        {
            EditorUtility.DisplayDialog("Spawn hospital",
                "Não achei um hospital na cena Cidade.\n\n" +
                "Coloque o prefab Hospital e tente de novo, ou selecione o hospital e use:\n" +
                "Recomeco → Cidade → Spawn pós-desmaio — na posição selecionada",
                "OK");
            return;
        }

        var spawnPos = hospital.position + hospital.forward * SpawnForwardMeters;
        var spawnRot = Quaternion.LookRotation(hospital.forward, Vector3.up);
        var spawn = EnsureSpawn(RecomecoSceneNames.HospitalEntrada, spawnPos, spawnRot);

        Selection.activeGameObject = spawn;
        SceneView.lastActiveSceneView?.FrameSelected();

        MarkCidadeDirty();
        EditorUtility.DisplayDialog("Spawn hospital",
            "Criado/atualizado: Spawn_" + RecomecoSceneNames.HospitalEntrada + "\n\n" +
            "Ajuste a posição/rotação no Scene view se precisar (seta verde = frente do player).\n" +
            "Salve a cena Cidade (Ctrl+S).",
            "OK");
    }

    [MenuItem(MenuRoot + "Spawn pós-desmaio — na posição selecionada")]
    static void SetupSpawnAtSelection()
    {
        if (!OpenCidadeScene())
            return;

        var selected = Selection.activeTransform;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Spawn hospital",
                "Selecione na Hierarchy um Empty ou o ponto exato da calçada em frente ao hospital.",
                "OK");
            return;
        }

        var spawn = EnsureSpawn(
            RecomecoSceneNames.HospitalEntrada,
            selected.position,
            selected.rotation);

        Selection.activeGameObject = spawn;
        SceneView.lastActiveSceneView?.FrameSelected();
        MarkCidadeDirty();

        EditorUtility.DisplayDialog("Spawn hospital",
            "Spawn_" + RecomecoSceneNames.HospitalEntrada + " definido na posição de \"" +
            selected.name + "\".\n\nSalve a cena Cidade (Ctrl+S).",
            "OK");
    }

    [MenuItem(MenuRoot + "Configurar hospital (prefab + spawn entrada)")]
    static void SetupHospitalPrefabAndSpawn()
    {
        if (!OpenCidadeScene())
            return;

        if (!File.Exists(HospitalPrefabPath))
        {
            EditorUtility.DisplayDialog("Hospital",
                "Prefab não encontrado:\n" + HospitalPrefabPath +
                "\n\nImporte o pack Simple Generic Buildings ou coloque o hospital manualmente e use " +
                "\"Spawn pós-desmaio — na frente do hospital\".",
                "OK");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalPrefabPath);
        var hospitalRoot = GameObject.Find(RecomecoSceneNames.HospitalRootName);
        if (hospitalRoot == null && HospitalSpawnUtility.TryFindHospitalRoot(out var existing))
            hospitalRoot = existing.gameObject;

        if (hospitalRoot == null)
        {
            hospitalRoot = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (hospitalRoot == null)
            {
                EditorUtility.DisplayDialog("Hospital", "Falha ao instanciar prefab do hospital.", "OK");
                return;
            }

            hospitalRoot.name = RecomecoSceneNames.HospitalRootName;
            Undo.RegisterCreatedObjectUndo(hospitalRoot, "Create Hospital");
            hospitalRoot.transform.position = new Vector3(-12f, 0f, -8f);
            hospitalRoot.transform.rotation = Quaternion.identity;
        }

        var spawnPos = hospitalRoot.transform.position + hospitalRoot.transform.forward * SpawnForwardMeters;
        EnsureSpawn(RecomecoSceneNames.HospitalEntrada, spawnPos, hospitalRoot.transform.rotation);

        MarkCidadeDirty();
        EditorUtility.DisplayDialog("Hospital",
            "Hospital + spawn configurados.\n\n" +
            "• " + hospitalRoot.name + "\n" +
            "• Spawn_" + RecomecoSceneNames.HospitalEntrada + "\n\n" +
            "Salve a cena Cidade.",
            "OK");
    }

    static bool OpenCidadeScene()
    {
        if (!File.Exists(CidadeScenePath))
        {
            EditorUtility.DisplayDialog("Cidade", "Cena não encontrada:\n" + CidadeScenePath, "OK");
            return false;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != CidadeScenePath)
            EditorSceneManager.OpenScene(CidadeScenePath, OpenSceneMode.Single);

        return true;
    }

    static void MarkCidadeDirty()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }

    static GameObject EnsureSpawn(string spawnId, Vector3 position, Quaternion rotation)
    {
        SceneSpawnPoint existing = null;
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp != null && sp.spawnId == spawnId)
            {
                existing = sp;
                break;
            }
        }

        if (existing != null)
        {
            Undo.RecordObject(existing.transform, "Move Hospital Spawn");
            existing.spawnId = spawnId;
            existing.transform.SetPositionAndRotation(position, rotation);
            return existing.gameObject;
        }

        var go = new GameObject("Spawn_" + spawnId);
        Undo.RegisterCreatedObjectUndo(go, "Create Hospital Spawn");
        go.transform.SetPositionAndRotation(position, rotation);
        var point = Undo.AddComponent<SceneSpawnPoint>(go);
        point.spawnId = spawnId;
        return go;
    }
}
#endif
