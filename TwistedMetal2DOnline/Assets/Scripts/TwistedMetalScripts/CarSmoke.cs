using UnityEngine;

public class CarSmoke : MonoBehaviour
{
    public GameObject smoke;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        smoke.SetActive(rb.velocity.magnitude > 0.1f);
    }
}
