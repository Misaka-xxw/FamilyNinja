using UnityEngine;

/// <summary>
/// 让背景精灵保持原始宽高比并覆盖整个正交相机画面，多余部分从边缘裁掉。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundCover : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool centerOnCamera = true;
    [SerializeField, Min(1f)] private float extraScale = 1.01f;

    private SpriteRenderer spriteRenderer;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastOrthographicSize;
    private float lastCameraAspect;

    /// <summary>
    /// 缓存组件并立即适配当前画面。
    /// </summary>
    private void OnEnable()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        FitToCamera();
    }

    /// <summary>
    /// 分辨率、宽高比或相机尺寸改变时重新适配背景。
    /// </summary>
    private void LateUpdate()
    {
        Camera camera = GetTargetCamera();
        if (camera == null)
        {
            return;
        }

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight
            || !Mathf.Approximately(camera.orthographicSize, lastOrthographicSize)
            || !Mathf.Approximately(camera.aspect, lastCameraAspect))
        {
            FitToCamera();
        }
    }

    /// <summary>
    /// 按 Cover 规则等比缩放背景，保证相机可见区域不会露底。
    /// </summary>
    public void FitToCamera()
    {
        Camera camera = GetTargetCamera();
        if (camera == null || spriteRenderer == null || spriteRenderer.sprite == null
            || !camera.orthographic)
        {
            return;
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float viewHeight = camera.orthographicSize * 2f;
        float viewWidth = viewHeight * camera.aspect;
        float coverScale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * extraScale;
        transform.localScale = new Vector3(coverScale, coverScale, 1f);

        if (centerOnCamera)
        {
            Vector3 position = transform.position;
            position.x = camera.transform.position.x;
            position.y = camera.transform.position.y;
            transform.position = position;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastOrthographicSize = camera.orthographicSize;
        lastCameraAspect = camera.aspect;
    }

    /// <summary>
    /// 获取指定相机；未指定时使用带 MainCamera 标签的相机。
    /// </summary>
    private Camera GetTargetCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        return targetCamera;
    }
}
