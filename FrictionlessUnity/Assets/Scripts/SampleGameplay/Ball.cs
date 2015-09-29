using UnityEngine;
using System.Collections;
using Frictionless;

public class Ball : MonoBehaviour
{
	void Start()
	{
		ServiceFactory.Instance.Resolve<MessageRouter>().AddHandler<DropCommand>(HandleDropCommand);
	}

	private void HandleDropCommand(DropCommand dropCommand)
	{
		GetComponent<Rigidbody>().isKinematic = false;
		GetComponent<Rigidbody>().AddForce(new Vector3(UnityEngine.Random.value * dropCommand.Force * 0.1f, 
		                               UnityEngine.Random.value * dropCommand.Force, 
		                               UnityEngine.Random.value * dropCommand.Force * 0.1f));
	}
}
