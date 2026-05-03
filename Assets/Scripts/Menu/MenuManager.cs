using UnityEngine;
using UnityEngine.SceneManagement;

// Scene build indices: 0 = Menu, 1 = Lab, 2 = Boss1, 3 = Boss2
// Singleton — place on a persistent GameObject in the menu scene.
// Wire MenuBox.onSliced events to these methods in the Inspector.
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // Starts the game — loads Lab (scene index 1)
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    // Load any scene by name (match exact Build Settings name)
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByIndex(int index)
    {
        SceneManager.LoadScene(index);
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
