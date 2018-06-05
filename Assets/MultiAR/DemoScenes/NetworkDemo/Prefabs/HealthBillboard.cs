using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBillboard : MonoBehaviour 
{

	void LateUpdate () 
	{
		// turn transform to the main camera
		transform.LookAt(Camera.main.transform);
	}

}
