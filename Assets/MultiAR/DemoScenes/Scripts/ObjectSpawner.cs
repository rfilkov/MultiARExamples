using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour 
{
	[Tooltip("Prefab to be spawn there, where the user taps on screen.")]
	public GameObject objectPrefab;

	// reference to the MultiARManager
	private MultiARManager arManager;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;
	}
	
	void Update () 
	{
		// check for tap
		if (Input.touchCount > 0 && objectPrefab && arManager)
		{
			Touch touch = Input.GetTouch(0);
			if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
			{
				MultiARInterop.TrackableHit hit;
				if(arManager.RaycastScreenToWorld(touch.position, out hit))
				{
					// Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
					// world evolves.
					//var anchor = Session.CreateAnchor(hit.Point, Quaternion.identity);

					// Intanstiate an Andy Android object as a child of the anchor; it's transform will now benefit
					// from the anchor's tracking.
					//GameObject spawnObj = Instantiate(m_andyAndroidPrefab, hit.Point, Quaternion.identity,
					//	anchor.transform);

					GameObject spawnObj = Instantiate(objectPrefab, hit.point, Quaternion.identity);
					Camera arCamera = arManager.GetMainCamera();

					if(arCamera)
					{
						spawnObj.transform.LookAt(arCamera.transform);
						// avoid rotation around x
						Vector3 objRotation = spawnObj.transform.rotation.eulerAngles;
						spawnObj.transform.rotation = Quaternion.Euler(0f, objRotation.y, objRotation.z);
					}

					// Use a plane attachment component to maintain Andy's y-offset from the plane
					// (occurs after anchor updates).
					//spawnObj.GetComponent<PlaneAttachment>().Attach(hit.Plane);
				}
			}
		}

	}

}
