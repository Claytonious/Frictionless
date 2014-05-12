using UnityEngine;
using System.Collections;
using Frictionless;

public class Ball : MonoBehaviour
{
	void Start()
	{
		ServiceFactory.Resolve<MessageRouter>().AddHandler<DropCommand>(HandleDropCommand);
	}

	private void HandleDropCommand(DropCommand dropCommand)
	{
		rigidbody.isKinematic = false;
		rigidbody.AddForce(new Vector3(UnityEngine.Random.value * dropCommand.Force * 0.1f, 
		                               UnityEngine.Random.value * dropCommand.Force, 
		                               UnityEngine.Random.value * dropCommand.Force * 0.1f));
	}
}
