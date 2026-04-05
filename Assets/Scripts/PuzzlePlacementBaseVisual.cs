using UnityEngine;

[ExecuteAlways]
public class PuzzlePlacementBaseVisual : MonoBehaviour
{
    [System.Serializable]
    private class RendererMaterialState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    [SerializeField] private Color baseColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private RendererMaterialState[] originalStates;
    [SerializeField] private Material completedMaterialOverride;

    private static Material s_sharedBaseMaterial;
    private Renderer[] _renderers;
    private bool _showingBase = true;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        CacheOriginalMaterials();
        ApplyBaseMaterial();
    }

    private void OnEnable()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (originalStates == null || originalStates.Length == 0)
        {
            CacheOriginalMaterials();
        }

        ApplyBaseMaterial();
    }

    private void OnValidate()
    {
        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (originalStates == null || originalStates.Length == 0)
        {
            CacheOriginalMaterials();
        }

        ApplyBaseMaterial();
    }

    public void ShowCompletedVisual()
    {
        if (_renderers == null || originalStates == null || originalStates.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            CacheOriginalMaterials();
        }

        _showingBase = false;
        if (completedMaterialOverride != null)
        {
            ApplyCompletedMaterialOverride();
        }
        else
        {
            RestoreOriginalMaterials();
        }
    }

    public void HideBaseVisual()
    {
        _showingBase = false;

        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = false;
            }
        }
    }

    public void ShowBaseVisual()
    {
        _showingBase = true;

        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = true;
            }
        }

        ApplyBaseMaterial();
    }

    private void ApplyBaseMaterial()
    {
        if (!_showingBase)
        {
            return;
        }

        EnsureMaterial();
        if (s_sharedBaseMaterial == null || _renderers == null)
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

            Material[] materials = currentRenderer.sharedMaterials;
            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = s_sharedBaseMaterial;
            }

            currentRenderer.sharedMaterials = materials;
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (_renderers == null || originalStates == null)
        {
            return;
        }

        for (int i = 0; i < originalStates.Length; i++)
        {
            RendererMaterialState state = originalStates[i];
            if (state == null || state.renderer == null || state.originalMaterials == null)
            {
                continue;
            }

            state.renderer.sharedMaterials = state.originalMaterials;
        }
    }

    private void ApplyCompletedMaterialOverride()
    {
        if (_renderers == null || completedMaterialOverride == null)
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

            Material[] currentMaterials = currentRenderer.sharedMaterials;
            for (int j = 0; j < currentMaterials.Length; j++)
            {
                currentMaterials[j] = completedMaterialOverride;
            }

            currentRenderer.sharedMaterials = currentMaterials;
            currentRenderer.enabled = true;
        }
    }

    private void CacheOriginalMaterials()
    {
        if (_renderers == null)
        {
            return;
        }

        if (originalStates != null && originalStates.Length == _renderers.Length)
        {
            bool hasCompleteState = true;
            for (int i = 0; i < originalStates.Length; i++)
            {
                if (originalStates[i] == null || originalStates[i].renderer == null || originalStates[i].originalMaterials == null || originalStates[i].originalMaterials.Length == 0)
                {
                    hasCompleteState = false;
                    break;
                }
            }

            if (hasCompleteState)
            {
                return;
            }
        }

        originalStates = new RendererMaterialState[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer currentRenderer = _renderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = currentRenderer.sharedMaterials;
            Material[] materialCopy = new Material[sharedMaterials.Length];
            for (int j = 0; j < sharedMaterials.Length; j++)
            {
                materialCopy[j] = sharedMaterials[j];
            }

            originalStates[i] = new RendererMaterialState
            {
                renderer = currentRenderer,
                originalMaterials = materialCopy
            };
        }
    }

    private void EnsureMaterial()
    {
        if (s_sharedBaseMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return;
        }

        s_sharedBaseMaterial = new Material(shader);
        if (s_sharedBaseMaterial.HasProperty("_BaseColor"))
        {
            s_sharedBaseMaterial.SetColor("_BaseColor", baseColor);
        }
        if (s_sharedBaseMaterial.HasProperty("_Color"))
        {
            s_sharedBaseMaterial.SetColor("_Color", baseColor);
        }
        if (s_sharedBaseMaterial.HasProperty("_Smoothness"))
        {
            s_sharedBaseMaterial.SetFloat("_Smoothness", 0.05f);
        }
        if (s_sharedBaseMaterial.HasProperty("_Metallic"))
        {
            s_sharedBaseMaterial.SetFloat("_Metallic", 0f);
        }
    }

}
