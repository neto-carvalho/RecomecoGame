using TMPro;
using UnityEngine;

public static class GameplayDigitalClockStyle
{
    static TMP_FontAsset s_font;

    public static void Apply(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        var font = ResolveFont();
        if (font != null)
            label.font = font;

        label.fontSize = 40f;
        label.fontStyle = FontStyles.Normal;
        label.characterSpacing = 6f;
        label.color = new Color(0.22f, 0.98f, 0.42f, 1f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
    }

    static TMP_FontAsset ResolveFont()
    {
        if (s_font != null)
            return s_font;

        s_font = Resources.Load<TMP_FontAsset>("Fonts/DSEG7Classic-Bold SDF");
        if (s_font != null)
            return s_font;

        s_font = Resources.Load<TMP_FontAsset>("Fonts & Materials/DSEG7Classic-Bold SDF");
        return s_font;
    }
}
