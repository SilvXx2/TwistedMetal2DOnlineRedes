using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

internal sealed class RestartMatchButtonsController
{
    private readonly Button[] restartMatchButtons;
    private readonly Object persistentTarget;
    private readonly string persistentMethodName;
    private readonly UnityAction clickAction;

    public RestartMatchButtonsController(
        Button[] restartMatchButtons,
        Object persistentTarget,
        string persistentMethodName,
        UnityAction clickAction)
    {
        this.restartMatchButtons = restartMatchButtons;
        this.persistentTarget = persistentTarget;
        this.persistentMethodName = persistentMethodName;
        this.clickAction = clickAction;
    }

    public void SetInteractable(bool canInteract)
    {
        if (restartMatchButtons == null)
        {
            return;
        }

        for (int i = 0; i < restartMatchButtons.Length; i++)
        {
            Button restartButton = restartMatchButtons[i];
            if (restartButton != null)
            {
                restartButton.interactable = canInteract;
            }
        }
    }

    public void SetListeners(bool register)
    {
        if (restartMatchButtons == null)
        {
            return;
        }

        for (int i = 0; i < restartMatchButtons.Length; i++)
        {
            Button restartButton = restartMatchButtons[i];
            if (restartButton == null)
            {
                continue;
            }

            restartButton.onClick.RemoveListener(clickAction);

            if (register && !HasPersistentCallback(restartButton))
            {
                restartButton.onClick.AddListener(clickAction);
            }
        }
    }

    private bool HasPersistentCallback(Button restartButton)
    {
        int persistentEventCount = restartButton.onClick.GetPersistentEventCount();
        for (int i = 0; i < persistentEventCount; i++)
        {
            Object target = restartButton.onClick.GetPersistentTarget(i);
            string methodName = restartButton.onClick.GetPersistentMethodName(i);

            if (target == persistentTarget && methodName == persistentMethodName)
            {
                return true;
            }
        }

        return false;
    }
}
