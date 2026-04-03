using System.Collections.Generic;
using UnityEngine;

public class ExcavationDirtPile : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private int gridX = 9;
    [SerializeField] private int gridZ = 9;
    [SerializeField] private float chunkSize = 0.18f;
    [SerializeField] private float spacing = 0.16f;
    [SerializeField] private float moundRadius = 0.78f;
    [SerializeField] private float maxHeight = 0.3f;
    [SerializeField] private float verticalJitter = 0.03f;
    [SerializeField] private float horizontalJitter = 0.025f;

    [Header("Look")]
    [SerializeField] private Material dirtMaterial;
    [SerializeField] private bool generateOnStart = true;

    private readonly List<Transform> _chunks = new List<Transform>();

    public float ClearedRatio
    {
        get
        {
            if (_chunks.Count == 0)
            {
                return 0f;
            }

            int cleared = 0;
            for (int i = 0; i < _chunks.Count; i++)
            {
                if (!_chunks[i].gameObject.activeSelf)
                {
                    cleared++;
                }
            }

            return cleared / (float)_chunks.Count;
        }
    }

    private void Start()
    {
        if (transform.childCount > 0)
        {
            CacheExistingChunks();
            return;
        }

        if (generateOnStart)
        {
            GeneratePile();
        }
    }

    [ContextMenu("Generate Dirt Pile")]
    public void GeneratePile()
    {
        ClearGeneratedChildren();
        _chunks.Clear();

        float startX = -((gridX - 1) * spacing) * 0.5f;
        float startZ = -((gridZ - 1) * spacing) * 0.5f;

        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                Vector2 flatPosition = new Vector2(startX + (x * spacing), startZ + (z * spacing));
                float distance = flatPosition.magnitude;
                if (distance > moundRadius)
                {
                    continue;
                }

                float normalized = 1f - Mathf.Clamp01(distance / moundRadius);
                float height = Mathf.Lerp(chunkSize * 0.35f, maxHeight, normalized);

                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = $"DirtChunk_{x}_{z}";
                chunk.transform.SetParent(transform, false);
                chunk.transform.localPosition = new Vector3(
                    flatPosition.x + Random.Range(-horizontalJitter, horizontalJitter),
                    (height * 0.5f) + Random.Range(0f, verticalJitter),
                    flatPosition.y + Random.Range(-horizontalJitter, horizontalJitter));
                chunk.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                chunk.transform.localScale = new Vector3(
                    chunkSize * Random.Range(0.85f, 1.15f),
                    height,
                    chunkSize * Random.Range(0.85f, 1.15f));

                Renderer renderer = chunk.GetComponent<Renderer>();
                if (renderer != null && dirtMaterial != null)
                {
                    renderer.sharedMaterial = dirtMaterial;
                }

                _chunks.Add(chunk.transform);
            }
        }
    }

    public bool Excavate(Vector3 worldPoint, float radius)
    {
        bool removedAny = false;
        float radiusSqr = radius * radius;

        for (int i = 0; i < _chunks.Count; i++)
        {
            Transform chunk = _chunks[i];
            if (chunk == null || !chunk.gameObject.activeSelf)
            {
                continue;
            }

            Vector3 comparisonPoint = chunk.position;
            comparisonPoint.y = worldPoint.y;
            if ((comparisonPoint - worldPoint).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            chunk.gameObject.SetActive(false);
            removedAny = true;
        }

        return removedAny;
    }

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CacheExistingChunks()
    {
        _chunks.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                _chunks.Add(child);
            }
        }
    }
}
