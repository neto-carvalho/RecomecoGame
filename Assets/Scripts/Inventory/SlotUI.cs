using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [Tooltip("Imagem que exibe o ícone do item (deixe vazio para usar o primeiro Image filho)")]
    public Image icon;
    [Tooltip("Texto que exibe a quantidade no canto do slot (deixe vazio para usar o primeiro Text filho)")]
    public TextMeshProUGUI quantityText;

    void Awake()
    {
        ResolveReferences();
    }

    void Start()
    {
        if (icon != null) icon.enabled = false;
        if (quantityText != null) quantityText.text = "";
    }

    void ResolveReferences()
    {
        // Referência de outro Slot (copiada ao duplicar)? Descarta.
        if (icon != null && icon.transform.parent != transform) icon = null;
        if (quantityText != null && quantityText.transform.parent != transform) quantityText = null;

        // Sempre pega Icon e Quantity dos filhos DESTE Slot (por tipo, não por nome)
        if (icon == null || quantityText == null)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (icon == null)
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null) icon = img;
                }
                if (quantityText == null)
                {
                    TextMeshProUGUI txt = child.GetComponent<TextMeshProUGUI>();
                    if (txt != null) quantityText = txt;
                }
            }
        }
        // Fallback: GetComponentsInChildren (inclui o Image do próprio Slot se for fundo)
        if (icon == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            if (images != null && images.Length > 1) icon = images[1];
            else if (images != null && images.Length == 1) icon = images[0];
        }
        if (quantityText == null)
            quantityText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void SetItem(ItemData item, int quantity)
    {
        ResolveReferences();

        if (item == null || quantity <= 0)
        {
            if (icon != null)
            {
                icon.sprite = null;
                icon.enabled = false;
            }
            if (quantityText != null) quantityText.text = "";
            return;
        }

        if (icon != null)
        {
            icon.sprite = item.icon;
            icon.enabled = item.icon != null;
            icon.preserveAspect = true;
            icon.color = Color.white;
            if (icon.gameObject.activeSelf == false)
                icon.gameObject.SetActive(true);
            if (item.icon == null)
                UnityEngine.Debug.LogWarning("SlotUI: Item '" + item.itemName + "' não tem ícone definido. Atribua um Sprite no ScriptableObject.");
        }

        if (quantityText != null)
        {
            quantityText.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            if (quantityText.gameObject.activeSelf == false)
                quantityText.gameObject.SetActive(true);
            if (quantity > 1)
                quantityText.text = quantity.ToString();
            else
                quantityText.text = "";
        }
    }
}