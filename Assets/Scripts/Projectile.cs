﻿using UnityEngine;
using System.Collections;

public abstract class Projectile : MonoBehaviour 
{
	public bool playerShot = false;
	public float damage = 5f;
	public float knockback = 2f;
	public float gravity = 0f;
	public float shotSpeed = 15f;
	public float lifetime = 3f;
	public bool autoDestroy = true;
	public bool destroyOnEnemy = true;
	public bool destroyOnWorld = true;

	[HideInInspector]
	public Vector3 direction;
	[HideInInspector]
	public Vector3 velocity;
	[HideInInspector]
	public bool disableMovement = false;

	protected CharacterController2D controller;
	protected SpriteRenderer spriteRenderer;

	public Sprite Sprite
	{
		get
		{
			return spriteRenderer.sprite;
		}
	}

	protected virtual void Awake()
	{
		controller = GetComponent<CharacterController2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();

		if (playerShot)
		{
			tag = "PlayerProjectile";
		}

		if (autoDestroy)
		{
			StartCoroutine(FailsafeDestroy());
		}
	}

	protected virtual void OnTriggerEnter2D(Collider2D trigger)
	{
		if (trigger.gameObject.layer == LayerMask.NameToLayer("Collider"))
		{
			CheckDestroyWorld();
		}
	}

	protected virtual void OnTriggerStay2D(Collider2D trigger)
	{
		OnTriggerEnter2D(trigger);
	}

	protected void InitialUpdate()
	{
		velocity = controller.velocity;
	}

	protected void ApplyMovement()
	{
		if (!disableMovement)
		{
			velocity.x = direction.x * shotSpeed;
			direction.y += (gravity * Time.fixedDeltaTime) / 10f;
			velocity.y = direction.y * shotSpeed;
		}

		controller.move(velocity * Time.fixedDeltaTime);
	}

	protected void Flip()
	{
		transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
	}

	public void Move(Vector3 velocity)
	{
		controller.move(velocity * Time.deltaTime);
	}

	public void CheckDestroyEnemy()
	{
		if (destroyOnEnemy)
		{
			DoDestroy();
		}
	}

	public void CheckDestroyWorld()
	{
		if (destroyOnWorld)
		{
			DoDestroy();
		}
	}

	public void DoDestroy()
	{
		ExplodeEffect.Explode(transform, velocity, spriteRenderer.sprite);
		Destroy(gameObject);
	}

	protected IEnumerator FailsafeDestroy()
	{
		yield return new WaitForSeconds(lifetime);

		ExplodeEffect.Explode(transform, velocity, Sprite);
		Destroy(gameObject);
	}
}
