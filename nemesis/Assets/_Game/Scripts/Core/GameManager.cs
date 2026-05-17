using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum GameState { MainMenu, HeroIntro, BossFight, Victory, Dead }

    [SerializeField] private GameConfig config;

    public static GameManager Instance { get; private set; }

    public GameConfig Config => config;
    public GameState CurrentState { get; private set; }
    public int DeathCount { get; private set; }
    public string SessionId { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SessionId = System.Guid.NewGuid().ToString();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void ChangeState(GameState newState)
    {
        Debug.Log($"[GameManager] State changed: {CurrentState} -> {newState}");
        CurrentState = newState;
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log($"[GameManager] Loading scene: {sceneName}");
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (asyncLoad != null && !asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void RegisterPlayerDeath()
    {
        DeathCount++;
        Debug.Log($"[GameManager] Player died. Total deaths: {DeathCount}");
        ChangeState(GameState.Dead);
    }

    public void RegisterVictory()
    {
        Debug.Log("[GameManager] Boss defeated!");
        ChangeState(GameState.Victory);
    }

    /// <summary>
    /// Call when restarting a boss run (e.g. from death screen).
    /// </summary>
    public void ResetForNewRun()
    {
        ChangeState(GameState.BossFight);
    }
}
