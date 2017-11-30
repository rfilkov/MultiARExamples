using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShooter : MonoBehaviour 
{

	[Tooltip("Prefab to be used as cannonball.")]
	public GameObject ballPrefab;

	[Tooltip("Factor used to determine the force, according to target point distance.")]
	public float forceFactor = 100f;


	// reference to the MultiARManager
	private MultiARManager arManager;



	// Use this for initialization
	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;
	}
	
	// Update is called once per frame
	void Update () 
	{
		// check for click
		if (ballPrefab && arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				// raycast scene objects (including overlay surfaces)
				MultiARInterop.TrackableHit hit;

				if(arManager.RaycastToScene(true, out hit))
				{
					// gets the main camera
					Camera arCamera = arManager.GetMainCamera();
					if (!arCamera)
						return;

					// instantiate the cannonball. schedule it for destroy in 5 seconds
					GameObject cannonBall = Instantiate(ballPrefab, arCamera.transform.position, arCamera.transform.rotation);
					cannonBall.name = "cannonBall";
					Destroy (cannonBall, 3f); 

					// fire the cannonball
					FireCannonball(cannonBall, arCamera.transform.position, hit.point);
				}
			}
		}

	}

	/// Fires the cannonball to the given point.
	private void FireCannonball(GameObject cannonBall, Vector3 startPoint, Vector3 targetPoint)
	{
		// get the rigid body
		Rigidbody cannonBallRB = cannonBall ? cannonBall.GetComponent<Rigidbody>() : null;
		if (cannonBallRB == null)
			return;

		// estimate direction and distance
		Vector3 fireDir = (targetPoint - startPoint).normalized;
		float fireDist = (targetPoint - startPoint).magnitude;

		// apply the force
		Vector3 forceToApply = fireDir * fireDist * forceFactor;
		cannonBallRB.AddForce(forceToApply);
	}

}
