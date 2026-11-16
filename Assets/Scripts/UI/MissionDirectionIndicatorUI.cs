using TMPro;
using UnityEngine;

public class MissionDirectionIndicatorUI : MonoBehaviour
{
    const float LabelBottomOffset = 108f;

    RectTransform _labelRect;
    TextMeshProUGUI _labelText;

    void OnEnable()
    {
        MissionProgress.Changed += RefreshVisibility;
        RefreshVisibility();
    }

    void OnDisable()
    {
        MissionProgress.Changed -= RefreshVisibility;
    }

    public void Wire(RectTransform arrowRect, TextMeshProUGUI arrowIcon, RectTransform labelRect, TextMeshProUGUI labelText)
    {
        if (arrowRect != null)
            arrowRect.gameObject.SetActive(false);

        _labelRect = labelRect;
        _labelText = labelText;
        RefreshVisibility();
    }

    void LateUpdate()
    {
        if (!isActiveAndEnabled)
            return;

        var target = MissionObjectiveLocator.GetCurrentTarget();
        if (!target.HasTarget)
        {
            SetVisible(false);
            return;
        }

        var playerPos = ResolvePlayerPosition();
        if (!playerPos.HasValue)
        {
            SetVisible(false);
            return;
        }

        var distance = PlanarDistance(playerPos.Value, target.WorldPosition);
        var label = "Objetivo: " + target.Label + " · " + FormatDistance(distance);

        if (_labelText != null)
            _labelText.text = label;

        if (_labelRect != null)
        {
            _labelRect.gameObject.SetActive(true);
            _labelRect.anchorMin = new Vector2(0.5f, 0f);
            _labelRect.anchorMax = new Vector2(0.5f, 0f);
            _labelRect.pivot = new Vector2(0.5f, 0f);
            _labelRect.anchoredPosition = new Vector2(0f, LabelBottomOffset);
        }
    }

    void RefreshVisibility()
    {
        var target = MissionObjectiveLocator.GetCurrentTarget();
        SetVisible(target.HasTarget);
    }

    void SetVisible(bool visible)
    {
        if (_labelRect != null)
            _labelRect.gameObject.SetActive(visible);
    }

    static Vector3? ResolvePlayerPosition()
    {
        var player = PlayerScenePersistence.TravelingPlayer;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return player.transform.position;
    }

    static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static string FormatDistance(float meters)
    {
        if (meters >= 100f)
            return Mathf.RoundToInt(meters) + "m";

        if (meters >= 10f)
            return Mathf.RoundToInt(meters) + "m";

        return Mathf.RoundToInt(meters) + "m";
    }

}
