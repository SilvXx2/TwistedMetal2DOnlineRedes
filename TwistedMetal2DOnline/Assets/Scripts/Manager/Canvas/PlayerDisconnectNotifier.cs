using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class PlayerDisconnectNotifier : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [Tooltip("Panel raíz de la notificación (se activa/desactiva).")]
    [SerializeField] private GameObject notificationPanel;

    [Tooltip("Texto TMP donde se muestra el mensaje de desconexión.")]
    [SerializeField] private TextMeshProUGUI notificationText;

    [Header("Configuración")]
    [Tooltip("Segundos que permanece visible la notificación.")]
    [SerializeField] private float displayDuration = 4f;

    private Coroutine hideRoutine;

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (notificationPanel == null || notificationText == null)
        {
            return;
        }

        string colorName = PlayerDisconnectColorResolver.GetColorName(otherPlayer);
        Color playerColor = PlayerDisconnectColorResolver.GetColor(otherPlayer);

        string hexColor = ColorUtility.ToHtmlStringRGB(playerColor);
        notificationText.text = $"Jugador <color=#{hexColor}>{colorName}</color> se ha desconectado";

        ShowNotification();
    }

    private void ShowNotification()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        notificationPanel.SetActive(true);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        notificationPanel.SetActive(false);
        hideRoutine = null;
    }

    private void OnDisable()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }
}
