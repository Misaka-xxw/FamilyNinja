using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 按逐渐增强的波次生成角色，并为角色配置初始运动。
/// </summary>
[DisallowMultipleComponent]
public class CharacterSpawner : MonoBehaviour
{
    [Header("角色")]
    [SerializeField] private List<GameObject> alivePrefabs = new List<GameObject>();
    [SerializeField] private Transform characterParent;

    [Header("波次")]
    [SerializeField, Min(0.1f)] private float firstWaveDelay = 0.5f;
    [SerializeField, Min(0.3f)] private float initialWaveInterval = 2.2f;
    [SerializeField, Min(0.3f)] private float minimumWaveInterval = 0.7f;
    [SerializeField, Range(0.01f, 0.3f)] private float intervalDecreasePerWave = 0.06f;
    [SerializeField, Min(0f)] private float characterDelayInWave = 0.15f;

    [Header("抛出参数")]
    [SerializeField, Min(0.1f)] private float gravity = 12f;
    [SerializeField] private Vector2 horizontalSpeedRange = new Vector2(2.2f, 4.5f);
    [SerializeField] private Vector2 apexHeightRatioRange = new Vector2(0.58f, 0.88f);
    [SerializeField] private Vector2 angularSpeedRange = new Vector2(-100f, 100f);

    private Coroutine spawnRoutine;
    private int waveIndex;
    private bool hasWarnedInvalidPrefab;

    /// <summary>
    /// 从第一波开始生成；重复调用不会创建多条协程。
    /// </summary>
    public void BeginSpawning()
    {
        StopSpawning();
        ClearCharacters();
        waveIndex = 0;
        hasWarnedInvalidPrefab = false;
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// 停止后续角色生成。
    /// </summary>
    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    /// <summary>
    /// 清理上一局仍留在场景中的存活角色和死亡碎片。
    /// </summary>
    public void ClearCharacters()
    {
        CharacterMovement[] characters = FindObjectsOfType<CharacterMovement>();
        foreach (CharacterMovement character in characters)
        {
            Destroy(character.gameObject);
        }
    }

    /// <summary>
    /// 按时间间隔持续生成难度递增的波次。
    /// </summary>
    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(firstWaveDelay);

        while (GameController.Instance != null && GameController.Instance.IsPlaying)
        {
            int amount = GetWaveAmount(waveIndex);
            for (int i = 0; i < amount; i++)
            {
                SpawnCharacter();
                if (i < amount - 1 && characterDelayInWave > 0f)
                {
                    yield return new WaitForSeconds(characterDelayInWave);
                }
            }

            waveIndex++;
            float interval = Mathf.Max(minimumWaveInterval,
                initialWaveInterval - waveIndex * intervalDecreasePerWave);
            yield return new WaitForSeconds(interval);
        }

        spawnRoutine = null;
    }

    /// <summary>
    /// 计算当前波次的随机角色数量，前期增长较慢，后期逐步增加。
    /// </summary>
    private int GetWaveAmount(int currentWave)
    {
        int minimum = 1 + currentWave / 8;
        int maximum = 2 + currentWave / 4;
        return Random.Range(minimum, maximum + 1);
    }

    /// <summary>
    /// 从左、右或下方选点，生成一个不会飞过上边界的角色。
    /// </summary>
    private void SpawnCharacter()
    {
        GameObject prefab = GetRandomValidPrefab();
        if (prefab == null)
        {
            if (!hasWarnedInvalidPrefab)
            {
                Debug.LogWarning(
                    "CharacterSpawner 没有可用的角色预制体。请将 Project 窗口中的 _alive 预制体资源拖入 Alive Prefabs，不要使用场景实例。",
                    this);
                hasWarnedInvalidPrefab = true;
            }

            return;
        }

        Bounds bounds = ScreenBounds.GetWorldBounds();
        int edge = Random.Range(0, 3);
        float inset = 0.15f;
        Vector3 position;
        float horizontalVelocity;

        if (edge == 0)
        {
            position = new Vector3(bounds.min.x + inset,
                Random.Range(bounds.min.y, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.28f)), 0f);
            horizontalVelocity = Random.Range(horizontalSpeedRange.x, horizontalSpeedRange.y);
        }
        else if (edge == 1)
        {
            position = new Vector3(bounds.max.x - inset,
                Random.Range(bounds.min.y, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.28f)), 0f);
            horizontalVelocity = -Random.Range(horizontalSpeedRange.x, horizontalSpeedRange.y);
        }
        else
        {
            position = new Vector3(Random.Range(bounds.min.x + inset, bounds.max.x - inset), bounds.min.y + inset, 0f);
            horizontalVelocity = Random.Range(-horizontalSpeedRange.y, horizontalSpeedRange.y);
        }

        // 通过目标最高点反推初始竖直速度，确保抛物线最高点低于屏幕顶部。
        float targetApex = Mathf.Lerp(bounds.min.y, bounds.max.y,
            Random.Range(apexHeightRatioRange.x, apexHeightRatioRange.y));
        float minimumRiseHeight = bounds.size.y * 0.42f;
        float maximumRiseHeight = Mathf.Max(0.5f, bounds.max.y - inset - position.y);
        float heightToApex = Mathf.Clamp(targetApex - position.y, minimumRiseHeight, maximumRiseHeight);
        float verticalVelocity = Mathf.Sqrt(2f * gravity * heightToApex);

        if (edge == 2)
        {
            // 依据预计回落到底边所需时间限制横向速度，防止从底部生成后过早飞出侧边。
            float flightTime = verticalVelocity * 2f / gravity;
            float distanceToLeft = position.x - bounds.min.x;
            float distanceToRight = bounds.max.x - position.x;
            float availableDistance = horizontalVelocity < 0f ? distanceToLeft : distanceToRight;
            float safeHorizontalSpeed = availableDistance * 0.85f / Mathf.Max(0.1f, flightTime);
            horizontalVelocity = Mathf.Sign(horizontalVelocity) * Mathf.Min(Mathf.Abs(horizontalVelocity), safeHorizontalSpeed);
        }

        GameObject character = Instantiate(prefab, position, Quaternion.identity, characterParent);
        if (character.GetComponent<CharacterShadow>() == null)
        {
            character.AddComponent<CharacterShadow>();
        }

        CharacterMovement movement = character.GetComponent<CharacterMovement>();
        if (movement == null)
        {
            movement = character.AddComponent<CharacterMovement>();
        }

        movement.Initialize(new Vector2(horizontalVelocity, verticalVelocity), gravity,
            Random.Range(angularSpeedRange.x, angularSpeedRange.y));
    }

    /// <summary>
    /// 从列表中随机选择仍然存在的预制体资源，并排除可能被销毁的场景实例。
    /// </summary>
    private GameObject GetRandomValidPrefab()
    {
        GameObject selectedPrefab = null;
        int validCount = 0;

        foreach (GameObject prefab in alivePrefabs)
        {
            if (prefab == null || prefab.scene.IsValid())
            {
                continue;
            }

            validCount++;
            if (Random.Range(0, validCount) == 0)
            {
                selectedPrefab = prefab;
            }
        }

        return selectedPrefab;
    }
}
