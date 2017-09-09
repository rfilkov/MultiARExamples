using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiARManager : MonoBehaviour 
{
	[Tooltip("The preferred AR platform. Should be available, too.")]
	public MultiARInterop.ARPlatform preferredPlatform = MultiARInterop.ARPlatform.None;

	[Tooltip("Whether to get the tracked feature points.")]
	public bool getPointCloud = false;

	[Tooltip("Mesh-prefab used to display the feature points in the scene.")]
	public GameObject displayPointCloud;


	[Tooltip("UI-Text to display information messages.")]
	public UnityEngine.UI.Text infoText;

	//[Tooltip("UI-Text to display debug messages.")]
	//public UnityEngine.UI.Text debugText;


	// singleton instance of MultiARManager
	protected static MultiARManager instance = null;
	protected bool instanceInited = false;

	// selected AR interface
	protected ARPlatformInterface arInterface = null;

	// the most actual AR-data
	protected MultiARInterop.MultiARData arData = new MultiARInterop.MultiARData();

	// the last time the point cloud was displayed
	protected double lastPointCloudTimestamp = 0.0;
	// mesh used to display the point cloud
	protected Mesh pointCloudMesh = null;

	// current frame timestamp and camera tracking state
	protected double lastFrameTimestamp = 0.0;
	protected MultiARInterop.CameraTrackingState cameraTrackingState = MultiARInterop.CameraTrackingState.Unknown;


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

		return 0f;
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
	/// Raycasts from screen point to the world.
	/// </summary>
	/// <returns><c>true</c>, if a plane was hit, <c>false</c> otherwise.</returns>
	/// <param name="screenPos">Screen position.</param>
	/// <param name="hit">Hit data.</param>
	public bool RaycastScreenToWorld(Vector2 screenPos, out MultiARInterop.TrackableHit hit)
	{
		if(arInterface != null)
		{
			return arInterface.RaycastScreenToWorld(screenPos, out hit);
		}

		hit = new MultiARInterop.TrackableHit();
		return false;
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
	/// Gets the current point cloud data.
	/// </summary>
	/// <returns>The point cloud data.</returns>
	public Vector3[] GetPointCloudData()
	{
		Vector3[] pcData = new Vector3[arData.pointCloudLength];
		System.Array.Copy(arData.pointCloudData, pcData, arData.pointCloudLength);

		return pcData;
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
	}

	void Start()
	{
		// initialize point cloud
		if (displayPointCloud) 
		{
			GameObject pointCloudObj = Instantiate(displayPointCloud);
			MeshFilter pointCloudMF = pointCloudObj.GetComponent<MeshFilter>();

			if (pointCloudMF) 
			{
				pointCloudMesh = pointCloudMF.mesh;
				pointCloudMesh.Clear();
			}
		}
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
			infoText.text = "Tracker: " + arInterface.GetCameraTrackingState() + " " + arInterface.GetTrackingErrorMessage() +
				string.Format(", Light: {0:F3}", arInterface.GetLightIntensity()) +
				"\nTimestamp: " + lastFrameTimestamp.ToString();
		}

		// check the tracking state
		if (arInterface == null || cameraTrackingState != MultiARInterop.CameraTrackingState.NormalTracking)
		{
			const int LOST_TRACKING_SLEEP_TIMEOUT = 15;
			Screen.sleepTimeout = LOST_TRACKING_SLEEP_TIMEOUT;
			return;
		}

		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		// display the point cloud
		if(displayPointCloud && arData.pointCloudTimestamp > lastPointCloudTimestamp)
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
	}

}
