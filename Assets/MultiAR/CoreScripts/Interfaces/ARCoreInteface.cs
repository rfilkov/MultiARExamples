using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

// Handle InstantPreview input in the Editor
using GoogleARCore.CrossPlatform;


#if UNITY_EDITOR
using Input = GoogleARCore.InstantPreviewInput;
#endif


public class ARCoreInteface : ARBaseInterface, ARPlatformInterface 
{
	[Tooltip("Reference to the ARCore-Device prefab.")]
	public GameObject arCoreDevicePrefab;

//	[Tooltip("Reference to the TrackedPlane prefab.")]
//	public GameObject trackedPlanePrefab;

//	[Tooltip("Whether to attach the game objects to the planes, where they are anchored.")]
//	public bool attachObjectsToPlanes = false;

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
	private TrackingState cameraTrackingState = TrackingState.Stopped;

	// current light intensity
	protected float currentLightIntensity = 1f;

//	// newly detected planes
//	private List<TrackedPlane> newTrackedPlanes = new List<TrackedPlane>();

	// all detected planes
	private List<DetectedPlane> allTrackedPlanes = new List<DetectedPlane>();

	// regarding overlay surfaces
	private List<string> alSurfacesToDelete = new List<string>();

//	// colors to use for plane display
//	private Color[] planeColors = new Color[] { 
//		Color.blue, Color.cyan, Color.green, Color.grey, Color.magenta, Color.red, Color.white, Color.yellow 
//	};

	// input action and screen position
	private MultiARInterop.InputAction inputAction = MultiARInterop.InputAction.None;
	private Vector2 inputPos = Vector2.zero, startInputPos = Vector2.zero;
	private Vector3 inputNavCoordinates = Vector3.zero;
	private double inputTimestamp = 0.0, startTimestamp = 0.0;

	// is ARCore quitting flag
	private bool m_IsQuitting = false;

	// reference to the instantiated ar-core-device prefab and its renderer component
	private GameObject arCoreDeviceObj = null;
    private ARCoreBackgroundRenderer arCodeRenderer = null;
    private Material backgroundMat = null;

    // image-anchor database
    private AugmentedImageDatabase arImageDatabase = null;

