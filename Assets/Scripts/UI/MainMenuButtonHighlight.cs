using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Destaca o botão do menu ao passar o mouse (estilo do mockup).
/// </summary>
public class MainMenuButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] MainMenuController menu;
    [SerializeField] int buttonIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menu != null)
            menu.OnMenuButtonHoverEnter(buttonIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (menu != null)
            menu.OnMenuButtonHoverExit();
    }
}
