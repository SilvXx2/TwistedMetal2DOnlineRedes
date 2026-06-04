using UnityEngine;
using Photon.Pun;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;
using UnitySceneUtility = UnityEngine.SceneManagement.SceneUtility;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneManager : MonoBehaviour
{
    private PhotonManager photonManager;
    private bool waitingMainMenuAfterLeave = false;

#if UNITY_EDITOR
    [Header("Escenas (Scene Asset)")]
    [SerializeField] private SceneAsset victoryScreenAsset;
    [SerializeField] private SceneAsset defeatScreenAsset;
    [SerializeField] private SceneAsset mainMenuAsset;
    [SerializeField] private SceneAsset Level1Asset;
    [SerializeField] private SceneAsset Level2Asset;

#endif

    [SerializeField, HideInInspector] private string victoryScenePath;
    [SerializeField, HideInInspector] private string defeatScenePath;
    [SerializeField, HideInInspector] private string mainMenuScenePath;
    [SerializeField, HideInInspector] private string level1ScenePath;
    [SerializeField, HideInInspector] private string level2ScenePath;

    public void LoadVictoryScreen() => LoadScene(victoryScenePath);
    public void LoadDefeatScreen()  => LoadScene(defeatScenePath);
    public void LoadDefeatScene()   => LoadDefeatScreen();
    public void LoadMainMenu()
    {
        if (TryLeaveRoomBeforeMainMenu())
        {
            return;
        }

        LoadScene(mainMenuScenePath);
    }

    public void LoadLevel1()        => LoadScene(level1ScenePath);
    public void LoadLevel2()        => LoadScene(level2ScenePath);

    private void OnDestroy()
    {
        UnbindRoomLeft();
    }

    private void LoadScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return;

        int buildIndex = UnitySceneUtility.GetBuildIndexByScenePath(scenePath);
        if (buildIndex < 0) return;

        UnitySceneManager.LoadScene(buildIndex);
    }

    private bool TryLeaveRoomBeforeMainMenu()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            return false;
        }

        if (waitingMainMenuAfterLeave)
        {
            return true;
        }

        photonManager = PhotonManager.Instance;
        if (photonManager == null)
        {
            return false;
        }

        waitingMainMenuAfterLeave = true;
        photonManager.RoomLeft += OnRoomLeft;
        photonManager.LeaveCurrentRoom();
        return true;
    }

    private void OnRoomLeft()
    {
        if (!waitingMainMenuAfterLeave)
        {
            return;
        }

        waitingMainMenuAfterLeave = false;
        UnbindRoomLeft();
        LoadScene(mainMenuScenePath);
    }

    private void UnbindRoomLeft()
    {
        if (photonManager == null)
        {
            return;
        }

        photonManager.RoomLeft -= OnRoomLeft;
        photonManager = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        victoryScenePath  = GetScenePath(victoryScreenAsset);
        defeatScenePath   = GetScenePath(defeatScreenAsset);
        mainMenuScenePath = GetScenePath(mainMenuAsset);
        level1ScenePath   = GetScenePath(Level1Asset);
        level2ScenePath   = GetScenePath(Level2Asset);
    }

    private static string GetScenePath(SceneAsset sceneAsset)
    {
        if (sceneAsset == null) return string.Empty;
        return AssetDatabase.GetAssetPath(sceneAsset);
    }
#endif
}
