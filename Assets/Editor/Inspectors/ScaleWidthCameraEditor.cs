﻿using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

[CustomEditor(typeof(ScaleWidthCamera))]
public class ScaleWidthCameraEditor : Editor
{
	private AnimBool showEditorOverride;
	private AnimBool showWorldSpaceUI;
	private SerializedObject serializedTarget;

	private ScaleWidthCamera Target
	{
		get { return (ScaleWidthCamera)target; }
	}

	void OnEnable()
	{
		showEditorOverride = new AnimBool(Target.overrideSettings);
		showWorldSpaceUI = new AnimBool(Target.useWorldSpaceUI);
		serializedTarget = new SerializedObject(Target);
	}

	public override void OnInspectorGUI()
	{
		serializedTarget.Update();

		EditorGUILayout.LabelField("Current FOV", ScaleWidthCamera.FOV.ToString());
		EditorGUILayout.Space();

		Target.overrideSettings = EditorGUILayout.Toggle("Override Settings", Target.overrideSettings);
		showEditorOverride.target = Target.overrideSettings;

		if (EditorGUILayout.BeginFadeGroup(showEditorOverride.faded))
		{
			EditorGUI.indentLevel++;

			Target.overrideFOV = EditorGUILayout.IntField("Override FOV", Target.overrideFOV);

			EditorGUI.indentLevel--;
		}

		EditorGUILayout.EndFadeGroup();

		showWorldSpaceUI.target = EditorGUILayout.Toggle("Use World Space UI", showWorldSpaceUI.target);
		Target.useWorldSpaceUI = showWorldSpaceUI.value;

		if (EditorGUILayout.BeginFadeGroup(showWorldSpaceUI.faded))
		{
			EditorGUI.indentLevel++;

			Target.worldSpaceUI = (RectTransform)EditorGUILayout.ObjectField("World Space UI", Target.worldSpaceUI, typeof(RectTransform), true);

			if (Target.worldSpaceUI == null)
			{
				EditorGUILayout.HelpBox("No world space UI selected!", MessageType.Error);
			}

			EditorGUI.indentLevel--;
		}

		EditorGUILayout.EndFadeGroup();

		if (GUI.changed)
		{
			EditorUtility.SetDirty(Target);
		}

		Repaint();
	}
}