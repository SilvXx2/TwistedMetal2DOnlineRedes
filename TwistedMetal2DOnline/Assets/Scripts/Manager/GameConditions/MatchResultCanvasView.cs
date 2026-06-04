using UnityEngine;

internal sealed class MatchResultCanvasView
{
    private readonly GameObject gameplayCanvas;
    private readonly GameObject victoryCanvas;
    private readonly GameObject defeatCanvas;

    public MatchResultCanvasView(GameObject gameplayCanvas, GameObject victoryCanvas, GameObject defeatCanvas)
    {
        this.gameplayCanvas = gameplayCanvas;
        this.victoryCanvas = victoryCanvas;
        this.defeatCanvas = defeatCanvas;
    }

    public void ShowGameplayOnly()
    {
        SetCanvasActive(gameplayCanvas, true);
        SetCanvasActive(victoryCanvas, false);
        SetCanvasActive(defeatCanvas, false);
    }

    public void ShowResult(bool localPlayerWon)
    {
        SetCanvasActive(gameplayCanvas, false);
        SetCanvasActive(victoryCanvas, localPlayerWon);
        SetCanvasActive(defeatCanvas, !localPlayerWon);
    }

    private static void SetCanvasActive(GameObject canvas, bool active)
    {
        if (canvas != null)
        {
            canvas.SetActive(active);
        }
    }
}
