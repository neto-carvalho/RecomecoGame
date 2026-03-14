using UnityEngine;
using System.Collections;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager instance;

    public GameObject latinhaPrefab;

    [Tooltip("Quantidade de latinhas ao iniciar a cena")]
    public int quantidadeInicial = 10;
    [Tooltip("Raio da área de spawn (em unidades)")]
    public float areaSpawn = 20f;
    [Tooltip("Segundos para uma nova latinha aparecer após uma ser coletada")]
    public float tempoRespawn = 8f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        for (int i = 0; i < quantidadeInicial; i++)
        {
            SpawnLatinha();
        }
    }

    public void SpawnLatinha()
    {
        Vector3 posicao = new Vector3(
            UnityEngine.Random.Range(-areaSpawn, areaSpawn),
            1,
            UnityEngine.Random.Range(-areaSpawn, areaSpawn)
        );

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