using System;
using UnityEngine;

/// <summary>
/// 管理一局游戏的状态、分数和生命，并协调角色生成。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterSpawner), typeof(SlashTrail))]
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    [Header("游戏设置")]
    [SerializeField, Min(1)] private int maxLives = 3;
    [SerializeField] private bool gameStarted = true;

    public bool IsPlaying { get; private set; }
    public int Score { get; private set; }
    public int Lives { get; private set; }

    public event Action<int> ScoreChanged;
    public event Action<int> LivesChanged;
    public event Action GameEnded;

    private CharacterSpawner characterSpawner;

    /// <summary>
    /// 建立单例并取得同物体上的生成器。
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        characterSpawner = GetComponent<CharacterSpawner>();
    }

    /// <summary>
    /// 当前暂时跳过开始界面，进入场景后直接开始游戏。
    /// </summary>
    private void Start()
    {
        if (gameStarted)
        {
            StartGame();
        }
    }

    /// <summary>
    /// 重置本局数据并开始生成角色。
    /// </summary>
    public void StartGame()
    {
        Score = 0;
        Lives = maxLives;
        IsPlaying = true;
        ScoreChanged?.Invoke(Score);
        LivesChanged?.Invoke(Lives);
        characterSpawner.BeginSpawning();
    }

    /// <summary>
    /// 角色被切中时增加分数。
    /// </summary>
    public void AddScore(int amount = 1)
    {
        if (!IsPlaying || amount <= 0)
        {
            return;
        }

        Score += amount;
        ScoreChanged?.Invoke(Score);
    }

    /// <summary>
    /// 角色未被切中并离开屏幕时扣除一条生命。
    /// </summary>
    public void RegisterMiss()
    {
        if (!IsPlaying)
        {
            return;
        }

        Lives = Mathf.Max(0, Lives - 1);
        LivesChanged?.Invoke(Lives);
        if (Lives == 0)
        {
            EndGame();
        }
    }

    /// <summary>
    /// 结束当前游戏并停止继续生成角色。
    /// </summary>
    public void EndGame()
    {
        if (!IsPlaying)
        {
            return;
        }

        IsPlaying = false;
        characterSpawner.StopSpawning();
        GameEnded?.Invoke();
    }

    /// <summary>
    /// 清理单例引用。
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
