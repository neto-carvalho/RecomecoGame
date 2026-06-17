#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class NatureTerrainSetupMenu
{
    const string OutputFolder = "Assets/Terrain/NatureZone";
    const string GrassLayerPath = "Assets/TerrainSampleAssets/TerrainLayers/Grass_A_TerrainLayer.terrainlayer";
    const string DirtLayerPath = "Assets/TerrainSampleAssets/TerrainLayers/Muddy_TerrainLayer.terrainlayer";
    const string GrassDryLayerPath = "Assets/TerrainSampleAssets/TerrainLayers/Grass_Dry_TerrainLayer.terrainlayer";

    [MenuItem("Recomeco/Terreno/Criar zona natureza (mato + estrada + lago)")]
    static void CreateNatureZone()
    {
        EnsureFolder(OutputFolder);

        var grass = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrassLayerPath);
        var dirt = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DirtLayerPath);
        var grassDry = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrassDryLayerPath);

        if (grass == null || dirt == null)
        {
            EditorUtility.DisplayDialog(
                "Terreno natureza",
                "Não encontrei as Terrain Layers em TerrainSampleAssets.\n" +
                "Confirma que existem:\n" + GrassLayerPath + "\n" + DirtLayerPath,
                "OK");
            return;
        }

        var layers = grassDry != null
            ? new[] { grass, grassDry, dirt }
            : new[] { grass, dirt };

        const int size = 256;
        const float terrainWorldSize = 200f;
        var dataPath = $"{OutputFolder}/NatureTerrainData.asset";
        var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if (terrainData == null)
        {
            terrainData = new TerrainData
            {
                heightmapResolution = size + 1,
                alphamapResolution = 512,
                baseMapResolution = 1024,
                size = new Vector3(terrainWorldSize, 40f, terrainWorldSize),
            };
            AssetDatabase.CreateAsset(terrainData, dataPath);
        }

        terrainData.terrainLayers = layers;
        ApplyGentleHillsAndLakeBowl(terrainData);
        ApplyBasePaint(terrainData, layers.Length);

        var pivot = Selection.activeTransform != null
            ? Selection.activeTransform.position
            : new Vector3(0f, 0f, 0f);
        pivot.y = 0f;

        var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
        terrainGo.name = "NatureTerrain_MatoEstradaLago";
        terrainGo.transform.position = pivot;
        terrainGo.transform.SetParent(Selection.activeTransform, true);

        CreateLakeWater(terrainGo.transform, terrainData, terrainWorldSize);

        Selection.activeGameObject = terrainGo;
        Undo.RegisterCreatedObjectUndo(terrainGo, "Create Nature Terrain");

        EditorUtility.DisplayDialog(
            "Terreno natureza criado",
            "Objeto: NatureTerrain_MatoEstradaLago\n\n" +
            "Próximos passos no Unity:\n" +
            "1. Seleciona o terreno → ícone Pincel (Paint Terrain) → Paint Texture\n" +
            "2. Pinta Muddy_TerrainLayer em faixa para a estrada de terra\n" +
            "3. Ajusta posição/escala do filho \"Lake_Water\"\n" +
            "4. Opcional: Paint Details com prefabs em TerrainSampleAssets/Prefabs\n\n" +
            "Na cena da cidade: desativa o Terrain antigo ou move este terreno para a zona desejada.",
            "OK");

        Debug.Log("[Recomeco] Terreno natureza criado em " + dataPath);
    }

    static void ApplyGentleHillsAndLakeBowl(TerrainData data)
    {
        var res = data.heightmapResolution;
        var heights = new float[res, res];
        var center = new Vector2(res * 0.72f, res * 0.28f);
        var lakeRadius = res * 0.14f;

        for (var y = 0; y < res; y++)
        {
            for (var x = 0; x < res; x++)
            {
                var nx = x / (float)(res - 1);
                var ny = y / (float)(res - 1);
                var h = 0.08f
                        + Mathf.PerlinNoise(nx * 3.2f, ny * 3.2f) * 0.12f
                        + Mathf.PerlinNoise(nx * 8f + 12f, ny * 8f) * 0.04f;

                var dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < lakeRadius)
                {
                    var t = dist / lakeRadius;
                    var bowl = Mathf.SmoothStep(0f, 1f, t);
                    h = Mathf.Lerp(0.02f, h, bowl);
                }

                heights[y, x] = h;
            }
        }

        data.SetHeights(0, 0, heights);
    }

    static void ApplyBasePaint(TerrainData data, int layerCount)
    {
        var aw = data.alphamapWidth;
        var ah = data.alphamapHeight;
        var maps = new float[ah, aw, layerCount];

        for (var y = 0; y < ah; y++)
        {
            for (var x = 0; x < aw; x++)
            {
                var u = x / (float)(aw - 1);
                var v = y / (float)(ah - 1);

                var roadCenter = 0.5f;
                var roadHalfWidth = 0.06f;
                var onRoad = Mathf.Abs(u - roadCenter) < roadHalfWidth;

                if (layerCount >= 3)
                {
                    maps[y, x, 0] = onRoad ? 0f : 0.85f;
                    maps[y, x, 1] = onRoad ? 0f : 0.15f;
                    maps[y, x, 2] = onRoad ? 1f : 0f;
                }
                else
                {
                    maps[y, x, 0] = onRoad ? 0f : 1f;
                    maps[y, x, 1] = onRoad ? 1f : 0f;
                }
            }
        }

        data.SetAlphamaps(0, 0, maps);
    }

    static void CreateLakeWater(Transform terrainRoot, TerrainData data, float worldSize)
    {
        var res = data.heightmapResolution;
        var lakeCx = (int)(res * 0.72f);
        var lakeCy = (int)(res * 0.28f);
        var h = data.GetHeight(lakeCx, lakeCy) * data.size.y;

        var lake = GameObject.CreatePrimitive(PrimitiveType.Plane);
        lake.name = "Lake_Water";
        lake.transform.SetParent(terrainRoot, false);

        var worldX = lakeCx / (float)(res - 1) * worldSize;
        var worldZ = lakeCy / (float)(res - 1) * worldSize;
        lake.transform.localPosition = new Vector3(worldX, h + 0.35f, worldZ);
        lake.transform.localRotation = Quaternion.identity;
        lake.transform.localScale = new Vector3(5f, 1f, 5f);

        Object.DestroyImmediate(lake.GetComponent<Collider>());

        var matPath = $"{OutputFolder}/LakeWater_Mat.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            mat = new Material(shader)
            {
                color = new Color(0.15f, 0.45f, 0.65f, 0.75f),
            };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            AssetDatabase.CreateAsset(mat, matPath);
        }

        lake.GetComponent<Renderer>().sharedMaterial = mat;
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
