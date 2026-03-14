using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    public UnityEngine.UI.Image icon;
    public TextMeshProUGUI quantityText;

    void Start()
    {
        icon.enabled = false;

        if (quantityText != null)
        {
            quantityText.text = "";
        }
    }

    public void SetItem(ItemData item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            icon.enabled = false;
            if (quantityText != null) quantityText.text = "";
            return;
        }

        icon.sprite = item.icon;
        icon.enabled = true;

        if (quantity > 1)
            quantityText.text = quantity.ToString();
        else
            quantityText.text = "";
    }
}