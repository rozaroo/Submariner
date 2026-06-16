using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    private bool isTransitioning = false; 

    public void PlayGame()
    {
        if (isTransitioning) 
        {
            return; 
        }
        
        isTransitioning = true;

        StopMusic();
        SceneTransitionManager.Instance.LoadSceneWithFade("InteriorSubmarine");
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