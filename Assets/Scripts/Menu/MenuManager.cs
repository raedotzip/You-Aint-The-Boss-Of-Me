using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton — place on a persistent GameObject in the menu scene.
// Wire MenuBox.onSliced events to these methods in the Inspector.
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // Load any scene by name (match exact Build Settings name)
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
