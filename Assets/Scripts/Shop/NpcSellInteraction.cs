using UnityEngine;

/// <summary>
/// Permite vender para o NPC: perto dele, E abre o minigame de precisão (SellMinigameUI).
/// Adicionado em runtime pelo CartoonLowPolyCityLiteNpcBootstrap.
/// </summary>
public class NpcSellInteraction : MonoBehaviour
{
    [Tooltip("Distância máxima para interagir")]
    public float interactDistance = 3f;

    SidewalkNpcWalker _walker;
    bool _playerWasInRange;

    void Awake()
    {
        _walker = GetComponent<SidewalkNpcWalker>();
    }

    void Update()
    {
        if (SellMinigameUI.IsOpen)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        var inRange = player != null &&
                      Vector3.Distance(player.transform.position, transform.position) < interactDistance;

        if (inRange && !_playerWasInRange)
            InteractionUI.ShowMessage("Aperte E para vender ao pedestre", this);
        if (!inRange && _playerWasInRange)
            InteractionUI.HideMessage(this);
        _playerWasInRange = inRange;

        if (!inRange || player == null)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            InteractionUI.HideMessage(this);
            SellMinigameUI.Open(_walker, player);
        }
    }

    void OnDisable()
    {
        InteractionUI.HideMessage(this);
    }
}
