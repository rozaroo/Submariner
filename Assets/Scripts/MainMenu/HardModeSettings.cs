using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HardModeSettings : MonoBehaviour
{
    private const string PreferenceKey = "HardMode";
    private const string UnlockKey = "HardModeUnlocked";

    [SerializeField] private TMP_Text label;

    private static bool IsUnlocked => PlayerPrefs.GetInt(UnlockKey, 0) == 1;
    public static bool IsEnabled => IsUnlocked && PlayerPrefs.GetInt(PreferenceKey, 0) != 0;

    private void OnEnable()
    {
        GetComponent<Button>().interactable = IsUnlocked;
        RefreshLabel();
    }

    public static void UnlockAfterVictory()
    {
        if (IsUnlocked) return;

        PlayerPrefs.SetInt(PreferenceKey, 0);
        PlayerPrefs.SetInt(UnlockKey, 1);
        PlayerPrefs.Save();
    }

    public void ToggleHardMode()
    {
        if (!IsUnlocked) return;

        PlayerPrefs.SetInt(PreferenceKey, IsEnabled ? 0 : 1);
        PlayerPrefs.Save();
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (label == null) return;

        if (!IsUnlocked) label.text = "<size=80%>HARD MODE: LOCKED</size>";
        else label.text = IsEnabled ? "HARD MODE: ON" : "HARD MODE: OFF";
    }
}
