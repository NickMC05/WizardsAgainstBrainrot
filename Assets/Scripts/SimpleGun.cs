using UnityEngine;

public class SimpleGun : MonoBehaviour
{
    public GameObject bulletPrefab;  // Drag your bullet prefab here in the Inspector
    public Transform firingPoint;     // The point from which the bullet will be fired
    public float bulletSpeed = 20f;   // Speed of the bullet

    void Update()
    {
        // Check if the left mouse button is pressed
        if (Input.GetMouseButtonDown(0)) // 0 corresponds to the left mouse button
        {
            FireBullet();
        }
    }

    void FireBullet()
    {
        // Instantiate the bullet at the firing point's position and rotation
        GameObject bullet = Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);

        // Get the Rigidbody component and set its velocity
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.linearVelocity = firingPoint.forward * bulletSpeed;
    }
}