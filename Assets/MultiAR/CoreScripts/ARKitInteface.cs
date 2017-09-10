﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class ARKitInteface : MonoBehaviour, ARPlatformInterface 
{
	[Tooltip("Material used for camera background.")]
	public Material yuvMaterial;

	[Tooltip("Reference to the TrackedPlane prefab.")]
	public GameObject trackedPlanePrefab;

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

	// whether the frame event was added or not
	private bool isAREventsAdded = false;

	// last frame timestamp
	private double lastFrameTimestamp = 0.0;

	// current tracking state
	private ARTrackingState cameraTrackingState = ARTrackingState.ARTrackingStateNotAvailable;
	private ARTrackingStateReason cameraTrackingReason = ARTrackingStateReason.ARTrackingStateReasonNone;

	// current light intensity
	protected float currentLightIntensity = 0f;
	protected float currentColorTemperature = 0f;

	// tracked planes timestamp
	private double trackedPlanesTimestamp = 0.0;

	// plane anchors and visualizations
	private Dictionary<string, ARPlaneAnchorGameObject> planeAnchorMap = new Dictionary<string, ARPlaneAnchorGameObject>();


	/// <summary>
	/// Gets the AR platform supported by the interface.
	/// </summary>
	/// <returns>The AR platform.</returns>
	public MultiARInterop.ARPlatform GetARPlatform()
	{
		return MultiARInterop.ARPlatform.ArKit;
	}

	/// <summary>
	/// Determines whether the platform is available or not.
	/// </summary>
	/// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
	public bool IsPlatformAvailable()
	{
#if UNITY_EDITOR || UNITY_IOS
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
	/// Gets the last frame timestamp.
	/// </summary>
	/// <returns>The last frame timestamp.</returns>
	public double GetLastFrameTimestamp()
	{
		return lastFrameTimestamp;
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
	/// Gets the state of the camera tracking.
	/// </summary>
	/// <returns>The camera tracking state.</returns>
	public MultiARInterop.CameraTrackingState GetCameraTrackingState()
	{
		switch(cameraTrackingState)
		{
		case ARTrackingState.ARTrackingStateNotAvailable:
			return MultiARInterop.CameraTrackingState.NotInitialized;
		case ARTrackingState.ARTrackingStateNormal:  // should be ARTrackingState.ARTrackingStateLimited
			return MultiARInterop.CameraTrackingState.LimitedTracking;
		case ARTrackingState.ARTrackingStateLimited:  // should be ARTrackingState.ARTrackingStateNormal
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
		if (cameraTrackingReason != ARTrackingStateReason.ARTrackingStateReasonNone) 
		{
			return cameraTrackingReason.ToString();
		}

		return string.Empty;
	}

	/// <summary>
	/// Gets the tracked planes timestamp.
	/// </summary>
	/// <returns>The tracked planes timestamp.</returns>
	public double GetTrackedPlanesTimestamp()
	{
		return trackedPlanesTimestamp;
	}

	/// <summary>
	/// Gets the count of currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes count.</returns>
	public int GetTrackedPlanesCount()
	{
		return planeAnchorMap.Count;
	}

	/// <summary>
	/// Gets the currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes.</returns>
	public MultiARInterop.TrackedPlane[] GetTrackedPlanes()
	{
		MultiARInterop.TrackedPlane[] trackedPlanes = new MultiARInterop.TrackedPlane[planeAnchorMap.Count];

		// get current planes list
		List<ARPlaneAnchorGameObject> listPlaneObjs = new List<ARPlaneAnchorGameObject>(planeAnchorMap.Values);
		int i = 0;

		foreach (ARPlaneAnchorGameObject arpag in listPlaneObjs) 
		{
			ARPlaneAnchor planeAnchor = arpag.planeAnchor;
			trackedPlanes[i] = new MultiARInterop.TrackedPlane();

			trackedPlanes[i].position = UnityARMatrixOps.GetPosition(planeAnchor.transform);
			trackedPlanes[i].rotation = UnityARMatrixOps.GetRotation(planeAnchor.transform);
			trackedPlanes[i].bounds = new Vector2(planeAnchor.extent.x * 0.1f, planeAnchor.extent.z * 0.1f);

			i++;
		}

		// clear temporary lists
		listPlaneObjs.Clear();

		return trackedPlanes;
	}

	/// <summary>
	/// Raycasts from screen point to the world.
	/// </summary>
	/// <returns><c>true</c>, if a plane was hit, <c>false</c> otherwise.</returns>
	/// <param name="screenPos">Screen position.</param>
	/// <param name="hit">Hit data.</param>
	public bool RaycastScreenToWorld(Vector2 screenPos, out MultiARInterop.TrackableHit hit)
	{
		hit = new MultiARInterop.TrackableHit();
		if(!isInitialized)
			return false;

		var viewPos = mainCamera.ScreenToViewportPoint(screenPos);
		ARPoint point = new ARPoint {
			x = viewPos.x,
			y = viewPos.y
		};

		// prioritize result types
		ARHitTestResultType[] resultTypes = {
			ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent, 
			// if you want to use infinite planes use this:
			//ARHitTestResultType.ARHitTestResultTypeExistingPlane,
			ARHitTestResultType.ARHitTestResultTypeHorizontalPlane, 
			ARHitTestResultType.ARHitTestResultTypeFeaturePoint
		}; 

		foreach (ARHitTestResultType resultType in resultTypes)
		{
			List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, resultType);

			if (hitResults.Count > 0) 
			{
				foreach (var hitResult in hitResults) 
				{
					if(hitResult.isValid)
					{
						hit.point = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
						hit.distance = (float)hitResult.distance;
						hit.anchorId = hitResult.anchorIdentifier;

						return true;
					}
				}
			}
		}

		return false;
	}

	// -- // -- // -- // -- // -- // -- // -- // -- // -- // -- //

	void Start()
	{
		if(!isInterfaceEnabled)
			return;

		if(!yuvMaterial)
		{
			Debug.LogError("ARKit-interface cannot start: YuvMaterial is not set.");
			return;
		}

		// modify the main camera in the scene
		Camera currentCamera = MultiARInterop.GetMainCamera();
		if(!currentCamera)
		{
			GameObject currentCameraObj = new GameObject("Main Camera");
			currentCameraObj.tag = "MainCamera";

			currentCamera = currentCameraObj.AddComponent<Camera>();
		}

		// reset camera position & rotation
		currentCamera.transform.position = Vector3.zero;
		currentCamera.transform.rotation = Quaternion.identity;

		// set camera parameters
		currentCamera.clearFlags = CameraClearFlags.Depth;
		currentCamera.nearClipPlane = 0.1f;
		currentCamera.farClipPlane = 30f;

		// reference to the AR main camera
		mainCamera = currentCamera;

		// add camera parent
		if(currentCamera.transform.parent == null)
		{
			GameObject cameraParent = new GameObject("CameraParent");
			currentCamera.transform.SetParent(cameraParent.transform);
		}

		// add the needed camera components
		UnityARVideo arVideo = currentCamera.gameObject.AddComponent<UnityARVideo>();
		arVideo.m_ClearMaterial = yuvMaterial;

		currentCamera.gameObject.AddComponent<UnityARCameraNearFar>();

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

		// add the needed component
		currentLight.gameObject.AddComponent<UnityARAmbient>();

		// reference to the AR directional light
		//directionalLight = currentLight;

		// create camera manager
		GameObject camManagerObj = new GameObject("ARCameraManager");
		UnityARCameraManager camManager = camManagerObj.AddComponent<UnityARCameraManager>();
		camManager.m_camera = currentCamera;

		// check for point cloud getter
		if(arManager.getPointCloud)
		{
			MultiARInterop.MultiARData arData = arManager.GetARData();

			arData.pointCloudData = new Vector3[0];
			arData.pointCloudLength = 0;
			arData.pointCloudTimestamp = 0.0;
		}

		// check for tracked plane display
		if(arManager.displayTrackedSurfaces && trackedPlanePrefab)
		{
			UnityARUtility.InitializePlanePrefab(trackedPlanePrefab);
		}

		// add needed events
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
		UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += ARSessionTrackingChanged;

		UnityARSessionNativeInterface.ARAnchorAddedEvent += AddPlaneAnchor;
		UnityARSessionNativeInterface.ARAnchorUpdatedEvent += UpdatePlaneAnchor;
		UnityARSessionNativeInterface.ARAnchorRemovedEvent += RemovePlaneAnchor;

		isAREventsAdded = true;


		// interface is initialized
		isInitialized = true;
	}

	public void OnDestroy()
	{
		// remove event handlers
		if(isAREventsAdded)
		{
			isAREventsAdded = false;

			UnityARSessionNativeInterface.ARFrameUpdatedEvent -= ARFrameUpdated;
			UnityARSessionNativeInterface.ARSessionTrackingChangedEvent -= ARSessionTrackingChanged;

			UnityARSessionNativeInterface.ARAnchorAddedEvent -= AddPlaneAnchor;
			UnityARSessionNativeInterface.ARAnchorUpdatedEvent -= UpdatePlaneAnchor;
			UnityARSessionNativeInterface.ARAnchorRemovedEvent -= RemovePlaneAnchor;
		}

		// destroy persistent plane objects
		List<ARPlaneAnchorGameObject> listPlaneObjs = new List<ARPlaneAnchorGameObject>(planeAnchorMap.Values);
		foreach (ARPlaneAnchorGameObject arpag in listPlaneObjs) 
		{
			if(arpag.gameObject)
			{
				GameObject.Destroy(arpag.gameObject);
			}
		}

		// clear plane anchor lists
		listPlaneObjs.Clear();
		planeAnchorMap.Clear();
	}

	// invoked by FrameUpdated-event
	public void ARFrameUpdated(UnityARCamera camera)
	{
		// current timestamp
		lastFrameTimestamp = GetCurrentTimestamp();

		// current light intensity
		currentLightIntensity = camera.lightEstimation.ambientIntensity / 1000f;
		currentColorTemperature = camera.lightEstimation.ambientColorTemperature;

		// point cloud
		if(arManager.getPointCloud)
		{
			MultiARInterop.MultiARData arData = arManager.GetARData();

			arData.pointCloudData = camera.pointCloudData;
			arData.pointCloudLength = arData.pointCloudData.Length;
			arData.pointCloudTimestamp = lastFrameTimestamp;
		}
	}

	// invoked by TrackingChanged-event
	public void ARSessionTrackingChanged(UnityARCamera camera)
	{
		cameraTrackingState = camera.trackingState;
		cameraTrackingReason = camera.trackingReason;
	}

	// invoked by AnchorAdded-event
	public void AddPlaneAnchor(ARPlaneAnchor arPlaneAnchor)
	{
		GameObject go = null;
		if(arManager.displayTrackedSurfaces)
		{
			go = UnityARUtility.CreatePlaneInScene(arPlaneAnchor);
			go.AddComponent<DontDestroyOnLoad>();  // these GOs persist across scene loads
		}

		ARPlaneAnchorGameObject arpag = new ARPlaneAnchorGameObject();
		arpag.planeAnchor = arPlaneAnchor;
		arpag.gameObject = go;

		planeAnchorMap.Add(arPlaneAnchor.identifier, arpag);
		trackedPlanesTimestamp = GetLastFrameTimestamp();
	}

	// invoked by AnchorUpdated-event
	public void UpdatePlaneAnchor(ARPlaneAnchor arPlaneAnchor)
	{
		if (planeAnchorMap.ContainsKey(arPlaneAnchor.identifier)) 
		{
			ARPlaneAnchorGameObject arpag = planeAnchorMap[arPlaneAnchor.identifier];
			arpag.planeAnchor = arPlaneAnchor;

			if(arpag.gameObject)
			{
				UnityARUtility.UpdatePlaneWithAnchorTransform(arpag.gameObject, arPlaneAnchor);
			}
			
			planeAnchorMap[arPlaneAnchor.identifier] = arpag;
			trackedPlanesTimestamp = GetLastFrameTimestamp();
		}
	}

	// invoked by AnchorRemoved-event
	public void RemovePlaneAnchor(ARPlaneAnchor arPlaneAnchor)
	{
		if (planeAnchorMap.ContainsKey(arPlaneAnchor.identifier)) 
		{
			ARPlaneAnchorGameObject arpag = planeAnchorMap [arPlaneAnchor.identifier];

			if(arpag.gameObject)
			{
				GameObject.Destroy(arpag.gameObject);
			}

			planeAnchorMap.Remove(arPlaneAnchor.identifier);
			trackedPlanesTimestamp = GetLastFrameTimestamp();
		}
	}

	// returns the timestamp in seconds
	private double GetCurrentTimestamp()
	{
		double dTimestamp = System.DateTime.Now.Ticks;
		dTimestamp /= 10000000.0;

		return dTimestamp;
	}

//	void Update()
//	{
//		if(!isInitialized)
//			return;
//
//		// ....
//	}


}
