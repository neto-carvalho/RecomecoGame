using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNeedsHud : MonoBehaviour
{
    Image _healthFill;
    Image _hungerFill;
    Image _reputationFill;
    TextMeshProUGUI _healthLabel;
    TextMeshProUGUI _hungerLabel;
    TextMeshProUGUI _reputationLabel;

    void OnEnable()
    {
        PlayerNeeds.Changed += Refresh;
    }

    void OnDisable()
    {
        PlayerNeeds.Changed -= Refresh;
    }

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        if (PlayerNeeds.Instance != null)
            Refresh();
    }

    public void Wire(
        Image healthFill,
        Image hungerFill,
        Image reputationFill,
        TextMeshProUGUI healthLabel,
        TextMeshProUGUI hungerLabel,
        TextMeshProUGUI reputationLabel)
    {
        _healthFill = healthFill;
        _hungerFill = hungerFill;
        _reputationFill = reputationFill;
        _healthLabel = healthLabel;
        _hungerLabel = hungerLabel;
        _reputationLabel = reputationLabel;
        Refresh();
    }

    void Refresh()
    {
        var needs = PlayerNeeds.Instance;
        if (needs == null)
        {
            SetBar(_healthFill, _healthLabel, 1f, "Vida");
            SetBar(_hungerFill, _hungerLabel, 1f, "Fome");
            SetBar(_reputationFill, _reputationLabel, 1f, "Rep.");
            return;
        }

        SetBar(_healthFill, _healthLabel, needs.Health / PlayerNeeds.MaxHealth, "Vida");
        SetBar(_hungerFill, _hungerLabel, needs.Hunger / PlayerNeeds.MaxHunger, "Fome");
        SetBar(_reputationFill, _reputationLabel, needs.Reputation / PlayerNeeds.MaxReputation, "Rep.");
    }

    static void SetBar(Image fill, TextMeshProUGUI label, float normalized, string title)
    {
        normalized = Mathf.Clamp01(normalized);
        if (fill != null)
        {
            fill.type = Image.Type.Simple;
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(normalized, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        if (label != null)
            label.text = title + " " + Mathf.RoundToInt(normalized * 100f) + "%";
    }
}
