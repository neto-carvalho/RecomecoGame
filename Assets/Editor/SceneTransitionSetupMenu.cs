#if UNITY_EDITOR
using System.IO;
using Controller;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionSetupMenu
{
    const string MenuRoot = "Recomeco/";
    const string FerroVelhoScenePath = "Assets/Scenes/FerroVelho.unity";
    const string CidadeScenePath = "Assets/Scenes/Cidade.unity";
    const string JunkyardDemoPath = "Assets/Junkyard models/Scenes/Demo.unity";
    const string BaseMeshPrefabPath = "Assets/ithappy/Creative_Characters_FREE/Prefabs/Base_Mesh.prefab";
    const string MovementControllerPath =
        "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Controllers/Character_Movement.controller";
    const string GameplaySettingsPath = "Assets/Resources/RecomecoGameplaySettings.asset";
    const string JunkyardRoot = "Assets/Junkyard models";
    static readonly Vector3 FerroVelhoSpawnPosition = new(201.9f, 8.29f, 0f);

    [MenuItem(MenuRoot + "Cenas/Criar cena FerroVelho (junkyard + venda)")]
    static void CreateFerroVelhoScene()
    {
        if (!File.Exists(JunkyardDemoPath))
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Não encontrei:\n" + JunkyardDemoPath + "\n\nImporte o pack Junkyard models.",
                "OK");
            return;
        }

        if (File.Exists(FerroVelhoScenePath))
        {
            if (!EditorUtility.DisplayDialog("Recomeco",
                    "Já existe " + FerroVelhoScenePath + ".\nAbrir e atualizar objetos de jogo?",
                    "Sim", "Cancelar"))
                return;
        }
        else if (!AssetDatabase.CopyAsset(JunkyardDemoPath, FerroVelhoScenePath))
        {
            EditorUtility.DisplayDialog("Recomeco", "Falha ao copiar cena do junkyard.", "OK");
            return;
        }

        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.OpenScene(FerroVelhoScenePath, OpenSceneMode.Single);

        EnsureSellZone();
        EnsureSpawn("EntradaFerroVelho", FerroVelhoSpawnPosition, "Ponto de entrada na cena do ferro velho");
        FerroVelhoSceneGround.EnsureSceneGroundColliders(scene);
        FerroVelhoSceneColliders.EnsureColliders(scene);
        EnsureWalkableGround();
        RepositionFerroVelhoSellZone();
        EnsureReturnPortal();

        if (Object.FindFirstObjectByType<MoneyManager>() == null)
            CreateMoneyManager();

        EnsurePlayerInScene();
        CompleteGameplayInActiveScene(silent: true);

        EditorSceneManager.MarkSceneDirty(scene);
        AddScenesToBuildSettings();
        EditorUtility.DisplayDialog("Recomeco",
            "Cena FerroVelho criada.\n\n" +
            "• Ajuste a posição de EntradaFerroVelho e Portal_VoltaCidade no Scene.\n" +
            "• Na cidade: Recomeco → Cenas → Portal para FerroVelho.\n" +
            "• Build Settings atualizado. Salve (Ctrl+S).",
            "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Portal para FerroVelho (cena ativa)")]
    static void AddPortalToFerroVelho()
    {
        var view = SceneView.lastActiveSceneView;
        var pos = view != null
            ? view.camera.transform.position + view.camera.transform.forward * 6f
            : Vector3.zero;

        var portal = CreatePortalObject("Portal_FerroVelho", pos);
        var zone = portal.GetComponent<SceneTransitionZone>();
        zone.targetSceneName = RecomecoSceneNames.FerroVelho;
        zone.targetSpawnId = "EntradaFerroVelho";
        zone.messageNear = "Aperte E para ir ao ferro velho";

        EnsureSpawnInActiveScene("EntradaCidade", pos + Vector3.back * 2f,
            "Onde o jogador aparece ao voltar do ferro velho");

        AddScenesToBuildSettings();
        MarkDirty();
        Selection.activeGameObject = portal;
        EditorUtility.DisplayDialog("Recomeco",
            "Portal_FerroVelho criado.\n\nMova para a entrada do ferro velho na cidade e salve a cena.",
            "OK");
    }

    [MenuItem(MenuRoot + "Cidade/Configurar spawn moradia inicial (Lugar abandonado)")]
    static void EnsureMoradiaInitialSpawn()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != RecomecoSceneNames.Cidade)
        {
            if (!File.Exists(CidadeScenePath))
            {
                EditorUtility.DisplayDialog("Recomeco",
                    "Cena Cidade não encontrada em:\n" + CidadeScenePath,
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Recomeco",
                    "Abra a cena Cidade para colocar o spawn na moradia inicial?",
                    "Abrir Cidade", "Cancelar"))
                return;

            scene = EditorSceneManager.OpenScene(CidadeScenePath, OpenSceneMode.Single);
        }

        var lugar = GameObject.Find("Lugar abandonado");
        if (lugar == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Não encontrei o objeto \"Lugar abandonado\" na Hierarchy.\n\n" +
                "Crie a pasta com esse nome onde montou os props e tente de novo.",
                "OK");
            return;
        }

        SceneSpawnPoint existing = null;
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp != null && sp.spawnId == RecomecoSceneNames.MoradiaInicial)
            {
                existing = sp;
                break;
            }
        }

        GameObject spawnGo;
        if (existing != null)
        {
            spawnGo = existing.gameObject;
        }
        else
        {
            spawnGo = new GameObject("Spawn_" + RecomecoSceneNames.MoradiaInicial);
            Undo.RegisterCreatedObjectUndo(spawnGo, "Create moradia spawn");
            var point = Undo.AddComponent<SceneSpawnPoint>(spawnGo);
            point.spawnId = RecomecoSceneNames.MoradiaInicial;
        }

        Undo.SetTransformParent(spawnGo.transform, lugar.transform, "Parent moradia spawn");
        spawnGo.transform.localPosition = Vector3.zero;
        spawnGo.transform.localRotation = Quaternion.identity;

        Selection.activeGameObject = spawnGo;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("Recomeco",
            "Spawn da moradia inicial configurado em \"Lugar abandonado\".\n\n" +
            "• Mova o filho Spawn_MoradiaInicial para o ponto exato onde o jogador deve aparecer.\n" +
            "• Gire a seta (eixo azul) para a direção inicial da câmera.\n" +
            "• Salve a cena (Ctrl+S).\n\n" +
            "Ao iniciar o jogo pela Cidade, o player nasce aqui.",
            "OK");
    }

    [MenuItem(MenuRoot + "Cidade/Ajustar moradia (escala + colisão)")]
    static void FixMoradiaScaleAndColliders()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != RecomecoSceneNames.Cidade)
        {
            if (!File.Exists(CidadeScenePath))
            {
                EditorUtility.DisplayDialog("Recomeco",
                    "Cena Cidade não encontrada em:\n" + CidadeScenePath,
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Recomeco",
                    "Abra a cena Cidade para ajustar a moradia inicial?",
                    "Abrir Cidade", "Cancelar"))
                return;

            scene = EditorSceneManager.OpenScene(CidadeScenePath, OpenSceneMode.Single);
        }

        var lugar = GameObject.Find(StreetPropsSceneColliders.MoradiaRootName);
        if (lugar == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Não encontrei \"Lugar abandonado\" na Hierarchy.",
                "OK");
            return;
        }

        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(GameplaySettingsPath);
        var scale = settings != null ? settings.playerScaleCity : 0.2f;

        Undo.RecordObject(lugar.transform, "Scale moradia");
        lugar.transform.localScale = Vector3.one * scale;

        var colliders = EnsureMoradiaCollidersWithUndo(lugar.transform);
        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("Recomeco",
            $"Moradia ajustada.\n\n" +
            $"• Escala de \"Lugar abandonado\": {scale:0.##}\n" +
            $"• Mesh Collider adicionado em {colliders} objeto(s)\n\n" +
            "Salve a cena (Ctrl+S) e teste no Play.",
            "OK");
    }

    static int EnsureMoradiaCollidersWithUndo(Transform root)
    {
        var count = 0;
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter == null || filter.sharedMesh == null)
                continue;

            var go = filter.gameObject;
            if (go.GetComponent<MeshRenderer>() == null)
                continue;
            if (go.GetComponent<SceneSpawnPoint>() != null)
                continue;

            var hasSolid = false;
            foreach (var col in go.GetComponents<Collider>())
            {
                if (col != null && col.enabled && !col.isTrigger)
                {
                    hasSolid = true;
                    break;
                }
            }

            if (hasSolid)
                continue;

            var collider = go.GetComponent<MeshCollider>();
            if (collider == null)
                collider = Undo.AddComponent<MeshCollider>(go);
            else
                Undo.RecordObject(collider, "Update moradia collider");

            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            collider.enabled = true;
            Undo.RecordObject(go, "Mark moradia prop static");
            go.isStatic = true;
            count++;
        }

        return count;
    }

    [MenuItem(MenuRoot + "Cenas/Portal voltar à cidade (cena FerroVelho)")]
    static void AddReturnPortalInFerroVelho()
    {
        EnsureReturnPortal();
        MarkDirty();
        EditorUtility.DisplayDialog("Recomeco",
            "Portal_VoltaCidade configurado.\n\nTarget Scene Name deve ser Cidade (nome da cena em Assets/Scenes/Cidade.unity).",
            "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Completar gameplay (câmera + player na cena ativa)")]
    static void CompleteGameplayMenu()
    {
        CompleteGameplayInActiveScene(silent: false);
    }

    [MenuItem(MenuRoot + "Cenas/Adicionar colliders nos objetos (ferro velho)")]
    static void AddPropCollidersToFerroVelho()
    {
        var scene = SceneManager.GetActiveScene();
        if (!FerroVelhoWalkableGround.IsFerroVelhoScene(scene) &&
            !FerroVelhoWalkableGround.HasJunkyardFerroVelhoLayout(scene))
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Abra a cena FerroVelho (ou junkyard Demo) antes de usar este menu.",
                "OK");
            return;
        }

        FerroVelhoSceneGround.EnsureSceneGroundColliders(scene);
        var count = FerroVelhoSceneColliders.EnsureColliders(scene);
        MarkDirty();
        EditorUtility.DisplayDialog("Recomeco",
            $"Mesh Collider adicionado em {count} objeto(s) sob Geometry (Props/Buildings).\n\n" +
            "Salve a cena (Ctrl+S) para guardar na cena.\n" +
            "Se algum objeto ainda não colidir, ative Read/Write no mesh (Import Settings).",
            "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Ativar chão da cena (Mesh Collider no terreno)")]
    static void EnableSceneTerrainGround()
    {
        var count = FerroVelhoSceneGround.EnsureSceneGroundColliders();
        var chao = GameObject.Find("Chao_FerroVelho");
        if (chao != null)
        {
            Undo.RecordObject(chao, "Disable fallback ground");
            chao.SetActive(false);
        }

        RepositionSpawnPointsForFerroVelho();
        MarkDirty();
        EditorUtility.DisplayDialog("Recomeco",
            count > 0
                ? "Mesh Collider adicionado no terreno (Geometry → Terrain → default).\n\n" +
                  "• Chao_FerroVelho foi desativado (chão invisível de reserva).\n" +
                  "• Confira se o objeto está marcado como Static.\n" +
                  "• Salve a cena (Ctrl+S)."
                : "Não encontrei Geometry/Terrain na cena.\n\nSelecione o mesh do chão, Add Component → Mesh Collider, desmarque Convex.",
            "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Adicionar chão Chao_FerroVelho (cena junkyard/Demo aberta)")]
    static void AddGroundToOpenScene()
    {
        EnsureWalkableGround();
        RepositionSpawnPointsForFerroVelho();
        MarkDirty();
        EditorUtility.DisplayDialog("Recomeco",
            "Chao_FerroVelho adicionado (ou já existia).\n\nProcure na Hierarchy por 'Chao_FerroVelho'.\nSalve a cena (Ctrl+S).",
            "OK");
    }

    [MenuItem(MenuRoot + "Cenas/Registar cenas no Build Settings")]
    static void AddScenesToBuildSettingsMenu()
    {
        AddScenesToBuildSettings();
        EditorUtility.DisplayDialog("Recomeco", "Build Settings atualizado com cena ativa + FerroVelho.", "OK");
    }

    [MenuItem(MenuRoot + "Player/Corrigir locomoção (ithappy)")]
    static void FixPlayerLocomotion()
    {
        CompleteGameplayInActiveScene(silent: false);
    }

    static void EnsureReturnPortal()
    {
        var existing = GameObject.Find("Portal_VoltaCidade");
        if (existing != null)
        {
            var existingZone = existing.GetComponent<SceneTransitionZone>();
            if (existingZone != null && SceneManager.GetActiveScene().name == RecomecoSceneNames.FerroVelho)
            {
                Undo.RecordObject(existingZone, "Update return portal");
                existingZone.targetSceneName = RecomecoSceneNames.Cidade;
            }

            return;
        }

        var view = SceneView.lastActiveSceneView;
        var pos = view != null
            ? view.camera.transform.position + view.camera.transform.forward * 4f
            : new Vector3(5f, 0f, 5f);

        var portal = CreatePortalObject("Portal_VoltaCidade", pos);
        var zone = portal.GetComponent<SceneTransitionZone>();
        zone.targetSceneName = SceneManager.GetActiveScene().name == RecomecoSceneNames.FerroVelho
            ? RecomecoSceneNames.Cidade
            : SceneManager.GetActiveScene().name;
        zone.targetSpawnId = "EntradaCidade";
        zone.messageNear = "Aperte E para voltar à cidade";
    }

    static string GuessCitySceneName()
    {
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled || string.IsNullOrEmpty(scene.path))
                continue;
            var name = Path.GetFileNameWithoutExtension(scene.path);
            if (name != "FerroVelho" && name.IndexOf("Ferro", System.StringComparison.OrdinalIgnoreCase) < 0)
                return name;
        }

        return RecomecoSceneNames.CityDemo;
    }

    static GameObject CreatePortalObject(string objectName, Vector3 position)
    {
        var go = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(go, "Create Portal");
        go.transform.position = position;

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(3f, 2.5f, 3f);
        box.center = new Vector3(0f, 1.25f, 0f);

        go.AddComponent<SceneTransitionZone>();
        return go;
    }

    static void EnsureSellZone()
    {
        var sellGo = GameObject.Find("FerroVelho_Venda") ?? GameObject.Find("FerroVelho");
        if (sellGo == null)
        {
            sellGo = new GameObject("FerroVelho_Venda");
            Undo.RegisterCreatedObjectUndo(sellGo, "Create Sell Zone");
            sellGo.transform.position = Vector3.zero;
        }

        if (sellGo.GetComponent<SellItems>() == null)
        {
            var sell = Undo.AddComponent<SellItems>(sellGo);
            sell.itemName = "Latinha";
            sell.pricePerUnit = 2;
            sell.sellDistance = 5f;
            sell.messageNear = "Aperte E para vender";
        }
    }

    static void EnsureSpawn(string spawnId, Vector3 position, string label)
    {
        SceneSpawnPoint existing = null;
        foreach (var sp in Object.FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
        {
            if (sp.spawnId != spawnId)
                continue;
            existing = sp;
            break;
        }

        if (existing != null)
        {
            Undo.RecordObject(existing.transform, "Move Spawn");
            existing.transform.position = position;
            return;
        }

        var go = new GameObject("Spawn_" + spawnId);
        Undo.RegisterCreatedObjectUndo(go, "Create Spawn");
        go.transform.position = position;
        var point = Undo.AddComponent<SceneSpawnPoint>(go);
        point.spawnId = spawnId;
    }

    static void RepositionSpawnPointsForFerroVelho()
    {
        var scene = SceneManager.GetActiveScene();
        if (!FerroVelhoWalkableGround.IsFerroVelhoScene(scene))
            return;
        EnsureSpawn("EntradaFerroVelho", FerroVelhoSpawnPosition, "");
    }

    static void RepositionFerroVelhoSellZone()
    {
        var sell = GameObject.Find("FerroVelho_Venda");
        if (sell == null)
            return;
        Undo.RecordObject(sell.transform, "Move sell zone");
        sell.transform.position = FerroVelhoSpawnPosition + new Vector3(6f, 0f, 4f);
    }

    static void EnsureWalkableGround()
    {
        var scene = SceneManager.GetActiveScene();
        if (!FerroVelhoWalkableGround.IsFerroVelhoScene(scene) &&
            !FerroVelhoWalkableGround.HasJunkyardFerroVelhoLayout(scene))
            return;
        var groundCenter = new Vector3(
            FerroVelhoSpawnPosition.x,
            FerroVelhoSpawnPosition.y - 1f,
            FerroVelhoSpawnPosition.z);
        var ground = GameObject.Find("Chao_FerroVelho");
        if (ground != null)
        {
            Undo.RecordObject(ground.transform, "Move ground");
            ground.transform.position = groundCenter;
            var existingBox = ground.GetComponent<BoxCollider>();
            if (existingBox != null)
            {
                Undo.RecordObject(existingBox, "Resize ground");
                existingBox.size = new Vector3(140f, 2f, 140f);
            }
            return;
        }

        ground = new GameObject("Chao_FerroVelho");
        Undo.RegisterCreatedObjectUndo(ground, "Create ground");
        ground.transform.position = groundCenter;
        ground.isStatic = true;
        var box = ground.AddComponent<BoxCollider>();
        box.size = new Vector3(140f, 2f, 140f);
    }

    static void ConvertJunkyardMaterialsToUrp()
    {
        var scene = SceneManager.GetActiveScene();
        if (!FerroVelhoWalkableGround.IsFerroVelhoScene(scene))
            return;

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
            return;

        var guids = AssetDatabase.FindAssets("t:Material", new[] { JunkyardRoot });
        var converted = 0;
        foreach (var guid in guids)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat != null && ConvertJunkyardMaterial(mat, urpLit))
            {
                EditorUtility.SetDirty(mat);
                converted++;
            }
        }

        if (converted > 0)
            Debug.Log($"[FerroVelho] {converted} materiais do junkyard convertidos para URP.");
    }

    static bool ConvertJunkyardMaterial(Material mat, Shader urpLit)
    {
        var shaderName = mat.shader != null ? mat.shader.name : "";
        if (shaderName.StartsWith("Universal Render Pipeline/") || shaderName.Contains("Skybox"))
            return false;

        var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
        var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
        mat.shader = urpLit;
        if (mainTex != null && mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", mainTex);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        return true;
    }

    static void EnsureSpawnInActiveScene(string spawnId, Vector3 position, string _)
    {
        EnsureSpawn(spawnId, position, spawnId);
    }

    static void CreateMoneyManager()
    {
        var go = new GameObject("MoneyManager");
        Undo.RegisterCreatedObjectUndo(go, "Create MoneyManager");
        Undo.AddComponent<MoneyManager>(go);
    }

    static void CompleteGameplayInActiveScene(bool silent)
    {
        EnsureGameplaySettingsAsset();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            EnsurePlayerInScene();
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player == null)
        {
            if (!silent)
                EditorUtility.DisplayDialog("Recomeco", "Não há Player na cena.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(player, "Complete Gameplay");

        FixPlayerLocomotionInternal(player);
        var cam = EnsureGameplayCamera(player.transform);
        WirePlayerCamera(player, cam);
        DisableStaticDemoCameras(cam.transform);

        var settings = AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(GameplaySettingsPath);
        if (settings != null)
        {
            PlayerAnimatorSetup.Apply(player, settings);
            var mover = player.GetComponent<CharacterMover>();
            if (mover != null)
                mover.SetLocomotionSpeeds(settings.walkSpeed, settings.runSpeed, settings.rotateSpeed);
        }

        if (player.GetComponent<CharacterGroundSnap>() == null)
            Undo.AddComponent<CharacterGroundSnap>(player);

        EnsureWalkableGround();
        RepositionSpawnPointsForFerroVelho();
        ConvertJunkyardMaterialsToUrp();

        MarkDirty();
        if (!silent)
            EditorUtility.DisplayDialog("Recomeco",
                "Gameplay configurado:\n• Câmera third-person (GameplayCamera)\n• Player com animação e velocidades\n• Câmeras estáticas do demo desativadas\n\nSalve a cena (Ctrl+S).",
                "OK");
    }

    static void FixPlayerLocomotionInternal(GameObject player)
    {
        var legacy = player.GetComponent<PlayerMovement>();
        if (legacy != null)
            Undo.DestroyObjectImmediate(legacy);

        var rootAnim = player.GetComponent<Animator>();
        Animator meshAnim = null;
        foreach (var a in player.GetComponentsInChildren<Animator>(true))
        {
            if (a == rootAnim)
                continue;
            if (a.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                meshAnim = a;
                break;
            }
        }

        if (rootAnim != null && meshAnim != null && rootAnim != meshAnim)
            Undo.DestroyObjectImmediate(rootAnim);

        if (player.GetComponent<CharacterMover>() == null)
            Undo.AddComponent<CharacterMover>(player);
        if (player.GetComponent<MovePlayerInput>() == null)
            Undo.AddComponent<MovePlayerInput>(player);
        if (player.GetComponent<PlayerLocomotionBootstrap>() == null)
            Undo.AddComponent<PlayerLocomotionBootstrap>(player);
    }

    static PlayerCamera EnsureGameplayCamera(Transform player)
    {
        var existing = Object.FindFirstObjectByType<PlayerCamera>();
        if (existing != null)
        {
            existing.SetPlayer(player);
            return existing;
        }

        var go = new GameObject("GameplayCamera");
        Undo.RegisterCreatedObjectUndo(go, "Create Gameplay Camera");
        go.tag = "MainCamera";
        go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        TryAddUrpCameraData(go);
        var thirdPerson = Undo.AddComponent<ThirdPersonCamera>(go);
        thirdPerson.SetPlayer(player);

        var scale = Mathf.Max(0.15f, player.lossyScale.y);
        go.transform.position = player.position + Vector3.up * (1.5f * scale) + Vector3.back * (4f * scale);
        if (player != null)
            go.transform.LookAt(player.position + Vector3.up * (1.2f * scale));

        return thirdPerson;
    }

    static void WirePlayerCamera(GameObject player, PlayerCamera cam)
    {
        if (cam == null)
            return;

        var input = player.GetComponent<MovePlayerInput>();
        if (input != null)
            input.BindPlayerCamera(cam);
        else
            cam.SetPlayer(player.transform);
    }

    static void DisableStaticDemoCameras(Transform keepCamera)
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam.transform == keepCamera || cam.transform.IsChildOf(keepCamera))
                continue;
            if (cam.GetComponent<PlayerCamera>() != null)
                continue;

            Undo.RecordObject(cam, "Disable demo camera");
            cam.enabled = false;
            var listener = cam.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }
    }

    static void TryAddUrpCameraData(GameObject go)
    {
        var urpType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null && go.GetComponent(urpType) == null)
            Undo.AddComponent(go, urpType);
    }

    static void EnsureGameplaySettingsAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<RecomecoGameplaySettings>(GameplaySettingsPath) != null)
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var settings = ScriptableObject.CreateInstance<RecomecoGameplaySettings>();
        settings.movementController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MovementControllerPath);
        settings.playerAvatar = FindPlayerAvatar();
        settings.walkSpeed = 3f;
        settings.runSpeed = 8f;
        settings.jumpHeight = 2.5f;
        settings.rotateSpeed = 200f;

        AssetDatabase.CreateAsset(settings, GameplaySettingsPath);
        AssetDatabase.SaveAssets();
    }

    static Avatar FindPlayerAvatar()
    {
        const string meshPath = "Assets/ithappy/Creative_Characters_FREE/Meshes/Base_Mesh.fbx";
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(meshPath))
        {
            if (asset is Avatar avatar)
                return avatar;
        }

        return null;
    }

    static void EnsurePlayerInScene()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null)
            return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseMeshPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("SceneTransitionSetup: Base_Mesh.prefab não encontrado.");
            return;
        }

        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(player, "Add Player");
        player.name = "Player";
        player.tag = "Player";
        player.transform.localScale = Vector3.one * 0.2f;

        if (player.GetComponent<CharacterController>() == null)
            player.AddComponent<CharacterController>();
        if (player.GetComponent<CharacterMover>() == null)
            player.AddComponent<CharacterMover>();
        if (player.GetComponent<MovePlayerInput>() == null)
            player.AddComponent<MovePlayerInput>();
        if (player.GetComponent<PlayerLocomotionBootstrap>() == null)
            player.AddComponent<PlayerLocomotionBootstrap>();
        if (player.GetComponent<Inventory>() == null)
            player.AddComponent<Inventory>();
    }

    static void AddScenesToBuildSettings()
    {
        var paths = new System.Collections.Generic.List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path))
                paths.Add(s.path);
        }

        void AddIfExists(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;
            if (!paths.Contains(path))
                paths.Add(path);
        }

        AddIfExists("Assets/Scenes/MenuInicial.unity");
        AddIfExists(FerroVelhoScenePath);
        AddIfExists(CidadeScenePath);
        var active = SceneManager.GetActiveScene().path;
        AddIfExists(active);

        paths.RemoveAll(p => p != null && (
            p.Replace('\\', '/').EndsWith("/Flat_Style_Vehicles/Demo/Demo.unity", System.StringComparison.OrdinalIgnoreCase) ||
            p.Replace('\\', '/').EndsWith("/Demo/Demo.unity", System.StringComparison.OrdinalIgnoreCase)));

        const string menuPath = "Assets/Scenes/MenuInicial.unity";
        if (paths.Remove(menuPath))
            paths.Insert(0, menuPath);

        var scenes = new EditorBuildSettingsScene[paths.Count];
        for (int i = 0; i < paths.Count; i++)
            scenes[i] = new EditorBuildSettingsScene(paths[i], true);

        EditorBuildSettings.scenes = scenes;
    }

    static void MarkDirty()
    {
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
#endif
