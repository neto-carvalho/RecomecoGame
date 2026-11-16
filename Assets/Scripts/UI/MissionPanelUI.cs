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
    TextMeshProUGUI _rewardText;
    Image _rewardIcon;
    Image _progressFill;

    static readonly Color RewardGold = new(0.95f, 0.85f, 0.35f, 1f);

    void OnEnable()
    {
        EnsureRewardRow();
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
        Image progressFill,
        TextMeshProUGUI rewardText = null,
        Image rewardIcon = null)
    {
        _titleText = titleText;
        _descriptionText = descriptionText;
        _hintText = hintText;
        _progressText = progressText;
        _flashText = flashText;
        _progressFill = progressFill;
        _rewardText = rewardText;
        _rewardIcon = rewardIcon;
        Refresh();
    }

    void EnsureRewardRow()
    {
        if (_rewardText != null)
            return;

        var existing = transform.Find("RewardRow");
        if (existing != null)
        {
            _rewardText = existing.Find("RewardLabel")?.GetComponent<TextMeshProUGUI>();
            _rewardIcon = existing.Find("RewardIcon")?.GetComponent<Image>();
            return;
        }

        var panelRect = transform as RectTransform;
        if (panelRect != null && panelRect.sizeDelta.y < 196f)
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 206f);

        var rewardGo = new GameObject("RewardRow");
        rewardGo.transform.SetParent(transform, false);
        var rewardRect = rewardGo.AddComponent<RectTransform>();
        rewardRect.anchorMin = new Vector2(0f, 1f);
        rewardRect.anchorMax = new Vector2(1f, 1f);
        rewardRect.pivot = new Vector2(0.5f, 1f);
        rewardRect.anchoredPosition = new Vector2(0f, -86f);
        rewardRect.sizeDelta = new Vector2(-24f, 30f);
        rewardGo.SetActive(false);

        var iconGo = new GameObject("RewardIcon");
        iconGo.transform.SetParent(rewardGo.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, 0f);
        iconRect.sizeDelta = new Vector2(28f, 28f);
        var icon = iconGo.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var labelGo = new GameObject("RewardLabel");
        labelGo.transform.SetParent(rewardGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(34f, 0f);
        labelRect.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 14f;
        label.fontStyle = FontStyles.Bold;
        label.color = RewardGold;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;

        _rewardText = label;
        _rewardIcon = icon;

        if (_hintText != null)
        {
            var hintRect = _hintText.rectTransform;
            hintRect.offsetMax = new Vector2(hintRect.offsetMax.x, -118f);
        }
    }

    public void Refresh()
    {
        MissionProgress.TryAdvanceSkippableSteps();
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

        if (_rewardText != null)
        {
            var showReward = !showCompleteFlash && !display.IsComplete && display.ShowReward;
            var rewardRoot = _rewardText.transform.parent;
            if (rewardRoot != null)
                rewardRoot.gameObject.SetActive(showReward);

            if (showReward)
                _rewardText.text = display.RewardLabel;

            if (_rewardIcon != null)
            {
                _rewardIcon.gameObject.SetActive(showReward && display.RewardIcon != null);
                if (showReward && display.RewardIcon != null)
                {
                    _rewardIcon.sprite = display.RewardIcon;
                    _rewardIcon.enabled = true;
                }
            }
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
