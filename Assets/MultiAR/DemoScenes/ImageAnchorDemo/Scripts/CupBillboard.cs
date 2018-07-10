using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CupBillboard : MonoBehaviour 
{

	void Start()
	{
		transform.localScale = new Vector3(-1f, 1f, 1f);
	}

	void LateUpdate () 
	{
		// turn transform to the main camera
		transform.LookAt(Camera.main.transform);
	}

}
