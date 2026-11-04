using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject mainCanvas;
    [SerializeField] private GameObject controlsCanvas;

    private bool isTransitioning = false; 

    public void ShowControls() => SetControlsVisible(true);

    public void CloseControls() => SetControlsVisible(false);

    private void SetControlsVisible(bool visible)
    {
        if (isTransitioning || mainCanvas == null || controlsCanvas == null) return;
        mainCanvas.SetActive(!visible);
        controlsCanvas.SetActive(visible);
    }

    public void PlayGame()
    {
        if (isTransitioning) 
        {
            return; 
        }
        
        isTransitioning = true;

        StopMusic();
        SceneTransitionManager.Instance.LoadSceneWithFade("Cinematic");
    }

    public void BackToMainMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        StopMusic();
        SceneTransitionManager.Instance.LoadSceneWithFade("MainMenu");
    }

    private void StopMusic()
    {
        // AkSoundEngine.PostEvent("Stop_BackgroundSubmarineMFX", gameObject);
        // Not Used RN
    }

    public void QuitGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
