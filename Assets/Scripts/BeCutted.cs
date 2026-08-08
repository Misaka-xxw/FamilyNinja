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
    /// 创建并配置一个死亡角色半片。
    /// </summary>
    private void CreateHalf(Vector2 cutPoint, Vector2 cutNormal, float keepSide,
        Vector2 velocity, float gravity, float angularSpeed)
    {
        GameObject half = Instantiate(deadPrefab, transform.position, transform.rotation, transform.parent);
        half.transform.localScale = transform.localScale;

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
