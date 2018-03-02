#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && (!UNITY_ANDROID && !UNITY_IOS && !UNITY_WSA)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta;
using Meta.Reconstruction;

public class Meta2Inteface : ARBaseInterface, ARPlatformInterface 
{
	[Tooltip("Reference to the Meta-Camera-Rig prefab.")]
	public GameObject metaCameraRigPrefab;

	[Tooltip("Reference to the Meta-Hands prefab.")]
	public GameObject metaHandsPrefab;

	[Tooltip("Time used for environment scanning after the start.")]
	public float environmentScanningTime = 20f;

	[Tooltip("Whether to keep the original Meta-hands functionality in vicinity only, or not.")]
	public bool keepMetaHandsFunctionality = false;


	// Whether the interface is enabled by MultiARManager
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
	private MultiARInterop.CameraTrackingState cameraTrackingState = MultiARInterop.CameraTrackingState.NotInitialized;

	// current light intensity
	protected float currentLightIntensity = 1f;

	// tracked planes timestamp
	private double trackedSurfacesTimestamp = 0.0;

	// surface renderer
	private MetaReconstruction metaReconstruction;
	private Transform surfaceRootTransform;
	private int surfacesCheckSum = 0;
	private List<int> surfaceCheckSum = new List<int>();

	// input action and screen position
	private MultiARInterop.InputAction inputAction = MultiARInterop.InputAction.None;
	private Vector3 startMousePos = Vector3.zero;
	private Vector3 inputNavCoordinates = Vector3.zero;
	private double inputTimestamp = 0.0, startTimestamp = 0.0;

	// meta object references
	private GameObject metaCameraRigObj;
	private GameObject metaHandsObj;

	private SlamLocalizer slamLocalizer;
	private HandsProvider handsProvider;

	private Meta.HandInput.Hand handLeft, handRight;
	private Meta.HandInput.CenterHandFeature palmLeft, palmRight;
	private bool handLeftGrabbing = false, handRightGrabbing = false;
	private bool handLeftClicked = false, handRightClicked = false;
	private float handLeftTime = 0f, handRightTime = 0f;


	/// <summary>
	/// Gets the AR platform supported by the interface.
	/// </summary>
	/// <returns>The AR platform.</returns>
	public MultiARInterop.ARPlatform GetARPlatform()
	{
		return MultiARInterop.ARPlatform.Meta2;
	}

	/// <summary>
	/// Determines whether the platform is available or not.
	/// </summary>
	/// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
	public bool IsPlatformAvailable()
	{
//#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && (!UNITY_ANDROID && !UNITY_IOS && !UNITY_WSA)
		return true;
//#else
//		return false;
//#endif
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
		return cameraTrackingState;
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
		return trackedSurfacesTimestamp;
	}

	/// <summary>
	/// Gets the count of currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces count.</returns>
	public int GetTrackedSurfacesCount()
	{
		return surfaceRootTransform ? surfaceRootTransform.childCount : 0;
	}

