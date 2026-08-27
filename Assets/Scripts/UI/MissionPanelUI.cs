using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionPanelUI : MonoBehaviour
{
    static readonly Color PanelBg = new(0.08f, 0.08f, 0.1f, 0.88f);
    static readonly Color TitleColor = new(0.95f, 0.78f, 0.15f, 1f);
    static readonly Color CompleteColor = new(0.35f, 1f, 0.45f, 1f);

    TextMeshProUGUI _titleText;
    TextMeshProUGUI _descriptionText;
    TextMeshProUGUI _hintText;
    TextMeshProUGUI _progressText;
    TextMeshProUGUI _flashText;
    Image _progressFill;

    void OnEnable()
    {
        MissionProgress.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        MissionProgress.Changed -= Refresh;
    }

    void Update()
    {
        if (MissionProgress.CompletionFlashUntil > Time.unscaledTime)
            return;

        if (_flashText != null && _flashText.gameObject.activeSelf)
            Refresh();
    }

    public void Wire(
        TextMeshProUGUI titleText,
        TextMeshProUGUI descriptionText,
        TextMeshProUGUI hintText,
        TextMeshProUGUI progressText,
        TextMeshProUGUI flashText,
        Image progressFill)
    {
        _titleText = titleText;
        _descriptionText = descriptionText;
        _hintText = hintText;
        _progressText = progressText;
        _flashText = flashText;
        _progressFill = progressFill;
        Refresh();
    }

    public void Refresh()
    {
        var latinhaCount = ResolveLatinhaCount();
        var display = MissionProgress.GetDisplay(latinhaCount);
        var showCompleteFlash = MissionProgress.CompletionFlashUntil > Time.unscaledTime;

        if (_titleText != null)
            _titleText.text = display.Title;

        if (_descriptionText != null)
        {
            _descriptionText.gameObject.SetActive(!showCompleteFlash);
            _descriptionText.text = display.Description;
            _descriptionText.color = display.IsComplete ? CompleteColor : Color.white;
        }

        if (_hintText != null)
        {
            var showHint = !showCompleteFlash &&
                           !string.IsNullOrEmpty(display.Hint) &&
                           !display.IsComplete;
            _hintText.gameObject.SetActive(showHint);
            if (showHint)
                _hintText.text = display.Hint;
        }

        if (_progressText != null)
        {
            var showProgress = display.ShowProgress && !showCompleteFlash && !display.IsComplete;
            _progressText.gameObject.SetActive(showProgress);
            if (showProgress)
                _progressText.text = display.ProgressText;
        }

        if (_progressFill != null)
        {
            var showBar = display.ShowProgress && !showCompleteFlash && !display.IsComplete;
            _progressFill.transform.parent.gameObject.SetActive(showBar);
            if (showBar && display.ProgressTarget > 0)
            {
                var fill = Mathf.Clamp01((float)display.ProgressCurrent / display.ProgressTarget);
                var rect = _progressFill.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(fill, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        if (_flashText != null)
        {
            _flashText.gameObject.SetActive(showCompleteFlash);
            if (showCompleteFlash)
                _flashText.text = "Missão concluída!";
        }
    }

    static int ResolveLatinhaCount()
    {
        var player = PlayerScenePersistence.TravelingPlayer;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return 0;

        var inventory = player.GetComponent<Inventory>();
        if (inventory == null)
            inventory = Object.FindFirstObjectByType<Inventory>();
        if (inventory == null)
            return 0;

        return inventory.GetItemCount(MissionProgress.CanItemName);
    }
}
