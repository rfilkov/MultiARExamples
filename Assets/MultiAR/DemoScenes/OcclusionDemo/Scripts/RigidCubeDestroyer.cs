using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidCubeDestroyer : MonoBehaviour 
{
	void Update () 
	{
		// destroy the object if it falls below -10m.
		if(transform.position.y <= -10f)
		{
			Destroy(transform.gameObject);
		}
	}
}
