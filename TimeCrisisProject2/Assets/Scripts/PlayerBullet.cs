using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float bulletLifetime = 5f;    // How long before bullet destroys itself
    [SerializeField] private float damage = 10f;           // How much damage this bullet deals
    [SerializeField] private LayerMask collisionLayers;  // This will show up as a layer selection in inspector
    private Vector3 bulletVelocity;

    public void Initialize(Vector3 velocity)
    {
        bulletVelocity = velocity;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Destroy bullet after lifetime expires
        Destroy(gameObject, bulletLifetime);
    }

    void Update()
    {
        // Move the bullet manually each frame
        transform.position += bulletVelocity * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)  // Changed from OnCollisionEnter
    {
        // Check if the collided object's layer is in our collision mask
        if ((collisionLayers & (1 << other.gameObject.layer)) != 0)
        {
            Debug.Log($"Bullet hit valid target: {other.gameObject.name} on layer: {LayerMask.LayerToName(other.gameObject.layer)}");
            Destroy(gameObject);
        }
    }
}
