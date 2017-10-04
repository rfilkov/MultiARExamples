using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

public class ARCoreInteface : MonoBehaviour, ARPlatformInterface 
{
	[Tooltip("Reference to the ARCore-Device prefab.")]
	public GameObject arCoreDevicePrefab;

	[Tooltip("Reference to the TrackedPlane prefab.")]
	public GameObject trackedPlanePrefab;

	[Tooltip("Whether to attach the game objects to the planes, where they are anchored.")]
	public bool attachObjectsToPlanes = false;

	//public GameObject envLightPrefab;

	[Tooltip("Whether the interface is enabled by MultiARManager.")]
	private bool isInterfaceEnabled = false;

	// Reference to the MultiARManager in the scene
	private MultiARManager arManager = null;

	// whether the interface was initialized
	private bool isInitialized = false;

	// reference to the AR camera in the scene
	private Camera mainCamera;

	// reference to the AR directional light
	//private Light directionalLight;

	// last frame timestamp
	private double lastFrameTimestamp = 0.0;

	// current tracking state
	private FrameTrackingState cameraTrackingState = FrameTrackingState.TrackingNotInitialized;

	// current light intensity
	protected float currentLightIntensity = 1f;

	// newly detected planes
	private List<TrackedPlane> newTrackedPlanes = new List<TrackedPlane>();

	// all detected planes
	private List<TrackedPlane> allTrackedPlanes = new List<TrackedPlane>();


	// colors to use for plane display
	private Color[] planeColors = new Color[] { 
		Color.blue, Color.cyan, Color.green, Color.grey, Color.magenta, Color.red, Color.white, Color.yellow 
	};

	// input action and screen position
	private MultiARInterop.InputAction inputAction = MultiARInterop.InputAction.None;
	private Vector2 inputPos = Vector2.zero;
	private double inputTimestamp = 0.0;


	/// <summary>
	/// Gets the AR platform supported by the interface.
	/// </summary>
	/// <returns>The AR platform.</returns>
	public MultiARInterop.ARPlatform GetARPlatform()
	{
		return MultiARInterop.ARPlatform.ArCore;
	}

	/// <summary>
	/// Determines whether the platform is available or not.
	/// </summary>
	/// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
	public bool IsPlatformAvailable()
	{
#if UNITY_EDITOR || UNITY_ANDROID
		return true;
#else
		return false;
#endif
	}

	/// <summary>
	/// Sets the enabled or disabled state of the interface.
	/// </summary>
	/// <param name="isEnabled">If set to <c>true</c> interface is enabled.</param>
	public void SetEnabled(bool isEnabled, MultiARManager arManager)
	{
		isInterfaceEnabled = isEnabled;
		this.arManager = arManager;
	}

	/// <summary>
	/// Determines whether the interface is enabled.
	/// </summary>
	/// <returns><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</returns>
	public bool IsEnabled()
	{
		return isInterfaceEnabled;
	}

	/// <summary>
	/// Determines whether the interface is initialized.
	/// </summary>
	/// <returns><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</returns>
	public bool IsInitialized()
	{
		return isInitialized;
	}

	/// <summary>
	/// Gets the main camera.
	/// </summary>
	/// <returns>The main camera.</returns>
	public Camera GetMainCamera()
	{
		return mainCamera;
	}

	/// <summary>
	/// Gets AR-detected light intensity.
	/// </summary>
	/// <returns>The light intensity.</returns>
	public float GetLightIntensity()
	{
		return currentLightIntensity;
	}

	/// <summary>
	/// Gets the last frame timestamp.
	/// </summary>
	/// <returns>The last frame timestamp.</returns>
	public double GetLastFrameTimestamp()
	{
		return lastFrameTimestamp;
	}

