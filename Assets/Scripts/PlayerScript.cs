using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody2D),
    typeof(CapsuleCollider2D))]
public class PlayerScript : MonoBehaviour
{
    [SerializeField] private CharacterStats stats;
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private AudioSource DashAudioSource;
    [SerializeField] private UIBehavior uiBheavior;
    private readonly UnityEvent InteractEvent = new();
    private Rigidbody2D rb;
    private PlayerInput playInput;
    private bool InputEnabled = true;
    private Vector2 moveInput;
    private Vector2 velocity;
    private float rotState;
    private float runTime;
    private bool dashAvailable = true;
    private float dashRunTime;
    private bool isDashing = false;
    private Animator animator;
    

    private void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponent<Animator>();
        rb.gravityScale = 0;
        stats = stats.CopyStats(stats);
        stats.onDeath.AddListener(OnDeath);
    }

    public Faction Allegiance
    {
        get => stats.Allegiance;
    }

    public int Damage
    {
        set
        {
            stats.Damage = value;
            uiBheavior.SetHPUI(stats.Health);
        }
    }

    private void OnDeath()
    {
        onDeath.Invoke();
    }

    public void ToggleInput()
    {
        InputEnabled = !InputEnabled;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if(context.started)
            InteractEvent.Invoke();
    }

    public void AddToInteract(UnityAction function)
    {
        InteractEvent.AddListener(function);
    }

    public void RemoveFromInteract(UnityAction function)
    {
        InteractEvent.RemoveListener(function);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveVector = context.ReadValue<Vector2>();
        moveInput = moveVector.normalized;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!dashAvailable || isDashing)
            return;
        InputEnabled = false;
        isDashing = true;
        stats.setIFrameTime(stats.DashTime, true);
        DashAudioSource.clip = stats.DashSound;
        DashAudioSource.Play();
        if (moveInput != new Vector2(0, 0))
        {
            rb.velocity = moveInput;
            return;
        }
        else
        {
            switch (rotState)
            {
                case 0f:
                    rb.velocity = Vector2.left;
                    break;
                case 0.33f:
                    rb.velocity = Vector2.down;
                    break;
                case 0.66f:
                    rb.velocity = Vector2.right;
                    break;
                case 1f:
                    rb.velocity = Vector2.up;
                    break;
            }
        }

        rb.velocity *= stats.DashVelocity / stats.DashTime * stats.DashCurve.Evaluate(0);
    }


    private void UpdateBlinkDash()
    {
        if (spriteRenderer.color.a == stats.OnDash.a)
        {
            spriteRenderer.color = new Color(1, 1, 1, 1);
        }
        else
        {
            spriteRenderer.color = stats.OnDash;
        }
    }

    private void UpdateBlinkDamage()
    {
        if (spriteRenderer.color.a == stats.OnDamage.a)
        {
            spriteRenderer.color = new Color(1, 1, 1, 1);
        }
        else
        {
            spriteRenderer.color = stats.OnDamage;
        }
    }

    private void UpdateBlink()
    {
        if (stats.IFrameActive())
        {
            if (stats.UpdateIFrameBlink())
            {
                if(stats.Dashed)
                {
                    UpdateBlinkDash();
                }
                else
                {
                    UpdateBlinkDamage();
                }
            }
        }
        else if (spriteRenderer.color.a != 1)
        {
            spriteRenderer.color = new Color(1, 1, 1, 1);
        }
    }
    
    // Update is called once per frame
    private void Update()
    {
        stats.UpdateIframe();
        UpdateBlink();
        if(InputEnabled)
            velocity = TranslateInputToVelocity(moveInput);
        if (isDashing)
        {
            runTime += Time.deltaTime;
            rb.velocity = rb.velocity.normalized * stats.DashVelocity / stats.DashTime * stats.DashCurve.Evaluate(runTime);
            if (runTime >= stats.DashTime)
            {
                uiBheavior.OnDash(stats.DashCooldown);
                dashAvailable = false;
                runTime = 0;
                isDashing = false;
                InputEnabled = true;
            }
        }
        else if (!dashAvailable)
        {
            dashRunTime += Time.deltaTime;
            if (dashRunTime >= stats.DashCooldown)
            {
                dashRunTime = 0;
                dashAvailable = true;
            }

        }
    }
    
    Vector2 TranslateInputToVelocity(Vector2 input)
    {
        // Make the character move along the X-axis
        return new Vector2(input.x * stats.Speed, input.y * stats.Speed);
    }


    private void MovementUpdate()
    {
        if(moveInput == new Vector2(0,0))
        {
            rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(0,0), 0.14f);
        }
        else
        {
            rb.velocity = velocity;
        }
    }

    private void FixedUpdate()
    {
        if (!isDashing)
            MovementUpdate();
        if (animator == null)
            return;
        if(velocity != new Vector2(0, 0) && !isDashing)
        {
            animator.SetFloat("RunState", 0.5f);
        }
        else
        {
            animator.SetFloat("RunState", 0);
        }
        if(velocity.x < 0)
        {
            rotState = 0f;
        }
        else if (velocity.x > 0)
        {
            rotState = 0.66f;
        }
        else if (velocity.y > 0)
        {
            rotState = 1f;
        }
        else if (velocity.y < 0)
        {
            rotState = 0.33f;
        }
        animator.SetFloat("RotState", rotState);
    }
}
