using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerNicknameDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private Transform rotationLockedTransform;
    
    [Header("Position Offset Settings")]
    [Tooltip("If true, uses the initial local position of the rotationLockedTransform as the world offset.")]
    [SerializeField] private bool useInitialLocalPositionAsOffset = true;
    [Tooltip("The world-space offset from the player's root position to display the nickname.")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0, 2f, 0);

    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = GetComponentInParent<PhotonView>();
        }
    }

    private void Start()
    {
        if (nicknameText == null)
        {
            nicknameText = GetComponentInChildren<TMP_Text>();
        }

        if (rotationLockedTransform == null && nicknameText != null)
        {
            Canvas parentCanvas = nicknameText.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.transform != transform)
            {
                rotationLockedTransform = parentCanvas.transform;
            }
            else
            {
                rotationLockedTransform = nicknameText.transform;
            }
        }

        if (rotationLockedTransform != null && useInitialLocalPositionAsOffset)
        {
            worldOffset = rotationLockedTransform.localPosition;
        }

        UpdateNickname();
    }

    private void UpdateNickname()
    {
        if (nicknameText == null)
        {
            Debug.LogWarning("[PlayerNicknameDisplay] No TMP_Text component found or assigned.");
            return;
        }

        if (photonView != null && photonView.Owner != null)
        {
            string name = photonView.Owner.NickName;
            if (string.IsNullOrEmpty(name))
            {
                name = $"Player {photonView.Owner.ActorNumber}";
            }
            nicknameText.text = name;
        }
        else
        {
            // Fallback for single-player/offline or before network is ready
            string name = PhotonNetwork.NickName;
            if (string.IsNullOrEmpty(name))
            {
                name = SaveManager.Instance.CurrentSave.PlayerNickname;
                if (string.IsNullOrEmpty(name))
                {
                    name = "Player";
                }
            }
            nicknameText.text = name;
        }
    }

    private void LateUpdate()
    {
        if (rotationLockedTransform != null)
        {
            // Force world position to follow the parent root position with a constant world offset
            rotationLockedTransform.position = transform.position + worldOffset;
            
            // Force world rotation to be identity so the text remains horizontal and upright
            rotationLockedTransform.rotation = Quaternion.identity;
        }
    }
}
