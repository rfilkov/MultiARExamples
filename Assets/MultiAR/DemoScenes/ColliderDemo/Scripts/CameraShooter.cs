using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShooter : MonoBehaviour 
{

	[Tooltip("Prefab to be used as cannonball.")]
	public GameObject ballPrefab;

	[Tooltip("Aim given distance (in meters) above the hit-point.")]
	[Range(0f, 0.5f)]
	public float aimAboveDistance = 0.1f;

	[Tooltip("Factor used to determine the force, according to target point distance.")]
	public float forceFactor = 100f;

	[Tooltip("Whether to destroy the object automatically, some time after its creation.")]
	public float destroyInSeconds = 3f;

	[Tooltip("List of available ball materials.")]
	public Material[] ballMaterials;

	// reference to the MultiARManager
	private MultiARManager arManager;
	// object instance counter
	private int objectCounter = 0;

	// Use this for initialization
	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;
	}
	
	// Update is called once per frame
	void Update () 
	{
        //// test only
        //if(arManager && arManager.IsInitialized())
        //{
        //    Texture texBack = arManager.GetBackgroundTex();
        //}

        // check for click
        if (ballPrefab && arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				// gets the main camera
				Camera arCamera = arManager.GetMainCamera();
				if (!arCamera)
					return;

				// raycast scene objects (including overlay surfaces)
				MultiARInterop.TrackableHit hit;
				Vector3 targetPoint = Vector3.zero;

				if (arManager.RaycastToScene (true, out hit))
					targetPoint = hit.point;
				else
					targetPoint = arCamera.transform.forward * 3f;  // emulate target in the line of sight

				// instantiate the cannonball. schedule it for destroy in 3 seconds
				GameObject cannonBall = Instantiate(ballPrefab, arCamera.transform.position, arCamera.transform.rotation);
				cannonBall.name = ballPrefab.name + "-" + objectCounter;
				objectCounter++;

				if(destroyInSeconds > 0f)
				{
					Destroy(cannonBall, destroyInSeconds);
				}

				// set random ball material
				SetBallMaterial(cannonBall);

				// fire the cannonball
				targetPoint.y -= aimAboveDistance;
				FireCannonball(cannonBall, arCamera.transform.position, targetPoint);
			}
		}

	}


	// sets random ball material from the list of available materials
	private void SetBallMaterial(GameObject cannonBall)
	{
		if(ballMaterials == null || ballMaterials.Length == 0)
			return;

		// get the mesh renderer
		MeshRenderer meshRenderer = cannonBall ? cannonBall.GetComponent<MeshRenderer>() : null;
		if (meshRenderer == null)
			return;

		// set random material
		int matIndex = Mathf.RoundToInt(Random.Range(0, ballMaterials.Length - 1));
		meshRenderer.material = ballMaterials[matIndex];
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
