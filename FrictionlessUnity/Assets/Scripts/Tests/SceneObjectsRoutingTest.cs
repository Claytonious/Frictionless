using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Frictionless;
using System;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class SceneObjectsRoutingTest
{
	[UnityTest]
	public IEnumerator MessagesInteractWithScene()
	{
		SceneManager.LoadScene("Tests", LoadSceneMode.Additive);
		yield return null;
		
		const float epsilon = 0.1f;

		var balls = Object.FindObjectsOfType<Ball>();
		Assert.IsTrue(balls?.Length > 0, "Failed to find objects to test - the scene isn't setup correctly for this test");
		var startPositions = new Vector3[balls.Length];
		for (var i = 0; i < balls.Length; i++)
			startPositions[i] = balls[i].transform.position;

		yield return new WaitForSeconds(2.0f);

		// Verify that nothing has changed yet
		for (var i = 0; i < balls.Length; i++)
		{
			Assert.IsFalse((startPositions[i] - balls[i].transform.position).magnitude > epsilon, "Balls are prematurely in motion - this should not have happened until a message was routed!");
		}

		MessageRouter.RaiseMessage(new DropCommand() { Force = 500.0f });

		yield return new WaitForSeconds(2.0f);

		// Verify that things have changed for all recipients
		for (int i = 0; i < balls.Length; i++)
		{
			Assert.IsFalse((startPositions[i] - balls[i].transform.position).magnitude <= epsilon, "Balls failed to respond to routed message");
		}
	}
}
