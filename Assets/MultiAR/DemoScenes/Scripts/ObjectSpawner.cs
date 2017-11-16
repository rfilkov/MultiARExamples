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
		// don't consider taps over the UI
		if(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
			return;

		// check for tap
		if (objectPrefab && arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				// raycast world
				//Vector2 screenPos = Input.GetTouch(0).position;
				MultiARInterop.TrackableHit hit;

				if(arManager.RaycastToWorld(true, out hit))
				{
					// instantiate the object and anchor it to the world position
					GameObject spawnObj = Instantiate(objectPrefab, hit.point, Quaternion.identity);
					arManager.AnchorGameObjectToWorld(spawnObj, hit);

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
