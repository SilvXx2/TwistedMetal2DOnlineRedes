using UnityEngine;

public class DestroyAfterAnimation : MonoBehaviour
{
    [SerializeField] private float destroyTime = 0.8f;

    private void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