	/// <summary>
	/// Gets the currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces.</returns>
	public MultiARInterop.TrackedSurface[] GetTrackedSurfaces(bool bGetPoints)
	{
		MultiARInterop.TrackedSurface[] trackedPlanes = new MultiARInterop.TrackedSurface[0];

		if(surfaceRootTransform)
		{
			int numSurfaces = surfaceRootTransform.childCount;
			trackedPlanes = new MultiARInterop.TrackedSurface[numSurfaces];

			for(int i = 0; i < numSurfaces; i++)
			{
				Transform surfaceTransform = surfaceRootTransform.GetChild(i);
				trackedPlanes[i] = new MultiARInterop.TrackedSurface();

				trackedPlanes[i].position = surfaceTransform.position;
				trackedPlanes[i].rotation = surfaceTransform.rotation;

				if(bGetPoints)
				{
					MeshFilter meshFilter = surfaceTransform.GetComponent<MeshFilter>();
					Mesh mesh = meshFilter ? meshFilter.mesh : null;

					trackedPlanes[i].bounds = mesh ? mesh.bounds.size : Vector3.zero; // todo
					trackedPlanes[i].points = mesh ? mesh.vertices : null;
					trackedPlanes[i].triangles = mesh ? mesh.triangles : null;
				}
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
		return new Vector2(Screen.width / 2f, Screen.height / 2f);
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
		if(!isInitialized || !mainCamera)
			return false;

		// ray-cast
		Transform camTransform = mainCamera.transform;
		Ray camRay = new Ray(camTransform.position, camTransform.forward);

		hit.rayPos = camRay.origin;
		hit.rayDir = camRay.direction;

		RaycastHit rayHit;
		if(Physics.Raycast(camRay, out rayHit, MultiARInterop.MAX_RAYCAST_DIST, Physics.DefaultRaycastLayers))
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
	/// <param name="hit">Array of hit data.</param>
	public bool RaycastAllToScene(bool fromInputPos, out MultiARInterop.TrackableHit[] hits)
	{
		hits = new MultiARInterop.TrackableHit[0];
		if(!isInitialized || !mainCamera)
			return false;

		// ray-cast
		Transform camTransform = mainCamera.transform;
		Ray camRay = new Ray(camTransform.position, camTransform.forward);

		RaycastHit[] rayHits = Physics.RaycastAll(camRay, MultiARInterop.MAX_RAYCAST_DIST, Physics.DefaultRaycastLayers);
		hits = new MultiARInterop.TrackableHit[rayHits.Length];

		for(int i = 0; i < rayHits.Length; i++)
		{
			RaycastHit rayHit = rayHits[i];
			hits[i] = new MultiARInterop.TrackableHit();

			hits[i].rayPos = camRay.origin;
			hits[i].rayDir = camRay.direction;

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
		if(!isInitialized || !mainCamera)
			return false;

		// ray-cast
		Transform camTransform = mainCamera.transform;
		Ray camRay = new Ray(camTransform.position, camTransform.forward);

		hit.rayPos = camRay.origin;
		hit.rayDir = camRay.direction;

		int surfaceLayer = MultiARInterop.GetSurfaceLayer();  // LayerMask.NameToLayer("SpatialSurface");
		//Debug.Log("SpatialSurfaceLayer: " + surfaceLayer);
		int layerMask = 1 << surfaceLayer;

		RaycastHit[] rayHits = Physics.RaycastAll(camRay, MultiARInterop.MAX_RAYCAST_DIST, layerMask);

		for(int i = 0; i < rayHits.Length; i++)
		{
			RaycastHit rayHit = rayHits[i];

			// check for child of SpatialMappingCollider
			//if(rayHit.transform.GetComponentInParent<SpatialMappingCollider>() != null)
			if(rayHit.collider != null)
			{
				hit.point = rayHit.point;
				hit.normal = rayHit.normal;
				hit.distance = rayHit.distance;
				hit.rotation = Quaternion.FromToRotation(Vector3.up, rayHit.normal);

				hit.psObject = rayHit;
				//Debug.Log(string.Format("Hit {0} at position {1}.", rayHit.collider.gameObject, rayHit.point));

				return true;
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
		sAnchorId = System.Guid.NewGuid().ToString();
		anchorObj.name = sAnchorId;

		anchorObj.transform.position = worldPosition;
		anchorObj.transform.rotation = worldRotation;
		//anchorObj.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);  // for debug only

//		WorldAnchor anchor = anchorObj.AddComponent<WorldAnchor>();
//		anchor.OnTrackingChanged += Anchor_OnTrackingChanged;

		// don't destroy it accross scenes
		DontDestroyOnLoad(anchorObj);  

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
			// get the child game objects
			List<GameObject> anchoredObjs = arData.allAnchorsDict[anchorId];
			arData.allAnchorsDict.Remove(anchorId);

			foreach(GameObject anchoredObj in anchoredObjs)
			{
				if(anchoredObj && anchoredObj.transform.parent)
				{
					//GameObject parentObj = anchoredObj.transform.parent.gameObject;
					anchoredObj.transform.parent = null;

					if(!keepObjActive)
					{
						anchoredObj.SetActive(false);
					}
				}
			}

			// remove the anchor from the system
			GameObject anchorObj = GameObject.Find(anchorId);
			if(anchorObj)
			{
//				WorldAnchor anchor = anchorObj.GetComponent<WorldAnchor>();
//				if(anchor)
//				{
//					anchor.OnTrackingChanged -= Anchor_OnTrackingChanged;
//				}

				Destroy(anchorObj);
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

		if(!metaCameraRigPrefab)
		{
			Debug.LogError("Meta2-interface cannot start: MetaCameraRig-prefab is not set.");
			return;
		}

		// disable the main camera, if any
		Camera currentCamera = MultiARInterop.GetMainCamera();
		if(currentCamera)
		{
			currentCamera.gameObject.SetActive(false);
		}

		// create Meta-Camera-Rig object in the scene
		metaCameraRigObj = Instantiate(metaCameraRigPrefab, Vector3.zero, Quaternion.identity);
		metaCameraRigObj.name = "MetaCameraRig";
		DontDestroyOnLoad(metaCameraRigObj);

		// reference to the AR main camera
		mainCamera = MultiARInterop.GetMainCamera(); // metaCameraRigObj.GetComponentInChildren<Camera>();

//		// add camera parent
//		if(currentCamera.transform.parent == null)
//		{
//			GameObject cameraParent = new GameObject("CameraParent");
//			currentCamera.transform.SetParent(cameraParent.transform);
//		}

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
		currentLight.color = new Color32(255, 254, 244, 255);

		// add the ar-light component
		//currentLight.gameObject.AddComponent<MultiARDirectionalLight>();

		// reference to the AR directional light
		//directionalLight = currentLight;

		// don't destroy the light between scenes
		DontDestroyOnLoad(currentLight.gameObject);

		// create meta-hands object in the scene
		if (metaHandsPrefab) 
		{
			metaHandsObj = Instantiate(metaHandsPrefab, Vector3.zero, Quaternion.identity);
			metaHandsObj.name = "MetaHands";
			DontDestroyOnLoad(metaHandsObj);
		}

		// reference to hands provider
		handsProvider = metaHandsObj.GetComponent<HandsProvider>();

		handsProvider.events.OnHandEnter.AddListener(HandsProvider_HandEnter);
		handsProvider.events.OnHandExit.AddListener(HandsProvider_HandExit);

		// there is no point cloud in WinMR
		MultiARInterop.MultiARData arData = arManager.GetARData();

		// check for point cloud getter
		if(arManager.pointCloudPrefab != null)
		{
			arData.pointCloudData = new Vector3[0];
			arData.pointCloudLength = 0;
			arData.pointCloudTimestamp = 0.0;
		}

		// get slam localizer
		slamLocalizer = metaCameraRigObj.GetComponentInChildren<SlamLocalizer>();

		if (slamLocalizer) 
		{
			slamLocalizer.onSlamLocalizerResetEvent.AddListener(SlamSensorsMappingReset);
			slamLocalizer.onSlamSensorsReady.AddListener(SlamSensorsMappingReady);
			slamLocalizer.onSlamMappingInProgress.AddListener(SlamSensorsMappingInProgress);
			slamLocalizer.onSlamInitializationFailed.AddListener(SlamSensorsMappingError);
			slamLocalizer.onSlamMapLoadingFailedEvent.AddListener(SlamSensorsMappingError);
			slamLocalizer.onSlamMappingComplete.AddListener(SlamSensorsMappingFinished);
			slamLocalizer.onSlamTrackingLost.AddListener(SlamSensorsMappingLost);
			slamLocalizer.onSlamTrackingRelocalized.AddListener(SlamSensorsMappingFinished);
		}

		// create meta reconstruction
		metaReconstruction = GameObject.FindObjectOfType<MetaReconstruction>();
//		if (!metaReconstruction) 
//		{
//			GameObject objMetaReconstruction = new GameObject();
//			objMetaReconstruction.name = "MetaReconstruction";
//			objMetaReconstruction.transform.SetParent(metaCameraRigObj.transform);
//
//			metaReconstruction = objMetaReconstruction.AddComponent<MetaReconstruction>();
//			//DontDestroyOnLoad(objMetaReconstruction);
//		}
//
//		// register meta reconstruction
//		var contextBridge = FindObjectOfType<BaseMetaContextBridge>();
//		if (contextBridge != null)
//		{
//			IMetaContextInternal _metaContext = (IMetaContextInternal)contextBridge.GetContext<IMetaContext>();
//
//			_metaContext.Add<IMeshGenerator>(new MeshGenerator(true, EnvironmentConstants.MaxTriangles));
//			_metaContext.Add<IModelFileManipulator>(new OBJFileManipulator());
//		}

		if (metaReconstruction) 
		{
			DontDestroyOnLoad(metaReconstruction.gameObject);

			// start co-routine to init meta reconstruction
			StartCoroutine(InitMetaReconstruction());

			// starts co-routine to check rendered surfaces
			StartCoroutine(CheckSurfacesRoutine());
		}

//		int surfaceLayer = MultiARInterop.GetSurfaceLayer();  // LayerMask.NameToLayer("SpatialSurface");
//		Debug.Log("SpatialSurfaceLayer: " + surfaceLayer);

		// interface is initialized
		isInitialized = true;
	}

	// init meta reconstruction after starting the component
	private IEnumerator InitMetaReconstruction()
	{
		// wait for meta reconstruction start
		while (metaReconstruction.GetState() == MetaReconstruction.ReconstructionState.None) 
		{
			yield return null;
		}

		MetaReconstruction.ReconstructionState recoState = metaReconstruction.GetState();
		if (recoState == MetaReconstruction.ReconstructionState.Initializing ||
			recoState == MetaReconstruction.ReconstructionState.Scanning)
		{
			// set surface material
			switch (arManager.useOverlaySurface) 
			{
			case MultiARManager.SurfaceRenderEnum.None:
				metaReconstruction.ScanningMaterial = null;
				break;

			case MultiARManager.SurfaceRenderEnum.Occlusion:
				metaReconstruction.ScanningMaterial = arManager.surfaceOcclusionMaterial;
				break;

			case MultiARManager.SurfaceRenderEnum.Visualization:
				metaReconstruction.ScanningMaterial = arManager.surfaceVisualizationMaterial;
				break;
			}

//			// init surface reconstruction
//			metaReconstruction.InitReconstruction();

			// wait for the sensors
			while (cameraTrackingState != MultiARInterop.CameraTrackingState.NormalTracking) 
			{
				yield return null;
			}

			// perform initial reconstruction
			recoState = metaReconstruction.GetState();
			if (recoState != MetaReconstruction.ReconstructionState.Scanning) 
			{
				EnvironmentScanController recoController = GameObject.FindObjectOfType<EnvironmentScanController>();
				if (recoController) 
				{
					UnityEngine.UI.Text recoText = GetRecoInfoText(recoController);
//					if (recoText) 
//					{
//						recoText.alignment = TextAnchor.UpperCenter;
//					}

					recoController.StartScanning();
					float remRecoTime = environmentScanningTime;

					while (remRecoTime > 0f) 
					{
						if (recoText) 
						{
							recoText.text = string.Format("Look around for {0:F0} seconds", remRecoTime);
						}

						yield return new WaitForSeconds(1f);
						remRecoTime -= 1f;
					}

					recoController.FinishScanning();
				}

			}

			// wait for meta reconstruction start
			//float waitTillTime = Time.time + 10f;
			while (metaReconstruction.ReconstructionRoot == null /**&& (Time.time < waitTillTime)*/) 
			{
				yield return null;
			}

			// get surface root
			GameObject objRenderer = metaReconstruction.ReconstructionRoot;
			if (objRenderer) 
			{
				objRenderer.layer = MultiARInterop.GetSurfaceLayer();
				DontDestroyOnLoad(objRenderer);

				MultiARInterop.MultiARData arData = arManager.GetARData();
				arData.surfaceRendererRoot = objRenderer;

				surfaceRootTransform = objRenderer.transform;
				DontDestroyOnLoad(objRenderer);
			}
		}
	}


	// setups the reconstruction info text to remain for the needed time; returns reference to the text-component
	private UnityEngine.UI.Text GetRecoInfoText(EnvironmentScanController recoController)
	{
		TemporalHelpAnimationMessageController[] helpAnims = recoController.gameObject.GetComponentsInChildren<TemporalHelpAnimationMessageController>();

		foreach (TemporalHelpAnimationMessageController helpAnim in helpAnims) 
		{
			if (helpAnim.gameObject.name == "ScanUIInitMessage") 
			{
				helpAnim._showDelay = 0f;
				helpAnim._stayTime = environmentScanningTime;

				UnityEngine.UI.Text[] texts = helpAnim.GetComponentsInChildren<UnityEngine.UI.Text>();
				foreach (UnityEngine.UI.Text text in texts) 
				{
					if (text.gameObject.name != "header") 
					{
						return text;
					}
				}
				break;
			}
		}

		return null;
	}

	// slam events
	private void SlamSensorsMappingReset()
	{
		cameraTrackingState = MultiARInterop.CameraTrackingState.NotInitialized;
	}

	private void SlamSensorsMappingReady()
	{
		cameraTrackingState = MultiARInterop.CameraTrackingState.LimitedTracking;
	}

	private void SlamSensorsMappingInProgress(float progress)
	{
		cameraTrackingState = MultiARInterop.CameraTrackingState.LimitedTracking;
	}

	private void SlamSensorsMappingFinished()
	{
		cameraTrackingState = MultiARInterop.CameraTrackingState.NormalTracking;
	}

	private void SlamSensorsMappingLost()
	{
		cameraTrackingState = MultiARInterop.CameraTrackingState.LimitedTracking;
	}

	private void SlamSensorsMappingError()
	{
		cameraTrackingState = MultiARInterop.CameraTrackingState.TrackingError;
	}

	// hand events
	private void HandsProvider_HandEnter(Meta.HandInput.Hand hand)
	{
		//Debug.Log("HandEnter - " + hand.HandId + ", type: " + hand.HandType + ", grabbing: " + hand.IsGrabbing);

		if (hand.HandType == Meta.HandInput.HandType.Left) 
		{
			handLeft = hand;
			palmLeft = handLeft.Palm;

			if (palmLeft && !keepMetaHandsFunctionality) 
			{
				palmLeft.MoveStateMachine(Meta.HandInput.PalmStateCommand.HoverEnter);
			}
		}
		else if (hand.HandType == Meta.HandInput.HandType.Right) 
		{
			handRight = hand;
			palmRight = handRight.Palm;

			if (palmRight && !keepMetaHandsFunctionality) 
			{
				palmRight.MoveStateMachine(Meta.HandInput.PalmStateCommand.HoverEnter);
			}
		}
	}

	private void HandsProvider_HandExit(Meta.HandInput.Hand hand)
	{
		//Debug.Log("HandExit - " + hand.HandId + ", type: " + hand.HandType + ", grabbing: " + hand.IsGrabbing);

		if (hand.HandType == Meta.HandInput.HandType.Left) 
		{
			if (palmLeft) 
			{
				if (handLeftGrabbing) 
				{
					handLeftGrabbing = false;
					if(!keepMetaHandsFunctionality)
						palmLeft.MoveStateMachine(Meta.HandInput.PalmStateCommand.Release);
				}

				if(!keepMetaHandsFunctionality)
					palmLeft.MoveStateMachine(Meta.HandInput.PalmStateCommand.HoverLeave);
			}

			handLeft = null;
			palmLeft = null;
		} 
		else if (hand.HandType == Meta.HandInput.HandType.Right) 
		{
			if (palmRight) 
			{
				if (handRightGrabbing) 
				{
					handRightGrabbing = false;
					if(!keepMetaHandsFunctionality)
						palmRight.MoveStateMachine(Meta.HandInput.PalmStateCommand.Release);
				}

				if(!keepMetaHandsFunctionality)
					palmRight.MoveStateMachine(Meta.HandInput.PalmStateCommand.HoverLeave);
			}

			handRight = null;
			palmRight = null;
		}
	}


	void OnDestroy()
	{
		// remove event handlers
		if(isInitialized)
		{
			isInitialized = false;

			// stop the coroutines
			StopAllCoroutines();

//			// stop meta reconstruction
//			if (metaReconstruction) 
//			{
//				metaReconstruction.StopReconstruction();
//			}

			if (handsProvider) 
			{
				handsProvider.events.OnHandEnter.RemoveAllListeners();
				handsProvider.events.OnHandExit.RemoveAllListeners();
			}

			if (slamLocalizer) 
			{
				slamLocalizer.onSlamLocalizerResetEvent.RemoveAllListeners();
				slamLocalizer.onSlamSensorsReady.RemoveAllListeners();
				slamLocalizer.onSlamMappingInProgress.RemoveAllListeners();
				slamLocalizer.onSlamInitializationFailed.RemoveAllListeners();
				slamLocalizer.onSlamMapLoadingFailedEvent.RemoveAllListeners();
				slamLocalizer.onSlamMappingComplete.RemoveAllListeners();

				slamLocalizer.onSlamTrackingLost.RemoveAllListeners();
				slamLocalizer.onSlamTrackingRelocalized.RemoveAllListeners();
			}

			if(surfaceRootTransform)
			{
				//Destroy(surfaceRootTransform.gameObject);
				surfaceRootTransform = null;
			}

			// destroy meta hands
			if (metaHandsObj) 
			{
				Destroy(metaHandsObj);
			}

			// destroy the meta components
			if (metaCameraRigObj) 
			{
				Destroy(metaCameraRigObj);
			}

			if(arManager)
			{
				// get arData-reference
				MultiARInterop.MultiARData arData = arManager.GetARData();

				// destroy all world anchors
				foreach(string anchorId in arData.allAnchorsDict.Keys)
				{
					// remove the anchor from the system
					GameObject anchorObj = GameObject.Find(anchorId);
//					if(anchorObj)
//					{
//						WorldAnchor anchor = anchorObj.GetComponent<WorldAnchor>();
//
//						if(anchor)
//						{
//							anchor.OnTrackingChanged -= Anchor_OnTrackingChanged;
//							Destroy(anchor);
//						}
//					}

					Destroy(anchorObj);
				}

				// clear the list
				arData.allAnchorsDict.Clear();
			}

		}
	}


	void Update()
	{
		if(!isInitialized)
			return;

		// frame timestamp
		if(cameraTrackingState != MultiARInterop.CameraTrackingState.NotInitialized)
		{
			// count frames only when the tracking is active
			lastFrameTimestamp = GetCurrentTimestamp();
		}

		if (keepMetaHandsFunctionality) 
		{
			// keep the original meta-hands functionality
			if (palmLeft) 
			{
				palmLeft.MaintainState();
				palmLeft._wasGrabbing = palmLeft.Hand ? palmLeft.Hand.IsGrabbing : false;
			}

			if (palmRight) 
			{
				palmRight.MaintainState();
				palmRight._wasGrabbing = palmRight.Hand ? palmRight.Hand.IsGrabbing : false;
			}

		}

		// check for grab-release input
		CheckForGrabRelease();

		// check for mouse input
		CheckForMouseAction();
	}


	// checks for grab-release input action
	private void CheckForGrabRelease()
	{
		if (handLeft != null && handLeft.IsGrabbing) 
		{
			//Debug.Log ("Left hand is grabbing.");

			if (!handLeftGrabbing) 
			{
				handLeftGrabbing = true;
				handLeftClicked = false;
				handLeftTime = Time.time;

				if (palmLeft && !keepMetaHandsFunctionality) 
				{
					palmLeft.MoveStateMachine(Meta.HandInput.PalmStateCommand.Grab);
				}
			}

			if (handLeftGrabbing && !handLeftClicked && (Time.time - handLeftTime) >= 0.25f) 
			{
				handLeftClicked = true;

				inputAction = MultiARInterop.InputAction.Click;
				startMousePos = handLeft.Palm.Position;
				inputTimestamp = lastFrameTimestamp;

				//Debug.Log("Left hand click");
			}

			// check for left grab
			if (handLeftGrabbing && (Time.time - handLeftTime) > 1f) 
			{
				inputAction = MultiARInterop.InputAction.Grip;

				inputNavCoordinates = (handLeft.Palm.Position - startMousePos) * 2f;
				inputTimestamp = lastFrameTimestamp;

				//Debug.Log("Left hand grip, nav: " + inputNavCoordinates);
			}
		}

		if (handLeft == null || !handLeft.IsGrabbing) 
		{
			if (handLeftGrabbing) 
			{
				handLeftGrabbing = false;

				inputAction = MultiARInterop.InputAction.Release;
				inputTimestamp = lastFrameTimestamp;

				if (palmLeft && !keepMetaHandsFunctionality) 
				{
					palmLeft.MoveStateMachine(Meta.HandInput.PalmStateCommand.Release);
				}

				//Debug.Log("Left hand release");
			}
		}

		if (handRight != null && handRight.IsGrabbing) 
		{
			//Debug.Log ("Right hand is grabbing.");

			if (!handRightGrabbing) 
			{
				handRightGrabbing = true;
				handRightClicked = false;
				handRightTime = Time.time;

				if (palmRight && !keepMetaHandsFunctionality) 
				{
					palmRight.MoveStateMachine(Meta.HandInput.PalmStateCommand.Grab);
				}
			}

			if (handRightGrabbing && !handRightClicked && (Time.time - handRightTime) >= 0.25f) 
			{
				handRightClicked = true;

				inputAction = MultiARInterop.InputAction.Click;
				startMousePos = handRight.Palm.Position;
				inputTimestamp = lastFrameTimestamp;

				//Debug.Log("Right hand click");
			}

			// check for right grab
			if (handRightGrabbing && (Time.time - handRightTime) > 1f) 
			{
				inputAction = MultiARInterop.InputAction.Grip;

				inputNavCoordinates = (handRight.Palm.Position - startMousePos) * 2f;
				inputTimestamp = lastFrameTimestamp;

				//Debug.Log("Right hand grip, nav: " + inputNavCoordinates);
			}
		}

		if (handRight == null || !handRight.IsGrabbing) 
		{
			if (handRightGrabbing) 
			{
				handRightGrabbing = false;

				inputAction = MultiARInterop.InputAction.Release;
				inputTimestamp = lastFrameTimestamp;

				if (palmRight && !keepMetaHandsFunctionality) 
				{
					palmRight.MoveStateMachine(Meta.HandInput.PalmStateCommand.Release);
				}

				//Debug.Log("Right hand release");
			}
		}

	}


	// check for mouse input action (as fallback input)
	private void CheckForMouseAction()
	{
		bool bInputAction = true;

		if(Input.GetMouseButtonDown(0))
		{
			inputAction = MultiARInterop.InputAction.Click;
			startMousePos = Input.mousePosition;
			startTimestamp = lastFrameTimestamp;
		}
		else if(Input.GetMouseButton(0))
		{
			if ((lastFrameTimestamp - startTimestamp) >= 0.25) 
			{
				inputAction = MultiARInterop.InputAction.Grip;

				//Vector3 screenSize = new Vector3(Screen.width, Screen.height, 0f);
				float screenMinDim = Screen.width < Screen.height ? Screen.width : Screen.height;
				Vector3 mouseRelPos = Input.mousePosition - startMousePos;
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
			//inputPos = Input.mousePosition;
			inputTimestamp = lastFrameTimestamp;
		}
	}

	// checks for changes in rendered surfaces
	private IEnumerator CheckSurfacesRoutine()
	{
		// wait for root transform
		while (!surfaceRootTransform) 
		{
			yield return null;
		}

		// changes trackedPlanesTimestamp if surfaces differ
		while(surfaceRootTransform)
		{
			MeshFilter[] meshFilters = surfaceRootTransform.GetComponentsInChildren<MeshFilter>();

			// make surfaceCheckSum with the same amount of elements as meshFilters
			while (surfaceCheckSum.Count < meshFilters.Length) 
			{
				surfaceCheckSum.Add(0);
			}
			while (surfaceCheckSum.Count > meshFilters.Length) 
			{
				surfaceCheckSum.RemoveAt(surfaceCheckSum.Count - 1);
			}

			int checkSum = 0;
			for(int i = 0; i < meshFilters.Length; i++)
			{
				MeshFilter meshFilter = meshFilters[i];
				Mesh mesh = meshFilter ? meshFilter.mesh : null;

				if(mesh)
				{
					int csMesh = mesh.vertexCount;
					checkSum += csMesh;

					if (surfaceCheckSum[i] != csMesh) 
					{
						surfaceCheckSum[i] = csMesh;

						// set surface layer
						meshFilter.gameObject.layer = MultiARInterop.GetSurfaceLayer();

						// set surface collider
						if (arManager.surfaceCollider) 
						{
							MeshCollider meshCollider = meshFilter.gameObject.GetComponent<MeshCollider>();
							if (!meshCollider) 
							{
								meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
							}

							meshCollider.sharedMesh = null;
							meshCollider.sharedMesh = mesh;
						}
					}
				}
			}

			if(surfacesCheckSum != checkSum)
			{
				surfacesCheckSum = checkSum;
				trackedSurfacesTimestamp = GetCurrentTimestamp();
				//Debug.Log("surfacesCheckSum: " + surfacesCheckSum + ", trackedPlanesTimestamp: " + trackedPlanesTimestamp);
			}

			// todo - check layers & colliders!

			yield return new WaitForSeconds(5f);  // 5 seconds between updatess
		}
	}


	// returns the timestamp in seconds
	private double GetCurrentTimestamp()
	{
		double dTimestamp = System.DateTime.Now.Ticks;
		dTimestamp /= 10000000.0;

		return dTimestamp;
	}

}

#endif
