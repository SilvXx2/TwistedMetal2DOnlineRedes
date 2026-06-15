using UnityEngine;
using Photon.Pun;
using System.Collections;

public class RayGunWeapon : MonoBehaviourPun
{
    [Header("Visual")]
    [SerializeField] private Sprite equippedSprite;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localScale = Vector3.one;
    [SerializeField] private int sortingOrder = 10;

    [Header("Shoot")]
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private int damage = 10;

    [Header("Laser Visual")]
    [SerializeField] private float lineDuration = 0.05f;
    [SerializeField] private float lineWidth = 0.4f;
    [SerializeField] private Color lineColor = Color.cyan;

    private GameObject weaponObject;
    private Transform weaponPivot;
    private LineRenderer lineRenderer;

    private bool activeWeapon;
    private float nextFire;

    private void Awake()
    {
        CreateWeaponVisual();
        SetupLineRenderer();
    }

    public void Activate()
    {
        activeWeapon = true;
        weaponObject.SetActive(true);
    }

    private void Update()
    {
        if (!activeWeapon || !photonView.IsMine)
            return;

        RotateToMouse();

        if (Input.GetMouseButton(0)
            && Time.time >= nextFire)
        {
            nextFire = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector2 origin = weaponPivot.position;
        Vector2 direction = weaponPivot.right;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, maxDistance);

        Collider2D target = null;
        Vector3 hitPoint = origin + direction * maxDistance;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.transform
                .IsChildOf(transform))
                continue;

            PhotonView targetView =
                hit.collider.GetComponent<PhotonView>()
                ?? hit.collider.GetComponentInParent<PhotonView>();

            if (targetView != null
                && targetView.ViewID == photonView.ViewID)
                continue;

            PlayerHealth health = hit.collider.GetComponent<PlayerHealth>()
                ?? hit.collider.GetComponentInParent<PlayerHealth>();

            if (health == null)
                continue;

            target = hit.collider;
            hitPoint = hit.point;

            health.photonView.RPC("TakeDamage", RpcTarget.All, damage, photonView.OwnerActorNr);
            break;
        }

        photonView.RPC(nameof(RPC_ShowLaser), RpcTarget.All, (Vector3)origin, hitPoint);
    }

    [PunRPC]
    private void RPC_ShowLaser(
        Vector3 start,
        Vector3 end)
    {
        StopAllCoroutines();
        StartCoroutine(
            LaserRoutine(start, end)
        );
    }

    private IEnumerator LaserRoutine(
        Vector3 start,
        Vector3 end)
    {
        lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        yield return new WaitForSeconds(
            lineDuration
        );

        lineRenderer.enabled = false;
    }

    private void RotateToMouse()
    {
        Vector3 mouse =
            Camera.main.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouse.z = 0f;

        Vector2 dir =
            mouse - weaponPivot.position;

        float angle =
            Mathf.Atan2(
                dir.y,
                dir.x
            ) * Mathf.Rad2Deg;

        weaponPivot.rotation =
            Quaternion.Euler(
                0,
                0,
                angle
            );
    }

    private void CreateWeaponVisual()
    {
        weaponObject =
            new GameObject("RayGun");

        weaponObject.transform.SetParent(
            transform,
            false
        );

        weaponObject.transform.localPosition =
            localPosition;

        weaponObject.transform.localScale =
            localScale;

        SpriteRenderer sr =
            weaponObject.AddComponent<SpriteRenderer>();

        sr.sprite = equippedSprite;
        sr.sortingOrder = sortingOrder;

        weaponPivot =
            weaponObject.transform;

        weaponObject.SetActive(false);
    }

    private void SetupLineRenderer()
    {
        lineRenderer =
            gameObject.AddComponent<LineRenderer>();

        lineRenderer.enabled = false;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth * 0.5f;

        lineRenderer.material =
            new Material(
                Shader.Find(
                    "Sprites/Default"
                )
            );

        lineRenderer.startColor =
            lineColor;

        lineRenderer.endColor =
            lineColor;

        lineRenderer.sortingOrder = 100;
    }
}
