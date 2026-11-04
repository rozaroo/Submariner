using TMPro;
using UnityEngine;

public class HardModeSettings : MonoBehaviour
{
    private const string PreferenceKey = "HardMode";

    [SerializeField] private TMP_Text label;

    public static bool IsEnabled => PlayerPrefs.GetInt(PreferenceKey, 1) != 0;

    private void OnEnable() => RefreshLabel();

    public void ToggleHardMode()
    {
        PlayerPrefs.SetInt(PreferenceKey, IsEnabled ? 0 : 1);
        PlayerPrefs.Save();
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (label != null) label.text = IsEnabled ? "HARD MODE: ON" : "HARD MODE: OFF";
    }
}
