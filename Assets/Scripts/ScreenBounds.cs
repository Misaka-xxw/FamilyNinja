using UnityEngine;

/// <summary>
/// 提供正交相机在指定世界平面上的屏幕边界数据。
/// </summary>
public static class ScreenBounds
{
    /// <summary>
    /// 获取主相机在世界坐标中的可见矩形。
    /// </summary>
    public static Bounds GetWorldBounds(float worldZ = 0f)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("场景中不存在带 MainCamera 标签的相机。");
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        float distance = Mathf.Abs(worldZ - camera.transform.position.z);
        Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
        Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, distance));
        Vector3 center = (bottomLeft + topRight) * 0.5f;
        Vector3 size = topRight - bottomLeft;
        size.z = 0f;
        center.z = worldZ;
        return new Bounds(center, size);
    }

    /// <summary>
    /// 获取屏幕左边界的世界坐标。
    /// </summary>
    public static float GetLeft(float worldZ = 0f) => GetWorldBounds(worldZ).min.x;

    /// <summary>
    /// 获取屏幕右边界的世界坐标。
    /// </summary>
    public static float GetRight(float worldZ = 0f) => GetWorldBounds(worldZ).max.x;

    /// <summary>
    /// 获取屏幕下边界的世界坐标。
    /// </summary>
    public static float GetBottom(float worldZ = 0f) => GetWorldBounds(worldZ).min.y;

    /// <summary>
    /// 获取屏幕上边界的世界坐标。
    /// </summary>
    public static float GetTop(float worldZ = 0f) => GetWorldBounds(worldZ).max.y;

    /// <summary>
    /// 获取屏幕可见区域的世界宽度。
    /// </summary>
    public static float GetWidth(float worldZ = 0f) => GetWorldBounds(worldZ).size.x;

    /// <summary>
    /// 获取屏幕可见区域的世界高度。
    /// </summary>
    public static float GetHeight(float worldZ = 0f) => GetWorldBounds(worldZ).size.y;

    /// <summary>
    /// 判断世界坐标是否处于扩展后的屏幕范围内。
    /// </summary>
    public static bool Contains(Vector3 position, float padding = 0f)
    {
        Bounds bounds = GetWorldBounds(position.z);
        return position.x >= bounds.min.x - padding && position.x <= bounds.max.x + padding
            && position.y >= bounds.min.y - padding && position.y <= bounds.max.y + padding;
    }
}
