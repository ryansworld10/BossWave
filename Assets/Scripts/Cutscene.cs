﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Cutscene : MonoBehaviour
{
	private bool cutsceneActive = false;

	private PlayerControl player;
	private Animator anim;

	void Awake()
	{
		player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControl>();
		anim = GetComponent<Animator>();
	}

	public void StartCutscene(bool disableInput = false)
	{
		if (!cutsceneActive)
		{
			if (disableInput)
			{
				player.DisableInput();
			}

			cutsceneActive = true;
			anim.SetTrigger("Start");
		}
		else
		{
			Debug.LogError("Tried to start a cutscene when one was already active!");
		}
	}

	public void EndCutscene(bool enableInput = false)
	{
		if (cutsceneActive)
		{
			if (enableInput && player.IsInputDisabled())
			{
				player.EnableInput();
			}

			cutsceneActive = false;
			anim.SetTrigger("End");
		}
		else
		{
			Debug.LogError("Tried to end a cutscene when there weren't any active!");
		}
	}
}