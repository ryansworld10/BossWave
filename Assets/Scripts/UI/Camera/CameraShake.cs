﻿using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
	#region Fields
	private static CameraShake instance;
	#endregion

	#region Public Properties
	public static CameraShake Instance
	{ get { return instance; } }
	#endregion

	#region MonoBehaviour
	private void Awake()
	{
		instance = this;
	}
	#endregion

	#region Public Methods
	public void Shake(float duration, Vector3 strength, bool randomizeDirection = true)
	{
		if (DOTween.IsTweening(transform))
			DOTween.Complete(transform);

		transform.DOPunchPosition(randomizeDirection ? strength.RandomSign() 
													 : strength,
								  duration);
	}
	#endregion
}
