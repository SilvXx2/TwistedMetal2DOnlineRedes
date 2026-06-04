using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDamageBlink : MonoBehaviour
{
    [SerializeField, Min(0.01f)]    private float blinkInterval = 0.1f;
    [SerializeField, Range(0f, 1f)] private float blinkAlpha    = 0.35f;

    [Tooltip("Si está vacío, busca todos los Renderer en hijos.")]
    [SerializeField] private Renderer[] targetRenderers;

    private Coroutine      blinkRoutine;
    private Material[]      blinkMaterials;
    private Color[]         baseColors;

    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        RefreshRendererCache();
    }

    public void Play(float duration)
    {
        if (duration <= 0f) return;

        if (blinkMaterials == null || blinkMaterials.Length == 0)
        {
            RefreshRendererCache();
        }

        if (blinkMaterials == null || blinkMaterials.Length == 0)
        {
            return;
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            RestoreColors();
        }

        CaptureCurrentColors();
        blinkRoutine = StartCoroutine(BlinkRoutine(duration));
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        float endTime  = Time.time + duration;
        bool  lowAlpha = false;

        while (Time.time < endTime)
        {
            lowAlpha = !lowAlpha;
            SetAlphaMultiplier(lowAlpha ? blinkAlpha : 1f);
            yield return new WaitForSeconds(blinkInterval);
        }

        RestoreColors();
        blinkRoutine = null;
    }

    private void RefreshRendererCache()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        blinkMaterials = new Material[targetRenderers.Length];
        baseColors = new Color[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer target = targetRenderers[i];
            if (target == null)
            {
                continue;
            }

            Material material = target.material;
            blinkMaterials[i] = material;

            if (TryGetTintColor(material, out Color tint))
            {
                baseColors[i] = tint;
            }
            else
            {
                baseColors[i] = Color.white;
            }
        }
    }

    private void CaptureCurrentColors()
    {
        for (int i = 0; i < blinkMaterials.Length; i++)
        {
            Material material = blinkMaterials[i];
            if (!TryGetTintColor(material, out Color tint))
            {
                continue;
            }

            baseColors[i] = tint;
        }
    }

    private void SetAlphaMultiplier(float multiplier)
    {
        for (int i = 0; i < blinkMaterials.Length; i++)
        {
            Material material = blinkMaterials[i];
            Color color = baseColors[i];
            color.r = baseColors[i].r * multiplier;
            color.g = baseColors[i].g * multiplier;
            color.b = baseColors[i].b * multiplier;
            color.a = baseColors[i].a * multiplier;
            TrySetTintColor(material, color);
        }
    }

    private void RestoreColors()
    {
        if (blinkMaterials == null || baseColors == null)
        {
            return;
        }

        for (int i = 0; i < blinkMaterials.Length; i++)
        {
            Material material = blinkMaterials[i];
            TrySetTintColor(material, baseColors[i]);
        }
    }

    private static bool TryGetTintColor(Material material, out Color color)
    {
        if (material == null)
        {
            color = Color.white;
            return false;
        }

        if (material.HasProperty(ColorPropertyId))
        {
            color = material.GetColor(ColorPropertyId);
            return true;
        }

        if (material.HasProperty(BaseColorPropertyId))
        {
            color = material.GetColor(BaseColorPropertyId);
            return true;
        }

        color = Color.white;
        return false;
    }

    private static bool TrySetTintColor(Material material, Color color)
    {
        if (material == null)
        {
            return false;
        }

        if (material.HasProperty(ColorPropertyId))
        {
            material.SetColor(ColorPropertyId, color);
            return true;
        }

        if (material.HasProperty(BaseColorPropertyId))
        {
            material.SetColor(BaseColorPropertyId, color);
            return true;
        }

        return false;
    }

    private void OnDisable()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
        RestoreColors();
    }
}