	// list of tracked augmented images
	private List<AugmentedImage> alTrackedAugmentedImages = new List<AugmentedImage>();


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
	/// Determines whether the interface is in tracking state or not
	/// </summary>
	/// <returns><c>true</c> if this instance is tracking; otherwise, <c>false</c>.</returns>
	public override bool IsTracking()
	{
		return cameraTrackingState == TrackingState.Tracking;
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
		case TrackingState.Stopped:
			return MultiARInterop.CameraTrackingState.NotInitialized;
		case TrackingState.Paused:
			return MultiARInterop.CameraTrackingState.LimitedTracking;
		case TrackingState.Tracking:
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
	/// Gets the tracked surfaces timestamp.
	/// </summary>
	/// <returns>The tracked surfaces timestamp.</returns>
	public double GetTrackedSurfacesTimestamp()
	{
		return lastFrameTimestamp;
	}

	/// <summary>
	/// Gets the count of currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces count.</returns>
	public int GetTrackedSurfacesCount()
	{
		return allTrackedPlanes.Count;
	}

	/// <summary>
	/// Gets the currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces.</returns>
	public MultiARInterop.TrackedSurface[] GetTrackedSurfaces(bool bGetPoints)
	{
		MultiARInterop.TrackedSurface[] trackedPlanes = new MultiARInterop.TrackedSurface[allTrackedPlanes.Count];

		for(int i = 0; i < allTrackedPlanes.Count; i++)
		{
			DetectedPlane surface = allTrackedPlanes[i];
			trackedPlanes[i] = new MultiARInterop.TrackedSurface();

			trackedPlanes[i].position = surface.CenterPose.position;
			trackedPlanes[i].rotation = surface.CenterPose.rotation;
			trackedPlanes[i].bounds = new Vector3(surface.ExtentX, 0f, surface.ExtentZ);

			if(bGetPoints)
			{
				List<Vector3> alPoints = new List<Vector3>();
				surface.GetBoundaryPolygon(alPoints);

				int vertexCount = alPoints.Count;
				Quaternion invRot = Quaternion.Inverse(surface.CenterPose.rotation);
				Vector3 centerPos = surface.CenterPose.position;

				for (int v = vertexCount - 1; v >= 0; v--) 
				{
					alPoints[v] -= centerPos;
					alPoints[v] = invRot * alPoints[v];

					if (Mathf.Abs(alPoints[v].y) > 0.1f) 
					{
						alPoints.RemoveAt(v);
					}
				}

				// get mesh indices
				List<int> meshIndices = MultiARInterop.GetMeshIndices(vertexCount);

				trackedPlanes[i].points = alPoints.ToArray();
				trackedPlanes[i].triangles = meshIndices.ToArray();
			}
		}

		return trackedPlanes;
	}

	/// <summary>
	/// Determines whether input action is available.for processing
	/// </summary>
	/// <returns><c>true</c> input action is available; otherwise, <c>false</c>.</returns>
	public bool IsInputAvailable(bool inclRelease)
	{
		if (inputAction != MultiARInterop.InputAction.None) 
		{
			return !inclRelease ? inputAction != MultiARInterop.InputAction.Release : true;
		}

		return false;
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
	/// Gets the input normalized navigation coordinates.
	/// </summary>
	/// <returns>The input nav coordinates.</returns>
	public Vector3 GetInputNavCoordinates()
	{
		return inputNavCoordinates;
	}

	/// <summary>
	/// Gets the current or default input position.
	/// </summary>
	/// <returns>The input position.</returns>
	/// <param name="defaultPos">If set to <c>true</c> returns the by-default position.</param>
	public Vector2 GetInputScreenPos(bool defaultPos)
	{
		return !defaultPos ? inputPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
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
		//inputTimestamp = lastFrameTimestamp;
	}

	/// <summary>
	/// Raycasts from screen point or camera to the scene colliders.
	/// </summary>
	/// <returns><c>true</c>, if an object was hit, <c>false</c> otherwise.</returns>
	/// <param name="fromInputPos">Whether to use the last input position for the raycast, or not.</param>
	/// <param name="hit">Hit data.</param>
	public bool RaycastToScene(bool fromInputPos, out MultiARInterop.TrackableHit hit)
	{
		hit = new MultiARInterop.TrackableHit();
		if(!isInitialized)
			return false;

		// ray-cast
		Vector2 screenPos = fromInputPos ? inputPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
		Ray screenRay = mainCamera.ScreenPointToRay(screenPos);

		hit.rayPos = screenRay.origin;
		hit.rayDir = screenRay.direction;

		RaycastHit rayHit;
		if(Physics.Raycast(screenRay, out rayHit, MultiARInterop.MAX_RAYCAST_DIST, Physics.DefaultRaycastLayers))
		{
			hit.point = rayHit.point;
			hit.normal = rayHit.normal;
			hit.distance = rayHit.distance;
			hit.rotation = Quaternion.FromToRotation(Vector3.up, rayHit.normal);

			hit.psObject = rayHit;

			return true;
		}

		return false;
	}

	/// <summary>
	/// Raycasts from screen point or camera to the scene colliders, and returns all hits.
	/// </summary>
	/// <returns><c>true</c>, if an object was hit, <c>false</c> otherwise.</returns>
	/// <param name="fromInputPos">Whether to use the last input position for the raycast, or not.</param>
	/// <param name="hits">Array of hit data.</param>
	public bool RaycastAllToScene(bool fromInputPos, out MultiARInterop.TrackableHit[] hits)
	{
		hits = new MultiARInterop.TrackableHit[0];
		if(!isInitialized)
			return false;

		// ray-cast
		Vector2 screenPos = fromInputPos ? inputPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
		Ray screenRay = mainCamera.ScreenPointToRay(screenPos);

		RaycastHit[] rayHits = Physics.RaycastAll(screenRay, MultiARInterop.MAX_RAYCAST_DIST, Physics.DefaultRaycastLayers);
		hits = new MultiARInterop.TrackableHit[rayHits.Length];

		for(int i = 0; i < rayHits.Length; i++)
		{
			RaycastHit rayHit = rayHits[i];
			hits[i] = new MultiARInterop.TrackableHit();

			hits[i].rayPos = screenRay.origin;
			hits[i].rayDir = screenRay.direction;

			hits[i].point = rayHit.point;
			hits[i].normal = rayHit.normal;
			hits[i].distance = rayHit.distance;
			hits[i].rotation = Quaternion.FromToRotation(Vector3.up, rayHit.normal);

			hits[i].psObject = rayHit;
		}

		return (hits.Length > 0);
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
		if(!isInitialized || (cameraTrackingState == TrackingState.Stopped))
			return false;
		
		TrackableHit intHit;
		TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinBounds | TrackableHitFlags.PlaneWithinPolygon | 
			TrackableHitFlags.FeaturePointWithSurfaceNormal;

		if(arManager && !arManager.hitTrackedSurfacesOnly)
		{
			raycastFilter |= TrackableHitFlags.PlaneWithinInfinity;
			raycastFilter |= TrackableHitFlags.FeaturePoint;
			raycastFilter |= TrackableHitFlags.FeaturePoint;
		}

		Vector2 screenPos = fromInputPos ? inputPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
		Ray screenRay = mainCamera.ScreenPointToRay(screenPos);

		hit.rayPos = screenRay.origin;
		hit.rayDir = screenRay.direction;

		if (Frame.Raycast(screenPos.x, screenPos.y, raycastFilter, out intHit))
		{
			hit.point = intHit.Pose.position;
			hit.normal = intHit.Pose.rotation * Vector3.up;
			hit.distance = intHit.Distance;
			hit.rotation = intHit.Pose.rotation;

			hit.psObject = intHit;

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
		string anchorId = string.Empty;

		if(hit.psObject != null && hit.psObject is TrackableHit)
		{
			// valid anchor - attach the tracked plane
			TrackableHit intHit = (TrackableHit)hit.psObject;
			Anchor anchor = intHit.Trackable.CreateAnchor(intHit.Pose);
			if (anchor == null)
				return string.Empty;
			
			anchorId = anchor.m_NativeHandle.ToString();
			DontDestroyOnLoad(anchor.gameObject);  // don't destroy it accross scenes

			if(gameObj)
			{
				gameObj.transform.SetParent(anchor.transform, true);
				gameObj.transform.localPosition = Vector3.zero;
				gameObj.transform.localRotation = Quaternion.identity;
			}

			MultiARInterop.MultiARData arData = arManager.GetARData();
			arData.allAnchorsDict[anchorId] = new List<GameObject>();

			if(gameObj)
			{
				arData.allAnchorsDict[anchorId].Add(gameObj);
			}
		}
		else
		{
			anchorId = AnchorGameObjectToWorld(gameObj, hit.point, hit.rotation);
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
		if(!isInitialized || (cameraTrackingState == TrackingState.Stopped))
			return string.Empty;

		if(arManager)
		{
			Pose pose = new Pose();
			pose.position = worldPosition;
			pose.rotation = worldRotation;

			Anchor anchor = Session.CreateAnchor(pose);
			if (anchor == null)
				return string.Empty;

			string anchorId = anchor.m_NativeHandle.ToString();
			DontDestroyOnLoad(anchor.gameObject);  // don't destroy it accross scenes

			if(gameObj)
			{
				gameObj.transform.SetParent(anchor.transform, true);
				gameObj.transform.localPosition = Vector3.zero;
				gameObj.transform.localRotation = Quaternion.identity;
			}

			MultiARInterop.MultiARData arData = arManager.GetARData();
			arData.allAnchorsDict[anchorId] = new List<GameObject>();

			if(gameObj)
			{
				arData.allAnchorsDict[anchorId].Add(gameObj);
			}

			return anchorId;
		}

		return string.Empty;
	}

	/// <summary>
	/// Anchors the game object to anchor object.
	/// </summary>
	/// <returns><c>true</c>, if game object was anchored, <c>false</c> otherwise.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="anchorObj">Anchor object.</param>
	public override bool AnchorGameObject(GameObject gameObj, GameObject anchorObj)
	{
		if(!isInitialized || anchorObj == null)
			return false;

		Anchor anchor = anchorObj.GetComponent<Anchor>();
		if(anchor == null)
			return false;
		
		if(arManager)
		{
			string anchorId = anchor.m_NativeHandle.ToString();
			DontDestroyOnLoad(anchorObj);  // don't destroy it accross scenes

			if(gameObj)
			{
				gameObj.transform.SetParent(anchorObj.transform, true);
				gameObj.transform.localPosition = Vector3.zero;
				gameObj.transform.localRotation = Quaternion.identity;
			}

			MultiARInterop.MultiARData arData = arManager.GetARData();
			arData.allAnchorsDict[anchorId] = new List<GameObject>();

			if(gameObj)
			{
				arData.allAnchorsDict[anchorId].Add(gameObj);
			}

			return true;
		}

		return false;
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

					Destroy(parentObj);  // todo: check if thus works in Preview2
				}
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// Pauses the AR session.
	/// </summary>
	/// <returns><c>true</c>, if session was paused, <c>false</c> if pausing AR session is not supported.</returns>
	public override bool PauseSession()
	{
		if (arCoreDeviceObj) 
		{
			ARCoreSession arCoreSession = arCoreDeviceObj.GetComponent<ARCoreSession>();

			if (arCoreSession) 
			{
				arCoreSession.enabled = false;
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// Resumes the AR session, if paused.
	/// </summary>
	public override void ResumeSession()
	{
		if (arCoreDeviceObj) 
		{
			ARCoreSession arCoreSession = arCoreDeviceObj.GetComponent<ARCoreSession>();

			if (arCoreSession) 
			{
				arCoreSession.enabled = true;
			}
		}
	}


	/// <summary>
	/// Saves the world anchor.
	/// </summary>
	/// <param name="gameObj">Anchored game object.</param>
	/// <param name="anchorSaved">Delegate invoked after the anchor gets saved.</param>
	public override void SaveWorldAnchor(GameObject gameObj, AnchorSavedDelegate anchorSaved)
	{
		Anchor anchor = gameObj != null ? gameObj.GetComponent<Anchor>() : null;
		if(anchor == null)
			anchor = gameObj != null ? gameObj.GetComponentInParent<Anchor>() : null;

		if (anchor == null) 
		{
			//Debug.Log("Anchor not found on object or parent");

			if (anchorSaved != null)
				anchorSaved(string.Empty, "AnchorNotFound");
			return;
		}

		//Debug.Log("Saving cloud anchor...");

		XPSession.CreateCloudAnchor(anchor).ThenAction(result =>
			{
				//Debug.Log("Saving cloud anchor complete. Result: " + result.Response + ", cloudId: " + (result.Anchor != null ? result.Anchor.CloudId : null));

				if (anchorSaved != null)
				{
					anchorSaved(result.Response == CloudServiceResponse.Success ? result.Anchor.CloudId : string.Empty,
						result.Response == CloudServiceResponse.Success ? string.Empty : result.Response.ToString());
				}
			});
	}

	/// <summary>
	/// Restores the world anchor.
	/// </summary>
	/// <param name="anchorId">Anchor identifier.</param>
	/// <param name="anchorRestored">Delegate invoked after the anchor gets restored.</param>
	public override void RestoreWorldAnchor(string anchorId, AnchorRestoredDelegate anchorRestored)
	{
		if (string.IsNullOrEmpty(anchorId)) 
		{
			if (anchorRestored != null)
				anchorRestored(null, "InvalidAnchorId");
			return;
		}

		XPSession.ResolveCloudAnchor(anchorId).ThenAction(result =>
			{
				if (anchorRestored != null)
				{
					anchorRestored(result.Response == CloudServiceResponse.Success ? result.Anchor.gameObject : null,
						result.Response == CloudServiceResponse.Success ? string.Empty : result.Response.ToString());
				}
			});
	}


	/// <summary>
	/// Inits the image anchors tracking.
	/// </summary>
	/// <param name="imageManager">Anchor image manager.</param>
	public override void InitImageAnchorsTracking(AnchorImageManager imageManager)
	{
		arImageDatabase = Resources.Load<AugmentedImageDatabase>("ArCoreImageDatabase");
	}


    /// <summary>
    /// Gets the background (reality) texture
    /// </summary>
    /// <param name="arData">AR data</param>
    /// <returns>The background texture, or null</returns>
    public override Texture GetBackgroundTex(MultiARInterop.MultiARData arData)
    {
        if(arData != null)
        {
            RenderTexture backTex = GetBackgroundTexureRef(arData);

            if(backTex != null && backgroundMat != null && 
                Session.Status.IsValid() && arData.backTexTime != lastFrameTimestamp)
            {
                arData.backTexTime = lastFrameTimestamp;
                Graphics.Blit(null, backTex, backgroundMat);
            }

            return backTex;
        }

        return null;
    }


    // -- // -- // -- // -- // -- // -- // -- // -- // -- // -- //

    public void Start()
	{
		if(!isInterfaceEnabled)
			return;
		
		if(!arCoreDevicePrefab)
		{
			Debug.LogError("ARCore-interface cannot start: ArCoreDevice-prefab is not set.");
			return;
		}

#if UNITY_EDITOR
        // initializes ar-core
        InitArCore();
#else
        Session.CheckApkAvailability().ThenAction(result =>
        {
            Debug.Log("ApkAvailabilityStatus: " + result.ToString());

            if (result == ApkAvailabilityStatus.SupportedInstalled)
            {
                // initializes ar-core
                InitArCore();
            }
            else if (result == ApkAvailabilityStatus.SupportedNotInstalled || result == ApkAvailabilityStatus.SupportedApkTooOld)
            {
                Session.RequestApkInstallation(true).ThenAction(installResult =>
                {
                    Debug.Log("ApkInstallationStatus: " + installResult.ToString());

                    if (installResult == ApkInstallationStatus.Success)
                    {
                        // initializes ar-core
                        InitArCore();
                    }
                    else
                    {
                        Debug.LogError("Sorry. AR-Core could not be installed on this device. Error code: " + installResult.ToString());
                    }
                });
            }
            else
            {
                Debug.LogError("Sorry. AR-Core cannot run on this device. Error code: " + result.ToString());
            }
        });
#endif
    }

    // initializes the AR-Core components
    private void InitArCore()
    {
        //Debug.Log("InitArCore started.");

        // disable the main camera, if any
        Camera currentCamera = MultiARInterop.GetMainCamera();
        if (currentCamera)
        {
            currentCamera.gameObject.SetActive(false);
        }

        // create ARCore-Device in the scene
        arCoreDeviceObj = Instantiate(arCoreDevicePrefab, Vector3.zero, Quaternion.identity);
        arCoreDeviceObj.name = "ARCore Device";
        DontDestroyOnLoad(arCoreDeviceObj);

        // get background material
        arCodeRenderer = FindObjectOfType<ARCoreBackgroundRenderer>();
        if (arCodeRenderer)
        {
            backgroundMat = arCodeRenderer.BackgroundMaterial;
        }

        // update the session config, if needed
        ARCoreSession arSession = arCoreDeviceObj.GetComponent<ARCoreSession>();
        if (arSession != null && arSession.SessionConfig != null && arImageDatabase != null)
        {
            arSession.SessionConfig.AugmentedImageDatabase = arImageDatabase;
        }

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
        if (!currentLight)
        {
            GameObject currentLightObj = new GameObject("Directional light");

            currentLight = currentLightObj.AddComponent<Light>();
            currentLight.type = LightType.Directional;
        }

        // reset light position & rotation
        currentLight.transform.position = Vector3.zero;
        currentLight.transform.rotation = Quaternion.Euler(40f, 40f, 0f);
        DontDestroyOnLoad(currentLight.gameObject);

        // set light parameters
        //currentLight.lightmapBakeType = LightmapBakeType.Mixed;
        currentLight.color = new Color32(255, 254, 244, 255);

        // add the ar-light component
        currentLight.gameObject.AddComponent<MultiARDirectionalLight>();

        // get ar-data
        MultiARInterop.MultiARData arData = arManager ? arManager.GetARData() : null;

        if (arManager && arManager.usePointCloudData)
        {
            arData.pointCloudData = new Vector3[MultiARInterop.MAX_POINT_COUNT];
            arData.pointCloudLength = 0;
            arData.pointCloudTimestamp = 0.0;
        }

        // create surface renderer
        if (arManager && arData != null)
        {
            arData.surfaceRendererRoot = new GameObject();
            arData.surfaceRendererRoot.name = "SurfaceRenderer";
            DontDestroyOnLoad(arData.surfaceRendererRoot);
        }

        // interface is initialized
        isInitialized = true;

        //Debug.Log("InitArCore finished.");
    }

    void OnDestroy()
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

		// estimate the tracking state
		SessionStatus status = Session.Status;
		if (status.IsError () || status.IsNotInitialized()) 
		{
			cameraTrackingState = TrackingState.Stopped;
			return;
		} 
		else if (status == SessionStatus.Tracking) 
		{
			cameraTrackingState = TrackingState.Tracking;
		} 
		else 
		{
			cameraTrackingState = TrackingState.Paused;
		}
			
		// get frame timestamp and light intensity
		lastFrameTimestamp = GetCurrentTimestamp();

		if (Frame.LightEstimate.State == LightEstimateState.Valid)
		{
			// Normalize pixel intensity by middle gray in gamma space.
			const float middleGray = 0.466f;
			currentLightIntensity = Frame.LightEstimate.PixelIntensity / middleGray;
		}

		// get point cloud, if needed
		MultiARInterop.MultiARData arData = arManager.GetARData();

		if(arManager.usePointCloudData)
		{
			if (Frame.PointCloud.PointCount > 0 && Frame.PointCloud.IsUpdatedThisFrame)
			{
				// Copy the point cloud points
				for (int i = 0; i < Frame.PointCloud.PointCount; i++)
				{
                    PointCloudPoint point = Frame.PointCloud.GetPointAsStruct(i);
                    arData.pointCloudData[i] = new Vector3(point.Position.x, point.Position.y, point.Position.z);
				}

				arData.pointCloudLength = Frame.PointCloud.PointCount;
				arData.pointCloudTimestamp = lastFrameTimestamp;
			}
		}

//		// display the tracked planes if needed
//		if(arManager.displayTrackedSurfaces && trackedPlanePrefab)
//		{
//			// get the new planes
//			Frame.GetNewPlanes(ref newTrackedPlanes);
//
//			// Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
//			for (int i = 0; i < newTrackedPlanes.Count; i++)
//			{
//				// Instantiate a plane visualization prefab and set it to track the new plane.
//				GameObject planeObject = Instantiate(trackedPlanePrefab, Vector3.zero, Quaternion.identity);
//				planeObject.GetComponent<GoogleARCore.HelloAR.TrackedPlaneVisualizer>().SetTrackedPlane(newTrackedPlanes[i]);
//
//				// Apply a random color and grid rotation.
//				planeObject.GetComponent<Renderer>().material.SetColor("_GridColor", planeColors[Random.Range(0, planeColors.Length - 1)]);
//				planeObject.GetComponent<Renderer>().material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
//			}
//		}

		// get all tracked planes
		Session.GetTrackables<DetectedPlane>(allTrackedPlanes, TrackableQueryFilter.All);

		// create overlay surfaces as needed
		if(arManager.useOverlaySurface != MultiARManager.SurfaceRenderEnum.None)
		{
			alSurfacesToDelete.Clear();
			alSurfacesToDelete.AddRange(arData.dictOverlaySurfaces.Keys);

			// estimate the material
			Material surfaceMat = arManager.GetSurfaceMaterial();
			int surfaceLayer = MultiARInterop.GetSurfaceLayer();

			for(int i = 0; i < allTrackedPlanes.Count; i++)
			{
				string surfId = allTrackedPlanes[i].m_TrackableNativeHandle.ToString();

				if(!arData.dictOverlaySurfaces.ContainsKey(surfId))
				{
					GameObject overlaySurfaceObj = new GameObject();
					overlaySurfaceObj.name = "surface-" + surfId;

					overlaySurfaceObj.layer = surfaceLayer;
					overlaySurfaceObj.transform.SetParent(arData.surfaceRendererRoot ? arData.surfaceRendererRoot.transform : null);

//					GameObject overlayCubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
//					overlayCubeObj.name = "surface-cube-" + surfId;
//					overlayCubeObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
//					overlayCubeObj.transform.SetParent(overlaySurfaceObj.transform);

					OverlaySurfaceUpdater overlaySurface = overlaySurfaceObj.AddComponent<OverlaySurfaceUpdater>();
					overlaySurface.SetSurfaceMaterial(surfaceMat);
					overlaySurface.SetSurfaceCollider(arManager.surfaceCollider, arManager.colliderMaterial);

					arData.dictOverlaySurfaces.Add(surfId, overlaySurface);
				}

				// update the surface mesh
				bool bValidSurface = UpdateOverlaySurface(arData.dictOverlaySurfaces[surfId], allTrackedPlanes[i]);

				if(bValidSurface && alSurfacesToDelete.Contains(surfId))
				{
					alSurfacesToDelete.Remove(surfId);
				}
			}

			// delete not tracked surfaces
			foreach(string surfId in alSurfacesToDelete)
			{
				OverlaySurfaceUpdater overlaySurface = arData.dictOverlaySurfaces[surfId];
				arData.dictOverlaySurfaces.Remove(surfId);

				Destroy(overlaySurface.gameObject);
			}
		}

		// check status of the anchors
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

					if(anchor == null || anchor.TrackingState == TrackingState.Stopped)
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

		// look for image anchors, if enabled
		if (arData.imageAnchorsEnabled) 
		{
			// Get updated augmented images for this frame.
			Session.GetTrackables<AugmentedImage>(alTrackedAugmentedImages, TrackableQueryFilter.Updated);

			foreach (var image in alTrackedAugmentedImages)
			{
				string sImageName = image.Name;
				bool wasImageTracked = dictImageAnchors.ContainsKey(sImageName);

				if (!wasImageTracked && image.TrackingState == TrackingState.Tracking)
				{
					// Create an anchor to ensure that ARCore keeps tracking this augmented image.
					Anchor anchor = image.CreateAnchor(image.CenterPose);
					anchor.gameObject.name = "ImageAnchor-" + sImageName;
					DontDestroyOnLoad(anchor.gameObject);

					alImageAnchorNames.Add(sImageName);
					dictImageAnchors.Add(sImageName, anchor.gameObject);
				}
				else if (wasImageTracked && image.TrackingState == TrackingState.Stopped)
				{
					// remove the anchor
					GameObject anchorObj = dictImageAnchors[sImageName];

					alImageAnchorNames.Remove(sImageName);
					dictImageAnchors.Remove(sImageName);

					GameObject.Destroy(anchorObj);
				}
			}
		}

	}


	// returns the timestamp in seconds
	private double GetCurrentTimestamp()
	{
		double dTimestamp = System.DateTime.Now.Ticks;
		dTimestamp /= 10000000.0;

		return dTimestamp;
	}


	// Updates overlay surface mesh. Returns true on success, false if the surface needs to be deleted
	private bool UpdateOverlaySurface(OverlaySurfaceUpdater overlaySurface, DetectedPlane trackedSurface)
	{
		// check for validity
		if (overlaySurface == null || trackedSurface == null)
		{
			return false;
		}
		else if (trackedSurface.SubsumedBy != null)
		{
			return false;
		}
		else if (trackedSurface.TrackingState != TrackingState.Tracking)
		{
			overlaySurface.SetEnabled(false);
			return true;
		}

		// enable the surface
		overlaySurface.SetEnabled(true);

		// estimate mesh vertices
		List<Vector3> meshVertices = new List<Vector3>();

		// GetBoundaryPolygon returns points in clockwise order.
		trackedSurface.GetBoundaryPolygon(meshVertices);
		int verticeLength = meshVertices.Count;

		// surface position & rotation
		Vector3 surfacePos = trackedSurface.CenterPose.position;  // Vector3.zero; // 
		Quaternion surfaceRot = trackedSurface.CenterPose.rotation; // Quaternion.identity; // 

		// estimate vertices relative to the center
		Quaternion invRot = Quaternion.Inverse(surfaceRot);
		for (int v = verticeLength - 1; v >= 0; v--) 
		{
			meshVertices[v] -= surfacePos;
			meshVertices[v] = invRot * meshVertices[v];

			if (Mathf.Abs(meshVertices[v].y) > 0.1f) 
			{
				meshVertices.RemoveAt(v);
			}
		}

		// estimate mesh indices
		List<int> meshIndices = MultiARInterop.GetMeshIndices(meshVertices.Count);

		// update the surface mesh
		overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);

		return true;
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
				inputNavCoordinates = Vector3.zero;
				startInputPos = touch.position;
				startTimestamp = lastFrameTimestamp;
				break;

			case TouchPhase.Moved:
			case TouchPhase.Stationary:
				if ((lastFrameTimestamp - startTimestamp) >= 0.25) 
				{
					inputAction = MultiARInterop.InputAction.Grip;

					float screenMinDim = Screen.width < Screen.height ? Screen.width : Screen.height;
					Vector3 mouseRelPos = touch.position - startInputPos;
					inputNavCoordinates = mouseRelPos / screenMinDim;
				}
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
		else
		{
#if UNITY_EDITOR
			bool bInputAction = true;

			if(Input.GetMouseButtonDown(0))
			{
				inputAction = MultiARInterop.InputAction.Click;
				startInputPos = Input.mousePosition;
				startTimestamp = lastFrameTimestamp;
			}
			else if(Input.GetMouseButton(0))
			{
				if ((lastFrameTimestamp - startTimestamp) >= 0.25) 
				{
					inputAction = MultiARInterop.InputAction.Grip;

					//Vector3 screenSize = new Vector3(Screen.width, Screen.height, 0f);
					float screenMinDim = Screen.width < Screen.height ? Screen.width : Screen.height;
					Vector3 mouseRelPos = Input.mousePosition - (Vector3)startInputPos;
					inputNavCoordinates = mouseRelPos / screenMinDim;  // new Vector3(mouseRelPos.x / screenSize.x, mouseRelPos.y / screenSize.y, 0f);
				}
			}
			else if(Input.GetMouseButtonUp(0))
			{
				inputAction = MultiARInterop.InputAction.Release;
			}
			else
			{
				bInputAction = false;
			}

			if(bInputAction)
			{
				inputPos = Input.mousePosition;
				inputTimestamp = lastFrameTimestamp;
			}
#endif
		}
	}

	/// <summary>
	/// Quit the application if there was a connection error for the ARCore session.
	/// </summary>
	private void _QuitOnConnectionErrors()
	{
		if (m_IsQuitting)
		{
			return;
		}

		// Do not update if ARCore is not tracking.
		if (Session.Status == SessionStatus.ErrorSessionConfigurationNotSupported)
		{
			_ShowAndroidToastMessage("Invalid ARCore configuration.");
			m_IsQuitting = true;
			Invoke("_DoQuit", 0.5f);
		}
		else if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
		{
			_ShowAndroidToastMessage("Camera permission is needed to run this application.");
			m_IsQuitting = true;
			Invoke("_DoQuit", 0.5f);
		}
		else if (Session.Status.IsError())
		{
			_ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
			m_IsQuitting = true;
			Invoke("_DoQuit", 0.5f);
		}
	}


	// quit application
	private void _DoQuit()
	{
		Application.Quit();
	}


	// Show an Android toast message.
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
