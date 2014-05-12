using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Frictionless;
using System;

public class RoutingTest : MonoBehaviour
{
	IEnumerator Start()
	{
		const float epsilon = 0.1f;

		Ball[] balls = GameObject.FindObjectsOfType<Ball>();
		if (balls == null || balls.Length == 0)
			IntegrationTest.Fail(gameObject, "Failed to find objects to test - the scene isn't setup correctly for this test");
		Vector3[] startPositions = new Vector3[balls.Length];
		for (int i = 0; i < balls.Length; i++)
			startPositions[i] = balls[i].transform.position;

		yield return new WaitForSeconds(2.0f);

		// Verify that nothing has changed yet
		for (int i = 0; i < balls.Length; i++)
		{
			if ((startPositions[i] - balls[i].transform.position).magnitude > epsilon)
				IntegrationTest.Fail(gameObject, "Balls are prematurely in motion - this should not have happened until a message was routed!");
		}

		ServiceFactory.Resolve<MessageRouter>().RaiseMessage(new DropCommand() { Force = 500.0f });

		yield return new WaitForSeconds(2.0f);

		// Verify that things have changed for all recipients
		for (int i = 0; i < balls.Length; i++)
		{
			if ((startPositions[i] - balls[i].transform.position).magnitude <= epsilon)
				IntegrationTest.Fail(gameObject, "Balls failed to respond to routed message");
		}

		IntegrationTest.Pass(gameObject);
	}
}
