using UnityEngine;

public class CanvasKeyLoader : MonoBehaviour
{
    [Header("Canvas objetivo")]
    [SerializeField] private GameObject targetCanvas;

    [Header("Input")]
    [SerializeField] private KeyCode activationKey = KeyCode.Escape;

    [Header("Opciones")]
    [Tooltip("Desactiva los canvases hermanos del target dentro del mismo padre al activar.")]
    [SerializeField] private bool deactivateSiblings = true;

    [Header("Bloqueo")]
    [Tooltip("Mientras este canvas esté activo, el CanvasKeyLoader no funcionará.")]
    [SerializeField] private GameObject[] blockingCanvases;

    private void Update()
    {
        if (Input.GetKeyDown(activationKey))
            SwitchCanvas();
    }

    public void SwitchCanvas()
    {
        if (targetCanvas == null)
        {
            return;
        }

        if (IsBlocked())
        {
            return;
        }


        bool activate = !targetCanvas.activeSelf;

        if (deactivateSiblings && targetCanvas.transform.parent != null)
        {
            var parent = targetCanvas.transform.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i).gameObject;
                if (child != targetCanvas && child.activeSelf)
                    child.SetActive(false);
            }
        }

        targetCanvas.SetActive(activate);
    }

    private bool IsBlocked()
    {
        if (blockingCanvases == null || blockingCanvases.Length == 0)
            return false;

        foreach (var canvas in blockingCanvases)
        {
            if (canvas != null && canvas.activeSelf)
                return true;
        }

        return false;
    }

    #region API Pública
    public void SetTargetCanvas(GameObject go) => targetCanvas = go;
    public void SetActivationKey(KeyCode key) => activationKey = key;
    public void SetDeactivateSiblings(bool value) => deactivateSiblings = value;
    #endregion
}