using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager instance;

    [Tooltip("Dinheiro inicial em CENTAVOS (ex: 420 = R$ 4,20)")]
    public int initialMoney = 420;

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
            money = ResolveInitialMoneyCents();
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    static int ResolveInitialMoneyCents()
    {
        var settings = RecomecoGameplaySettings.Instance;
        if (settings != null)
            return Mathf.Max(0, settings.initialMoneyCents);

        return 420;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void AddMoney(int amount)
    {
        if (amount > 0)
            money += amount;
    }

    public bool RemoveMoney(int amount)
    {
        if (amount <= 0) return true;
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    public int GetMoney()
    {
        return money;
    }

    public void SetMoney(int amount)
    {
        money = Mathf.Max(0, amount);
    }
}
