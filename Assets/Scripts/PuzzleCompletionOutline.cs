using System.Collections;
using UnityEngine;

public class PuzzleCompletionOutline : MonoBehaviour
{
    [SerializeField] private Color outlineColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float outlineWidth = 4f;
    [SerializeField] private float duration = 1f;

    private Material _outlineMaskMaterial;
    private Material _outlineFillMaterial;
    private Renderer[] _renderers;
    private Coroutine _activePulse;
    private bool _isOutlined;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void Configure(Color color, float width)
    {
        outlineColor = color;
        outlineWidth = width;

        if (_outlineFillMaterial != null)
        {
            _outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
            _outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
        }
    }

    public void PlayPulse()
    {
        if (_activePulse != null)
        {
            StopCoroutine(_activePulse);
            RemoveOutline();
        }

        _activePulse = StartCoroutine(PulseRoutine());
    }

    public void SetOutlined(bool isOutlined)
    {
        if (_activePulse != null)
        {
            StopCoroutine(_activePulse);
            _activePulse = null;
        }

        if (_isOutlined == isOutlined)
        {
            return;
        }

        EnsureMaterials();
        _isOutlined = isOutlined;

        if (isOutlined)
        {
            ApplyOutline();
        }
        else
        {
            RemoveOutline();
        }
    }

    private IEnumerator PulseRoutine()
    {
        EnsureMaterials();
        ApplyOutline();
        yield return new WaitForSeconds(duration);
        if (!_isOutlined)
        {
            RemoveOutline();
        }
        _activePulse = null;
    }

    private void EnsureMaterials()
    {
        if (_outlineMaskMaterial == null)
        {
            Material template = Resources.Load<Material>("Materials/OutlineMask");
            _outlineMaskMaterial = template != null ? new Material(template) : null;
        }

        if (_outlineFillMaterial == null)
        {
            Material template = Resources.Load<Material>("Materials/OutlineFill");
            if (template != null)
            {
                _outlineFillMaterial = new Material(template);
                _outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
                _outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
            }
        }
    }

    private void ApplyOutline()
    {
        if (_outlineMaskMaterial == null || _outlineFillMaterial == null || _renderers == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer currentRenderer = _renderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = currentRenderer.sharedMaterials;
            int materialCount = sharedMaterials.Length;
            Material[] outlinedMaterials = new Material[materialCount + 2];
            for (int j = 0; j < materialCount; j++)
            {
                outlinedMaterials[j] = sharedMaterials[j];
            }

            outlinedMaterials[materialCount] = _outlineMaskMaterial;
            outlinedMaterials[materialCount + 1] = _outlineFillMaterial;
            currentRenderer.materials = outlinedMaterials;
        }
    }

    private void RemoveOutline()
    {
        if (_renderers == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer currentRenderer = _renderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            Material[] currentMaterials = currentRenderer.materials;
            if (currentMaterials.Length < 2)
            {
                continue;
            }

            int newLength = currentMaterials.Length;
            if (currentMaterials[newLength - 1] == _outlineFillMaterial)
            {
                newLength--;
            }

            if (newLength > 0 && currentMaterials[newLength - 1] == _outlineMaskMaterial)
            {
                newLength--;
            }

            if (newLength == currentMaterials.Length)
            {
                continue;
            }

            Material[] trimmedMaterials = new Material[newLength];
            for (int j = 0; j < newLength; j++)
            {
                trimmedMaterials[j] = currentMaterials[j];
            }

            currentRenderer.materials = trimmedMaterials;
        }
    }
}
