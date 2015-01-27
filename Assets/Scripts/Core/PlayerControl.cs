﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class PlayerControl : MonoBehaviour
{
	public static PlayerControl instance;

	public float maxHealth = 100f;
	public float invincibilityPeriod = 2f;
	public float gravity = -35f;
	public float walkSpeed = 10f;
	public float runSpeed = 17.5f;
	public float continuousRunSpeed = 10f;
	public float groundDamping = 10f;
	public float inAirDamping = 5f;
	public float jumpHeight = 5f;
	public float comboStartKills = 3f;
	public float comboDecreaseTime = 1f;
	public int maxScore = 999999999;
	public int maxMicrochips = 99999;
	public Gun startingGun;
	public float minAltIdleTime = 5f;
	public float maxAltIdleTime = 10f;
	public List<string> altIdleAnimations;

	[HideInInspector]
	public Gun gun;
	[HideInInspector]
	public Vector3 velocity;
	[HideInInspector]
	private float normalizedHorizontalSpeed = 0;
	[HideInInspector]
	public float speedMultiplier = 1f;
	[HideInInspector]
	public Transform popupMessagePoint;
	[HideInInspector]
	public int score = 0;
	[HideInInspector]
	public int combo = 1;
	[HideInInspector]
	public int microchips = 0;
	[HideInInspector]
	public bool continuouslyRunning = false;
	[HideInInspector]
	public List<SpriteRenderer> spriteRenderers;

	private CharacterController2D controller;
	private Animator anim;
	private RaycastHit2D lastControllerColliderHit;

	private float health;
	private bool dead = false;
	private bool right;
	private bool left;
	private bool jump;
	private bool run;
	private bool usingGun = false;
	private bool disableInput = false;
	private bool inPortal = false;

	#if MOBILE_INPUT
	private bool lastJump;
	#endif

	private float lastHitTime;
	private bool canTakeDamage = true;
	private float flashTimer = 0f;
	private float flashTime = 0.25f;
	private float smoothFlashTime;

	private float currentMaxCombo = 1f;
	private float comboTimer = 0f;
	private float killChain = 0f;

	private float altIdleTimer = 0f;
	private float altIdleTime = 0f;

	private bool cancelGoTo = false;
	private bool useTargetPoint = false;
	private bool goToFaceRight = false;
	private bool reEnableAfterMove = true;
	private bool inertiaAfterMove = false;
	private Vector3 targetPoint;

	public float Health
	{
		get { return health; }

		set
		{
			if (value < health)
			{
				lastHitTime = Time.time;
			}

			health = Mathf.Clamp(value, 0f, maxHealth);
			CheckDeath();
		}
	}

	public bool Dead
	{
		get { return dead; }
	}

	public bool FacingRight
	{
		get { return transform.localScale.x > 0; }
	}

	public bool IsGrounded
	{
		get { return controller.isGrounded; }
	}

	private float newAltIdleTime
	{
		get
		{
			return Random.Range(minAltIdleTime, maxAltIdleTime);
		}
	}

	void Awake()
	{
		instance = this;

		popupMessagePoint = transform.FindChild("popupMessage");

		anim = GetComponent<Animator>();
		controller = GetComponent<CharacterController2D>();
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>().ToList<SpriteRenderer>();
		gun = GetComponentInChildren<Gun>();

		health = maxHealth;

		lastHitTime = Time.time - invincibilityPeriod;
		altIdleTime = newAltIdleTime;
	}

	void Start()
	{
		SwapGun(startingGun);

		foreach (SpriteRenderer spriteRenderer in spriteRenderers)
		{
			if (spriteRenderer.name != "Full")
			{
				spriteRenderer.color = Color.clear;
			}
		}
	}

	void Update()
	{
		if (!disableInput)
		{
			right = CrossPlatformInputManager.GetAxis("Horizontal") > 0f;
			left = CrossPlatformInputManager.GetAxis("Horizontal") < 0f;

			#if MOBILE_INPUT
			run = Mathf.Abs(CrossPlatformInputManager.GetAxis("Horizontal")) > 0.7f;
			bool jumpInput = CrossPlatformInputManager.GetAxis("Vertical") > 0.6f;
			jump = jump || (jumpInput && !lastJump);
			lastJump = jumpInput;
			#else
			run = CrossPlatformInputManager.GetButton("Run") && gun.NoInput;
			jump = jump || (CrossPlatformInputManager.GetButtonDown("Jump") && controller.isGrounded);
			#endif
		}

		run = (run && (right || left)) || continuouslyRunning;

		anim.SetBool("Walking", right || left || continuouslyRunning);
		anim.SetBool("Running", run);
	}

	void FixedUpdate()
	{
		velocity = controller.velocity;

		if (IsGrounded)
		{
			velocity.y = 0f;
		}

		anim.SetBool("Grounded", controller.isGrounded);
		anim.SetBool("Falling", velocity.y < 0f);
		anim.SetFloat("Gun Angle", gun.transform.rotation.eulerAngles.z);

		if (useTargetPoint && disableInput)
		{
			if (transform.position.x < targetPoint.x && !left)
			{
				right = true;
			}
			else if (transform.position.x > targetPoint.x && !right)
			{
				left = true;
			}
			else
			{
				cancelGoTo = true;	
			}

			if (cancelGoTo)
			{
				if (goToFaceRight && transform.localScale.x < 0)
				{
					Flip();
				}
				if (!goToFaceRight && transform.localScale.x > 0)
				{
					Flip();
				}

				ResetInput();

				useTargetPoint = false;

				if (!inertiaAfterMove)
				{
					velocity.x = 0f;
				}

				if (reEnableAfterMove)
				{
					EnableInput();
				}
			}
		}

		if (combo > 1)
		{
			comboTimer += Time.deltaTime;

			if (comboTimer >= Mathf.Clamp(comboDecreaseTime - (0.25f * (currentMaxCombo - combo)), comboDecreaseTime * 0.25f, comboDecreaseTime))
			{
				combo--;
				killChain = combo == 1 ? 0f : GetNextCombo() - combo;
				comboTimer = 0f;
			}
		}

		if (health > 0f)
		{
			canTakeDamage = Time.time > lastHitTime + invincibilityPeriod;

			if (!canTakeDamage)
			{
				flashTimer += Time.deltaTime;
				smoothFlashTime = Mathf.Lerp(smoothFlashTime, 0.05f, 0.025f);

				if (flashTimer > smoothFlashTime)
				{
					SetRenderersEnabled(alternate: true);
					flashTimer = 0f;
				}
			}
			else
			{
				SetRenderersEnabled(true);
				smoothFlashTime = flashTime;
			}
		}

		if (right)
		{
			normalizedHorizontalSpeed = 1f;
		}
		else if (left)
		{
			normalizedHorizontalSpeed = -1f;
		}
		else
		{
			normalizedHorizontalSpeed = 0f;

			if (!jump && gun.NoInput)
			{
				altIdleTimer += Time.deltaTime;

				if (altIdleTimer >= altIdleTime)
				{
					anim.SetTrigger(altIdleAnimations[Random.Range(0, altIdleAnimations.Count)]);
					altIdleTimer = 0f;
					altIdleTime = newAltIdleTime;
				}
			}
		}

		if (gun.NoInput)
		{
			if (usingGun)
			{
				usingGun = false;
				SetRenderersVisible(alternate: true);
			}

			if (continuouslyRunning && transform.localScale.x < 0f)
			{
				Flip();
			}

			if (right && transform.localScale.x < 0f)
			{
				Flip();
			}
			else if (left && !continuouslyRunning && transform.localScale.x > 0f)
			{
				Flip();
			}
		}
		else
		{
			if (!usingGun)
			{
				usingGun = true;
				SetRenderersVisible(alternate: true);
			}

			if (gun.FacingRight && transform.localScale.x < 0f)
			{
				Flip();
			}
			else if (!gun.FacingRight && transform.localScale.x > 0f)
			{
				Flip();
			}
		}

		if (jump && controller.isGrounded)
		{
			if (!inPortal)
			{
				velocity.y = Mathf.Sqrt(Mathf.Max(0f, 2f * jumpHeight * -gravity));
				anim.SetTrigger("Jump");
			}

			jump = false;
		}

		float smoothedMovementFactor = controller.isGrounded ? groundDamping : inAirDamping;

		velocity.x = Mathf.Lerp(velocity.x,
								normalizedHorizontalSpeed * (run ? (continuouslyRunning && !useTargetPoint ? continuousRunSpeed
																										   : runSpeed)
																 : walkSpeed) * speedMultiplier,
								Time.fixedDeltaTime * smoothedMovementFactor);
		velocity.y += gravity * Time.fixedDeltaTime;

		controller.move(velocity * Time.fixedDeltaTime);		
	}

	void OnTriggerEnter2D(Collider2D enemy)
	{
		if (enemy.tag == "Enemy" || enemy.tag == "Projectile")
		{
			if (canTakeDamage)
			{
				if (health > 0f)
				{
					TakeDamage(enemy.gameObject);
				}
			}
		}

		if (enemy.tag == "Portal")
		{
			inPortal = true;
		}
	}

	void OnTriggerStay2D(Collider2D enemy)
	{
		OnTriggerEnter2D(enemy);
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if (other.tag == "Portal")
		{
			inPortal = false;
		}
	}

	public void TakeDamage(GameObject enemy, float damage = 0f, Vector2 knockback = default(Vector2))
	{
		if (!canTakeDamage)
		{
			return;
		}

		float knockbackDirection = 1f;

		if (enemy.tag == "Enemy")
		{
			Enemy currentEnemy = enemy.GetComponent<Enemy>();

			if (!currentEnemy.spawned)
			{
				return;
			}

			damage = (damage == 0f) ? currentEnemy.damage : damage;
			knockback = (knockback == default(Vector2)) ? currentEnemy.knockback : knockback;
			knockbackDirection = Mathf.Sign(transform.position.x - enemy.transform.position.x);
		}
		else if (enemy.tag == "Projectile")
		{
			Projectile currentProjectile = enemy.GetComponent<Projectile>();
			damage = currentProjectile.damage;
			knockback = currentProjectile.knockback;
			knockbackDirection = Mathf.Sign(currentProjectile.direction.x);
			currentProjectile.CheckDestroyEnemy();
		}

		if (damage != 0f)
		{
			Health -= damage;

			if (health > 0f)
			{
				Sequence knockbackSequence = DOTween.Sequence();

				knockbackSequence
					.AppendInterval(0.1f)
					.AppendCallback(() =>
					{
						velocity.x = Mathf.Sqrt(Mathf.Pow(knockback.x, 2) * -gravity) * knockbackDirection;
						velocity.y = Mathf.Sqrt(knockback.y * -gravity);

						controller.move(velocity * Time.deltaTime);
						lastHitTime = Time.time;
					});
			}
		}
	}

	public void Move(Vector3 velocity)
	{
		controller.move(velocity * Time.deltaTime);
	}

	public int AddPoints(int points)
	{
		int newPoints = points * combo;
		score = Mathf.Clamp(score + newPoints, 0, maxScore);

		return newPoints;
	}

	public int AddPointsFromEnemy(float enemyHealth, float enemyDamage)
	{
		killChain++;
		comboTimer = 0f;

		if (killChain >= GetNextCombo())
		{
			combo++;
			currentMaxCombo = combo;
		}

		int newPoints = Mathf.RoundToInt(enemyHealth * enemyDamage + (enemyHealth / maxHealth * 100)) * combo;
		score = Mathf.Clamp(score + newPoints, 0, maxScore);

		return newPoints;
	}

	public void AddMicrochips(int newMicrochips)
	{
		microchips = Mathf.Clamp(microchips + newMicrochips, 0, maxMicrochips);
	}

	public void SwapGun(Gun newGun)
	{
		Transform oldTransform = gun.transform;
		spriteRenderers.Remove(gun.GetComponent<SpriteRenderer>());
		Destroy(gun.gameObject);
		Gun gunInstance = Instantiate(newGun, oldTransform.position, oldTransform.rotation) as Gun;
		gunInstance.transform.parent = transform;
		gunInstance.transform.localScale = oldTransform.localScale;
		gun = gunInstance;
		spriteRenderers.Add(gun.GetComponent<SpriteRenderer>());
	}

	public void ResetSpeed(float delay)
	{
		StartCoroutine(ResetSpeedCoroutine(delay));
	}

	public void GoToPoint(Vector3 point, bool faceRight, bool autoEnableInput = true, bool inertia = false)
	{
		targetPoint = point;
		useTargetPoint = true;
		goToFaceRight = faceRight;
		cancelGoTo = false;
		reEnableAfterMove = autoEnableInput;
		inertiaAfterMove = inertia;
		DisableInput();
	}

	public void CancelGoTo()
	{
		cancelGoTo = true;
	}

	public void DisableInput()
	{
		disableInput = true;
		gun.disableInput = true;
		ResetInput();
	}

	public void EnableInput()
	{
		ResetInput();
		disableInput = false;
		gun.disableInput = false;
	}

	public bool IsInputDisabled()
	{
		return disableInput;
	}

	private void ResetInput()
	{
		left = right = run = jump = false;
	}

	private bool CheckDeath()
	{
		if (health <= 0f && !dead)
		{
			dead = true;

			SetRenderersEnabled(false);
			collider2D.enabled = false;
			DisableInput();

			foreach (SpriteRenderer sprite in spriteRenderers)
			{
				sprite.transform.localScale = transform.localScale;
				ExplodeEffect.Explode(sprite.transform, velocity, sprite.sprite);
			}
		}

		return dead;
	}

	private void Flip()
	{
		transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
	}

	private IEnumerator ResetSpeedCoroutine(float delay)
	{
		yield return new WaitForSeconds(delay);
		speedMultiplier = 1f;
	}

	private void SetRenderersEnabled(bool enabled = true, bool alternate = false)
	{
		foreach (SpriteRenderer sprite in spriteRenderers)
		{
			if (alternate)
			{
				sprite.enabled = !sprite.enabled;
			}
			else
			{
				sprite.enabled = enabled;
			}
		}
	}

	private void SetRenderersVisible(bool enabled = true, bool alternate = false)
	{
		foreach (SpriteRenderer sprite in spriteRenderers)
		{
			if (alternate)
			{
				sprite.color = sprite.color == Color.white ? Color.clear : Color.white;
			}
			else
			{
				sprite.color = enabled ? Color.white : Color.clear;
			}
		}
	}

	private float GetNextCombo()
	{
		float nextCombo = comboStartKills - 1f;

		for (int i = 1; i <= combo; i++)
		{
			nextCombo += i;
		}

		return nextCombo;
	}
}

