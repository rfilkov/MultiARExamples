using UnityEngine;
using System.Collections;


public class KeyMove : MonoBehaviour 
{

	public float keyStep = 0.05f;


	void Update()
	{
		Vector3 currentPos = transform.position;
		Vector3 deltaPos = Vector3.zero;

		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) 
		{
			deltaPos = transform.forward * keyStep;

		}
		else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) 
		{
			deltaPos = -transform.forward * keyStep;
		}
		else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) 
		{
			deltaPos = -transform.right * keyStep;
		}
		else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) 
		{
			deltaPos = transform.right * keyStep;
		}
		else if (Input.GetKey(KeyCode.Q)) 
		{
			deltaPos = transform.up * keyStep;
		}
		else if (Input.GetKey(KeyCode.C)) 
		{
			deltaPos = -transform.up * keyStep;
		}

		transform.position = currentPos + deltaPos;
	}

}

