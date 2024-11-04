// Movement system based of of Karlson's movement system, with some modifications to allow for wallrunning and other features


//TODO: Add MagBounce function to allow for wallbouncing (Copy of Roblox parkours wallbounce)
// Fini
// Add Roll
// Add Fall damage system

using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices.WindowsRuntime;
using Packages.Rider.Editor.UnitTesting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Assignables")]
    [Tooltip("this is a reference to the MainCamera object, not the parent of it.")]
    public Transform playerCam;
    [Tooltip("reference to orientation object, needed for moving forward and not up or something.")]
    public Transform orientation;
    [Tooltip("LayerMask for ground layer, important because otherwise the collision detection wont know what ground is")]
    public LayerMask whatIsGround;
    private Rigidbody rb;

    [Header("Rotation and look")]
    private float xRotation;
    [Tooltip("mouse/look sensitivity")]
    public float sensitivity = 50f;
    private float sensMultiplier = 1.5f;

    [Header("Movement")]
    [Tooltip("additive force amount. every physics update that forward is pressed, this force (multiplied by 1/tickrate) will be added to the player.")]
    public float moveSpeed = 4500;
    [Tooltip("maximum local velocity before input is cancelled")]
    public float maxSpeed = 20;
    [Tooltip("normal countermovement when not crouching.")]
    public float counterMovement = 0.175f;
    private float threshold = 0.01f;
    [Tooltip("the maximum angle the ground can have relative to the players up direction.")]
    public float maxSlopeAngle = 35f;
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    [Tooltip("forward force for when a crouch is started.")]
    public float slideForce = 400;
    [Tooltip("countermovement when sliding. this doesnt work the same way as normal countermovement.")]
    public float slideCounterMovement = 0.2f;
    private bool readyToJump = true;
    private float jumpCooldown = 0.25f;
    [Tooltip("this determines the jump force but is also applied when jumping off of walls, if you decrease it, you may end up being able to walljump and then get back onto the wall leading to infinite height.")]
    public float jumpForce = 550f; 
    float x, y;
    bool jumping;
    public bool canWallRun;
    private Vector3 normalVector = Vector3.up;


	[Header("MagBounce")]
    public float bounceForce = 1.2f; 
    [Tooltip("The force applied to the player when they perform a MagBounce")]

    [Header("Glideing")]
    public bool isFlying;

	public bool LockedCamera = false; // Locks the camera to the player
	
    [Header("Wallrunning")]
    private float actualWallRotation;
    private float wallRotationVel;
    private Vector3 wallNormalVector;
    [Tooltip("when wallrunning, an upwards force is constantly applied to negate gravity by about half (at default), increasing this value will lead to more upwards force and decreasing will lead to less upwards force.")]
    public float wallRunGravity = 1;
    [Tooltip("when a wallrun is started, an upwards force is applied, this describes that force.")]
    public float initialForce = 20f; 
    [Tooltip("float to choose how much force is applied outwards when ending a wallrun. this should always be greater than Jump Force")]
    public float escapeForce = 600f;
    private float wallRunRotation;
    [Tooltip("how much you want to rotate the camera sideways while wallrunning")]
    public float wallRunRotateAmount = 10f;
    [Tooltip("a bool to check if the player is wallrunning because thats kinda necessary.")]
    public bool isWallRunning;
    [Tooltip("a bool to determine whether or not to actually allow wallrunning.")]
    public bool useWallrunning = true;

    [Header("Collisions")]
    [Tooltip("a bool to check if the player is on the ground.")]
    public bool grounded;
    [Tooltip("a bool to check if the player is currently crouching.")]
    public bool crouching;
    private bool surfing;
    private bool cancellingGrounded;
    private bool cancellingSurf;
    private bool cancellingWall;
    private bool onWall;
    private bool cancelling;
    


    public static PlayerMovement Instance { get; private set; }

    void Awake()
    {

        Instance = this;

        rb = GetComponent<Rigidbody>();
        
        //Create a physic material with no friction to allow for wallrunning and smooth movement not being dependant
        //and smooth movement not being dependant on the in-built unity physics engine, apart from collisions.
                PhysicMaterial mat = new PhysicMaterial("tempMat");


        mat.bounceCombine = PhysicMaterialCombine.Average;

        mat.bounciness = 0;

        mat.frictionCombine = PhysicMaterialCombine.Minimum;

        mat.staticFriction = 0;
        mat.dynamicFriction = 0;

        gameObject.GetComponent<Collider>().material = mat;
    }
    public GameObject player;
    public AudioClip particleSound;
    private Vector3 rot;
    void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        readyToJump = true;
        rot = transform.eulerAngles;
        wallNormalVector = Vector3.up;
        if (particleEffect != null)
        {
            particleEffect.SetActive(false);
        }
        if (player != null)
        {
            playerAudioSource = player.AddComponent<AudioSource>();
            playerAudioSource.clip = particleSound;
            playerAudioSource.playOnAwake = false;
        }
    }



