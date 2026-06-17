using System.Collections;
using TMPro;
using UnityEngine;

public class TauntNotifier : MonoBehaviour
{
    public static TauntNotifier Instance { get; private set; }

    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float displayDuration = 3f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowTaunt(string attackerName, string victimName, string tauntText)
    {
        if (notificationText == null)
        {
            Debug.LogWarning($"[TauntNotifier] UI References missing. Msg: {attackerName} -> {victimName}: {tauntText}");
            return;
        }

        notificationText.text = $"<color=red>{attackerName}</color> se burla de <color=blue>{victimName}</color>:\n\"{tauntText}\"";

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        notificationText.gameObject.SetActive(true);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        notificationText.gameObject.SetActive(false);
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
