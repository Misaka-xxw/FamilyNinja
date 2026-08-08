using UnityEngine;

/// <summary>
/// 为死亡角色的单个 SpriteRenderer 配置切割材质，并负责释放材质。
/// </summary>
[DisallowMultipleComponent]
public class SliceFragment : MonoBehaviour
{
    private Material sliceMaterial;

    /// <summary>
    /// 设置局部切线信息，并选择保留切线的指定一侧。
    /// </summary>
    public void Initialize(SpriteRenderer spriteRenderer, Shader shader, Vector2 cutPoint,
        Vector2 cutNormal, float keepSide)
    {
        if (shader == null)
        {
            shader = Shader.Find("FamilyNinja/SpriteSlice");
        }

        if (shader == null)
        {
            Debug.LogError("找不到 FamilyNinja/SpriteSlice Shader。", this);
            return;
        }

        sliceMaterial = new Material(shader)
        {
            name = "Runtime Sprite Slice Material"
        };
        sliceMaterial.SetVector("_CutPoint", cutPoint);
        sliceMaterial.SetVector("_CutNormal", cutNormal.normalized);
        sliceMaterial.SetFloat("_KeepSide", keepSide);
        spriteRenderer.material = sliceMaterial;
    }

    /// <summary>
    /// 释放此半片独占的运行时材质。
    /// </summary>
    private void OnDestroy()
    {
        if (sliceMaterial != null)
        {
            Destroy(sliceMaterial);
        }
    }
}
