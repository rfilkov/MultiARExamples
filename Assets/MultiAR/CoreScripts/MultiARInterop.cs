using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiARInterop 
{
	/// <summary>
	/// AR platform enum.
	/// </summary>
	public enum ARPlatform : int
	{
		None = 0,
		ArKit = 1,
		ArCore = 2,
		WindowsMR = 3,
		Meta2 = 4
	}

	/// <summary>
	/// Tracking state for AR frames.
	/// </summary>
	public enum CameraTrackingState : int
	{
		Unknown = -1,
		NotInitialized = 0,
		LimitedTracking = 1,
		NormalTracking = 2,
		TrackingError = 3
	}

	/// <summary>
	/// Input action types.
	/// </summary>
	public enum InputAction : int
	{
		None = 0,
		Click = 1,
		Grip = 2,
		Release = 3,
		SpeechCommand = 11,
		CustomCommand = 19
	}

	/// <summary>
	/// Contains information about a raycast hit against a physical object
	/// </summary>
	public struct TrackableHit
	{
		public Vector3 rayPos;
		public Vector3 rayDir;

		public Vector3 point;
		public Vector3 normal;
		public float distance;
		public Quaternion rotation;

		public object psObject;
		//public Plane plane;
	}

	/// <summary>
	/// Contains information about the currently tracked surfaces
	/// </summary>
	public struct TrackedSurface
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 bounds;

		public Vector3[] points;
		public int[] triangles;
	}

	/// <summary>
	/// Contains the most actual AR-data
	/// </summary>
	public class MultiARData
	{
		// point cloud data from the last frame
		public Vector3[] pointCloudData = new Vector3[0];
		public int pointCloudLength = 0;
		public double pointCloudTimestamp = 0.0;

		// anchored objects
		public Dictionary<string, List<GameObject>> allAnchorsDict = new Dictionary<string, List<GameObject>>();

		// overlay surfaces
		public GameObject surfaceRendererRoot = null;
		public Dictionary<string, OverlaySurfaceUpdater> dictOverlaySurfaces = new Dictionary<string, OverlaySurfaceUpdater>();

		// image anchors
		public bool imageAnchorsEnabled = false;

        // background texture
        public RenderTexture backgroundTex = null;
        public int backScreenW = 0, backScreenH = 0;
        public double backTexTime = 0.0;

        public int fixedBackTexW = 0, fixedBackTexH = 0;
        public bool isFixedBackTexSize = false;
	}

	/// <summary>
	/// Maximum point count used for the point-cloud mesh
	/// </summary>
	public const int MAX_POINT_COUNT = 61440;

	/// <summary>
	/// Maximum raycast distance.
	/// </summary>
	public const float MAX_RAYCAST_DIST = 20f;

	// the index of the "by default" spatial surface layer
	public const int SPATIAL_SURFACE_LAYER = 31;


	/// <summary>
	/// Gets the main camera in the scene
	/// </summary>
	/// <returns>The main camera.</returns>
	public static Camera GetMainCamera()
	{
		return Camera.main;
	}


	/// <summary>
	/// Gets the directional light object in the scene
	/// </summary>
	/// <returns>The directional light.</returns>
	public static Light GetDirectionalLight()
	{
		Light[] objLights = GameObject.FindObjectsOfType<Light>();

		foreach(Light objLight in objLights)
		{
			if(objLight.type == LightType.Directional)
			{
				return objLight;
			}
		}

		return null;
	}


	/// <summary>
	/// Shows the cursor.
	/// </summary>
	/// <param name="cursorTrans">Cursor transform.</param>
	/// <param name="target">Target position.</param>
	/// <param name="surfDistance">Distance from the surface.</param>
	/// <param name="defDistance">By-default distance.</param>
	/// <param name="smoothFactor">Smooth factor.</param>
	public static void ShowCursor(Transform cursorTrans, TrackableHit target, float surfDistance, float defDistance, float smoothFactor)
	{
		// the forward vector is looking back
		Vector3 lookForward = -target.rayDir;

		Vector3 targetPosition = Vector3.zero;
		Quaternion targetRotation = Quaternion.identity;

		if (target.point != Vector3.zero)
		{
			targetPosition = target.point + (lookForward * surfDistance);

			Vector3 lookRotation = Vector3.Slerp(target.normal, lookForward, 0.5f);
			targetRotation = Quaternion.LookRotation((lookRotation != lookForward ? lookRotation : Vector3.zero), Vector3.up);
		}
		else
		{
			targetPosition = target.rayPos + target.rayDir * defDistance;
			targetRotation = lookForward.sqrMagnitude > 0f ? Quaternion.LookRotation(lookForward, Vector3.up) : cursorTrans.rotation;
		}

		cursorTrans.position = Vector3.Lerp(cursorTrans.position, targetPosition, smoothFactor * Time.deltaTime);
		cursorTrans.rotation = Quaternion.Lerp(cursorTrans.rotation, targetRotation, smoothFactor * Time.deltaTime);
	}


	/// <summary>
	/// Returns the screen image as 2d-texture, or null in case of error
	/// </summary>
	/// <returns>The photo JPEG.</returns>
	/// <param name="mainCamera">Main camera.</param>
	public static Texture2D MakeScreenShot(Camera mainCamera)
	{
		if (mainCamera == null)
			return null;
		
		int resWidth = Screen.width;
		int resHeight = Screen.height;

		RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);

		// render the main camera image
		if (mainCamera && mainCamera.enabled) 
		{
			mainCamera.targetTexture = rt;
			mainCamera.Render();
			mainCamera.targetTexture = null;
		}

		// get the screenshot
		RenderTexture prevActiveTex = RenderTexture.active;
		RenderTexture.active = rt;

		Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
		screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);

		// clean-up
		RenderTexture.active = prevActiveTex;
		GameObject.Destroy(rt);

		// to encode the image as jpeg, use the following code
