using UnityEngine;
using System.Collections;


public class BulletScript : MonoBehaviour 
{
	void OnCollisionEnter(Collision collision)
	{
		var hit = collision.gameObject;
		var health = hit.GetComponent<PlayerHealth>();

		if (health  != null)
		{
			health.TakeDamage(10);
		}

		Destroy(gameObject);
	}
}
