﻿using UnityEngine;
using System.Collections;
using Frictionless;

public class TestGUI : MonoBehaviour
{
	private void OnGUI()
	{
		float y = 40.0f;
		if (GUI.Button(new Rect(40.0f, y, 100.0f, 40.0f), "Drop"))
		{
			MessageRouter.RaiseMessage(new DropCommand() { Force = 500.0f });
		}
	}
}