using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

public class ARKitInteface : MonoBehaviour, ARPlatformInterface 
{
	[Tooltip("Material used for camera background.")]
	public Material yuvMaterial;

	[Tooltip("Whether the interface is enabled by MultiARManager.")]
	private bool isInterfaceEnabled = false;

	// Reference to the MultiARManager in the scene
	private MultiARManager arManager = null;

	// whether the interface was initialized
	private bool isInitialized = false;

	// reference to the AR camera in the scene
	private Camera mainCamera;

	// whether the frame event was added or not
	private bool isARFrameEventAdded = false;

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
		return true;
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
	/// Raycasts from screen point to the world.
	/// </summary>
	/// <returns><c>true</c>, if a plane was hit, <c>false</c> otherwise.</returns>
	/// <param name="screenPos">Screen position.</param>
	/// <param name="hit">Hit data.</param>
	public bool RaycastWorld(Vector2 screenPos, out MultiARInterop.TrackableHit hit)
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

		// create camera manager
		GameObject camManagerObj = new GameObject("ARCameraManager");
		UnityARCameraManager camManager = camManagerObj.AddComponent<UnityARCameraManager>();
		camManager.m_camera = currentCamera;

		// check for point cloud
		if(arManager.getPointCloud)
		{
			MultiARInterop.MultiARData arData = arManager.GetARData();

			arData.pointCloudData = new Vector3[0];
			arData.pointCloudLength = 0;
			arData.pointCloudTimestamp = 0.0;

			UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
			isARFrameEventAdded = true;
		}

		// interface is initialized
		isInitialized = true;
	}

	public void OnDestroy()
	{
		if(isARFrameEventAdded)
		{
			isARFrameEventAdded = false;
			UnityARSessionNativeInterface.ARFrameUpdatedEvent -= ARFrameUpdated;
		}
	}

	public void ARFrameUpdated(UnityARCamera camera)
	{
		MultiARInterop.MultiARData arData = arManager.GetARData();

		arData.pointCloudData = camera.pointCloudData;
		arData.pointCloudLength = arData.pointCloudData.Length;
		arData.pointCloudTimestamp = System.DateTime.Now.Ticks;
	}


}
