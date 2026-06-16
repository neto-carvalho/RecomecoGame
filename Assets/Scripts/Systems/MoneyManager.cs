using UnityEngine;

/// <summary>
/// Sistema central de dinheiro do jogador (Fase 5 do roadmap).
/// Valores em CENTAVOS (R$ 4,20 = 420). Singleton: use MoneyManager.instance.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager instance;

    [Tooltip("Dinheiro inicial em CENTAVOS (ex: 420 = R$ 4,20)")]
    public int initialMoney = 420;

    /// <summary>Formata centavos como moeda (ex.: 550 → "R$ 5,50").</summary>
    public static string FormatBRL(int cents)
    {
        var sign = cents < 0 ? "-" : "";
        cents = Mathf.Abs(cents);
        return $"{sign}R$ {cents / 100},{cents % 100:00}";
    }

    private int money;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            money = initialMoney;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
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

    /// <summary>Define o saldo (restauro entre cenas).</summary>
    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
    }
}
