﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestObjectScaler : MonoBehaviour 
{
	[Tooltip("Prefab to be placed and scaled in the scene.")]
	public GameObject objectPrefab;

	[Tooltip("Cube prefab, used to outline object bounds.")]
	public GameObject boundsPrefab;

	[Tooltip("Smooth factor for object scaling.")]
	public float smoothFactor = 10f;

	[Tooltip("UI-Text to show information messages.")]
	public UnityEngine.UI.Text infoText;

	// reference to the MultiARManager
	//private MultiARManager arManager;

	// instantiated object
	private GameObject objectInstance;
	private Renderer objectRenderer;
	private float lastInstanceTime = 0f;

	// instantiated cube to outline object's bounds
	private GameObject boundsInstance = null;

	// start object scale and target scale
	private Vector3 objectScale = Vector3.one;
	private Vector3 targetScale = Vector3.one;


	void Start () 
	{
		// get reference to MultiARManager
		//arManager = MultiARManager.Instance;
	}

	void Update () 
	{
		// check for click
		if (objectPrefab) // && arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			//MultiARInterop.InputAction action = arManager.GetInputAction();

			if (Input.GetKeyDown(KeyCode.Space) && (Time.time - lastInstanceTime) >= 1.5f)
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

				hit.point = new Vector3(0f, 0f, 1f);
				//if(arManager.RaycastToWorld(true, out hit))
				{
					// instantiate the object and anchor it to the world position
					objectInstance = Instantiate(objectPrefab, hit.point, Quaternion.identity);
					//arManager.AnchorGameObjectToWorld(objectInstance, hit);

					// get object renderer & initial scale
					objectRenderer = GetObjectRenderer(objectInstance);
					objectScale = objectInstance.transform.localScale;

					// look at the camera
					Camera arCamera = Camera.main; // arManager.GetMainCamera();
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
			else if(objectInstance && Input.GetAxis("Horizontal") != 0f) // action == MultiARInterop.InputAction.Grip)
			{
				// get nav coordinates
				Vector3 navCoords = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f);  // arManager.GetInputNavCoordinates();

				// estimate the scale change and target scale
				Vector3 scaleChange = navCoords.x >= 0 ? (objectScale * navCoords.x) : ((objectScale / 2f) * navCoords.x);
				targetScale = objectScale + scaleChange;

				objectInstance.transform.localScale = Vector3.Lerp(objectInstance.transform.localScale, targetScale, smoothFactor * Time.deltaTime);

				if(infoText)
				{
					float fScaleChange = 1f + (navCoords.x >= 0 ? navCoords.x : (0.5f * navCoords.x));
					infoText.text = string.Format("Scale change: {0:F2}", fScaleChange);
				}

				// outline object bounds
				if (objectRenderer && boundsPrefab) 
				{
					Bounds objectBounds = objectRenderer.bounds;

					// instantiate bounds-cube, if needed
					if (boundsInstance == null) 
					{
						boundsInstance = GameObject.Instantiate(boundsPrefab);
						boundsInstance.transform.SetParent(objectInstance.transform);
					}

					// set the bounds-cube tras=nsform
					boundsInstance.transform.position = objectBounds.center;
					boundsInstance.transform.rotation = objectInstance.transform.rotation;

					Vector3 objScale = objectInstance.transform.localScale;
					Vector3 boundsScale = new Vector3(objectBounds.size.x / objScale.x, objectBounds.size.y / objScale.y, objectBounds.size.z / objScale.z);
					boundsInstance.transform.localScale = boundsScale;
				}
			}
			else if(Input.GetAxis("Horizontal") == 0f) //action == MultiARInterop.InputAction.Release)
			{
				// instantiate bounds-cube, if needed
				if (boundsInstance != null) 
				{
					Destroy(boundsInstance);
					boundsInstance = null;
				}

				if(infoText)
				{
					infoText.text = "Tap to place the object, then drag right or left, to scale it up or down.";
				}
			}
		}
	}


	// returns the biggest mesh renderer
	private Renderer GetObjectRenderer(GameObject obj)
	{
		Renderer objRenderer = null;
		float objBoundsSize = 0f;

		Renderer[] meshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
		if (meshRenderers == null || meshRenderers.Length == 0) 
		{
			meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
		}

		if (meshRenderers != null && meshRenderers.Length > 0) 
		{
			foreach (Renderer renderer in meshRenderers) 
			{
				Bounds bounds = renderer.bounds;
				float boundsSize = bounds.size.x + bounds.size.y + bounds.size.z;

				if (boundsSize > objBoundsSize) 
				{
					objRenderer = renderer;
					objBoundsSize = boundsSize;
				}
			}
		}

		return objRenderer;
	}


	// removes the object instance and detaches it from the world
	private void DestroyObjectInstance()
	{
		if(objectInstance)
		{
//			// remove object anchor, if it was anchored before
//			string anchorId = arManager.GetObjectAnchorId(objectInstance);
//			if (anchorId != string.Empty) 
//			{
//				arManager.RemoveGameObjectAnchor(anchorId, false);
//			}

			// destroy object
			Destroy(objectInstance);
			objectInstance = null;
		}
	}

}
