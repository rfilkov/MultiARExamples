using System.Collections;
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

	// last frame timestamp
	private double lastFrameTimestamp = 0.0;

	// current tracking state
	private ARTrackingState cameraTrackingState = ARTrackingState.ARTrackingStateNotAvailable;
	private ARTrackingStateReason cameraTrackingReason = ARTrackingStateReason.ARTrackingStateReasonNone;

	// current light intensity
	protected float currentLightIntensity = 1f;
	protected float currentColorTemperature = 0f;

	// tracked planes timestamp
	private double trackedPlanesTimestamp = 0.0;

	// plane anchors
	private Dictionary<string, ARPlaneAnchorGameObject> planeAnchorDict = new Dictionary<string, ARPlaneAnchorGameObject>();

	// the overlay surfaces
	private GameObject surfaceRendererRoot = null;
	private Dictionary<string, OverlaySurfaceUpdater> dictOverlaySurfaces = new Dictionary<string, OverlaySurfaceUpdater>();

	// input action and screen position
	private MultiARInterop.InputAction inputAction = MultiARInterop.InputAction.None;
	private Vector2 inputPos = Vector2.zero, startInputPos = Vector2.zero;
	private double inputTimestamp = 0.0;


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
			switch(cameraTrackingReason)
			{
			case ARTrackingStateReason.ARTrackingStateReasonInitializing:
				return "Initializing";
			case ARTrackingStateReason.ARTrackingStateReasonExcessiveMotion:
				return "ExcessiveMotion";
			case ARTrackingStateReason.ARTrackingStateReasonInsufficientFeatures:
				return "InsufficientFeatures";
			default:
				return cameraTrackingReason.ToString();
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// Gets the tracked surfaces timestamp.
	/// </summary>
	/// <returns>The tracked surfaces timestamp.</returns>
	public double GetTrackedSurfacesTimestamp()
	{
		return trackedPlanesTimestamp;
	}

	/// <summary>
	/// Gets the count of currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces count.</returns>
	public int GetTrackedSurfacesCount()
	{
		return planeAnchorDict.Count;
	}

	/// <summary>
	/// Gets the currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces.</returns>
	public MultiARInterop.TrackedSurface[] GetTrackedSurfaces(bool bGetPoints)
	{
		MultiARInterop.TrackedSurface[] trackedPlanes = new MultiARInterop.TrackedSurface[planeAnchorDict.Count];

		// get current planes list
		List<ARPlaneAnchorGameObject> listPlaneObjs = new List<ARPlaneAnchorGameObject>(planeAnchorDict.Values);
		int i = 0;

		foreach (ARPlaneAnchorGameObject arpag in listPlaneObjs) 
		{
			ARPlaneAnchor planeAnchor = arpag.planeAnchor;
			trackedPlanes[i] = new MultiARInterop.TrackedSurface();

			trackedPlanes[i].position = UnityARMatrixOps.GetPosition(planeAnchor.transform);
			trackedPlanes[i].rotation = UnityARMatrixOps.GetRotation(planeAnchor.transform);
			trackedPlanes[i].bounds = planeAnchor.extent;

			if(bGetPoints)
			{
				Vector3[] meshVertices = new Vector3[4];

				Vector3 planeHalf = trackedPlanes[i].bounds * 0.5f;
				meshVertices[0] = new Vector3(-planeHalf.x, planeHalf.y, planeHalf.z);
				meshVertices[1] = new Vector3(planeHalf.x, planeHalf.y, planeHalf.z);
				meshVertices[2] = new Vector3(planeHalf.x, planeHalf.y, -planeHalf.z);
				meshVertices[3] = new Vector3(-planeHalf.x, planeHalf.y, -planeHalf.z);
				trackedPlanes[i].points = meshVertices;

				// get mesh indices
				List<int> meshIndices = MultiARInterop.GetMeshIndices(meshVertices.Length);

				trackedPlanes[i].triangles = meshIndices.ToArray();
			}

			i++;
		}

		// clear temporary lists
		listPlaneObjs.Clear();

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
	/// Gets the current or default input position.
	/// </summary>
	/// <returns>The input position.</returns>
	/// <param name="defaultPos">If set to <c>true</c> returns the by-default position.</param>
	public Vector2 GetInputPos(bool defaultPos)
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
		if(!isInitialized)
			return false;

		Vector2 screenPos = fromInputPos ? inputPos : new Vector2(Screen.width / 2f, Screen.height / 2f);
		Ray screenRay = mainCamera.ScreenPointToRay(screenPos);

		hit.rayPos = screenRay.origin;
		hit.rayDir = screenRay.direction;

		Vector3 viewPos = mainCamera.ScreenToViewportPoint(screenPos);
		ARPoint point = new ARPoint {
			x = viewPos.x,
			y = viewPos.y
		};

		// prioritize result types
		List<ARHitTestResultType> allowedResultTypes = new List<ARHitTestResultType>();
		allowedResultTypes.Add(ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent);
		allowedResultTypes.Add(ARHitTestResultType.ARHitTestResultTypeHorizontalPlane);

		if(arManager && !arManager.hitTrackedSurfacesOnly)
		{
			allowedResultTypes.Add(ARHitTestResultType.ARHitTestResultTypeExistingPlane);  // infinite planes
			allowedResultTypes.Add(ARHitTestResultType.ARHitTestResultTypeFeaturePoint);
		}

		foreach (ARHitTestResultType resultType in allowedResultTypes)
		{
			List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, resultType);

			if (hitResults.Count > 0) 
			{
				foreach (var hitResult in hitResults) 
				{
					if(hitResult.isValid)
					{
						hit.point = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
						hit.normal = UnityARMatrixOps.GetRotation(hitResult.worldTransform) * Vector3.up;
						hit.distance = (float)hitResult.distance;
						//hit.anchorId = hitResult.anchorIdentifier;

						return true;
					}
				}
			}
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
		string sAnchorId = string.Empty;
		if(!isInitialized || !arManager)
			return sAnchorId;

		GameObject anchorObj = new GameObject();
		//GameObject anchorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
		anchorObj.transform.position = worldPosition;
		anchorObj.transform.rotation = worldRotation;
		anchorObj.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);  // for debug only

		UnityARUserAnchorData anchorData = UnityARSessionNativeInterface.GetARSessionNativeInterface().AddUserAnchorFromGameObject(anchorObj); 
		sAnchorId = anchorData.identifierStr;
		DontDestroyOnLoad(anchorObj);  // don't destroy it accross scenes

		if(gameObj)
		{
			gameObj.transform.SetParent(anchorObj.transform, true);
			gameObj.transform.localPosition = Vector3.zero;
		}

		MultiARInterop.MultiARData arData = arManager.GetARData();
		arData.allAnchorsDict[sAnchorId] = new List<GameObject>();

		if(gameObj)
		{
			arData.allAnchorsDict[sAnchorId].Add(gameObj);
		}

		return sAnchorId;
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
			UnityARSessionNativeInterface.GetARSessionNativeInterface().RemoveUserAnchor(anchorId);

			// get the child game objects
			List<GameObject> anchoredObjs = arData.allAnchorsDict[anchorId];
			arData.allAnchorsDict.Remove(anchorId);

			foreach(GameObject anchoredObj in anchoredObjs)
			{
				if(anchoredObj && anchoredObj.transform.parent)
				{
					GameObject parentObj = anchoredObj.transform.parent.gameObject;
					anchoredObj.transform.parent = null;

					if(!keepObjActive)
					{
						anchoredObj.SetActive(false);
					}

					Destroy(parentObj);
				}
			}

			return true;
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
		DontDestroyOnLoad(currentCamera.gameObject);

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
		DontDestroyOnLoad(currentLight.gameObject);

		// set light parameters
		//currentLight.lightmapBakeType = LightmapBakeType.Mixed;
		currentLight.color = new Color32(255, 254, 244, 255);

		// add the ar-light component
		currentLight.gameObject.AddComponent<MultiARDirectionalLight>();

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

		// create surface renderer
		surfaceRendererRoot = new GameObject();
		surfaceRendererRoot.name = "SurfaceRenderer";
		DontDestroyOnLoad(surfaceRendererRoot);

		// check for tracked plane display
		if(arManager.displayTrackedSurfaces && trackedPlanePrefab)
		{
			UnityARUtility.InitializePlanePrefab(trackedPlanePrefab);
		}

		// add needed events
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
		UnityARSessionNativeInterface.ARSessionTrackingChangedEvent += ARSessionTrackingChanged;

		UnityARSessionNativeInterface.ARAnchorAddedEvent += PlaneAnchorAdded;
		UnityARSessionNativeInterface.ARAnchorUpdatedEvent += PlaneAnchorUpdated;
		UnityARSessionNativeInterface.ARAnchorRemovedEvent += PlaneAnchorRemoved;

		UnityARSessionNativeInterface.ARUserAnchorAddedEvent += UserAnchorAdded;
		UnityARSessionNativeInterface.ARUserAnchorRemovedEvent += UserAnchorRemoved;

		// interface is initialized
		isInitialized = true;
	}

	void OnDestroy()
	{
		// remove event handlers
		if(isInitialized)
		{
			isInitialized = false;

			UnityARSessionNativeInterface.ARFrameUpdatedEvent -= ARFrameUpdated;
			UnityARSessionNativeInterface.ARSessionTrackingChangedEvent -= ARSessionTrackingChanged;

			UnityARSessionNativeInterface.ARAnchorAddedEvent -= PlaneAnchorAdded;
			UnityARSessionNativeInterface.ARAnchorUpdatedEvent -= PlaneAnchorUpdated;
			UnityARSessionNativeInterface.ARAnchorRemovedEvent -= PlaneAnchorRemoved;

			UnityARSessionNativeInterface.ARUserAnchorAddedEvent -= UserAnchorAdded;
			UnityARSessionNativeInterface.ARUserAnchorRemovedEvent -= UserAnchorRemoved;

			// destroy persistent plane objects
			List<ARPlaneAnchorGameObject> listPlaneObjs = new List<ARPlaneAnchorGameObject>(planeAnchorDict.Values);
			foreach (ARPlaneAnchorGameObject arpag in listPlaneObjs) 
			{
				if(arpag.gameObject)
				{
					GameObject.Destroy(arpag.gameObject);
				}
			}

			// clear plane anchor lists
			listPlaneObjs.Clear();
			planeAnchorDict.Clear();
		}
	}

	// invoked by FrameUpdated-event
	private void ARFrameUpdated(UnityARCamera camera)
	{
		// current timestamp
		lastFrameTimestamp = GetCurrentTimestamp();

		// current light intensity
		UnityARLightEstimate lightEstimate = camera.lightData.arLightEstimate;
		currentLightIntensity = lightEstimate.ambientIntensity / 1000f;
		currentColorTemperature = lightEstimate.ambientColorTemperature;

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
	private void ARSessionTrackingChanged(UnityARCamera camera)
	{
		cameraTrackingState = camera.trackingState;
		cameraTrackingReason = camera.trackingReason;
	}

	// invoked by AnchorAdded-event
	private void PlaneAnchorAdded(ARPlaneAnchor arPlaneAnchor)
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

		planeAnchorDict.Add(arPlaneAnchor.identifier, arpag);
		trackedPlanesTimestamp = GetLastFrameTimestamp();

		// create overlay surfaces as needed
		if(arManager.useOverlaySurface != MultiARManager.SurfaceRenderEnum.None)
		{
			// estimate the material
			Material surfaceMat = arManager.GetSurfaceMaterial();
			int surfaceLayer = MultiARInterop.GetSurfaceLayer();

			string surfId = arPlaneAnchor.identifier;
			if(!dictOverlaySurfaces.ContainsKey(surfId))
			{
				GameObject overlaySurfaceObj = new GameObject();
				overlaySurfaceObj.name = "surface-" + surfId;

				overlaySurfaceObj.layer = surfaceLayer;
				overlaySurfaceObj.transform.SetParent(surfaceRendererRoot.transform);

				GameObject overlayCubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
				overlayCubeObj.name = "surface-cube-" + surfId;
				overlayCubeObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
				overlayCubeObj.transform.SetParent(overlaySurfaceObj.transform);

				OverlaySurfaceUpdater overlaySurface = overlaySurfaceObj.AddComponent<OverlaySurfaceUpdater>();
				overlaySurface.SetSurfaceMaterial(surfaceMat);
				overlaySurface.SetSurfaceCollider(arManager.overlaySurfaceColliders);

				dictOverlaySurfaces.Add(surfId, overlaySurface);
			}

			// update the surface mesh
			UpdateOverlaySurface(dictOverlaySurfaces[surfId], arPlaneAnchor);
		}

	}

	// Updates overlay surface mesh. Returns true on success, false if the surface needs to be deleted
	private bool UpdateOverlaySurface(OverlaySurfaceUpdater overlaySurface, ARPlaneAnchor arPlaneAnchor)
	{
		// check for validity
		if (overlaySurface == null)
		{
			return false;
		}

		// estimate mesh vertices & indices
		overlaySurface.SetEnabled(true);

		List<Vector3> meshVertices = new List<Vector3>();

		// surface position & rotation
		Vector3 surfacePos = UnityARMatrixOps.GetPosition(arPlaneAnchor.transform);  // Vector3.zero; // 
		Quaternion surfaceRot = UnityARMatrixOps.GetRotation(arPlaneAnchor.transform); // Quaternion.identity; // 

		// add the center offset
		//surfacePos += arPlaneAnchor.center;
		Vector3 centerPos = surfaceRot * arPlaneAnchor.center;
		surfacePos += centerPos;

		Vector3 planeHalf = arPlaneAnchor.extent * 0.5f;
		meshVertices.Add(new Vector3(-planeHalf.x, planeHalf.y, planeHalf.z));
		meshVertices.Add(new Vector3(planeHalf.x, planeHalf.y, planeHalf.z));
		meshVertices.Add(new Vector3(planeHalf.x, planeHalf.y, -planeHalf.z));
		meshVertices.Add(new Vector3(-planeHalf.x, planeHalf.y, -planeHalf.z));

		// estimate mesh indices
		List<int> meshIndices = MultiARInterop.GetMeshIndices(meshVertices.Count);

		// update the surface mesh
		overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);

		return true;
	}

	// invoked by AnchorUpdated-event
	private void PlaneAnchorUpdated(ARPlaneAnchor arPlaneAnchor)
	{
		string surfId = arPlaneAnchor.identifier;

		// update plane anchor
		if (planeAnchorDict.ContainsKey(surfId)) 
		{
			ARPlaneAnchorGameObject arpag = planeAnchorDict[surfId];
			arpag.planeAnchor = arPlaneAnchor;

			if(arpag.gameObject)
			{
				UnityARUtility.UpdatePlaneWithAnchorTransform(arpag.gameObject, arPlaneAnchor);
			}
			
			planeAnchorDict[surfId] = arpag;
			trackedPlanesTimestamp = GetLastFrameTimestamp();
		}

		// update overlay surface
		if (dictOverlaySurfaces.ContainsKey(surfId)) 
		{
			UpdateOverlaySurface(dictOverlaySurfaces[surfId], arPlaneAnchor);
		}
	}

	// invoked by AnchorRemoved-event
	private void PlaneAnchorRemoved(ARPlaneAnchor arPlaneAnchor)
	{
		string surfId = arPlaneAnchor.identifier;

		// remove plane anchor
		if (planeAnchorDict.ContainsKey(surfId)) 
		{
			ARPlaneAnchorGameObject arpag = planeAnchorDict[surfId];

			if(arpag != null && arpag.gameObject)
			{
				GameObject.Destroy(arpag.gameObject);
			}

			planeAnchorDict.Remove(surfId);
			trackedPlanesTimestamp = GetLastFrameTimestamp();
		}

		// remove overlay surface
		if (dictOverlaySurfaces.ContainsKey(surfId)) 
		{
			OverlaySurfaceUpdater overlaySurface = dictOverlaySurfaces[surfId];
			dictOverlaySurfaces.Remove(surfId);

			Destroy(overlaySurface.gameObject);
		}
	}

	// invoked by UserAnchorAdded-event
	private void UserAnchorAdded(ARUserAnchor anchor)
	{
		Debug.Log("Anchor added: " + anchor.identifier);
	}

	// invoked by UserAnchorRemoved-event
	private void UserAnchorRemoved(ARUserAnchor anchor)
	{
		if(!arManager)
			return;
		
		MultiARInterop.MultiARData arData = arManager.GetARData();
		if (arData.allAnchorsDict.ContainsKey(anchor.identifier))
		{
			List<GameObject> anchoredObjs = arData.allAnchorsDict[anchor.identifier];
			arData.allAnchorsDict.Remove(anchor.identifier);

			foreach(GameObject anchoredObj in anchoredObjs)
			{
				if(anchoredObj && anchoredObj.transform.parent)
				{
					GameObject parentObj = anchoredObj.transform.parent.gameObject;
					anchoredObj.transform.parent = null;
					anchoredObj.SetActive(false);

					Destroy(parentObj);
				}
			}

			Debug.Log("Anchor removed: " + anchor.identifier);
		}

	}

	// returns the timestamp in seconds
	private double GetCurrentTimestamp()
	{
		double dTimestamp = System.DateTime.Now.Ticks;
		dTimestamp /= 10000000.0;

		return dTimestamp;
	}



	void Update()
	{
		if(!isInitialized)
			return;

		// check for input (touch)
		CheckForInputAction();
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
				startInputPos = touch.position;
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

}