	/// <summary>
	/// Gets the state of the camera tracking.
	/// </summary>
	/// <returns>The camera tracking state.</returns>
	public MultiARInterop.CameraTrackingState GetCameraTrackingState()
	{
		switch(cameraTrackingState)
		{
		case FrameTrackingState.TrackingNotInitialized:
			return MultiARInterop.CameraTrackingState.NotInitialized;
		case FrameTrackingState.LostTracking:
			return MultiARInterop.CameraTrackingState.LimitedTracking;
		case FrameTrackingState.Tracking:
			return MultiARInterop.CameraTrackingState.NormalTracking;
		}

		return MultiARInterop.CameraTrackingState.Unknown;
	}

	/// <summary>
	/// Gets the tracking error message, if any.
	/// </summary>
	/// <returns>The tracking error message.</returns>
	public string GetTrackingErrorMessage()
	{
		return string.Empty;
	}

	/// <summary>
	/// Gets the tracked planes timestamp.
	/// </summary>
	/// <returns>The tracked planes timestamp.</returns>
	public double GetTrackedPlanesTimestamp()
	{
		return lastFrameTimestamp;
	}

	/// <summary>
	/// Gets the count of currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes count.</returns>
	public int GetTrackedPlanesCount()
	{
		return allTrackedPlanes.Count;
	}

	/// <summary>
	/// Gets the currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes.</returns>
	public MultiARInterop.TrackedPlane[] GetTrackedPlanes()
	{
		MultiARInterop.TrackedPlane[] trackedPlanes = new MultiARInterop.TrackedPlane[allTrackedPlanes.Count];

		for(int i = 0; i < allTrackedPlanes.Count; i++)
		{
			TrackedPlane plane = allTrackedPlanes[i];
			trackedPlanes[i] = new MultiARInterop.TrackedPlane();

			trackedPlanes[i].position = plane.Position;
			trackedPlanes[i].rotation = plane.Rotation;
			trackedPlanes[i].bounds = plane.Bounds;
		}

		return trackedPlanes;
	}

	/// <summary>
	/// Gets the input action.
	/// </summary>
	/// <returns>The input action.</returns>
	public MultiARInterop.InputAction GetInputAction()
	{
		return inputAction;
	}

	/// <summary>
	/// Gets the input-action timestamp.
	/// </summary>
	/// <returns>The input-action timestamp.</returns>
	public double GetInputTimestamp()
	{
		return inputTimestamp;
	}

	/// <summary>
	/// Clears the input action.
	/// </summary>
	public void ClearInputAction()
	{
		inputAction = MultiARInterop.InputAction.None;
		inputTimestamp = lastFrameTimestamp;
	}

	/// <summary>
	/// Raycasts from screen point or camera to the world.
	/// </summary>
	/// <returns><c>true</c>, if a plane was hit, <c>false</c> otherwise.</returns>
	/// <param name="screenPos">Screen position.</param>
	/// <param name="hit">Hit data.</param>
	public bool RaycastToWorld(bool fromInputPos, out MultiARInterop.TrackableHit hit)
	{
		hit = new MultiARInterop.TrackableHit();
		if(!isInitialized || (cameraTrackingState == FrameTrackingState.TrackingNotInitialized))
			return false;
		
		TrackableHit intHit;
		TrackableHitFlag raycastFilter = TrackableHitFlag.PlaneWithinBounds | TrackableHitFlag.PlaneWithinPolygon;

		if(arManager && !arManager.hitTrackedServicesOnly)
		{
			raycastFilter |= TrackableHitFlag.PlaneWithinInfinity;
			raycastFilter |= TrackableHitFlag.PointCloud;
		}

		Vector2 screenPos = fromInputPos ? inputPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
		if (Session.Raycast(mainCamera.ScreenPointToRay(screenPos), raycastFilter, out intHit))
		{
			hit.point = intHit.Point;
			hit.distance = intHit.Distance;
			hit.psObject = intHit.Plane;

			return true;
		}

		return false;
	}

