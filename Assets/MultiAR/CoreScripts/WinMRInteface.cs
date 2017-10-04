using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.WSA;

public class WinMRInteface : MonoBehaviour, ARPlatformInterface 
{
	[Tooltip("Material used for surface rendering. If left empty, will use the default surface material")]
	public Material surfaceRenderMaterial;

	[Tooltip("Whether the detected surfaces should have mesh colliders. This is needed for ray-casting.")]
	public bool useSurfaceColliders = true;

	[Tooltip("The layer used by the surface collider. 1 means default.")]
	private int surfaceColliderLayer = 31;

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
	private PositionalLocatorState cameraTrackingState = PositionalLocatorState.Unavailable;

	// current light intensity
	protected float currentLightIntensity = 1f;

	// tracked planes timestamp
	private double trackedPlanesTimestamp = 0.0;

	// surface renderer
	private SpatialMappingRenderer surfaceRenderer;
	private int surfacesCheckSum = 0;

	// surface collider
	private SpatialMappingCollider surfaceCollider;

	// input action and screen position
	private MultiARInterop.InputAction inputAction = MultiARInterop.InputAction.None;
	//private Vector2 inputPos = Vector2.zero;
	private double inputTimestamp = 0.0;


	/// <summary>
	/// Gets the AR platform supported by the interface.
	/// </summary>
	/// <returns>The AR platform.</returns>
	public MultiARInterop.ARPlatform GetARPlatform()
	{
		return MultiARInterop.ARPlatform.WindowsMR;
	}

	/// <summary>
	/// Determines whether the platform is available or not.
	/// </summary>
	/// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
	public bool IsPlatformAvailable()
	{
#if UNITY_EDITOR || UNITY_WSA
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
		case PositionalLocatorState.Unavailable:
			return MultiARInterop.CameraTrackingState.NotInitialized;
		case PositionalLocatorState.Inhibited:
		case PositionalLocatorState.Activating:
		case PositionalLocatorState.OrientationOnly:
			return MultiARInterop.CameraTrackingState.LimitedTracking;
		case PositionalLocatorState.Active:
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
		return trackedPlanesTimestamp;
	}

	/// <summary>
	/// Gets the count of currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes count.</returns>
	public int GetTrackedPlanesCount()
	{
		return surfaceRenderer ? surfaceRenderer.transform.childCount : 0;
	}

