using UnityEngine;

/// <summary>
/// 为角色创建一个半透明投影，并让所有角色的投影保持相同世界方向。
/// </summary>
[DisallowMultipleComponent]
public class CharacterShadow : MonoBehaviour
{
    [SerializeField] private Vector2 worldOffset = new Vector2(0.18f, -0.18f);
    [SerializeField, Range(0f, 1f)] private float alpha = 0.38f;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer shadowRenderer;
    private Transform shadowTransform;

    /// <summary>
    /// 取得主精灵并创建对应的投影精灵。
    /// </summary>
    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            return;
        }

        var shadowObject = new GameObject("Character Shadow");
        shadowTransform = shadowObject.transform;
        shadowTransform.SetParent(sourceRenderer.transform, false);
        shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        CopyRendererSettings();
        UpdateShadowTransform();
    }

    /// <summary>
    /// 在角色移动和旋转后修正投影的统一世界偏移。
    /// </summary>
    private void LateUpdate()
    {
        if (sourceRenderer == null || shadowRenderer == null)
        {
            return;
        }

        shadowRenderer.sprite = sourceRenderer.sprite;
        shadowRenderer.flipX = sourceRenderer.flipX;
        shadowRenderer.flipY = sourceRenderer.flipY;
        shadowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;
        UpdateShadowTransform();
    }

    /// <summary>
    /// 复制主精灵的渲染属性并设置投影颜色和层级。
    /// </summary>
    private void CopyRendererSettings()
    {
        shadowRenderer.sprite = sourceRenderer.sprite;
        shadowRenderer.flipX = sourceRenderer.flipX;
        shadowRenderer.flipY = sourceRenderer.flipY;
        shadowRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        shadowRenderer.sortingOrder = sourceRenderer.sortingOrder - 1;
        shadowRenderer.color = new Color(0f, 0f, 0f, alpha);
        shadowRenderer.maskInteraction = sourceRenderer.maskInteraction;
    }

    /// <summary>
    /// 将固定世界偏移换算为角色局部偏移，使方向不随角色旋转。
    /// </summary>
    private void UpdateShadowTransform()
    {
        shadowTransform.localPosition = sourceRenderer.transform.InverseTransformVector(worldOffset);
        shadowTransform.localRotation = Quaternion.identity;
        shadowTransform.localScale = Vector3.one;
    }
}