public float percentage;

private void FixedUpdate()
{
    if (isFlying)
    {
        
    }
    else
    {
        rb.drag = 0; // Reset drag when not flying
        Movement(); // Ensure this function exists for non-flying movement
    }
}


public GameObject particleEffect; // Reference to the particle effect object
private bool effectTriggered = false;
private float speedThreshold = 16f;
public AudioSource playerAudioSource; // Reference to the AudioSource on the player
private void Update()
    {
        MyInput();
        Look();

        // Check if Rigidbody velocity magnitude reaches or exceeds the threshold
        if (rb.velocity.magnitude >= speedThreshold && !effectTriggered)
        {
            // Activate the particle effect if not already active
            if (particleEffect != null)
            {
                particleEffect.SetActive(true);
            }
             if (playerAudioSource != null && particleSound != null)
            {
                playerAudioSource.Play();
            }
            effectTriggered = true;
        }



        else if (rb.velocity.magnitude < speedThreshold && effectTriggered)
        {
            // Deactivate the particle effect if speed is below the threshold
            if (particleEffect != null)
            {
                particleEffect.SetActive(false);
            }
            
            effectTriggered = false;
        }
    }

    private void LateUpdate()
    {
        //call the wallrunning Function
        WallRunning();
        WallRunRotate();
    }

    private void WallRunRotate()
    {
        FindWallRunRotation();
        float num = 12f;
        actualWallRotation = Mathf.SmoothDamp(actualWallRotation, wallRunRotation, ref wallRotationVel, num * Time.deltaTime);
        playerCam.localRotation = Quaternion.Euler(playerCam.rotation.eulerAngles.x, playerCam.rotation.eulerAngles.y, actualWallRotation);
    }

    /// <summary>
    /// MyInput, Handle any inputs from the player
    /// </summary>


private DateTime lastJumpPressTime;
private int jumpPressCount = 0;

private void MyInput()
{
    x = Input.GetAxisRaw("Horizontal");
    y = Input.GetAxisRaw("Vertical");
    jumping = Input.GetButton("Jump");
    crouching = Input.GetKey(KeyCode.LeftControl);

    // Mag Bounce
    if (Input.GetKeyDown(KeyCode.Q))
    {
        MagBounce();
    }
    
    // grounded - Lets player know if there grounded


    // Wall climb boost

    if(Input.GetKeyDown(KeyCode.E) && IsNearWall() && !isWallRunning) {
        rb.AddForce(Vector3.up * jumpForce * 1.5f);
        rb.AddForce(Vector3.up * jumpForce * 2f);
    }

    // Wall run code can be found in the JUMP() function
    // Crouching



    if (Input.GetKeyDown(KeyCode.LeftControl))
        StartCrouch();
    if (Input.GetKeyUp(KeyCode.LeftControl))
        StopCrouch();



    // Double press space to toggle flying
    if (Input.GetKeyDown(KeyCode.Space))
    {
        DateTime now = DateTime.UtcNow;
        if ((now - lastJumpPressTime).TotalMilliseconds < 500 && !isWallRunning && !grounded &&!IsNearWall()   )
        {
            jumpPressCount++;
           
        }
        else
        {
            jumpPressCount = 1;
        }

        lastJumpPressTime = now;

        if (jumpPressCount == 2)
        {
            isFlying = !isFlying;
            LockedCamera = !LockedCamera;
            jumpPressCount = 0;
        }
        else if (isFlying)
        {
            isFlying = false;
        }
    }
}
    private void StartCrouch()
    {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.2f && grounded)
        {
            if (grounded)
            {
                rb.AddForce(orientation.transform.forward * slideForce);
            }
        }
    }

    private void StopCrouch()
    {
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
    }

    private void Movement()
    {



        //Extra gravity
        rb.AddForce(Vector3.down * Time.deltaTime * 10);

        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;

        //Counteract sliding and sloppy movement
        CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then jump
        if (readyToJump && jumping) Jump();

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            return;
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        if (!grounded)
        {
            multiplier = 0.5f;
            multiplierV = 0.5f;
        }

        // Movement while sliding
        if (grounded && crouching) multiplierV = 0f;

        //Apply forces to move player
        rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
        rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier);
    }


