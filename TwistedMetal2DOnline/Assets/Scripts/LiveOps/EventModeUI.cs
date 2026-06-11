using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EventModeUI : MonoBehaviour
{
    [Header("Elementos de UI")]
    [Tooltip("El panel/contenedor principal del banner de evento. Se activa/desactiva según la config.")]
    [SerializeField] private GameObject eventPanel;

    [Tooltip("TextMeshPro que muestra el nombre del evento activo.")]
    [SerializeField] private TextMeshProUGUI eventNameText;

    [Tooltip("(Opcional) Image para el fondo/banner del evento. Puede cambiar de color.")]
    [SerializeField] private Image eventBannerImage;

    [Header("Animación")]
    [Tooltip("Si es true, el banner hace un efecto de pulso cuando hay un evento activo.")]
    [SerializeField] private bool animatePulse = true;

    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float pulseMinAlpha = 0.7f;

    private bool isAnimating;
    private float pulseTimer;

    private void Start()
    {
        SetBannerVisible(false);

        if (LiveOpsManager.Instance == null)
        {
            Debug.Log("[EventModeUI] LiveOpsManager no encontrado. Banner de evento deshabilitado.");
            return;
        }

        if (LiveOpsManager.Instance.IsReady)
        {
            ApplyEventConfig();
        }
        else
        {
            LiveOpsManager.Instance.OnLiveOpsReady += ApplyEventConfig;
        }
    }

    private void OnDestroy()
    {
        if (LiveOpsManager.Instance != null)
            LiveOpsManager.Instance.OnLiveOpsReady -= ApplyEventConfig;
    }

    private void Update()
    {
        if (!isAnimating || !animatePulse || eventBannerImage == null)
            return;

        pulseTimer += Time.deltaTime * pulseSpeed;
        float alpha = Mathf.Lerp(pulseMinAlpha, 1f, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);

        Color c = eventBannerImage.color;
        c.a = alpha;
        eventBannerImage.color = c;
    }

    private void ApplyEventConfig()
    {
        if (LiveOpsManager.Instance == null)
            return;

        GameConfig config = LiveOpsManager.Instance.Config;

        if (config.EventModeActive && !string.IsNullOrEmpty(config.EventModeName))
        {
            Debug.Log($"[EventModeUI] Evento activo: '{config.EventModeName}'");
            SetEventName(config.EventModeName);
            SetBannerVisible(true);
        }
        else
        {
            SetBannerVisible(false);
        }
    }

    private void SetBannerVisible(bool visible)
    {
        if (eventPanel != null)
            eventPanel.SetActive(visible);

        isAnimating = visible && animatePulse;
        pulseTimer = 0f;
    }

    private void SetEventName(string eventName)
    {
        if (eventNameText != null)
            eventNameText.text = eventName;
    }
}
