using UnityEngine;
using System.Collections;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    public GameObject latinhaPrefab;

    [Tooltip("Centro da área de spawn no plano XZ. Se vazio, usa a posição deste GameObject (SpawnManager).")]
    public Transform areaCenter;

    [Tooltip("Quantidade de latinhas ao iniciar a cena")]
    public int quantidadeInicial = 10;
    [Tooltip("Raio da área de spawn (em unidades)")]
    public float areaSpawn = 20f;
    [Tooltip("Segundos para uma nova latinha aparecer após uma ser coletada")]
    public float tempoRespawn = 8f;

    [Header("Chão (cidade / terreno)")]
    [Tooltip("Lança um raio para baixo e posiciona a latinha no chão.")]
    public bool snapToGround = true;
    [Tooltip("Altura inicial do raio acima do ponto aleatório")]
    public float raycastStartHeight = 80f;
    [Tooltip("Distância máxima do raio para baixo")]
    public float raycastMaxDistance = 200f;
    [Tooltip("Elevação acima do ponto de impacto")]
    public float heightAboveGround = 0.35f;
    [Tooltip("Camadas consideradas como chão (deixe Everything se não souber)")]
    public LayerMask groundLayers = ~0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (latinhaPrefab == null)
        {
            UnityEngine.Debug.LogError("SpawnManager: arraste o prefab 'Latinha' no campo Latinha Prefab. Nada será spawnado até corrigir.");
            return;
        }

        for (int i = 0; i < quantidadeInicial; i++)
        {
            SpawnLatinha();
        }
    }

    public void SpawnLatinha()
    {
        if (latinhaPrefab == null)
        {
            UnityEngine.Debug.LogWarning("SpawnManager.SpawnLatinha: Latinha Prefab não configurado.");
            return;
        }

        Vector3 center = areaCenter != null ? areaCenter.position : transform.position;

        Vector3 planar = center + new Vector3(
            UnityEngine.Random.Range(-areaSpawn, areaSpawn),
            0f,
            UnityEngine.Random.Range(-areaSpawn, areaSpawn)
        );

        Vector3 posicao = planar;
        if (snapToGround)
        {
            Vector3 origin = planar + Vector3.up * raycastStartHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastMaxDistance, groundLayers,
                    QueryTriggerInteraction.Ignore))
                posicao = hit.point + Vector3.up * heightAboveGround;
            else
            {
                // Sem collider de chão no raio: usa altura do centro do spawn (evita latinhas "sumidas" longe da cidade).
                posicao = new Vector3(planar.x, center.y + heightAboveGround, planar.z);
                UnityEngine.Debug.LogWarning(
                    "SpawnManager: raio não acertou chão (falta MeshCollider no chão ou Layer errada?). Latinha colocada na altura do centro do SpawnManager. Desmarque Snap To Ground para testar.");
            }
        }
        else
            posicao = new Vector3(planar.x, center.y + Mathf.Max(0.5f, heightAboveGround), planar.z);

        Instantiate(latinhaPrefab, posicao, Quaternion.identity);
    }

    public void RespawnLatinha()
    {
        StartCoroutine(RespawnCoroutine());
    }

    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(tempoRespawn);
        SpawnLatinha();
    }
}