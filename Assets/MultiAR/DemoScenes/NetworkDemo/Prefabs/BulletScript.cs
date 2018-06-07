using UnityEngine;
using System.Collections;


public class BulletScript : MonoBehaviour 
{

	public GameObject playerOwner;


	void OnCollisionEnter(Collision collision)
	{
		var hitObject = collision.gameObject;
		if (playerOwner != null && hitObject == playerOwner) 
		{
			Debug.Log("Colision with the same object: " + hitObject.name);
			return;
		}
		
		var health = hitObject.GetComponent<PlayerHealth>();

		if (health  != null)
		{
			Debug.Log(hitObject.name + " takes damage");
			health.TakeDamage(10);
		}

		Destroy(gameObject);
	}
}
