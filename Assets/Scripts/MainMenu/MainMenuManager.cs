using UnityEngine;
using UnityEngine.SceneManagement;
using AK.Wwise;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        StopMusic();
        SceneTransitionManager.Instance.LoadSceneWithFade("InteriorSubmarine");
    }

    public void BackToMainMenu()
    {
        StopMusic();
        SceneTransitionManager.Instance.LoadSceneWithFade("MainMenu");
    }

    private void StopMusic()
    {
        //AkSoundEngine.PostEvent("Stop_BackgroundSubmarineMFX", gameObject);
        //Not Used RN
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }}
