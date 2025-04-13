using UnityEngine;
using UnityEngine.InputSystem;  // Make sure this is added

public class PlayerController : MonoBehaviour
{
    // Add these variables
    private Vector2 lookInput;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float accCounter = 0.1f;
    private float nextFireTime;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private Transform characterModel;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float maxRotationX = 60f;
    [SerializeField] private float maxRotationY = 80f;
    private Vector3 defaultModelForward;
    private Vector2 move;
    private Vector2 mouseLook;
    private Vector2 joystickLook;
    private Vector3 rotationTarget;
    private float lastMoveStateX;
    private float lastMoveStateY;
    private bool isAcc;
    [SerializeField] private bool isPc = true;
    [SerializeField] private float fireRate = 0.2f;  // Time between shots
    private bool isFiring = false;  // Add this variable to track firing state
    [SerializeField] private Vector2 movementBounds = new Vector2(3.5f, 2.5f);  // Local offset bounds
    private Vector3 parentCenter;  // Center point for local boundaries
    [SerializeField] private LayerMask aimLayerMask;  // Keep this for aim plane detection

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    public void OnMouseLook(InputAction.CallbackContext context)
    {
        mouseLook = context.ReadValue<Vector2>();
    }

    public void OnJoystickLook(InputAction.CallbackContext context)
    {
        joystickLook = context.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext value)
    {
        // Update firing state based on button press/release
        if (value.started || value.performed)
        {
            isFiring = true;
        }
        else if (value.canceled)
        {
            isFiring = false;
        }
    }

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        nextFireTime = 0f;

        if (characterModel != null)
        {
            defaultModelForward = characterModel.forward;
        }
        
        // Store the parent's position as our center point
        parentCenter = transform.parent ? transform.parent.position : transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Cast ray from mouse position to aim plane
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        
        // Use raycast to get aim point, with fallback
        if (Physics.Raycast(ray, out hit, 1000f, aimLayerMask))
        {
            rotationTarget = hit.point;
        }
        else
        {
            // Fallback to a point at fixed distance if no hit
            rotationTarget = ray.GetPoint(10f);
        }

        movePayerWithAim();
        Debug.DrawLine(characterModel.position, rotationTarget, Color.red);

        // Check for firing with rate limiting
        if (isFiring && Time.time >= nextFireTime)  // Only fires if enough time has passed
        {
            nextFireTime = Time.time + fireRate;     // Sets the next allowed fire time
            Fire();
        }
    }

    public void movePayerWithAim()
    {
        if (characterModel != null)
        {
            // Rotation - now looking at mouse in XY plane
            Vector3 direction = rotationTarget - characterModel.position;
            
            if (direction != Vector3.zero)
            {
                // Calculate rotation to look at mouse while staying upright
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                
                // Only take the X and Y rotation
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, 0);
                
                characterModel.rotation = Quaternion.Slerp(
                    characterModel.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }

            // Acceleration logic
            if (move.x != 0 || move.y != 0)
            {
                isAcc = true;
            }
            else if (move.x == 0 && move.y == 0)
            {
                isAcc = false;
            }

            if (isAcc == true)
            {
                if (speed < maxSpeed)
                {
                    speed += accCounter;
                } else if (speed > maxSpeed)
                {
                    speed = maxSpeed;
                }
            }else if(isAcc == false)
            {
                if (speed > 0f)
                {
                    speed -= accCounter;
                } else if (speed < 0f)
                {
                    speed = 0f;
                }
            }

            // Boundary checking relative to parent center
            if (characterModel.position.x < parentCenter.x - movementBounds.x && move.x <= 0 || 
                characterModel.position.x > parentCenter.x + movementBounds.x && move.x >= 0)
            {
                move.x = 0;
            }
            if (characterModel.position.y < parentCenter.y - movementBounds.y && move.y <= 0 || 
                characterModel.position.y > parentCenter.y + movementBounds.y && move.y >= 0)
            {
                move.y = 0;
            }

            // Movement
            Vector3 movement = new Vector3(move.x, move.y, 0f).normalized;
            characterModel.Translate(movement * speed * Time.deltaTime, Space.World);
            lastMoveStateX = move.x;
            lastMoveStateY = move.y;
        }
    }

    private void Fire()
    {
        if (projectileSpawnPoint != null && projectilePrefab != null)
        {
            // Use the model's rotation for the firing direction
            Vector3 fireDirection = characterModel.forward;
            
            // Spawn the projectile at the spawn point
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, characterModel.rotation);
            
            // Initialize the bullet's velocity
            PlayerBullet bullet = projectile.GetComponent<PlayerBullet>();
            if (bullet != null)
            {
                bullet.Initialize(fireDirection * projectileSpeed);
            }
        }
    }
}
