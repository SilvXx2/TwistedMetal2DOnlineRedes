using UnityEngine;

public class ObstacleZoneMarker : MonoBehaviour
{
    [Tooltip("ID único de esta zona. Debe coincidir exactamente con el 'zoneId' en el JSON del Remote Config.")]
    [SerializeField] private string zoneId;

    public string ZoneId => zoneId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            Debug.LogWarning($"[ObstacleZoneMarker] El GameObject '{gameObject.name}' no tiene un Zone ID configurado.", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"[Zone: {zoneId}]");
    }
#endif
}
