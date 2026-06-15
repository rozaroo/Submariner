using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        SceneTransitionManager.Instance.LoadSceneWithFade("InteriorSubmarine");
    }

    public void QuitGame()
    {
        
        Application.Quit();

#if UNITY_EDITOR
        
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
