using TMPro;
using UnityEngine;

public class MissionDirectionIndicatorUI : MonoBehaviour
{
    const float EdgePadding = 56f;
    const float OnScreenLabelBottomOffset = 120f;

    static readonly Color ArrowColor = new(0.95f, 0.78f, 0.15f, 1f);
    static readonly Color LabelColor = new(0.95f, 0.92f, 0.85f, 1f);

    RectTransform _arrowRect;
    TextMeshProUGUI _arrowIcon;
    RectTransform _labelRect;
    TextMeshProUGUI _labelText;

    Camera _camera;

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
        _arrowRect = arrowRect;
        _arrowIcon = arrowIcon;
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

        _camera = ResolveCamera();
        if (_camera == null)
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
        var screenPoint = GetClampedScreenPoint(_camera, target.WorldPosition, EdgePadding, out var isOnScreen);
        var label = target.Label + " — " + FormatDistance(distance);

        if (_labelText != null)
            _labelText.text = label;

        if (isOnScreen)
        {
            if (_arrowRect != null)
                _arrowRect.gameObject.SetActive(false);

            if (_labelRect != null)
            {
                _labelRect.gameObject.SetActive(true);
                _labelRect.position = new Vector3(
                    Screen.width * 0.5f,
                    OnScreenLabelBottomOffset,
                    0f);
            }
        }
        else
        {
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var dir = screenPoint - center;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            if (_arrowRect != null)
            {
                _arrowRect.gameObject.SetActive(true);
                _arrowRect.position = new Vector3(screenPoint.x, screenPoint.y, 0f);
                _arrowRect.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }

            if (_labelRect != null)
            {
                _labelRect.gameObject.SetActive(true);
                var labelOffset = dir.sqrMagnitude > 0.001f ? dir.normalized * 42f : Vector2.up * 42f;
                _labelRect.position = new Vector3(screenPoint.x + labelOffset.x, screenPoint.y + labelOffset.y, 0f);
            }
        }
    }

    void RefreshVisibility()
    {
        var target = MissionObjectiveLocator.GetCurrentTarget();
        SetVisible(target.HasTarget);
    }

    void SetVisible(bool visible)
    {
        if (_arrowRect != null)
            _arrowRect.gameObject.SetActive(visible);
        if (_labelRect != null)
            _labelRect.gameObject.SetActive(visible);
    }

    static Camera ResolveCamera()
    {
        var traveling = PlayerScenePersistence.TravelingPlayer;
        if (traveling != null)
        {
            var playerCam = traveling.GetComponentInChildren<Camera>(true);
            if (playerCam != null && playerCam.enabled)
                return playerCam;
        }

        if (Camera.main != null)
            return Camera.main;

        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                return cam;
        }

        return null;
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

    static Vector2 GetClampedScreenPoint(Camera camera, Vector3 worldPos, float padding, out bool isOnScreen)
    {
        var screenPos = camera.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        var point = new Vector2(screenPos.x, screenPos.y);
        var dir = point - center;

        isOnScreen = screenPos.z > 0f &&
                     screenPos.x > padding && screenPos.x < Screen.width - padding &&
                     screenPos.y > padding && screenPos.y < Screen.height - padding;

        if (isOnScreen)
            return point;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.up;

        dir.Normalize();

        var boundsW = Screen.width * 0.5f - padding;
        var boundsH = Screen.height * 0.5f - padding;
        var scaleX = boundsW / Mathf.Max(Mathf.Abs(dir.x), 0.001f);
        var scaleY = boundsH / Mathf.Max(Mathf.Abs(dir.y), 0.001f);
        var scale = Mathf.Min(scaleX, scaleY);

        return center + dir * scale;
    }
}
