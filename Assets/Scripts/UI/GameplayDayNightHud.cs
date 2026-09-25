using TMPro;
using UnityEngine;

public class GameplayDayNightHud : MonoBehaviour
{
    TextMeshProUGUI _clock;

    public void Wire(TextMeshProUGUI clock)
    {
        _clock = clock;
        Refresh();
    }

    void OnEnable()
    {
        if (GameplayDayNightCycle.Instance != null)
            GameplayDayNightCycle.Instance.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (GameplayDayNightCycle.Instance != null)
            GameplayDayNightCycle.Instance.Changed -= Refresh;
    }

    void Update()
    {
        if (GameplayDayNightCycle.Instance != null)
            Refresh();
    }

    void Refresh()
    {
        if (_clock == null)
            return;

        var cycle = GameplayDayNightCycle.Instance;
        _clock.text = cycle != null ? cycle.GetClockTimeText() : "00:00";
    }
}
