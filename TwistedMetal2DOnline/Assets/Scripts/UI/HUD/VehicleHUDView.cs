using UnityEngine;
using UnityEngine.UI;

public class VehicleHUDView : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private Image nitroFill;

    private IVehicleStats dataSource;

    private readonly Color healthHighColor = new Color(0.2f, 0.85f, 0.3f);
    private readonly Color healthMidColor = new Color(0.95f, 0.85f, 0.15f);
    private readonly Color healthLowColor = new Color(0.9f, 0.2f, 0.15f);
    private readonly Color nitroHighColor = new Color(0f, 0.77f, 1f);
    private readonly Color nitroMidColor = new Color(0f, 0.4f, 0.6f);
    private readonly Color nitroLowColor = new Color(0f, 0.15f, 0.25f);

    private void Update()
    {
        TryBindDataSource();

        if (dataSource == null)
            return;

        float health = Mathf.Clamp01(dataSource.HealthPercent);
        float nitro = Mathf.Clamp01(dataSource.NitroPercent);

        if (healthFill != null)
        {
            healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, health, Time.deltaTime * 8f);
            healthFill.color = GetHealthColor(health);
        }

        if (nitroFill != null)
        {
            nitroFill.fillAmount = Mathf.Lerp(nitroFill.fillAmount, nitro, Time.deltaTime * 8f);
            nitroFill.color = GetNitroColor(nitro);
        }
    }

    private void TryBindDataSource()
    {
        PlayerHealth localPlayer = PlayerHealth.LocalPlayerInstance;

        if (localPlayer == null)
        {
            dataSource = null;
            return;
        }

        if (dataSource != null)
            return;

        VehicleStatsAdapter adapter = localPlayer.GetComponent<VehicleStatsAdapter>();
        if (adapter != null)
            dataSource = adapter;
    }

    private Color GetHealthColor(float percent)
    {
        if (percent > 0.5f)
            return Color.Lerp(healthMidColor, healthHighColor, (percent - 0.5f) * 2f);
        return Color.Lerp(healthLowColor, healthMidColor, percent * 2f);
    }

    private Color GetNitroColor(float percent)
    {
        if (percent > 0.5f)
            return Color.Lerp(nitroMidColor, nitroHighColor, (percent - 0.5f) * 2f);
        return Color.Lerp(nitroLowColor, nitroMidColor, percent * 2f);
    }
}