private bool WallRunInvoked = false; // Has the wall run been started by the player
private float wallRunStartTime;      // Tracks when the wall run started
private float wallRunDebounceTime = 0.25f; // Debounce time in seconds

private void Jump()
{
    if (isWallRunning)
    {
        // Check if enough time has passed since wall run started
        if (Time.realtimeSinceStartup - wallRunStartTime >= wallRunDebounceTime)
        {
            // Apply jump force away from the wall
            rb.AddForce(wallNormalVector * jumpForce * 1.5f);
            rb.AddForce(wallNormalVector * jumpForce * 2f);

            isWallRunning = false; // Stop wallrunning when jumping off the wall
            WallRunInvoked = false; // Reset the wallrun invoked bool
            return; // Prevent extra code from running
        }
    }

    if (!isWallRunning)
    {
        if (!WallRunInvoked && !grounded && IsNearWall())
        {
           
            WallRunInvoked = true;
            wallRunStartTime = Time.realtimeSinceStartup; // Record start time of wall run
            return; // Prevent extra code from running
        }
    }

    if ((grounded || isWallRunning || surfing) && readyToJump && !isWallRunning)
    {
      
        Vector3 velocity = rb.velocity;
        readyToJump = false;

        // Apply normal jump force
        rb.AddForce(Vector2.up * jumpForce * 1.5f);
        rb.AddForce(normalVector * jumpForce * 0.5f);

        // Adjust vertical velocity to maintain a controlled jump
        if (rb.velocity.y < 0.5f)
        {
            rb.velocity = new Vector3(velocity.x, 0f, velocity.z);
        }
        else if (rb.velocity.y > 0f)
        {
            rb.velocity = new Vector3(velocity.x, velocity.y / 2f, velocity.z);
        }

        Invoke("ResetJump", jumpCooldown);
    }
}


    private void ResetJump()
    {
        readyToJump = true;
    }

private float desiredX;
public float gravityForce = 9.8f; // Downward force for realistic glide descent
private bool isFlyingStarted = false; // Track when flying has started
public float forwardAcceleration = 5f; // Reduced acceleration rate for smoother increase
public float maxGlideSpeed = 80f; // Maximum speed when gliding forward
public float speedDecayRate = 0.5f; // Rate at which speed decays when level
private float currentSpeed; // Current speed of the player when gliding
private bool canAccelerate = true; // Prevents infinite acceleration