	/// <summary>
	/// Gets the currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes.</returns>
	public MultiARInterop.TrackedPlane[] GetTrackedPlanes()
	{
		MultiARInterop.TrackedPlane[] trackedPlanes = new MultiARInterop.TrackedPlane[0];

		if(surfaceRenderer)
		{
			int numSurfaces = surfaceRenderer.transform.childCount;
			trackedPlanes = new MultiARInterop.TrackedPlane[numSurfaces];

			for(int i = 0; i < numSurfaces; i++)
			{
				Transform surfaceTransform = surfaceRenderer.transform.GetChild(i);
				trackedPlanes[i] = new MultiARInterop.TrackedPlane();

				trackedPlanes[i].position = surfaceTransform.position;
				trackedPlanes[i].rotation = surfaceTransform.rotation;
				trackedPlanes[i].bounds = Vector2.zero; // todo
			}
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
		if(!isInitialized || !surfaceCollider)
			return false;

		// ray-cast
		RaycastHit[] rayHits = Physics.RaycastAll(mainCamera.transform.position, mainCamera.transform.forward, 20f, Physics.DefaultRaycastLayers);

		for(int i = 0; i < rayHits.Length; i++)
		{
			RaycastHit rayHit = rayHits[i];

			// check for child of SpatialMappingCollider
			if(rayHit.transform.GetComponentInParent<SpatialMappingCollider>() != null)
			{
				hit.point = rayHit.point;
				hit.distance = rayHit.distance;

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

		//GameObject anchorObj = new GameObject();
		GameObject anchorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
		sAnchorId = System.Guid.NewGuid().ToString();
		anchorObj.name = sAnchorId;

		anchorObj.transform.position = worldPosition;
		anchorObj.transform.rotation = worldRotation;
		anchorObj.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);  // for debug only

		WorldAnchor anchor = anchorObj.AddComponent<WorldAnchor>();
		anchor.OnTrackingChanged += Anchor_OnTrackingChanged;

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
				WorldAnchor anchor = anchorObj.GetComponent<WorldAnchor>();

				if(anchor)
				{
					anchor.OnTrackingChanged -= Anchor_OnTrackingChanged;
					Destroy(anchor);
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

		// modify the main camera in the scene
		Camera currentCamera = MultiARInterop.GetMainCamera();
		if(!currentCamera)
		{
			GameObject currentCameraObj = new GameObject("Main Camera");
			currentCameraObj.tag = "MainCamera";

			currentCamera = currentCameraObj.AddComponent<Camera>();
		}

		// reset camera position & rotation
		//currentCamera.transform.position = Vector3.zero;
		currentCamera.transform.rotation = Quaternion.identity;

		// set camera parameters
		currentCamera.clearFlags = CameraClearFlags.SolidColor;
		currentCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
		currentCamera.nearClipPlane = 0.85f;  // HoloLens recommended
		//currentCamera.farClipPlane = 100f;

		// reference to the AR main camera
		mainCamera = currentCamera;

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

		// check for point cloud getter
		if(arManager.getPointCloud)
		{
			// there is no point cloud in WinMR
			MultiARInterop.MultiARData arData = arManager.GetARData();

			arData.pointCloudData = new Vector3[0];
			arData.pointCloudLength = 0;
			arData.pointCloudTimestamp = 0.0;
		}

		// set tracking state
		cameraTrackingState = WorldManager.state;
		WorldManager.OnPositionalLocatorStateChanged += WorldManager_OnPositionalLocatorStateChanged;

		// set tracking space type
		Debug.Log("Before: " + XRDevice.GetTrackingSpaceType());
		if(!XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale))
		{
			Debug.LogError("Cannot set room-scale space type!");
		}
		Debug.Log("After: " + XRDevice.GetTrackingSpaceType());

		// create surface renderer
		GameObject objRenderer = new GameObject();
		objRenderer.name = "SurfaceRenderer";
		DontDestroyOnLoad(objRenderer);

		surfaceRenderer = objRenderer.AddComponent<SpatialMappingRenderer>();
		surfaceRenderer.surfaceParent = objRenderer;

		if(arManager.displayTrackedSurfaces)
		{
			surfaceRenderer.renderState = SpatialMappingRenderer.RenderState.Visualization;
			if(surfaceRenderMaterial)
			{
				surfaceRenderer.visualMaterial = surfaceRenderMaterial;
			}
		}

		// create surface collider
		if(useSurfaceColliders)
		{
			GameObject objCollider = new GameObject();
			objCollider.name = "SurfaceCollider";
			DontDestroyOnLoad(objCollider);

			surfaceCollider = objCollider.AddComponent<SpatialMappingCollider>();
			surfaceCollider.surfaceParent = objCollider;

			surfaceCollider.lodType = SpatialMappingBase.LODType.Low;
			surfaceCollider.layer = surfaceColliderLayer;
		}

		// starts co-routine to check rendered surfaces
		StartCoroutine(CheckSurfacesRoutine());

		// interface is initialized
		isInitialized = true;
	}

	public void OnDestroy()
	{
		// remove event handlers
		if(isInitialized)
		{
			isInitialized = false;

			WorldManager.OnPositionalLocatorStateChanged -= WorldManager_OnPositionalLocatorStateChanged;

			if(surfaceRenderer)
			{
				Destroy(surfaceRenderer.gameObject);
				surfaceRenderer = null;
			}

			if(surfaceCollider)
			{
				Destroy(surfaceCollider.gameObject);
				surfaceCollider = null;
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
					if(anchorObj)
					{
						WorldAnchor anchor = anchorObj.GetComponent<WorldAnchor>();

						if(anchor)
						{
							anchor.OnTrackingChanged -= Anchor_OnTrackingChanged;
							Destroy(anchor);
						}
					}

					Destroy(anchorObj);
				}

				// clear the list
				arData.allAnchorsDict.Clear();
			}

		}
	}

	// invoked by WorldManager.OnPositionalLocatorStateChanged-event
	void WorldManager_OnPositionalLocatorStateChanged (PositionalLocatorState oldState, PositionalLocatorState newState)
	{
		cameraTrackingState = newState;
	}

	// invoked by Anchor.OnTrackingChanged-event
	void Anchor_OnTrackingChanged(WorldAnchor self, bool located)
	{
		if(!arManager || !self)
			return;

		MultiARInterop.MultiARData arData = arManager.GetARData();
		string anchorId = self.gameObject.name;

		Debug.Log("Anchor " + anchorId + " tracking: " + located);

		if (arData.allAnchorsDict.ContainsKey(anchorId))
		{
			GameObject anchorObj = GameObject.Find(anchorId);

			if(anchorObj)
			{
				anchorObj.SetActive(located);
			}
		}
	}


	void Update()
	{
		if(!isInitialized)
			return;

		// frame timestamp
		if(cameraTrackingState == PositionalLocatorState.Active)
		{
			// count frames only when the tracking is active
			lastFrameTimestamp = GetCurrentTimestamp();
		}

		// check for input (mouse)
		CheckForInputAction();
	}


	// check for input action (mouse)
	private void CheckForInputAction()
	{
		bool bInputAction = true;

		if(Input.GetMouseButtonDown(0))
		{
			inputAction = MultiARInterop.InputAction.Click;
		}
		else if(Input.GetMouseButton(0))
		{
			inputAction = MultiARInterop.InputAction.Grip;
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
		// changes trackedPlanesTimestamp if surfaces differ
		while(surfaceRenderer)
		{
			MeshFilter[] meshFilters = surfaceRenderer.GetComponentsInChildren<MeshFilter>();

			int checkSum = 0;
			for(int i = 0; i < meshFilters.Length; i++)
			{
				MeshFilter meshFilter = meshFilters[i];
				Mesh mesh = meshFilter ? meshFilter.mesh : null;

				if(mesh)
				{
					checkSum ^= (i ^ mesh.vertexCount ^ mesh.triangles.Length);
				}
			}

			if(surfacesCheckSum != checkSum)
			{
				surfacesCheckSum = checkSum;
				trackedPlanesTimestamp = GetCurrentTimestamp();
				Debug.Log("surfacesCheckSum: " + surfacesCheckSum + ", trackedPlanesTimestamp: " + trackedPlanesTimestamp);
			}

			yield return new WaitForSeconds(surfaceRenderer.secondsBetweenUpdates);
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
