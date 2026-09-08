using UnityEngine;

public class BedSaveInteract : MonoBehaviour
{
    [Tooltip("Distância máxima para dormir/salvar")]
    public float interactDistance = 2.5f;

    [Tooltip("Spawn usado ao continuar depois de salvar na cama")]
    public string saveSpawnId = RecomecoSceneNames.EntradaCasaElegante;

    public KeyCode interactKey = KeyCode.E;

    float _feedbackTimer;

    void OnDisable()
    {
        _feedbackTimer = 0f;
        InteractionUI.HideMessage(this);
    }

    void Update()
    {
        var player = InteractionProximity.GetPlayer();
        var playerTransform = player != null ? player.transform : null;
        var inRange = InteractionProximity.IsWithinRange(transform.position, interactDistance, playerTransform);

        if (!inRange)
        {
            if (_feedbackTimer > 0f)
                _feedbackTimer = 0f;
            InteractionUI.HideMessage(this);
            return;
        }

        if (_feedbackTimer > 0f)
        {
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f)
                ShowPrompt();
            return;
        }

        ShowPrompt();

        if (!Input.GetKeyDown(interactKey))
            return;

        if (SaveGameManager.SaveCurrentGame(saveSpawnId))
            ShowFeedback("Jogo salvo. Boa noite!");
        else
            ShowFeedback("Não foi possível salvar.");
    }

    void OnDestroy()
    {
        InteractionUI.HideMessage(this);
    }

    void ShowPrompt()
    {
        InteractionUI.ShowMessage("Aperte E para dormir e salvar o jogo", this);
    }

    void ShowFeedback(string message)
    {
        _feedbackTimer = 2.2f;
        InteractionUI.ShowMessage(message, this);
    }
}
