using UnityEngine;

/// <summary>
/// 监听生命减少事件，并让背景产生短促且逐渐衰减的震动反馈。
/// </summary>
[DisallowMultipleComponent]
public class BackgroundShake : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.22f;
    [SerializeField, Min(0f)] private float strength = 0.18f;
    [SerializeField, Min(1f)] private float frequency = 45f;

    private GameController gameController;
    private Camera mainCamera;
    private float remainingTime;
    private int previousLives = int.MinValue;
    private Vector2 randomSeed;

    /// <summary>
    /// 游戏控制器初始化完成后订阅生命变化事件。
    /// </summary>
    private void Start()
    {
        mainCamera = Camera.main;
        gameController = GameController.Instance;
        if (gameController != null)
        {
            gameController.LivesChanged += OnLivesChanged;
        }
    }

    /// <summary>
    /// 仅在生命数下降时触发震动，开局重置生命不会触发。
    /// </summary>
    private void OnLivesChanged(int currentLives)
    {
        if (previousLives != int.MinValue && currentLives < previousLives)
        {
            remainingTime = duration;
            randomSeed = Random.insideUnitCircle * 100f;
        }

        previousLives = currentLives;
    }

    /// <summary>
    /// 在背景适配完成后叠加震动偏移，并在结束时回到相机中心。
    /// </summary>
    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = mainCamera.transform.position.x;
        position.y = mainCamera.transform.position.y;

        if (remainingTime > 0f)
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.unscaledDeltaTime);
            float progress = remainingTime / duration;
            float time = Time.unscaledTime * frequency;
            Vector2 noise = new Vector2(
                Mathf.PerlinNoise(randomSeed.x, time) * 2f - 1f,
                Mathf.PerlinNoise(randomSeed.y, time) * 2f - 1f);
            position += (Vector3)(noise * strength * progress);
        }

        transform.position = position;
    }

    /// <summary>
    /// 解除生命变化事件监听。
    /// </summary>
    private void OnDestroy()
    {
        if (gameController != null)
        {
            gameController.LivesChanged -= OnLivesChanged;
        }
    }
}
