using UnityEngine;

/// <summary>
/// Sistema central de dinheiro do jogador (Fase 5 do roadmap).
/// Singleton: use MoneyManager.instance para acessar.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager instance;

    [Tooltip("Dinheiro inicial do jogador (ex: 420 conforme GDD)")]
    public int initialMoney = 420;

    private int money;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            money = initialMoney;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Adiciona valor ao dinheiro atual.</summary>
    public void AddMoney(int amount)
    {
        if (amount > 0)
            money += amount;
    }

    /// <summary>Remove valor do dinheiro. Retorna true se tinha saldo suficiente.</summary>
    public bool RemoveMoney(int amount)
    {
        if (amount <= 0) return true;
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    /// <summary>Retorna o dinheiro atual do jogador.</summary>
    public int GetMoney()
    {
        return money;
    }
}
