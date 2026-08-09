using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 检测刀光线段与角色多边形的入点和出点，并生成被切开的两个部分。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public class BeCutted : MonoBehaviour
{
    [Header("切开结果")]
    [SerializeField] private GameObject deadPrefab;
    [SerializeField] private Shader sliceShader;
    [SerializeField, Min(0f)] private float separateSpeed = 2.8f;
    [SerializeField, Min(0f)] private float extraAngularSpeed = 90f;

    [Header("有效切割判定")]
    [SerializeField, Range(0f, 0.5f)] private float minimumSmallerAreaRatio = 0.12f;
    [SerializeField, Range(0f, 1f)] private float minimumCutLengthRatio = 0.18f;

    private PolygonCollider2D polygonCollider;
    private CharacterMovement characterMovement;
    private Vector2 localEntryPoint;
    private bool hasEntryPoint;
    private bool hasBeenCut;

    /// <summary>
    /// 缓存角色碰撞体和运动组件。
    /// </summary>
    private void Awake()
    {
        polygonCollider = GetComponent<PolygonCollider2D>();
        characterMovement = GetComponent<CharacterMovement>();
    }

    /// <summary>
    /// 监听全局滑动输入。
    /// </summary>
    private void OnEnable()
    {
        SlashTrail.SlashStarted += OnSlashStarted;
        SlashTrail.SlashMoved += OnSlashMoved;
        SlashTrail.SlashEnded += OnSlashEnded;
    }

    /// <summary>
    /// 停止监听全局滑动输入。
    /// </summary>
    private void OnDisable()
    {
        SlashTrail.SlashStarted -= OnSlashStarted;
        SlashTrail.SlashMoved -= OnSlashMoved;
        SlashTrail.SlashEnded -= OnSlashEnded;
    }

    /// <summary>
    /// 如果滑动从角色内部开始，则将起始位置作为入点。
    /// </summary>
    private void OnSlashStarted(Vector2 worldPoint)
    {
        hasEntryPoint = polygonCollider.OverlapPoint(worldPoint);
        if (hasEntryPoint)
        {
            localEntryPoint = transform.InverseTransformPoint(worldPoint);
        }
    }

    /// <summary>
    /// 检测当前滑动线段和多边形所有边的交点，并在取得入点与出点后执行切割。
    /// </summary>
    private void OnSlashMoved(Vector2 worldStart, Vector2 worldEnd)
    {
        if (hasBeenCut || !isActiveAndEnabled)
        {
            return;
        }

        Vector2 localStart = transform.InverseTransformPoint(worldStart);
        Vector2 localEnd = transform.InverseTransformPoint(worldEnd);
        List<SegmentHit> hits = GetIntersections(localStart, localEnd);

        if (!hasEntryPoint)
        {
            if (hits.Count >= 2)
            {
                Cut(hits[0].Point, hits[hits.Count - 1].Point);
                return;
            }

            if (hits.Count == 1 || polygonCollider.OverlapPoint(worldEnd))
            {
                localEntryPoint = hits.Count == 1 ? hits[0].Point : localStart;
                hasEntryPoint = true;
            }
        }
        else if (hits.Count > 0)
        {
            Vector2 exitPoint = hits[hits.Count - 1].Point;
            if ((exitPoint - localEntryPoint).sqrMagnitude > 0.0001f)
            {
                Cut(localEntryPoint, exitPoint);
            }
        }
    }

    /// <summary>
    /// 一次滑动结束时清除尚未形成完整切线的入点。
    /// </summary>
    private void OnSlashEnded()
    {
        hasEntryPoint = false;
    }

    /// <summary>
    /// 获取局部线段与 PolygonCollider2D 各条边的交点，并按滑动方向排序。
    /// </summary>
    private List<SegmentHit> GetIntersections(Vector2 segmentStart, Vector2 segmentEnd)
    {
        var hits = new List<SegmentHit>();
        for (int pathIndex = 0; pathIndex < polygonCollider.pathCount; pathIndex++)
        {
            Vector2[] path = polygonCollider.GetPath(pathIndex);
            for (int i = 0; i < path.Length; i++)
            {
                Vector2 edgeStart = path[i] + polygonCollider.offset;
                Vector2 edgeEnd = path[(i + 1) % path.Length] + polygonCollider.offset;
                if (TryIntersectSegments(segmentStart, segmentEnd, edgeStart, edgeEnd,
                    out Vector2 point, out float progress))
                {
                    bool duplicate = hits.Exists(hit => (hit.Point - point).sqrMagnitude < 0.000001f);
                    if (!duplicate)
                    {
                        hits.Add(new SegmentHit(point, progress));
                    }
                }
            }
        }

        hits.Sort((first, second) => first.Progress.CompareTo(second.Progress));
        return hits;
    }

    /// <summary>
    /// 使用二维叉积计算两条有限线段是否相交。
    /// </summary>
    private static bool TryIntersectSegments(Vector2 firstStart, Vector2 firstEnd,
        Vector2 secondStart, Vector2 secondEnd, out Vector2 point, out float progress)
    {
        Vector2 firstDirection = firstEnd - firstStart;
        Vector2 secondDirection = secondEnd - secondStart;
        float denominator = Cross(firstDirection, secondDirection);
        if (Mathf.Abs(denominator) < 0.00001f)
        {
            point = default;
            progress = 0f;
            return false;
        }

        Vector2 difference = secondStart - firstStart;
        progress = Cross(difference, secondDirection) / denominator;
        float secondProgress = Cross(difference, firstDirection) / denominator;
        point = firstStart + firstDirection * progress;
        return progress >= 0f && progress <= 1f && secondProgress >= 0f && secondProgress <= 1f;
    }

    /// <summary>
    /// 计算二维向量叉积。
    /// </summary>
    private static float Cross(Vector2 first, Vector2 second)
    {
        return first.x * second.y - first.y * second.x;
    }

    /// <summary>
    /// 生成死亡角色的两份副本，分别显示切线两侧并施加分离速度。
    /// </summary>
    private void Cut(Vector2 entryPoint, Vector2 exitPoint)
    {
        if (!IsCutLargeEnough(entryPoint, exitPoint))
        {
            hasEntryPoint = false;
            return;
        }

        if (deadPrefab == null)
        {
            Debug.LogWarning($"{name} 尚未给 BeCutted 配置 deadPrefab。", this);
            hasEntryPoint = false;
            return;
        }

        hasBeenCut = true;
        Vector2 cutDirection = (exitPoint - entryPoint).normalized;
        Vector2 localNormal = new Vector2(-cutDirection.y, cutDirection.x);
        Vector2 worldNormal = transform.TransformDirection(localNormal).normalized;
        Vector2 baseVelocity = characterMovement == null ? Vector2.zero : characterMovement.Velocity;
        float gravity = characterMovement == null ? 12f : characterMovement.Gravity;
        float baseAngularSpeed = characterMovement == null ? 0f : characterMovement.AngularSpeed;
        Vector2 cutPoint = (entryPoint + exitPoint) * 0.5f;

        CreateHalf(cutPoint, localNormal, 1f, baseVelocity + worldNormal * separateSpeed,
            gravity, baseAngularSpeed + extraAngularSpeed);
        CreateHalf(cutPoint, localNormal, -1f, baseVelocity - worldNormal * separateSpeed,
            gravity, baseAngularSpeed - extraAngularSpeed);

        GameController.Instance?.AddScore();
        Destroy(gameObject);
    }

    /// <summary>
    /// 同时检查切口长度和切线两侧面积，避免擦过头发等边缘区域时触发切割。
    /// </summary>
    private bool IsCutLargeEnough(Vector2 entryPoint, Vector2 exitPoint)
    {
        Vector2 cutVector = exitPoint - entryPoint;
        float characterSize = GetLocalColliderSize();
        if (cutVector.magnitude < characterSize * minimumCutLengthRatio)
        {
            return false;
        }

        Vector2 cutNormal = new Vector2(-cutVector.y, cutVector.x).normalized;
        Vector2 cutPoint = (entryPoint + exitPoint) * 0.5f;
        float positiveArea = 0f;
        float negativeArea = 0f;

        for (int pathIndex = 0; pathIndex < polygonCollider.pathCount; pathIndex++)
        {
            Vector2[] sourcePath = polygonCollider.GetPath(pathIndex);
            var path = new List<Vector2>(sourcePath.Length);
            foreach (Vector2 point in sourcePath)
            {
                path.Add(point + polygonCollider.offset);
            }

            positiveArea += CalculateClippedArea(path, cutPoint, cutNormal, true);
            negativeArea += CalculateClippedArea(path, cutPoint, cutNormal, false);
        }

        float totalArea = positiveArea + negativeArea;
        if (totalArea <= 0.00001f)
        {
            return false;
        }

        float smallerAreaRatio = Mathf.Min(positiveArea, negativeArea) / totalArea;
        return smallerAreaRatio >= minimumSmallerAreaRatio;
    }

    /// <summary>
    /// 获取碰撞体所有路径在角色局部坐标中的包围盒对角线长度。
    /// </summary>
    private float GetLocalColliderSize()
    {
        Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int pathIndex = 0; pathIndex < polygonCollider.pathCount; pathIndex++)
        {
            Vector2[] path = polygonCollider.GetPath(pathIndex);
            foreach (Vector2 sourcePoint in path)
            {
                Vector2 point = sourcePoint + polygonCollider.offset;
                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }
        }

        return (maximum - minimum).magnitude;
    }

    /// <summary>
    /// 用半平面裁剪多边形，并计算保留部分的面积。
    /// </summary>
    private static float CalculateClippedArea(List<Vector2> polygon, Vector2 cutPoint,
        Vector2 cutNormal, bool keepPositive)
    {
        if (polygon.Count < 3)
        {
            return 0f;
        }

        var clipped = new List<Vector2>();
        Vector2 previous = polygon[polygon.Count - 1];
        float previousDistance = Vector2.Dot(previous - cutPoint, cutNormal);
        bool previousInside = keepPositive ? previousDistance >= 0f : previousDistance <= 0f;

        foreach (Vector2 current in polygon)
        {
            float currentDistance = Vector2.Dot(current - cutPoint, cutNormal);
            bool currentInside = keepPositive ? currentDistance >= 0f : currentDistance <= 0f;

            if (currentInside != previousInside)
            {
                float progress = previousDistance / (previousDistance - currentDistance);
                clipped.Add(Vector2.Lerp(previous, current, progress));
            }

            if (currentInside)
            {
                clipped.Add(current);
            }

            previous = current;
            previousDistance = currentDistance;
            previousInside = currentInside;
        }

        return CalculatePolygonArea(clipped);
    }

    /// <summary>
    /// 使用鞋带公式计算多边形面积。
    /// </summary>
    private static float CalculatePolygonArea(List<Vector2> polygon)
    {
        if (polygon.Count < 3)
        {
            return 0f;
        }

        float twiceArea = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 current = polygon[i];
            Vector2 next = polygon[(i + 1) % polygon.Count];
            twiceArea += current.x * next.y - next.x * current.y;
        }

        return Mathf.Abs(twiceArea) * 0.5f;
    }

    /// <summary>
    /// 创建并配置一个死亡角色半片。
    /// </summary>
    private void CreateHalf(Vector2 cutPoint, Vector2 cutNormal, float keepSide,
        Vector2 velocity, float gravity, float angularSpeed)
    {
        GameObject half = Instantiate(deadPrefab, transform.position, transform.rotation, transform.parent);
        half.transform.localScale = transform.localScale;

        if (half.GetComponent<CharacterShadow>() == null)
        {
            half.AddComponent<CharacterShadow>();
        }

        SpriteRenderer[] renderers = half.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            SliceFragment fragment = spriteRenderer.gameObject.AddComponent<SliceFragment>();
            fragment.Initialize(spriteRenderer, sliceShader, cutPoint, cutNormal, keepSide);
        }

        CharacterMovement movement = half.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            movement = half.AddComponent<CharacterMovement>();
        }

        movement.Initialize(velocity, gravity, angularSpeed, false);
    }

    private readonly struct SegmentHit
    {
        public Vector2 Point { get; }
        public float Progress { get; }

        /// <summary>
        /// 保存一个交点及其在线段上的进度。
        /// </summary>
        public SegmentHit(Vector2 point, float progress)
        {
            Point = point;
            Progress = progress;
        }
    }
}
