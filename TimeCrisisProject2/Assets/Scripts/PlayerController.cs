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
    [SerializeField] private Vector2 screenBounds = new Vector2(0.1f, 0.1f); // Now 10% from edges instead of 80%
    private Vector3 viewportPoint;
    private float lockedZPosition;  // This will now store local Z position

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
            // Store the local Z position relative to parent
            lockedZPosition = characterModel.localPosition.z;
        }
        
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
        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Fire();
        }
    }

    public void movePayerWithAim()
    {
        if (characterModel != null)
        {
            // Rotation code
            Vector3 direction = rotationTarget - characterModel.position;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
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

            if (isAcc)
            {
                if (speed < maxSpeed)
                {
                    speed += accCounter;
                } else if (speed > maxSpeed)
                {
                    speed = maxSpeed;
                }
            }
            else
            {
                if (speed > 0f)
                {
                    speed -= accCounter;
                } else if (speed < 0f)
                {
                    speed = 0f;
                }
            }

            // Movement with screen bounds check - don't normalize initially
            Vector3 movement = new Vector3(move.x, move.y, 0f);  // Removed .normalized
            
            // Try movement on each axis separately
            Vector3 currentPos = characterModel.position;
            Vector3 newPositionX = currentPos + new Vector3(movement.x, 0, 0) * speed * Time.deltaTime;
            Vector3 newPositionY = currentPos + new Vector3(0, movement.y, 0) * speed * Time.deltaTime;
            
            // Convert both positions to local space
            Vector3 localPosX = characterModel.parent.InverseTransformPoint(newPositionX);
            Vector3 localPosY = characterModel.parent.InverseTransformPoint(newPositionY);
            localPosX.z = lockedZPosition;
            localPosY.z = lockedZPosition;
            
            // Convert back to world space
            newPositionX = characterModel.parent.TransformPoint(localPosX);
            newPositionY = characterModel.parent.TransformPoint(localPosY);

            Vector3 viewportPointX = mainCamera.WorldToViewportPoint(newPositionX);
            Vector3 viewportPointY = mainCamera.WorldToViewportPoint(newPositionY);
            
            Vector3 finalMovement = Vector3.zero;
            
            // Check X bounds
            if (viewportPointX.x >= screenBounds.x && viewportPointX.x <= (1 - screenBounds.x))
            {
                finalMovement.x = movement.x;
            }
            
            // Check Y bounds
            if (viewportPointY.y >= screenBounds.y && viewportPointY.y <= (1 - screenBounds.y))
            {
                finalMovement.y = movement.y;
            }

            // Normalize the final movement after determining valid directions
            if (finalMovement != Vector3.zero)
            {
                finalMovement.Normalize();
                Vector3 newPosition = currentPos + finalMovement * speed * Time.deltaTime;
                Vector3 localPos = characterModel.parent.InverseTransformPoint(newPosition);
                localPos.z = lockedZPosition;
                characterModel.position = characterModel.parent.TransformPoint(localPos);
            }

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

    // Optional: Helper method to visualize bounds in Scene view
    void OnDrawGizmos()
    {
        if (mainCamera != null)
        {
            // Draw screen bounds
            Vector3 bl = mainCamera.ViewportToWorldPoint(new Vector3(screenBounds.x, screenBounds.y, 10));
            Vector3 tr = mainCamera.ViewportToWorldPoint(new Vector3(1 - screenBounds.x, 1 - screenBounds.y, 10));
            Vector3 br = mainCamera.ViewportToWorldPoint(new Vector3(1 - screenBounds.x, screenBounds.y, 10));
            Vector3 tl = mainCamera.ViewportToWorldPoint(new Vector3(screenBounds.x, 1 - screenBounds.y, 10));

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }
    }
}