// <summary>
// Look, Handles the camera rotation and player orientation
// </summary>
private void Look()
{
    // Get mouse input for rotating the camera
    float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
    float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

    // Find current look rotation
    Vector3 cameraRotation = playerCam.transform.localRotation.eulerAngles;
    desiredX = cameraRotation.y + mouseX;

    // Rotate, and also make sure we don't over- or under-rotate
    xRotation -= mouseY;
    float clamp = 89.5f;
    xRotation = Mathf.Clamp(xRotation, -clamp, clamp);

    // Perform the rotations
    playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
    orientation.transform.localRotation = Quaternion.Euler(0, desiredX, 0);

    if (isFlying)
    {
        // Initialize flying with current speed only once
        if (!isFlyingStarted)
        {
            currentSpeed = Mathf.Clamp(rb.velocity.magnitude, 0, maxGlideSpeed);
            isFlyingStarted = true;
        }

        // Calculate forward direction based on camera orientation
        Vector3 forwardDirection = playerCam.transform.forward;

        // Prevent upward movement; limit to horizontal or downward glide
        if (forwardDirection.y > -0.1f)
        {
            forwardDirection.y = -0.1f;
            canAccelerate = false; // Stop acceleration when looking up or level
        }
        else
        {
            canAccelerate = true; // Allow acceleration when looking down
        }

        // Apply forward acceleration based on downward pitch angle if allowed
        if (canAccelerate)
        {
            float pitchFactor = Mathf.Clamp01(-forwardDirection.y); // Higher when looking down
            currentSpeed += forwardAcceleration * pitchFactor * Time.deltaTime;
        }
        else
        {
            // Gradually reduce speed when not accelerating
            currentSpeed -= speedDecayRate * Time.deltaTime;
        }

        // Clamp speed to avoid freezing or going negative
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxGlideSpeed);

        // Apply calculated velocity in the forward direction
        rb.velocity = forwardDirection * currentSpeed;

        // Apply gravity for a realistic descent
        rb.AddForce(Vector3.down * gravityForce, ForceMode.Acceleration);


    }
    else
    {
        // Reset when not flying
        rb.drag = 0;
        isFlyingStarted = false; // Reset flag for the next start of gliding
        canAccelerate = false; // Prevent unintended acceleration
    }
}

    // <summary>
    // CounterMovement, Counteracts the movement of the player
    // </summary>
    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        //Slow down sliding
        if (crouching)
        {
            rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
        {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
        {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook()
    {
        float lookAngle = orientation.transform.eulerAngles.y;
        float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

        float u = Mathf.DeltaAngle(lookAngle, moveAngle);
        float v = 90 - u;

        float magnitue = rb.velocity.magnitude;
        float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
        float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

        return new Vector2(xMag, yMag);
    }
    //a lot of math (dont touch)
    private void FindWallRunRotation()
    {

        if (!isWallRunning)
        {
            wallRunRotation = 0f;
            return;
        }
        _ = new Vector3(0f, playerCam.transform.rotation.y, 0f).normalized;
        new Vector3(0f, 0f, 1f);
        float num = 0f;
        float current = playerCam.transform.rotation.eulerAngles.y;
        if (Math.Abs(wallNormalVector.x - 1f) < 0.1f)
        {
            num = 90f;
        }
        else if (Math.Abs(wallNormalVector.x - -1f) < 0.1f)
        {
            num = 270f;
        }
        else if (Math.Abs(wallNormalVector.z - 1f) < 0.1f)
        {
            num = 0f;
        }
        else if (Math.Abs(wallNormalVector.z - -1f) < 0.1f)
        {
            num = 180f;
        }
        num = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), wallNormalVector, Vector3.up);
        float num2 = Mathf.DeltaAngle(current, num);
        wallRunRotation = (0f - num2 / 90f) * wallRunRotateAmount;
        if (!useWallrunning)
        {
            return;
        }
        if ((Mathf.Abs(wallRunRotation) < 4f && y > 0f && Math.Abs(x) < 0.1f) || (Mathf.Abs(wallRunRotation) > 22f && y < 0f && Math.Abs(x) < 0.1f))
        {
            if (!cancelling)
            {
                cancelling = true;
                CancelInvoke("CancelWallrun");
                Invoke("CancelWallrun", 0.2f);
            }
        }
        else
        {
            cancelling = false;
            CancelInvoke("CancelWallrun");
        }
    }


    private bool IsSurf(Vector3 v)
    {
        float num = Vector3.Angle(Vector3.up, v);
        if (num < 89f)
        {
            return num > maxSlopeAngle;
        }
        return false;
    }

    private bool IsWall(Vector3 v)
    {
        return Math.Abs(90f - Vector3.Angle(Vector3.up, v)) < 0.05f;
    }

    private bool IsRoof(Vector3 v)
    {
        return v.y == -1f;
    }

    /// <summary>
    /// Handle ground detection 
    /// OnCollisionStay is called once per frame for every Collider or Rigidbody that touches another Collider or Rigidbody.
    /// </summary> 
    
    public Vector3 wallRunNormal; // The normal of the wall the player is running on
    public bool isVaildWall = false; // Not in use 
    public float WallRunTimer;
private void OnCollisionStay(Collision other)
{
    int layer = other.gameObject.layer;
    if ((int)whatIsGround != ((int)whatIsGround | (1 << layer)))
    {
        return;
    }

    for (int i = 0; i < other.contactCount; i++)
    {
        Vector3 normal = other.contacts[i].normal;

        if (IsFloor(normal))
        {
            if (isWallRunning)
            {
                isWallRunning = false;
                WallRunInvoked = false;
            }

            canWallRun = false;
            grounded = true;
            normalVector = normal;
            cancellingGrounded = false;
            CancelInvoke("StopGrounded");

            // Stop flying when grounded
            isFlying = false;
            LockedCamera = false;



        }

        if (IsWall(normal) && (layer == (int)whatIsGround || (int)whatIsGround == -1 || layer == LayerMask.NameToLayer("Ground") || layer == LayerMask.NameToLayer("ground")))
        {
            isVaildWall = true;
            canWallRun = true;
            wallRunNormal = normal;
            if (WallRunInvoked)
            {
                WallRunTimer = Time.realtimeSinceStartup;
                StartWallRun(normal);
                onWall = true;
                cancellingWall = false;
                CancelInvoke("StopWall");
            }
        }
        else
        {
            isVaildWall = false;
        }

        if (IsSurf(normal))
        {
            surfing = true;
            cancellingSurf = false;
            CancelInvoke("StopSurf");
        }

        IsRoof(normal);
    }

    float num = 3f;
    if (!cancellingGrounded)
    {
        cancellingGrounded = true;
        Invoke("StopGrounded", Time.deltaTime * num);
    }
    if (!cancellingWall)
    {
        cancellingWall = true;
        Invoke("StopWall", Time.deltaTime * num);
    }
    if (!cancellingSurf)
    {
        cancellingSurf = true;
        Invoke("StopSurf", Time.deltaTime * num);
    }
}

    private void StopGrounded()
    {
        grounded = false;
    }

    private void StopWall()
    {
        onWall = false;
        isWallRunning = false;
        WallRunInvoked = false; 
    }

    private void StopSurf()
    {
        surfing = false;
    }

// Functions for movement 


private void MagBounce() // Bounce off wall (Inspired by Roblox Parkour MagRail)
{
    if (IsNearWall() && Input.GetKeyDown(KeyCode.Q))
    {
            rb.AddForce(wallNormalVector * jumpForce * 1.5f);
            rb.AddForce(wallNormalVector * jumpForce * 2f);

            isWallRunning = false; // Stop wallrunning when jumping off the wall
            WallRunInvoked = false; // Reset the wallrun invoked bool

        Debug.Log("MagBounce activated: Launching player!");
    }
}


// Wallrunning functions
private void CancelWallrun()
{
   
    Invoke("CancelWallrun", 0.2f);
    rb.AddForce(wallNormalVector * escapeForce, ForceMode.Impulse);
    isWallRunning = false;
    WallRunInvoked = false; 
}

private void StartWallRun(Vector3 normal)
{
    if (!grounded)
    {
       
        wallNormalVector = normal;
        if (!isWallRunning)
        {
            
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * initialForce, ForceMode.Impulse);
        }
        isWallRunning = true;
    }
}

