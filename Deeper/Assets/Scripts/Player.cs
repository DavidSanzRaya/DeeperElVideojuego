using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private BoxCollider2D colider;
    private float time;
    [SerializeField]
    private LayerMask mask;

    private bool jump;
    private bool jumpHeld;
    private float move;
    public float Move {get { return move;}}

    private bool useJump;
    private bool doubleJumpUsed;
    private float whenJumpPressed;
    private float whenGroudedStoped;
    private bool bufferedJumpAvailable;
    private bool coyoteTimeAvailable;
    private bool endedJumpEarly;
    private bool grounded;
    public bool Grounded { get { return grounded;} }
    private bool lookingRight;

    private Vector2 velocity;
    public Vector2 Velocity { get { return velocity;}}

    [SerializeField]
    private Animator anim;

    [SerializeField]
    private float distanceForGrounded = 0.05f;
    [SerializeField]
    private float bufferer = 0.07f;
    [SerializeField]
    private float coyoteTime = 0.07f;
    [SerializeField]
    private float jumpForce = 25;
    [SerializeField]
    private float doubleJumpForce = 20;

    [SerializeField]
    private float groundDeceleration = 70;
    [SerializeField]
    private float airDeceleration = 60;
    [SerializeField]
    private float maxSpeed = 10;
    [SerializeField]
    private float acceleration = 100;

    [SerializeField]
    private float groundedGravity = -1.5f;
    [SerializeField]
    private float gravity = 65;
    [SerializeField]
    private float maxFallSpeed = 20;
    [SerializeField]
    private float endedJumpEarlyGravityMultyplier = 3;

    [SerializeField]
    private GameObject deathAnim;

    [SerializeField]
    private AudioClip jumpClip;
    [SerializeField]
    private AudioClip dieClip;

    private AudioSource audioSource;

    private void OnEnable()
    {
        time = 0;
        move = 0;
        jump = false;
        useJump = false;
        jumpHeld = false;

        
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        colider = GetComponent<BoxCollider2D>();
        Physics2D.queriesStartInColliders = false;
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        Death.OnEndDeathAnimation += Respawn;
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        GetInputs();
        HandleCollisions();
        HandleJumps();
        HandleXVelocity();
        HandleYVelocity();
        rb.velocity = velocity;
    }

    private void GetInputs()
    {
        if (jump)
        {
            jump = false;
            useJump = true;
            whenJumpPressed = time;
        }

        if (move < 0)
        {
            lookingRight = true;
        }
        else if (move > 0)
        {
            lookingRight = false;
        }
    }

    private void HandleCollisions()
    {
        bool groundCollision = Physics2D.BoxCast(new Vector2(transform.position.x + colider.offset.x, transform.position.y + colider.offset.y), colider.size, 0, Vector2.down, distanceForGrounded, ~mask);
        bool ceilingCollision = Physics2D.BoxCast(transform.position, colider.size, 0, Vector2.up, distanceForGrounded, ~mask);

        if (ceilingCollision)
        {
            velocity.y = Mathf.Min(0, velocity.y);
        }

        if (!grounded && groundCollision)
        {
            grounded = true;
            coyoteTimeAvailable = true;
            bufferedJumpAvailable = true;
            endedJumpEarly = false;
            doubleJumpUsed = false;
        }

        else if (!groundCollision && grounded)
        {
            grounded = false;
            whenGroudedStoped = time;
        }
    }

    private void HandleJumps()
    {
        if (!endedJumpEarly && !grounded && !jumpHeld && rb.velocity.y > 0)
        {
            endedJumpEarly = true;
        }

        if (useJump)
        {
            if (bufferedJumpAvailable && time < whenJumpPressed + bufferer)
            {
                if (grounded || coyoteTimeAvailable && time < whenGroudedStoped + coyoteTime)
                {
                    Jump();
                }
            }
            else
            {
                if (!doubleJumpUsed)
                {
                    DoubleJump();
                }
                else
                {
                    useJump = false;
                }
            }
        }
    }

    private void Jump()
    {
        audioSource.PlayOneShot(jumpClip);
        useJump = false;
        endedJumpEarly = false;
        bufferedJumpAvailable = false;
        coyoteTimeAvailable = false;
        velocity.y = jumpForce;
        anim.SetTrigger("Jump");
    }

    private void DoubleJump()
    {
        endedJumpEarly = false;
        velocity.y = doubleJumpForce;
        doubleJumpUsed = true;
        anim.SetTrigger("Jump");
        audioSource.PlayOneShot(jumpClip);
    }

    private void HandleXVelocity()
    {
        if (move == 0)
        {
            float deceleration = grounded ? groundDeceleration : airDeceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            velocity.x = Mathf.MoveTowards(velocity.x, move * maxSpeed, acceleration * Time.fixedDeltaTime);
        }
    }

    private void HandleYVelocity()
    {
        if (grounded && velocity.y <= 0)
        {
            velocity.y = groundedGravity;
        }

        else
        {
            float currentGravity = gravity;
            if (endedJumpEarly && velocity.y > 0)
            {
                currentGravity *= endedJumpEarlyGravityMultyplier;
            }
            velocity.y = Mathf.MoveTowards(velocity.y, -maxFallSpeed, currentGravity * Time.fixedDeltaTime);
        }
    }

    public bool GetDirection()
    {
        return lookingRight;
    }

    public void OnMove(InputAction.CallbackContext input)
    {
        float newMove = input.ReadValue<Vector2>().x;
        move = newMove;
    }

    public void OnJump(InputAction.CallbackContext input)
    {
        if (input.performed)
        {
            jump = true;
            jumpHeld = true;
        }
        else if (input.canceled)
        {
            jumpHeld = false;
        }
    }

    public void OnDie()
    {
        GetComponent<Collider2D>().enabled = false;
        audioSource.PlayOneShot(dieClip);
        anim.gameObject.SetActive(false);
        GetComponent<PlayerInput>().actions.Disable();
        GameObject g = Instantiate(deathAnim, transform.position, Quaternion.identity);
        g.transform.localScale = anim.transform.localScale;
    }

    public void Respawn()
    {
        GetComponent<Collider2D>().enabled = true;
        anim.gameObject.SetActive(true);
        GetComponent<PlayerInput>().actions.Enable();
        transform.position = CheckpointManager.instance.GetCurrentCheckpoint().transform.position;
        Debug.Log(CheckpointManager.instance.GetCurrentCheckpoint().transform.position);
    }

    public void OnGoDeeper()
    {
        GetComponent<PlayerInput>().actions.Disable();
        anim.SetTrigger("GoDeeper");
    }

    public void OnStoppedGoingDeeper()
    {
        GetComponent<PlayerInput>().actions.Enable();
    }

    public void OnNextLevel(int nextLevel)
    {
        float multiplier = 1 - 0.1f * nextLevel;
        distanceForGrounded *= multiplier;
        jumpForce *= multiplier;
        doubleJumpForce *= multiplier;
        groundDeceleration *= multiplier;
        airDeceleration *= multiplier;
        maxSpeed *= multiplier;
        acceleration *= multiplier;
        groundedGravity *= multiplier;
        gravity *= multiplier;
        maxFallSpeed *= multiplier;
        endedJumpEarlyGravityMultyplier *= multiplier;
    }
}