	/// <summary>
	/// Anchors the game object to world.
	/// </summary>
	/// <returns>The game object to world.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="hit">Trackable hit.</param>
	public string AnchorGameObjectToWorld(GameObject gameObj, MultiARInterop.TrackableHit hit)
	{
		string anchorId = AnchorGameObjectToWorld(gameObj, hit.point, Quaternion.identity);

		if(!string.IsNullOrEmpty(anchorId) && hit.psObject != null && attachObjectsToPlanes)
		{
			// valid anchor - attach the tracked plane
			TrackedPlane trackedPlane = (TrackedPlane)hit.psObject;

			GoogleARCore.HelloAR.PlaneAttachment planeAttachment = gameObj.GetComponent<GoogleARCore.HelloAR.PlaneAttachment>();
			if(planeAttachment == null)
			{
				planeAttachment = gameObj.AddComponent<GoogleARCore.HelloAR.PlaneAttachment>();
			}

			planeAttachment.Attach(trackedPlane);
		}

		return anchorId;
	}

	/// <summary>
	/// Anchors the game object to world.
	/// </summary>
	/// <returns>The anchor Id, or empty string.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="worldPosition">World position.</param>
	/// <param name="worldRotation">World rotation.</param>
	public string AnchorGameObjectToWorld(GameObject gameObj, Vector3 worldPosition, Quaternion worldRotation)
	{
		if(!isInitialized || (cameraTrackingState == FrameTrackingState.TrackingNotInitialized))
			return string.Empty;

		if(arManager)
		{
			Anchor anchor = Session.CreateAnchor(worldPosition, worldRotation);
			DontDestroyOnLoad(anchor.gameObject);  // don't destroy it accross scenes

			if(gameObj)
			{
				gameObj.transform.SetParent(anchor.transform, true);
				gameObj.transform.localPosition = Vector3.zero;
			}

			MultiARInterop.MultiARData arData = arManager.GetARData();
			arData.allAnchorsDict[anchor.Id] = new List<GameObject>();

			if(gameObj)
			{
				arData.allAnchorsDict[anchor.Id].Add(gameObj);
			}

			return anchor.Id;
		}

		return string.Empty;
	}

	/// <summary>
	/// Unparents the game object and removes the anchor from the system (if possible).
	/// </summary>
	/// <returns><c>true</c>, if game object anchor was removed, <c>false</c> otherwise.</returns>
	/// <param name="anchorId">Anchor identifier.</param>
	/// <param name="keepObjActive">If set to <c>true</c> keeps the object active afterwards.</param>
	public bool RemoveGameObjectAnchor(string anchorId, bool keepObjActive)
	{
		if(!isInitialized || !arManager)
			return false;

		MultiARInterop.MultiARData arData = arManager.GetARData();
		if(arData.allAnchorsDict.ContainsKey(anchorId))
		{
			// remove the anchor from the system
			// ARCore doesn't provide API for removal (as of preview version)
			// all anchor game objects remain in inactive state

			// get the child game object
			List<GameObject> anchoredObjs = arData.allAnchorsDict[anchorId];
			arData.allAnchorsDict.Remove(anchorId);

			foreach(GameObject anchoredObj in anchoredObjs)
			{
				// detach the parent
				if(anchoredObj && anchoredObj.transform.parent)
				{
					GameObject parentObj = anchoredObj.transform.parent.gameObject;
					anchoredObj.transform.parent = null;

					if(!keepObjActive)
					{
						anchoredObj.SetActive(false);
					}

					//Destroy(parentObj);  // ARCore uses the object internally
				}

				if(anchoredObj)
				{
					// remove the plane attachment
					GoogleARCore.HelloAR.PlaneAttachment planeAttachment = anchoredObj.GetComponent<GoogleARCore.HelloAR.PlaneAttachment>();
					if(planeAttachment != null)
					{
						Destroy(planeAttachment);
					}
				}
			}

			return true;
		}

		return false;
	}

