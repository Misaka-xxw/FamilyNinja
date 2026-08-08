using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 读取鼠标或单指滑动，并用四角流线网格绘制短暂的白色刀光。
/// </summary>
[DisallowMultipleComponent]
public class SlashTrail : MonoBehaviour
{
    [Header("刀光外观")]
    [SerializeField, Min(0.01f)] private float startWidth = 0.28f;
    [SerializeField, Range(0.02f, 1f)] private float endWidthRatio = 0.16f;
    [SerializeField, Min(0.02f)] private float lifetime = 0.16f;
    [SerializeField, Range(0f, 1f)] private float endAlpha = 0.2f;
    [SerializeField, Min(0.001f)] private float minimumDistance = 0.04f;
    [SerializeField] private int sortingOrder = 100;

    private readonly List<TrailPiece> pieces = new List<TrailPiece>();
    private Material trailMaterial;
    private Camera mainCamera;
    private Vector3 lastPosition;
    private bool isDrawing;

    private sealed class TrailPiece
    {
        public GameObject GameObject;
        public Material Material;
        public float CreatedTime;
    }

    /// <summary>
    /// 创建适合透明刀光的运行时材质。
    /// </summary>
    private void Awake()
    {
        mainCamera = Camera.main;
        Shader shader = Shader.Find("Sprites/Default");
        trailMaterial = shader == null ? null : new Material(shader);
    }

    /// <summary>
    /// 每帧更新输入、生成刀光片段并清理过期片段。
    /// </summary>
    private void Update()
    {
        UpdateInput();
        UpdatePieces();
    }

    /// <summary>
    /// 同时兼容移动端单指触摸和编辑器/PC 鼠标输入。
    /// </summary>
    private void UpdateInput()
    {
        if (TryGetPointer(out Vector2 screenPosition, out bool began, out bool ended))
        {
            Vector3 worldPosition = ScreenToWorld(screenPosition);
            if (began || !isDrawing)
            {
                lastPosition = worldPosition;
                isDrawing = true;
            }
            else if (Vector3.Distance(lastPosition, worldPosition) >= minimumDistance)
            {
                CreatePiece(lastPosition, worldPosition);
                lastPosition = worldPosition;
            }

            if (ended)
            {
                isDrawing = false;
            }
        }
        else
        {
            isDrawing = false;
        }
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
    /// 在两个采样点之间创建头宽尾尖、尾部透明度较低的四角刀光。
    /// </summary>
    private void CreatePiece(Vector3 start, Vector3 end)
    {
        if (trailMaterial == null)
        {
            return;
        }

        Vector2 direction = end - start;
        Vector2 normal = new Vector2(-direction.y, direction.x).normalized;
        float halfStartWidth = startWidth * 0.5f;
        float halfEndWidth = halfStartWidth * endWidthRatio;

        var gameObjectPiece = new GameObject("Slash Trail Piece");
        gameObjectPiece.transform.SetParent(transform, false);
        MeshFilter filter = gameObjectPiece.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObjectPiece.AddComponent<MeshRenderer>();
        Material pieceMaterial = new Material(trailMaterial);
        renderer.sharedMaterial = pieceMaterial;
        renderer.sortingOrder = sortingOrder;

        Mesh mesh = new Mesh { name = "Slash Trail Quad" };
        mesh.vertices = new[]
        {
            transform.InverseTransformPoint(start + (Vector3)(normal * halfStartWidth)),
            transform.InverseTransformPoint(start - (Vector3)(normal * halfStartWidth)),
            transform.InverseTransformPoint(end + (Vector3)(normal * halfEndWidth)),
            transform.InverseTransformPoint(end - (Vector3)(normal * halfEndWidth))
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.colors = new[]
        {
            Color.white, Color.white,
            new Color(1f, 1f, 1f, endAlpha), new Color(1f, 1f, 1f, endAlpha)
        };
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;

        pieces.Add(new TrailPiece
        {
            GameObject = gameObjectPiece,
            Material = pieceMaterial,
            CreatedTime = Time.unscaledTime
        });
    }

    /// <summary>
    /// 让旧刀光逐渐淡出，并销毁超过寿命的网格对象。
    /// </summary>
    private void UpdatePieces()
    {
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            TrailPiece piece = pieces[i];
            float progress = (Time.unscaledTime - piece.CreatedTime) / lifetime;
            if (progress >= 1f)
            {
                Destroy(piece.GameObject);
                Destroy(piece.Material);
                pieces.RemoveAt(i);
                continue;
            }

            Color color = Color.white;
            color.a = 1f - progress;
            piece.Material.color = color;
        }
    }

    /// <summary>
    /// 释放运行时创建的材质。
    /// </summary>
    private void OnDestroy()
    {
        if (trailMaterial != null)
        {
            Destroy(trailMaterial);
        }
    }
}
