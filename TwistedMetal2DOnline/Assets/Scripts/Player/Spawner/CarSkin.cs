using Photon.Pun;
using UnityEngine;

public class CarSkin : MonoBehaviourPun
{
    private const string CarSkinKey = "CarSkin";

    [SerializeField] private SpriteRenderer carSpriteRenderer;
    [SerializeField] private Sprite[] carSprites;

    private void Awake()
    {
        if (carSpriteRenderer == null)
            carSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        int skinIndex = 0;

        if (photonView.Owner != null &&
            photonView.Owner.CustomProperties.TryGetValue(CarSkinKey, out object value))
        {
            skinIndex = (int)value;
        }

        SetSkin(skinIndex);
    }

    private void SetSkin(int index)
    {
        if (carSprites == null || carSprites.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, carSprites.Length - 1);
        carSpriteRenderer.sprite = carSprites[index];
    }
}