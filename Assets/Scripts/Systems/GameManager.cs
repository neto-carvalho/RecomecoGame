using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public int latinhas = 0;
    public int dinheiro = 0;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddLatinha()
    {
        latinhas++;
    }

    public void VenderLatinhas()
    {
        int valor = latinhas * 2;

        dinheiro += valor;
        latinhas = 0;

        UnityEngine.Debug.Log("Dinheiro total: $" + dinheiro);
    }
}