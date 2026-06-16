using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    const string MoneyRowChildName = "MoneyRow";
    const string MoneyAmountChildName = "MoneyAmount";
    const string MoneyIconChildName = "MoneyIcon";

    TextMeshProUGUI _moneyText;
    Image _moneyIcon;

    void Awake()
    {
        ResolveReferences();
    }

    void Start()
    {
        ResolveReferences();
        RefreshMoneyText();
    }

    void ResolveReferences()
    {
        if (_moneyText == null)
        {
            var amount = transform.Find(MoneyRowChildName + "/" + MoneyAmountChildName);
            if (amount == null)
                amount = transform.Find(MoneyAmountChildName);

            _moneyText = amount != null
                ? amount.GetComponent<TextMeshProUGUI>()
                : GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (_moneyIcon == null)
        {
            var icon = transform.Find(MoneyRowChildName + "/" + MoneyIconChildName);
            if (icon == null)
                icon = transform.Find(MoneyIconChildName);

            if (icon != null)
                _moneyIcon = icon.GetComponent<Image>();
        }
    }

    void Update()
    {
        RefreshMoneyText();
    }

    void RefreshMoneyText()
    {
        if (_moneyText == null)
            return;

        var cents = MoneyManager.instance != null ? MoneyManager.instance.GetMoney() : 0;
        _moneyText.text = MoneyManager.FormatBRL(cents);
    }

    public void ConfigureMoneyDisplay(TextMeshProUGUI moneyText, Image moneyIcon, Sprite sprite)
    {
        _moneyText = moneyText;
        _moneyIcon = moneyIcon;

        if (_moneyIcon != null && sprite != null)
        {
            _moneyIcon.sprite = sprite;
            _moneyIcon.preserveAspect = true;
            _moneyIcon.enabled = true;
        }

        RefreshMoneyText();
    }
}