	// -- // -- // -- // -- // -- // -- // -- // -- // -- // -- //

	public void Start()
	{
		if(!isInterfaceEnabled)
			return;
		
		if(!arCoreDevicePrefab)
		{
			Debug.LogError("ARCore-interface cannot start: ArCoreDevicePrefab is not set.");
			return;
		}

		// disable the main camera, if any
		Camera currentCamera = MultiARInterop.GetMainCamera();
		if(currentCamera)
		{
			currentCamera.gameObject.SetActive(false);
		}

		// create ARCore-Device in the scene
		GameObject arCoreDeviceObj = Instantiate(arCoreDevicePrefab, Vector3.zero, Quaternion.identity);
		arCoreDeviceObj.name = "ARCore Device";

		// reference to the AR main camera
		mainCamera = arCoreDeviceObj.GetComponentInChildren<Camera>();

//		// disable directional light, if any
//		Light currentLight = MultiARInterop.GetDirectionalLight();
//		if(currentLight)
//		{
//			currentLight.gameObject.SetActive(false);
//		}
//
//		// create AR environmental light
//		GameObject envLight = new GameObject("Evironmental Light");
//		//envLight.transform.position = Vector3.zero;
//		//envLight.transform.rotation = Quaternion.identity;
//		envLight.AddComponent<EnvironmentalLight>();
//
//		// reference to the AR directional light
//		//directionalLight = envLight.GetComponent<Light>();

		// modify the directional light
		Light currentLight = MultiARInterop.GetDirectionalLight();
		if(!currentLight)
		{
			GameObject currentLightObj = new GameObject("Directional light");

			currentLight = currentLightObj.AddComponent<Light>();
			currentLight.type = LightType.Directional;
		}

		// reset light position & rotation
		currentLight.transform.position = Vector3.zero;
		currentLight.transform.rotation = Quaternion.Euler(40f, 40f, 0f);

		// set light parameters
		//currentLight.lightmapBakeType = LightmapBakeType.Mixed;
		currentLight.color = new Color32(255, 254, 244, 255);

		// add the ar-light component
		currentLight.gameObject.AddComponent<MultiARDirectionalLight>();


		if(arManager && arManager.getPointCloud)
		{
			MultiARInterop.MultiARData arData = arManager.GetARData();

			arData.pointCloudData = new Vector3[MultiARInterop.MAX_POINT_COUNT];
			arData.pointCloudLength = 0;
			arData.pointCloudTimestamp = 0.0;
		}

		// interface is initialized
		isInitialized = true;
	}

	public void OnDestroy()
	{
	}


