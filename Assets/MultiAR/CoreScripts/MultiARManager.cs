using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiARManager : MonoBehaviour 
{
	[Tooltip("The preferred AR platform. Should be available, too.")]
	public MultiARInterop.ARPlatform preferredPlatform = MultiARInterop.ARPlatform.None;

	[Tooltip("Whether to apply the AR-detected light.")]
	public bool applyARLight = true;

	[Tooltip("Whether to get the tracked feature points.")]
	public bool getPointCloud = false;

	[Tooltip("Mesh-prefab used to display the point cloud in the scene.")]
	public GameObject pointCloudPrefab;

	[Tooltip("Whether to display the tracked surfaces.")]
	public bool displayTrackedSurfaces = false;

	[Tooltip("Whether the world raycasts may hit tracked surfaces only, or points from the cloud in general.")]
	public bool hitTrackedSurfacesOnly = true;

	public enum SurfaceRenderEnum : int { None, Occlusion, Visualization };
	[Tooltip("Whether to create scene surfaces to overlay the ar-detected surfaces.")]
	public SurfaceRenderEnum useOverlaySurface = SurfaceRenderEnum.Visualization;

	[Tooltip("Material used to render the overlay surfaces.")]
	public Material surfaceVisualizationMaterial;

	[Tooltip("Material used to render the overlay surfaces.")]
	public Material surfaceOcclusionMaterial;

	[Tooltip("Whether the overlay surfaces should have colliders.")]
	public bool overlaySurfaceColliders = true;

	[Tooltip("Physic material to be used by surface colliders.")]
	public PhysicMaterial colliderPhysicMaterial;

	public enum ShowCursorEnum : int { Never, OnSurfacesOnly, OnSceneObjects, Always };
	[Tooltip("How the cursor should be visualized.")]
	public ShowCursorEnum showCursor = ShowCursorEnum.Always;

	[Tooltip("The cursor object that will be visualized.")]
	public Transform cursorObject;

	[Tooltip("UI-Text to display tracker information messages.")]
	public UnityEngine.UI.Text infoText;

	//[Tooltip("UI-Text to display debug messages.")]
	//public UnityEngine.UI.Text debugText;


	// singleton instance of MultiARManager
	protected static MultiARManager instance = null;
	protected bool instanceInited = false;

	// selected AR interface
	protected ARPlatformInterface arInterface = null;
	protected bool isInitialized = false;

	// the most actual AR-data
	protected MultiARInterop.MultiARData arData = new MultiARInterop.MultiARData();

	// the last time the point cloud was displayed
	protected double lastPointCloudTimestamp = 0.0;
	// mesh used to display the point cloud
	protected Mesh pointCloudMesh = null;

	// current frame timestamp and camera tracking state
	protected double lastFrameTimestamp = 0.0;
	protected MultiARInterop.CameraTrackingState cameraTrackingState = MultiARInterop.CameraTrackingState.Unknown;

	// available graphic raycasters
	protected UnityEngine.UI.GraphicRaycaster[] uiRaycasters = null;


	/// <summary>
	/// Gets the instance of MultiARManager.
	/// </summary>
	/// <value>The instance of MultiARManager.</value>
	public static MultiARManager Instance
	{
		get
		{
			return instance;
		}
	}

	/// <summary>
	/// Determines whether MultiARManager is successfully initialized.
	/// </summary>
	/// <returns><c>true</c> if MultiARManager is initialized; otherwise, <c>false</c>.</returns>
	public bool IsInitialized()
	{
		return isInitialized;
	}

	/// <summary>
	/// Gets the AR data-holder.
	/// </summary>
	/// <returns>The AR data-holder.</returns>
	public MultiARInterop.MultiARData GetARData()
	{
		return arData;
	}

	/// <summary>
	/// Gets the AR main camera.
	/// </summary>
	/// <returns>The AR main camera.</returns>
	public Camera GetMainCamera()
	{
		if(arInterface != null)
		{
			return arInterface.GetMainCamera();
		}

		return null;
	}

	/// <summary>
	/// Gets AR-detected light intensity.
	/// </summary>
	/// <returns>The light intensity.</returns>
	public float GetLightIntensity()
	{
		if(arInterface != null)
		{
			return arInterface.GetLightIntensity();
		}

		return 1f;
	}

	/// <summary>
	/// Gets the last frame timestamp.
	/// </summary>
	/// <returns>The last frame timestamp.</returns>
	public double GetLastFrameTimestamp()
	{
		if(arInterface != null)
		{
			return arInterface.GetLastFrameTimestamp();
		}

		return 0.0;
	}

	/// <summary>
	/// Gets the state of the camera tracking.
	/// </summary>
	/// <returns>The camera tracking state.</returns>
	public MultiARInterop.CameraTrackingState GetCameraTrackingState()
	{
		if(arInterface != null)
		{
			return arInterface.GetCameraTrackingState();
		}

		return MultiARInterop.CameraTrackingState.Unknown;
	}

	/// <summary>
	/// Gets the current point cloud timestamp.
	/// </summary>
	/// <returns>The point cloud timestamp.</returns>
	public double GetPointCloudTimestamp()
	{
		return arData.pointCloudTimestamp;
	}

	/// <summary>
	/// Gets the length of the point cloud data.
	/// </summary>
	/// <returns>The point cloud data length.</returns>
	public int GetPointCloudLength()
	{
		return arData.pointCloudLength;
	}

	/// <summary>
	/// Gets the current point cloud data.
	/// </summary>
	/// <returns>The point cloud data.</returns>
	public Vector3[] GetPointCloudData()
	{
		Vector3[] pcData = new Vector3[arData.pointCloudLength];
		System.Array.Copy(arData.pointCloudData, pcData, arData.pointCloudLength);

		return pcData;
	}

	/// <summary>
	/// Gets the tracked surfaces timestamp.
	/// </summary>
	/// <returns>The tracked surfaces timestamp.</returns>
	public double GetTrackedSurfacesTimestamp()
	{
		if(arInterface != null)
		{
			return arInterface.GetTrackedSurfacesTimestamp();
		}

		return 0.0;
	}

	/// <summary>
	/// Gets the count of currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces count.</returns>
	public int GetTrackedSurfacesCount()
	{
		if(arInterface != null)
		{
			return arInterface.GetTrackedSurfacesCount();
		}

		return 0;
	}

	/// <summary>
	/// Gets the currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces.</returns>
	public MultiARInterop.TrackedSurface[] GetTrackedSurfaces(bool bGetPoints)
	{
		if(arInterface != null)
		{
			return arInterface.GetTrackedSurfaces(bGetPoints);
		}

		// no tracked planes
		MultiARInterop.TrackedSurface[] trackedPlanes = new MultiARInterop.TrackedSurface[0];
		return trackedPlanes;
	}

	/// <summary>
	/// Gets the current cursor position or Vector3.zero
	/// </summary>
	/// <returns>The cursor position.</returns>
	public Vector3 GetCursorPosition()
	{
		if(cursorObject)
		{
			return cursorObject.position;
		}

		return Vector3.zero;
	}


	/// <summary>
	/// Determines whether input action is available.for processing
	/// </summary>
	/// <returns><c>true</c> input action is available; otherwise, <c>false</c>.</returns>
	public bool IsInputAvailable(bool inclRelease)
	{
		if(arInterface != null)
		{
			return arInterface.IsInputAvailable(inclRelease);
		}

		return false;
	}

	/// <summary>
	/// Gets the input action.
	/// </summary>
	/// <returns>The input action.</returns>
	public MultiARInterop.InputAction GetInputAction()
	{
		if(arInterface != null)
		{
			MultiARInterop.InputAction inputAction = arInterface.GetInputAction();

			// input action should be consumed only once
			if(inputAction != MultiARInterop.InputAction.None)
			{
				arInterface.ClearInputAction();

				if(CheckForCanvasInputAction())
				{
					inputAction = MultiARInterop.InputAction.None;
				}
			}

			return inputAction;
		}

		return MultiARInterop.InputAction.None;
	}

	/// <summary>
	/// Gets the input normalized navigation coordinates.
	/// </summary>
	/// <returns>The input nav coordinates.</returns>
	public Vector3 GetInputNavCoordinates()
	{
		if(arInterface != null)
		{
			return arInterface.GetInputNavCoordinates();
		}

		return Vector3.zero;
	}

	// checks if the input action ray-casts UI element or not
	private bool CheckForCanvasInputAction()
	{
		if(uiRaycasters != null && arInterface != null)
		{
			Vector2 inputPos =  arInterface.GetInputScreenPos(false);

			foreach(UnityEngine.UI.GraphicRaycaster gr in uiRaycasters)
			{
				GameObject rayHit = MultiARInterop.RaycastUI(gr, inputPos);
				if(rayHit != null)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Gets the input-action timestamp.
	/// </summary>
	/// <returns>The input-action timestamp.</returns>
	public double GetInputTimestamp()
	{
		if(arInterface != null)
		{
			return arInterface.GetInputTimestamp();
		}

		return 0.0;
	}

	/// <summary>
	/// Clears the input action.
	/// </summary>
	public void ClearInputAction()
	{
		if(arInterface != null)
		{
			arInterface.ClearInputAction();
		}
	}

	/// <summary>
	/// Raycasts from screen point or camera to the scene colliders.
	/// </summary>
	/// <returns><c>true</c>, if an object was hit, <c>false</c> otherwise.</returns>
	/// <param name="fromInputPos">Whether to use the last input position for the raycast, or not.</param>
	/// <param name="hit">Hit data.</param>
	public bool RaycastToScene(bool fromInputPos, out MultiARInterop.TrackableHit hit)
	{
		if(arInterface != null)
		{
			return arInterface.RaycastToScene(fromInputPos, out hit);
		}

		hit = new MultiARInterop.TrackableHit();
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
		if(arInterface != null)
		{
			return arInterface.RaycastAllToScene(fromInputPos, out hits);
		}

		hits = new MultiARInterop.TrackableHit[0];
		return false;
	}

	/// <summary>
	/// Raycasts from screen point or camera to the world.
	/// </summary>
	/// <returns><c>true</c>, if a plane was hit, <c>false</c> otherwise.</returns>
	/// <param name="screenPos">Screen position.</param>
	/// <param name="hit">Hit data.</param>
	public bool RaycastToWorld(bool fromInputPos, out MultiARInterop.TrackableHit hit)
	{
		if(arInterface != null)
		{
			return arInterface.RaycastToWorld(fromInputPos, out hit);
		}

		hit = new MultiARInterop.TrackableHit();
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
		if(arInterface != null)
		{
			return arInterface.AnchorGameObjectToWorld(gameObj, hit);
		}

		return string.Empty;
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
		if(arInterface != null)
		{
			return arInterface.AnchorGameObjectToWorld(gameObj, worldPosition, worldRotation);
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
		if(arInterface != null)
		{
			return arInterface.RemoveGameObjectAnchor(anchorId, keepObjActive);
		}

		return false;
	}

	/// <summary>
	/// Attaches a game object to anchor.
	/// </summary>
	/// <returns>The anchor Id, or empty string.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="anchorId">Anchor identifier.</param>
	/// <param name="makeObjActive">If set to <c>true</c> makes the game object active.</param>
	/// <param name="createNewAnchor">If set to <c>true</c> creates new anchor, if needed.</param>
	public string AttachObjectToAnchor(GameObject gameObj, string anchorId, bool makeObjActive, bool createNewAnchor)
	{
		if(gameObj && !string.IsNullOrEmpty(anchorId))
		{
			// add the object to the anchor list
			if(arData.allAnchorsDict.ContainsKey(anchorId))
			{
				List<GameObject> anchoredObjs = arData.allAnchorsDict[anchorId];
				if(anchoredObjs.Count > 0 && anchoredObjs[0] && anchoredObjs[0].transform.parent)
				{
					// make it parent
					gameObj.transform.SetParent(anchoredObjs[0].transform.parent, true);
					if(makeObjActive && !gameObj.activeSelf)
					{
						gameObj.SetActive(true);
					}

					anchoredObjs.Add(gameObj);
					arData.allAnchorsDict[anchorId] = anchoredObjs;

					return anchorId;
				}
			}
		}
		else if(gameObj && createNewAnchor)
		{
			// create new anchor
			anchorId = AnchorGameObjectToWorld(gameObj, gameObj.transform.position, Quaternion.identity);
			if(anchorId != string.Empty && makeObjActive && !gameObj.activeSelf)
			{
				gameObj.SetActive(true);
			}

			return anchorId;
		}

		return string.Empty;
	}

	/// <summary>
	/// Detaches a game object from anchor.
	/// </summary>
	/// <returns>The anchor Id, or empty string.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="anchorId">Anchor identifier.</param>
	/// <param name="keepObjActive">If set to <c>true</c> keeps the object active afterwards.</param>
	/// <param name="keepEmptyAnchor">If set to <c>true</c> doesn't remove the anchor when no more objects are attached to it.</param>
	public string DetachObjectFromAnchor(GameObject gameObj, string anchorId, bool keepObjActive, bool keepEmptyAnchor)
	{
		if(gameObj && !string.IsNullOrEmpty(anchorId))
		{
			// remove the object from the anchor list
			if(arData.allAnchorsDict.ContainsKey(anchorId))
			{
				List<GameObject> anchoredObjs = arData.allAnchorsDict[anchorId];

				// remove the parent
				gameObj.transform.parent = null;
				if(keepObjActive && !gameObj.activeSelf)
				{
					gameObj.SetActive(true);
				}

				anchoredObjs.Remove(gameObj);
				arData.allAnchorsDict[anchorId] = anchoredObjs;

				// remove the anchor, too
				if(anchoredObjs.Count == 0 && !keepEmptyAnchor)
				{
					RemoveGameObjectAnchor(anchorId, keepObjActive);
					anchorId = string.Empty;
				}

				return anchorId;
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// Gets the anchors count.
	/// </summary>
	/// <returns>The anchors count.</returns>
	public int GetAnchorsCount()
	{
		return arData.allAnchorsDict.Keys.Count;
	}

	/// <summary>
	/// Gets the anchored objects count.
	/// </summary>
	/// <returns>The anchored objects count.</returns>
	public int GetAnchoredObjectsCount()
	{
		int iObjCount = 0;

		foreach(string anchorId in arData.allAnchorsDict.Keys)
		{
			iObjCount += arData.allAnchorsDict[anchorId].Count;
		}

		return iObjCount;
	}

	/// <summary>
	/// Gets all game-object anchor identifiers.
	/// </summary>
	/// <returns>The all anchor identifiers.</returns>
	public List<string> GetAllObjectAnchorIds()
	{
		return new List<string>(arData.allAnchorsDict.Keys);
	}

	/// <summary>
	/// Determines whether the specified anchorId is valid.
	/// </summary>
	/// <returns><c>true</c> if anchorId is valid; otherwise, <c>false</c>.</returns>
	/// <param name="anchorId">Anchor identifier.</param>
	public bool IsValidAnchorId(string anchorId)
	{
		if(!string.IsNullOrEmpty(anchorId))
		{
			return arData.allAnchorsDict.ContainsKey(anchorId);
		}

		return false;
	}

	/// <summary>
	/// Gets the anchored object.
	/// </summary>
	/// <returns>The anchored object or null.</returns>
	/// <param name="anchorId">Anchor identifier.</param>
	public List<GameObject> GetAnchoredObjects(string anchorId)
	{
		if(arData.allAnchorsDict.ContainsKey(anchorId))
		{
			return arData.allAnchorsDict[anchorId];
		}

		return null;
	}

	/// <summary>
	/// Gets the anchorId of the given object, or empty string if not found.
	/// </summary>
	/// <returns>The object anchorId.</returns>
	/// <param name="gameObj">Game object.</param>
	public string GetObjectAnchorId(GameObject gameObj)
	{
		if(gameObj == null)
			return string.Empty;
		
		foreach(string anchorId in arData.allAnchorsDict.Keys)
		{
			List<GameObject> anchoredObjs = arData.allAnchorsDict[anchorId];

			foreach(GameObject anchoredObj in anchoredObjs)
			{
				if(anchoredObj == gameObj)
				{
					return anchorId;
				}
			}
		}

		return string.Empty;
	}

	/// <summary>
	/// Gets the specified surface material, or null if there is no material set.
	/// </summary>
	/// <returns>The surface material.</returns>
	public Material GetSurfaceMaterial()
	{
		Material surfaceMat = null;

		switch (useOverlaySurface) 
		{
			case SurfaceRenderEnum.Visualization:
				surfaceMat = surfaceVisualizationMaterial;
				break;

			case SurfaceRenderEnum.Occlusion:
				surfaceMat = surfaceOcclusionMaterial;
				break;
		}

		return surfaceMat;
	}


	/// <summary>
	/// Refreshs the object references after next scene load.
	/// </summary>
	public void RefreshSceneReferences()
	{
		uiRaycasters = FindObjectsOfType<UnityEngine.UI.GraphicRaycaster>();
	}


	// -- // -- // -- // -- // -- // -- // -- // -- // -- // -- //

	void Awake()
	{
		// initializes the singleton instance of MultiARManager
		if(instance == null)
		{
			instance = this;
			DontDestroyOnLoad(this);

			instanceInited = true;
		}
		else if(instance != this)
		{
			Destroy(gameObject);
			return;
		}

		// locate available AR interfaces in the scene
		MonoBehaviour[] monoScripts = FindObjectsOfType<MonoBehaviour>();

		ARPlatformInterface arIntAvailable = null;
		arInterface = null;

		for(int i = 0; i < monoScripts.Length; i++)
		{
			if((monoScripts[i] is ARPlatformInterface) && monoScripts[i].enabled)
			{
				ARPlatformInterface arInt = (ARPlatformInterface)monoScripts[i];
				arInt.SetEnabled(false, null);

				if(arInt.IsPlatformAvailable())
				{
					Debug.Log(arInt.GetARPlatform().ToString() + " is available.");

					if(arIntAvailable == null)
					{
						arIntAvailable = arInt;
					}

					if(arInt.GetARPlatform() == preferredPlatform)
					{
						arInterface = arInt;
					}
				}
			}
		}

		if(arInterface == null)
		{
			arInterface = arIntAvailable;
		}

		// report the selected platform
		if(arInterface != null)
		{
			arInterface.SetEnabled(true, this);
			Debug.Log("Selected AR-Platform: " + arInterface.GetARPlatform().ToString());
		}
		else
		{
			Debug.LogError("No suitable AR-Interface found. Please check the scene setup.");
		}

		// set initialization status
		isInitialized = (arInterface != null);
	}

	void Start()
	{
		// initialize point cloud
		if (pointCloudPrefab) 
		{
			GameObject pointCloudObj = Instantiate(pointCloudPrefab);
			MeshFilter pointCloudMF = pointCloudObj.GetComponent<MeshFilter>();

			if (pointCloudMF) 
			{
				pointCloudMesh = pointCloudMF.mesh;
				pointCloudMesh.Clear();
			}
		}

//		if(surfaceVisualizationMaterial == null && useOverlaySurface != SurfaceRenderEnum.None)
//		{
//			// get the default material
//			if(useOverlaySurface == MultiARManager.SurfaceRenderEnum.Occlusion)
//				surfaceVisualizationMaterial = (Material)Resources.Load("SpatialMappingOcclusion", typeof(Material));
//			else if(useOverlaySurface == MultiARManager.SurfaceRenderEnum.Visualization)
//				surfaceVisualizationMaterial = (Material)Resources.Load("SpatialMappingWireframe", typeof(Material));
//		}

		// refreshes object references for the current scene
		RefreshSceneReferences();
	}

	void OnDestroy()
	{
		if(instanceInited)
		{
			instance = null;
		}
	}


	void Update () 
	{
		if(arInterface != null)
		{
			// get last frame timestamp and tracking state
			lastFrameTimestamp = arInterface.GetLastFrameTimestamp();
			cameraTrackingState = arInterface.GetCameraTrackingState();
		}

		// show the tracking state
		if(infoText)
		{
			int numSurfaces = GetTrackedSurfacesCount();
			int numAnchors = GetAnchorsCount();
			int numObjects = GetAnchoredObjectsCount();

			infoText.text = "Tracker: " + arInterface.GetCameraTrackingState () + " " + arInterface.GetTrackingErrorMessage () +
				string.Format ("\nLight: {0:F3}", arInterface.GetLightIntensity ()) + ", Surfaces: " + numSurfaces + ", Anchors: " + numAnchors + ", Objects: " + numObjects;
				//+ "\nTimestamp: " + lastFrameTimestamp.ToString();
		}

		// check the tracking state
		if (arInterface == null || cameraTrackingState != MultiARInterop.CameraTrackingState.NormalTracking)
		{
			const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
			Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
			return;
		}

		// don't fall asleep
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		// display the point cloud
		if(pointCloudPrefab && arData.pointCloudTimestamp > lastPointCloudTimestamp)
		{
			lastPointCloudTimestamp = arData.pointCloudTimestamp;
			int pointCloudLen = arData.pointCloudLength < MultiARInterop.MAX_POINT_COUNT ? arData.pointCloudLength : MultiARInterop.MAX_POINT_COUNT;

			int[] indices = new int[pointCloudLen];
			for (int i = 0; i < pointCloudLen; i++)
			{
				indices[i] = i;
			}

			pointCloudMesh.Clear();
			pointCloudMesh.vertices = arData.pointCloudData;
			pointCloudMesh.SetIndices(indices, MeshTopology.Points, 0, false);
		}

		// show cursor if needed
		if(cursorObject && showCursor != ShowCursorEnum.Never)
		{
			MultiARInterop.TrackableHit hit;
			hit.point = Vector3.zero;

			// check how to raycast
			bool isFromInputPos = IsInputAvailable(false);

			if (showCursor == ShowCursorEnum.OnSurfacesOnly) 
			{
				RaycastToWorld(isFromInputPos, out hit);
			}
			else
			{
				if (!RaycastToScene (isFromInputPos, out hit)) 
				{
					RaycastToWorld(isFromInputPos, out hit);
				}
			}

			if (infoText) 
			{
				string hitObjName = string.Empty;
				if (hit.psObject != null) 
				{
					if (hit.psObject is RaycastHit)
						hitObjName = ((RaycastHit)hit.psObject).transform.gameObject.name;
					else
						hitObjName = hit.psObject.ToString();
				}

				infoText.text += "\ninputPos: " + isFromInputPos + ", hit: " + hit.point + ", obj: " + hitObjName;
			}

			if(showCursor == ShowCursorEnum.Always || hit.point != Vector3.zero)
			{
				MultiARInterop.ShowCursor(cursorObject, hit, 0.03f, 2f, 50f);
			}
		}

	}

}
