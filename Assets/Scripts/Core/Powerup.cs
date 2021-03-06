﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public abstract class Powerup : MonoBehaviour
{
	#region Fields
	public bool autoDestroy = true;
	public float minLifetime = 10f;
	public float maxLifetime = 15f;

	protected SpriteRenderer spriteRenderer;

	private BoxCollider2D pickupCollider;
	#endregion

	#region MonoBehaviour
	protected virtual void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();

		pickupCollider = GetComponent<BoxCollider2D>();

		if (autoDestroy)
		{
			Invoke("Destroy", Random.Range(minLifetime, maxLifetime));
		}
	}

	protected virtual void OnTriggerEnter2D(Collider2D trigger)
	{
		if (trigger.tag == "Player" && pickupCollider.bounds.Intersects(trigger.bounds))
		{
			Pickup();
		}
	}

	protected virtual void OnTriggerStay2D(Collider2D trigger)
	{
		OnTriggerEnter2D(trigger);
	}
	#endregion

	#region Internal Helper Methods
	protected virtual void Pickup()
	{
		ExplodeEffect.Instance.Explode(transform, Vector3.zero, spriteRenderer.sprite);
		Destroy(gameObject);
	}
	#endregion

	#region Public Methods
	public void Destroy()
	{
		ExplodeEffect.Instance.Explode(transform, Vector3.zero, spriteRenderer.sprite);
		Destroy(gameObject);
	}
	#endregion
}
