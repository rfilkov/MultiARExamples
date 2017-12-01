using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectScaler : MonoBehaviour 
{
	[Tooltip("Prefab to be placed and zoomed in the scene.")]
	public GameObject objectPrefab;

	[Tooltip("Smooth factor for object scaling.")]
	public float smoothFactor = 10f;

	[Tooltip("UI-Text to show information messages.")]
	public UnityEngine.UI.Text infoText;

	// reference to the MultiARManager
	private MultiARManager arManager;

	// instantiated object
	private GameObject objectInstance;
	private float lastInstanceTime = 0f;

	// start object scale and target scale
	private Vector3 objectScale = Vector3.one;
	private Vector3 targetScale = Vector3.one;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;
	}

	void Update () 
	{
		// check for click
		if (objectPrefab && arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click && (Time.time - lastInstanceTime) >= 1.5f)
			{
				// dont allow too often instance creation
				lastInstanceTime = Time.time;

				// remove current object, if any
				if(objectInstance)
				{
					DestroyObjectInstance();
				}

				// raycast world
				MultiARInterop.TrackableHit hit;
				if(arManager.RaycastToWorld(true, out hit))
				{
					// instantiate the object and anchor it to the world position
					objectInstance = Instantiate(objectPrefab, hit.point, Quaternion.identity);
					arManager.AnchorGameObjectToWorld(objectInstance, hit);

					// get the initial scale
					objectScale = objectInstance.transform.localScale;

					// look at the camera
					Camera arCamera = arManager.GetMainCamera();
					if(arCamera)
					{
						objectInstance.transform.LookAt(arCamera.transform);
						// avoid rotation around x
						Vector3 objRotation = objectInstance.transform.rotation.eulerAngles;
						objectInstance.transform.rotation = Quaternion.Euler(0f, objRotation.y, objRotation.z);
					}

					if(infoText)
					{
						infoText.text = string.Format("{0} placed at {1}", objectPrefab.name, hit.point);
					}
				}
			}
			else if(objectInstance && action == MultiARInterop.InputAction.Grip)
			{
				// get nav coordinates
				Vector3 navCoords = arManager.GetInputNavCoordinates();

				// estimate the scale change and target scale
				Vector3 scaleChange = navCoords.x >= 0 ? (objectScale * navCoords.x) : ((objectScale / 2f) * navCoords.x);
				targetScale = objectScale + scaleChange;

				objectInstance.transform.localScale = Vector3.Lerp(objectInstance.transform.localScale, targetScale, smoothFactor * Time.deltaTime);

				if(infoText)
				{
					infoText.text = string.Format("Current scale: {0:F2}", objectInstance.transform.localScale.x);
				}
			}
			else if(action == MultiARInterop.InputAction.Release)
			{
				if(infoText)
				{
					infoText.text = "Tap to place the object, then drag right or left, to scale it up or down.";
				}
			}
		}
	}


	// removes the object instance and detaches it from the world
	private void DestroyObjectInstance()
	{
		if(objectInstance)
		{
			// remove object anchor, if it was anchored before
			string anchorId = arManager.GetObjectAnchorId(objectInstance);
			if (anchorId != string.Empty) 
			{
				arManager.RemoveGameObjectAnchor(anchorId, false);
			}

			// destroy object
			Destroy(objectInstance);
			objectInstance = null;
		}
	}

}