	void Update()
	{
		if(!isInitialized)
			return;

		// check for errors
		_QuitOnConnectionErrors();

		// check for input (touch)
		CheckForInputAction();

		// tracking state
		cameraTrackingState = Frame.TrackingState;
		if(cameraTrackingState == FrameTrackingState.TrackingNotInitialized)
			return;

		// get frame timestamp and light intensity
		lastFrameTimestamp = Frame.Timestamp;
		currentLightIntensity = Frame.LightEstimate.PixelIntensity;

		// get point cloud, if needed
		MultiARInterop.MultiARData arData = arManager.GetARData();

		if(arManager.getPointCloud)
		{
			PointCloud pointcloud = Frame.PointCloud;
			if (pointcloud.PointCount > 0 && pointcloud.Timestamp > arData.pointCloudTimestamp)
			{
				// Copy the point cloud points
				for (int i = 0; i < pointcloud.PointCount; i++)
				{
					arData.pointCloudData[i] = pointcloud.GetPoint(i);
				}

				arData.pointCloudLength = pointcloud.PointCount;
				arData.pointCloudTimestamp = pointcloud.Timestamp;
			}
		}

		// display the tracked planes if needed
		if(arManager.displayTrackedSurfaces && trackedPlanePrefab)
		{
			// get the new planes
			Frame.GetNewPlanes(ref newTrackedPlanes);

			// Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
			for (int i = 0; i < newTrackedPlanes.Count; i++)
			{
				// Instantiate a plane visualization prefab and set it to track the new plane.
				GameObject planeObject = Instantiate(trackedPlanePrefab, Vector3.zero, Quaternion.identity);
				planeObject.GetComponent<GoogleARCore.HelloAR.TrackedPlaneVisualizer>().SetTrackedPlane(newTrackedPlanes[i]);

				// Apply a random color and grid rotation.
				planeObject.GetComponent<Renderer>().material.SetColor("_GridColor", planeColors[Random.Range(0, planeColors.Length - 1)]);
				planeObject.GetComponent<Renderer>().material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
			}
		}

		// get all tracked planes
		Frame.GetAllPlanes(ref allTrackedPlanes);

		// check the status of the anchors
		List<string> alAnchorsToRemove = new List<string>();

		foreach(string anchorId in arData.allAnchorsDict.Keys)
		{
			List<GameObject> anchoredObjs = arData.allAnchorsDict[anchorId];

			foreach(GameObject anchoredObj in anchoredObjs)
			{
				Transform parentTrans = anchoredObj.transform.parent;

				if(parentTrans == null)
				{
					if(!alAnchorsToRemove.Contains(anchorId))
						alAnchorsToRemove.Add(anchorId);
					anchoredObj.SetActive(false);
				}
				else
				{
					Anchor anchor = parentTrans.GetComponent<Anchor>();

					if(anchor == null || anchor.TrackingState == AnchorTrackingState.StoppedTracking)
					{
						if(!alAnchorsToRemove.Contains(anchorId))
							alAnchorsToRemove.Add(anchorId);

						anchoredObj.transform.parent = null;  
						anchoredObj.SetActive(false);
					}
				}
			}
		}

		// remove the stopped anchors from our list
		foreach(string anchorId in alAnchorsToRemove)
		{
			arData.allAnchorsDict.Remove(anchorId);
		}

		// clean up
		alAnchorsToRemove.Clear();
	}

	// check for input action (phone touch)
	private void CheckForInputAction()
	{
		if (Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			bool bInputAction = true;

			switch(touch.phase)
			{
			case TouchPhase.Began:
				inputAction = MultiARInterop.InputAction.Click;
				break;

			case TouchPhase.Moved:
			case TouchPhase.Stationary:
				inputAction = MultiARInterop.InputAction.Grip;
				break;

			case TouchPhase.Ended:
				inputAction = MultiARInterop.InputAction.Release;
				break;

			default:
				bInputAction = false;
				break;
			}

			if(bInputAction)
			{
				inputPos = touch.position;
				inputTimestamp = lastFrameTimestamp;
			}
		}
	}

	/// <summary>
	/// Quit the application if there was a connection error for the ARCore session.
	/// </summary>
	private void _QuitOnConnectionErrors()
	{
		// Do not update if ARCore is not tracking.
		if (Session.ConnectionState == SessionConnectionState.DeviceNotSupported)
		{
			_ShowAndroidToastMessage("This device does not support ARCore.");
			Application.Quit();
		}
		else if (Session.ConnectionState == SessionConnectionState.UserRejectedNeededPermission)
		{
			_ShowAndroidToastMessage("Camera permission is needed to run this application.");
			Application.Quit();
		}
		else if (Session.ConnectionState == SessionConnectionState.ConnectToServiceFailed)
		{
			_ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
			Application.Quit();
		}
	}

	/// <summary>
	/// Show an Android toast message.
	/// </summary>
	/// <param name="message">Message string to show in the toast.</param>
	/// <param name="length">Toast message time length.</param>
	private static void _ShowAndroidToastMessage(string message)
	{
		AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

		if (unityActivity != null)
		{
			AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
			unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
				{
					AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
						message, 0);
					toastObject.Call("show");
				}));
		}
	}

}