//		byte[] btScreenShot = screenShot.EncodeToJPG();
//		GameObject.Destroy(screenShot);

		return screenShot;
	}


	/// <summary>
	/// Gets the mesh indices.
	/// </summary>
	/// <returns>The mesh indices.</returns>
	/// <param name="vertexCount">Vertex count.</param>
	public static List<int> GetMeshIndices(int vertexCount)
	{
		List<int> meshIndices = new List<int>();

		for (int i = 1; i < (vertexCount - 1); i++)
		{
			meshIndices.Add(0);
			meshIndices.Add(i);
			meshIndices.Add(i + 1);
		}

		return meshIndices;
	}


	/// <summary>
	/// Gets the predefined surface layer.
	/// </summary>
	/// <returns>The surface layer.</returns>
	public static int GetSurfaceLayer()
	{
		int surfaceLayer = LayerMask.NameToLayer("SpatialSurface");

		if (surfaceLayer < 0) 
		{
			surfaceLayer = SPATIAL_SURFACE_LAYER;
		}

		return surfaceLayer;
	}


	/// <summary>
	/// Raycasts to UI using the given graphic raycaster and input position.
	/// </summary>
	/// <returns>The U.</returns>
	/// <param name="gr">Gr.</param>
	/// <param name="inputPos">Input position.</param>
	public static GameObject RaycastUI(UnityEngine.UI.GraphicRaycaster gr, Vector2 inputPos)
	{
		if(gr == null)
			return null;
		
		UnityEngine.EventSystems.PointerEventData ped = new UnityEngine.EventSystems.PointerEventData(null);
		ped.position = inputPos;  // Input.mousePosition;

		List<UnityEngine.EventSystems.RaycastResult> results = new List<UnityEngine.EventSystems.RaycastResult>();
		gr.Raycast(ped, results);

		if (results.Count > 0) 
		{
			return results[0].gameObject;
		}

		return null;
	}


	/// <summary>
	/// Turns the object at the given hit-point to look at the camera.
	/// </summary>
	/// <returns>The object rotation.</returns>
	/// <param name="obj">Object (may be null, if only the rotation is needed).</param>
	/// <param name="cam">Camera.</param>
	/// <param name="hitPoint">Hit point.</param>
	/// <param name="hitNormal">Hit normal.</param>
	public static Quaternion TurnObjectToCamera(GameObject obj, Camera cam, Vector3 hitPoint, Vector3 hitNormal)
	{
		Quaternion objRotation = Quaternion.identity;

		if (cam) 
		{
			Plane hitPlane = new Plane(hitNormal, hitPoint);

			Vector3 planePoint = hitPlane.ClosestPointOnPlane(cam.transform.position);
			Vector3 planeCamDir = (planePoint - hitPoint).normalized;

			objRotation = Quaternion.LookRotation(planeCamDir, hitNormal);
		}

		if (obj) 
		{
			obj.transform.rotation = objRotation;
		}

		return objRotation;
	}


}