// Handles the wallrunning forces and gravity
private void WallRunning()
{
    if (isWallRunning)
    {
        rb.AddForce(-wallNormalVector * Time.deltaTime * moveSpeed);
        rb.AddForce(Vector3.up * Time.deltaTime * rb.mass * wallRunGravity * -Physics.gravity.y * 0.4f);
    }
}



/*
 ___      ___   _______ 
|   |    |   | |  _    |
|   |    |   | | |_|   |
|   |    |   | |       |
|   |___ |   | |  _   | 
|       ||   | | |_|   |
|_______||___| |_______|

Baisc functions for utility

*/



// <summary>
// IsNearWall, Checks if a player is near a wall (Returns bool)
// </summary>
private bool IsNearWall()
{
    RaycastHit hit;
    float checkDistance = 1.0f;
    if (Physics.Raycast(transform.position, transform.right, out hit, checkDistance) ||
        Physics.Raycast(transform.position, -transform.right, out hit, checkDistance) ||
        Physics.Raycast(transform.position, transform.forward, out hit, checkDistance) ||
        Physics.Raycast(transform.position, -transform.forward, out hit, checkDistance))
    {
        return true;
    }
    return false;
}

    // <summary>
    // IsFloor, Checks if a vector is a floor (Returns Vector3)
    // </summary>
    private bool IsFloor(Vector3 v)
    {
        return Vector3.Angle(Vector3.up, v) < maxSlopeAngle;
    }


}


