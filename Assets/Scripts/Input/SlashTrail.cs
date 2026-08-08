using UnityEngine;

/// <summary>
/// 读取鼠标或单指滑动，并使用连续轨迹绘制梭形刀光。
/// </summary>
[DisallowMultipleComponent]
public class SlashTrail : MonoBehaviour
{
    [Header("刀光外观")]
    [SerializeField, Min(0.01f)] private float maximumWidth = 0.32f;
    [SerializeField, Min(0.02f)] private float lifetime = 0.18f;
    [SerializeField, Min(0.001f)] private float minimumVertexDistance = 0.035f;
    [SerializeField, Range(2, 16)] private int cornerVertices = 5;
    [SerializeField] private int sortingOrder = 100;

    private Camera mainCamera;
    private Material trailMaterial;
    private TrailRenderer currentTrail;

    /// <summary>
    /// 准备刀光所需的透明材质。
    /// </summary>
    private void Awake()
    {
        mainCamera = Camera.main;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            trailMaterial = new Material(shader)
            {
                name = "Runtime Slash Trail Material"
            };
        }
    }

    /// <summary>
    /// 每帧根据鼠标或触摸状态开始、延伸或结束一条刀光。
    /// </summary>
    private void Update()
    {
        if (!TryGetPointer(out Vector2 screenPosition, out bool began, out bool ended))
        {
            return;
        }

        Vector3 worldPosition = ScreenToWorld(screenPosition);
        if (began)
        {
            BeginTrail(worldPosition);
        }

        if (currentTrail != null)
        {
            currentTrail.transform.position = worldPosition;
        }

        if (ended)
        {
            EndTrail();
        }
    }

    /// <summary>
    /// 创建一条独立轨迹，使上一次滑动可以自然消退。
    /// </summary>
    private void BeginTrail(Vector3 position)
    {
        EndTrail();

        GameObject trailObject = new GameObject("Slash Trail");
        trailObject.transform.SetParent(transform, true);
        trailObject.transform.position = position;

        currentTrail = trailObject.AddComponent<TrailRenderer>();
        currentTrail.sharedMaterial = trailMaterial;
        currentTrail.time = lifetime;
        currentTrail.minVertexDistance = minimumVertexDistance;
        currentTrail.widthMultiplier = maximumWidth;
        currentTrail.widthCurve = CreateSpindleCurve();
        currentTrail.colorGradient = CreateColorGradient();
        currentTrail.numCornerVertices = cornerVertices;
        currentTrail.numCapVertices = cornerVertices;
        currentTrail.textureMode = LineTextureMode.Stretch;
        currentTrail.alignment = LineAlignment.View;
        currentTrail.sortingOrder = sortingOrder;
        currentTrail.emitting = true;
        currentTrail.Clear();
    }

    /// <summary>
    /// 停止当前刀光并在其完全淡出后销毁对象。
    /// </summary>
    private void EndTrail()
    {
        if (currentTrail == null)
        {
            return;
        }

        currentTrail.emitting = false;
        Destroy(currentTrail.gameObject, lifetime + 0.05f);
        currentTrail = null;
    }

    /// <summary>
    /// 创建两端尖、中部饱满的宽度曲线，使整条刀光呈梭形。
    /// </summary>
    private static AnimationCurve CreateSpindleCurve()
    {
        return new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f),
            new Keyframe(0.28f, 1f),
            new Keyframe(0.68f, 0.82f),
            new Keyframe(1f, 0f, -4f, 0f));
    }

    /// <summary>
    /// 创建白色透明度渐变，让刀光末端更轻、更自然。
    /// </summary>
    private static Gradient CreateColorGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.15f, 0f),
                new GradientAlphaKey(1f, 0.25f),
                new GradientAlphaKey(0.35f, 1f)
            });
        return gradient;
    }

    /// <summary>
    /// 获取当前主指针状态，移动端优先使用第一根手指。
    /// </summary>
    private bool TryGetPointer(out Vector2 position, out bool began, out bool ended)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            position = touch.position;
            began = touch.phase == TouchPhase.Began;
            ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return true;
        }

        position = Input.mousePosition;
        began = Input.GetMouseButtonDown(0);
        ended = Input.GetMouseButtonUp(0);
        return Input.GetMouseButton(0) || began || ended;
    }

    /// <summary>
    /// 将屏幕坐标转换到本对象所在的世界平面。
    /// </summary>
    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return transform.position;
        }

        float distance = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);
        Vector3 result = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distance));
        result.z = transform.position.z;
        return result;
    }

    /// <summary>
    /// 结束未完成的刀光并释放运行时材质。
    /// </summary>
    private void OnDestroy()
    {
        EndTrail();
        if (trailMaterial != null)
        {
            Destroy(trailMaterial);
        }
    }
}
