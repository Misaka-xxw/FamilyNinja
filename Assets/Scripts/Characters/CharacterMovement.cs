using UnityEngine;

/// <summary>
/// 不依赖 Unity 物理系统，控制角色的抛物线移动与旋转。
/// </summary>
[DisallowMultipleComponent]
public class CharacterMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float despawnPadding = 1.5f;

    public Vector2 Velocity { get; private set; }
    public float AngularSpeed { get; private set; }
    public float Gravity => gravity;

    private float gravity;
    private bool initialized;
    private bool countsAsMiss = true;

    /// <summary>
    /// 设置角色的初速度、重力和角速度。
    /// </summary>
    public void Initialize(Vector2 initialVelocity, float gravityValue, float angularSpeed,
        bool shouldCountAsMiss = true)
    {
        Velocity = initialVelocity;
        gravity = Mathf.Abs(gravityValue);
        AngularSpeed = angularSpeed;
        countsAsMiss = shouldCountAsMiss;
        initialized = true;
    }

    /// <summary>
    /// 每帧手动积分速度和位置，并检测角色是否离场。
    /// </summary>
    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        Velocity += Vector2.down * gravity * deltaTime;
        transform.position += (Vector3)(Velocity * deltaTime);
        transform.Rotate(0f, 0f, AngularSpeed * deltaTime);

        if (HasLeftPlayArea())
        {
            if (countsAsMiss)
            {
                GameController.Instance?.RegisterMiss();
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 判断角色是否已从运动方向完全离开扩展屏幕范围。
    /// </summary>
    private bool HasLeftPlayArea()
    {
        Bounds bounds = ScreenBounds.GetWorldBounds(transform.position.z);
        Vector3 position = transform.position;
        bool below = position.y < bounds.min.y - despawnPadding && Velocity.y < 0f;
        bool left = position.x < bounds.min.x - despawnPadding && Velocity.x < 0f;
        bool right = position.x > bounds.max.x + despawnPadding && Velocity.x > 0f;
        return below || left || right;
    }
}
