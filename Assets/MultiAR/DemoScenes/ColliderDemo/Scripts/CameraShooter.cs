using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShooter : MonoBehaviour 
{

	[Tooltip("Prefab to be used as cannonball, when the user clicks on screen.")]
	public GameObject ballPrefab;

	[Range(10f, 50f)]
	public float fireAngle = 10f;


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
				// raycast world
				//Vector2 screenPos = Input.GetTouch(0).position;
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
					Destroy (cannonBall, 5f); 

					// fire the cannonball to the hit point
					FireCannonballToPoint(cannonBall, hit.point, fireAngle);
				}
			}
		}

	}

	/// Fires the cannonball to the given point.
	private void FireCannonballToPoint(GameObject cannonBall, Vector3 point, float fireAngle)
	{
		// get
		Rigidbody cannonBallRB = cannonBall ? cannonBall.GetComponent<Rigidbody>() : null;
		if (cannonBallRB == null)
			return;

		// estimate the needed velocity
		Vector3 velocity = GetBallisticVelocity(point, fireAngle);

		cannonBallRB.transform.position = transform.position;
		cannonBallRB.velocity = velocity;
	}

	// calculate the ballistic velocity to reach the destination point
	private Vector3 GetBallisticVelocity(Vector3 destination, float angle)
	{
		Vector3 dir = destination - transform.position; // target Direction
		float height = dir.y; // height difference
		dir.y = 0f; // retain only the horizontal difference
		float dist = dir.magnitude; // horizontal direction
		float a = angle * Mathf.Deg2Rad; // angle to radians
		dir.y = dist * Mathf.Tan(a); // set dir to the elevation angle.
		dist += height / Mathf.Tan(a); // correction for small height differences

		// Calculate the velocity magnitude
		float velocity = Mathf.Sqrt(dist * Physics.gravity.magnitude / Mathf.Sin(2f * a));

		return velocity * dir.normalized;
	}

}
