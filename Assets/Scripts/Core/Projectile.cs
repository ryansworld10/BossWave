﻿using UnityEngine;
using System.Collections;

public abstract class Projectile : MonoBehaviour
{
	#region Fields
	public bool enemyProjectile = false;
	public float damage = 5f;
	public Vector2 knockback = new Vector2(2f, 2f);
	public float gravity = 0f;
	public float shotSpeed = 15f;
	public float lifetime = 3f;
	public bool autoDestroy = true;
	public bool destroyOnEnemy = true;
	public bool destroyOnWorld = true;
	public bool correctRotation = true;
	public bool destroyShake = false;
	public float shakeDuration = 0.5f;
	public Vector3 shakeIntensity = new Vector3(0f, 0.5f, 0f);
	public string destroyEffect;

	[HideInInspector]
	public Vector3 direction;

	protected Vector3 velocity;
	protected CharacterController2D controller;
	protected SpriteRenderer spriteRenderer;
	protected Animator anim;
	#endregion

	#region Public Properties
	public Sprite Sprite
	{ get { return spriteRenderer == null ? null : spriteRenderer.sprite; } }

	public Color SpriteColor
	{
		get { return spriteRenderer == null ? Color.clear : spriteRenderer.color; }

		set
		{
			if (spriteRenderer != null)
				spriteRenderer.color = value;
		}
	}

	public Bounds Bounds
	{ get { return collider2D.bounds; } }
	#endregion

	#region Internal Properties
	protected LayerMask TriggerLayers
	{ get { return controller.triggerLayers; } }

	private bool UseRaycastTriggers
	{ get { return TriggerLayers != 0; } }
	#endregion

	#region MonoBehaviour
	protected virtual void Awake()
	{
		controller = GetComponent<CharacterController2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		anim = GetComponent<Animator>();

		if (autoDestroy)
			StartCoroutine(FailsafeDestroy());
	}

	protected virtual void OnEnable()
	{
		SpriteColor = Color.clear;

		if (controller != null)
		{
			if (UseRaycastTriggers)
				controller.OnRaycastTrigger += OnRaycastTrigger;

			controller.OnRaycastCollision += OnRaycastCollision;
		}
	}

	protected virtual void OnDisable()
	{
		if (controller != null)
		{
			if (UseRaycastTriggers)
				controller.OnRaycastTrigger -= OnRaycastTrigger;

			controller.OnRaycastCollision -= OnRaycastCollision;
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (!UseRaycastTriggers)
			HandleTrigger(other);
	}

	private void OnTriggerStay2D(Collider2D other)
	{
		OnTriggerEnter2D(other);
	}

	private void OnRaycastCollision(RaycastHit2D raycastInfo)
	{
		HandleCollision(raycastInfo.collider);
	}

	private void OnRaycastTrigger(RaycastHit2D raycastInfo)
	{
		if (UseRaycastTriggers)
			HandleTrigger(raycastInfo.collider);
	}
	#endregion

	#region Internal Update Methods
	protected void DoMovement()
	{
		velocity.x = direction.x * shotSpeed;
		direction.y += (gravity * Time.deltaTime) / 10f;
		velocity.y = direction.y * shotSpeed;

		if (correctRotation)
			transform.CorrectScaleForRotation(direction.DirectionToRotation2D());

		transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
		controller.Move(velocity * Time.deltaTime);
		velocity = controller.Velocity;
	}
	#endregion

	#region Internal Helper Methods
	protected virtual void HandleCollision(Collider2D other)
	{
		if (other.tag != "RunningBoundaries")
			CheckDestroyWorld();
	}

	protected virtual void HandleTrigger(Collider2D other)
	{ }

	protected void Flip()
	{
		transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
	}

	protected IEnumerator FailsafeDestroy()
	{
		yield return new WaitForSeconds(lifetime);

		DoDestroy();
	}
	#endregion

	#region Public Methods
	public virtual void Initialize(Vector3 newDirection)
	{
		if (direction != Vector3.zero)
			return;

		direction = newDirection;

		if (correctRotation)
			transform.CorrectScaleForRotation(direction.DirectionToRotation2D());

		SpriteColor = Color.white;
	}

	public void Move(Vector3 velocity)
	{
		controller.Move(velocity * Time.deltaTime);
		this.velocity = controller.Velocity;
	}

	public void CheckDestroyEnemy()
	{
		if (destroyOnEnemy)
			DoDestroy();
	}

	public void CheckDestroyWorld()
	{
		if (destroyOnWorld)
			DoDestroy();
	}

	public virtual void DoDestroy()
	{
		if (destroyShake)
			CameraShake.Instance.Shake(shakeDuration, shakeIntensity);

		if (destroyEffect != "")
			SpriteEffect.Instance.SpawnEffect(destroyEffect, transform.position, parent: LevelManager.Instance.foregroundLayer);

		ExplodeEffect.Instance.Explode(transform, velocity, Sprite);
		Destroy(gameObject);
	}
	#endregion
}
