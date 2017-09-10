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
			if (Input.GetTouch(0).phase == TouchPhase.Began)
			{
				Vector2 screenPos = Input.GetTouch(0).position;
				MultiARInterop.TrackableHit hit;

				if(arManager.RaycastScreenToWorld(screenPos, out hit))
				{
					// instantiate the object and anchor it to the world position
					GameObject spawnObj = Instantiate(objectPrefab, hit.point, Quaternion.identity);
					arManager.AnchorGameObjectToWorld(spawnObj, hit.point, Quaternion.identity);

					// look at the camera
					Camera arCamera = arManager.GetMainCamera();
					if(arCamera)
					{
						spawnObj.transform.LookAt(arCamera.transform);
						// avoid rotation around x
						Vector3 objRotation = spawnObj.transform.rotation.eulerAngles;
						spawnObj.transform.rotation = Quaternion.Euler(0f, objRotation.y, objRotation.z);
					}
				}
			}
		}

	}

}
