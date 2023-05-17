using System.Collections;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class playerController : MonoBehaviour
{
	private Animator anim;
	[SerializeField] healthBar healthBar;
	[SerializeField] GameObject deathMenu;

	// Player Inputs
	private float xMove;
	private float yMove;

	// Player Variables
	public bool playerStatus = true;
	private float speed = 20f;
	private float climbingSpeed = 8f;
	private float jumpingPower = 35f;
	private int deathCounter = 0;
	private bool onSpike;

	// Variables
	float spikeTimer = 1.5f;

	// Player Status
	private bool isFacingLeft;
	private bool isWalking;
	private bool isJumping;
	private bool isVines = false;

	private Rigidbody2D rigid;
	private SpriteRenderer rend;
	private Material originalMaterial;
	[SerializeField] private Transform groundCheck;
	[SerializeField] private LayerMask groundLayer;

	// Flash

	private float flashDuration = 0.125f;
	// White flash material
	[SerializeField] private Material flashMaterial;
	// Currently running flash coroutine
	private Coroutine flashRoutine;

	// Start is called before the first frame update
	void Start()
	{
		rend = gameObject.GetComponent<SpriteRenderer>();
		anim = GetComponent<Animator>();
		rigid = gameObject.GetComponent<Rigidbody2D>();
		originalMaterial = rend.material;
	}

	// Input direction, jumping, and animations
	void Update()
	{
		if(GameManager.gameManager.playerHealth.Health <= 0)
		{
			Death();
		}

		if (playerStatus)
		{
			// Checks input direction
			xMove = Input.GetAxisRaw("Horizontal");
			// Checks 
			yMove = Input.GetAxis("Vertical");

			// Checks if it needs to flip the player
			Flip();

			// If player is on spike deal damage every 1.5 seconds
			if (onSpike)
			{
				float spikeTime = 1.0f;
				spikeTimer += Time.deltaTime;
				if (spikeTimer > spikeTime)
				{
					GameManager.gameManager.PlayerDamager(20);
					spikeTimer = 0;
				} 

			}

			// Checks if player is grounded
			// *** Jump ***
			if (Input.GetButtonDown("Jump") && IsGrounded() || Input.GetButtonDown("Jump") && isVines)
			{
				isVines = false;
				rigid.gravityScale = 8f;
				rigid.velocity = new Vector2(rigid.velocity.x, jumpingPower);
				isJumping = true;
			}
			if (Input.GetButtonUp("Jump") && (rigid.velocity.y > 0f))
			{
				rigid.velocity = new Vector2(rigid.velocity.x, rigid.velocity.y * 0.5f);
				isJumping = false;
			}

			// *** Animations ***
			anim.SetBool("isFacingLeft", isFacingLeft);
			anim.SetBool("isWalking", isWalking);
			anim.SetBool("isJumping", isJumping);
		}

		// ** Test Damage + Heal **

		
		if (Input.GetKeyDown("b"))
		{
			PlayerDamager(10);
		}
		if (Input.GetKeyDown("n"))
		{
			PlayerHeal(10);
		}
		
	}

	// Update for physics - Walking, Climbing
	private void FixedUpdate(){

		if (playerStatus)
		{
			// *** Walking ***
			// Checking if the player is walking currently
			rigid.velocity = new Vector2(xMove * speed, rigid.velocity.y);
			if (xMove < 0 || xMove > 0)
			{
				isWalking = true;
			}
			else
			{
				isWalking = false;
			}

			// *** Climbing/Vines ***
			// if is climbing than make the gravity scale 0 to allow player to float
			if (isVines)
			{
				rigid.gravityScale = 0;
				rigid.velocity = new Vector2(rigid.velocity.x, yMove * climbingSpeed);
			}
			else
			{
				rigid.gravityScale = 8f;
			}
		}
	}

	// Draws Ground Check Radius
	void OnDrawGizmosSelected(){
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(groundCheck.position, 0.2f);
	}

	// If it enters the Vines make isVines true
	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.CompareTag("Vines"))
		{
			isVines = true;
		}
		if(collision.CompareTag("World Colliders"))
		{
			PlayerDamager(200);
		}
		if (collision.CompareTag("Spike"))
		{
			onSpike = true;
		}
	}

	// If it leaves the Vines make isVines false
	private void OnTriggerExit2D(Collider2D collision)
	{
		if (collision.CompareTag("Vines"))
		{
			isVines = false;
		}
		if (collision.CompareTag("Spike"))
		{
			onSpike = false;
		}
	}

	// Flips player on input control
	private void Flip()
	{
		if (isFacingLeft && xMove > 0f || !isFacingLeft && xMove < 0f)
		{
			isFacingLeft = !isFacingLeft;
			Vector3 localScale = transform.localScale;
			localScale.x *= -1f;
			transform.localScale = localScale;
		}
	}

	private IEnumerator FlashRoutine()
	{
		// Change material to flashing
		rend.material = flashMaterial;

		// Pause for "flashDuration" amount of seconds
		yield return new WaitForSeconds(flashDuration);

		// After change back to original material
		rend.material = originalMaterial;

		// Signify that the routine is finished
		flashRoutine = null;
	}
	
	private void Flash()
	{
		if(flashRoutine != null)
		{
			// Stop the routine
			StopCoroutine(flashRoutine);
		}

		// Start routine, and find the reference for it
		flashRoutine = StartCoroutine(FlashRoutine());
	}

	private void Death()
	{
		playerStatus = false;
		rigid.gravityScale = 0;
		if(deathCounter <= 0)
		{
			GameObject deathPopUp = Instantiate(deathMenu, new Vector3(0, 0, 0), Quaternion.identity);
		}
		deathCounter++;

	}

	// Checks if player is on the ground
	private bool IsGrounded()
	{
		// If the ground check of the player is on the ground return is grounded tru
		return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
	}
	
	// Damage Player
	private void PlayerDamager(int dmg)
	{
		GameManager.gameManager.PlayerDamager(dmg);
		//Flash();
		healthBar.SetHealth(GameManager.gameManager.playerHealth.Health);
	}

	// Heal Player
	private void PlayerHeal(int heal)
	{
		// Use parameter heal to change the game manager player health variable
		GameManager.gameManager.playerHealth.HealEntity(heal);
		// Change the slider using the health bar scripts method set health
		healthBar.SetHealth(GameManager.gameManager.playerHealth.Health);
	}
}
