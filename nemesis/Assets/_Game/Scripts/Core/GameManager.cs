using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    public enum GameState { MainMenu, HeroIntro, BossFight, Victory, Dead }

    [SerializeField] private GameConfig config;

    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeState(GameState newState)
    {
        Debug.Log($"[GameManager] State changed: {CurrentState} -> {newState}");
        CurrentState = newState;
    }

    public async void LoadScene(string sceneName)
    {
        Debug.Log($"[GameManager] Loading scene: {sceneName}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            await Task.Yield();
        }
    }
}
