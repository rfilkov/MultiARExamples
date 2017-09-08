using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

public class ARCoreInteface : MonoBehaviour, ARPlatformInterface 
{
	[Tooltip("Reference to the ARCore-Device prefab.")]
	public GameObject arCoreDevicePrefab;

	//public GameObject envLightPrefab;

	[Tooltip("Whether the interface is enabled by MultiARManager.")]
	private bool isInterfaceEnabled = false;

	// Reference to the MultiARManager in the scene
	private MultiARManager arManager = null;

	// whether the interface was initialized
	private bool isInitialized = false;

	// reference to the AR camera in the scene
	private Camera mainCamera;


	private const int MAX_POINT_COUNT = 61440;


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
		
		TrackableHit intHit;
		TrackableHitFlag raycastFilter = TrackableHitFlag.PlaneWithinBounds | TrackableHitFlag.PlaneWithinPolygon;

		if (Session.Raycast(mainCamera.ScreenPointToRay(screenPos), raycastFilter, out intHit))
		{
			hit.point = intHit.Point;
			hit.distance = intHit.Distance;
			hit.plane = new Plane();  // not finished

			// Create an anchor to allow ARCore to track the hitpoint as understanding of the physical
			// world evolves.
			Anchor anchor = Session.CreateAnchor(hit.point, Quaternion.identity);
			hit.anchor = anchor.gameObject;
			hit.anchorId = anchor.Id;

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

		// disable directional light, if any
		Light currentLight = MultiARInterop.GetDirectionalLight();
		if(currentLight)
		{
			currentLight.gameObject.SetActive(false);
		}

		// create ARCore-Device in the scene
		GameObject arCoreDeviceObj = Instantiate(arCoreDevicePrefab, Vector3.zero, Quaternion.identity);
		arCoreDeviceObj.name = "ARCore Device";

		// get reference to the AR camera
		mainCamera = arCoreDeviceObj.GetComponentInChildren<Camera>();

		// create AR environmental light
		GameObject envLight = new GameObject("Evironmental Light");
		//envLight.transform.position = Vector3.zero;
		//envLight.transform.rotation = Quaternion.identity;
		envLight.AddComponent<EnvironmentalLight>();

		if(arManager.getPointCloud)
		{
			MultiARInterop.MultiARData arData = arManager.GetARData();

			arData.pointCloudData = new Vector3[MAX_POINT_COUNT];
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
		_QuitOnConnectionErrors();

		// The tracking state must be FrameTrackingState.Tracking in order to access the Frame.
		if (Frame.TrackingState != FrameTrackingState.Tracking)
		{
			const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
			Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
			return;
		}

		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		// get ar-data holder
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

//		Frame.GetNewPlanes(ref m_newPlanes);
//
//		// Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
//		for (int i = 0; i < m_newPlanes.Count; i++)
//		{
//			// Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
//			// the origin with an identity rotation since the mesh for our prefab is updated in Unity World
//			// coordinates.
//			GameObject planeObject = Instantiate(m_trackedPlanePrefab, Vector3.zero, Quaternion.identity,
//				transform);
//			planeObject.GetComponent<TrackedPlaneVisualizer>().SetTrackedPlane(m_newPlanes[i]);
//
//			// Apply a random color and grid rotation.
//			planeObject.GetComponent<Renderer>().material.SetColor("_GridColor", m_planeColors[Random.Range(0,
//				m_planeColors.Length - 1)]);
//			planeObject.GetComponent<Renderer>().material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));
//		}
//
//		// Disable the snackbar UI when no planes are valid.
//		bool showSearchingUI = true;
//		Frame.GetAllPlanes(ref m_allPlanes);
//		for (int i = 0; i < m_allPlanes.Count; i++)
//		{
//			if (m_allPlanes[i].IsValid)
//			{
//				showSearchingUI = false;
//				break;
//			}
//		}
//
//		m_searchingForPlaneUI.SetActive(showSearchingUI);

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
