using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private bool isLocalPlayer = true;

    public bool IsLocalPlayer()
    {
        return isLocalPlayer;
    }

    public void SetLocalPlayer(bool value)
    {
        isLocalPlayer = value;
    }
}
