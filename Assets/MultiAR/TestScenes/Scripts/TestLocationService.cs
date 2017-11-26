using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class TestLocationService : MonoBehaviour 
{

	public Transform gyroParent;

	public Transform gyroTransform;

	public Transform compassTransform;

	public Text locInfoText;

	public Text gyroInfoText;

	public Text compassInfoText;

	public Text arInfoText;

	public GameObject planeTransformPrefab;

	public Material surfaceMaterial;


	private bool locationEnabled = false;
	private bool gyroEnabled = false;
	private bool compassEnabled = false;

	private float locInfoStayTime = 0f;

	private LocationInfo lastLoc;
	private Gyroscope gyro;
	private Quaternion initialGyroRotation = Quaternion.identity;

	private Transform cameraTransform;

	private float startHeading = 0f;
	private float startHeadingComp = 0f;
	private float startHeadingCam = 0f;
	private float startHeadingGyro = 0f;
	private bool startHeadingSet = false;

	private List<Transform> alPlaneTrans = new List<Transform>();
	private MultiARInterop.TrackedSurface[] loadedSurfaces = null;


	void Start () 
	{
		// start location
		if (SystemInfo.supportsLocationService) 
		{
			locationEnabled = true;
			Input.location.Start(1f, 0.1f);
		}
		else
		{
			if (locInfoText) 
			{
				locInfoText.text = "Location service is not supported.";
			}
		}

		// enable gyro
		if (SystemInfo.supportsGyroscope) 
		{
			gyroEnabled = true;

			gyro = Input.gyro;
			gyro.enabled = true;

			if (gyroParent) 
			{
				gyroParent.rotation = Quaternion.Euler(90f, 90f, 0f);
				initialGyroRotation = Quaternion.Euler(0f, 0f, 180f);
			}
		}
		else
		{
			if (gyroInfoText) 
			{
				gyroInfoText.text = "Gyroscope is not supported.";
			}
		}

		// enable compass
		{
			compassEnabled = true;
			Input.compass.enabled = true;
		}

	}

	
	// Update is called once per frame
	void Update () 
	{
		if (locInfoStayTime > 0f) 
		{
			locInfoStayTime -= Time.deltaTime;
		}

		// report location
		if (locationEnabled && locInfoText) 
		{
			lastLoc = Input.location.lastData;

			if (locInfoText && locInfoStayTime <= 0f) 
			{
				string sMessage = "LocStatus: " + Input.location.status.ToString() + ", Enabled: " + Input.location.isEnabledByUser;
				sMessage += "\nLat: " + lastLoc.latitude + ", Long: " + lastLoc.longitude + ", Alt: " + lastLoc.altitude;
				sMessage += "\nAccH: " + lastLoc.horizontalAccuracy + ", AccV: " + lastLoc.verticalAccuracy + ", Time: " + lastLoc.timestamp;

				locInfoText.text = sMessage;
			}
		}

		// report gyro
		if (gyroEnabled) 
		{
			Quaternion gyroAttitude = gyro.attitude;

			if (gyroTransform) 
			{
				Quaternion newRotation = gyroAttitude * initialGyroRotation;
				gyroTransform.localRotation = Quaternion.Slerp(gyroTransform.localRotation, newRotation, 10f * Time.deltaTime);
			}

			if (gyroInfoText) 
			{
				string sMessage = "GyroEnabled: " + gyro.enabled + ", Att: " + FormatQuat(gyroAttitude);
				if (gyroTransform) 
				{
					sMessage += "\nLocal: " + FormatQuat(gyroTransform.localRotation) + ", Global: " + FormatQuat(gyroTransform.rotation);

					Quaternion quatGL = (gyroAttitude * initialGyroRotation) * gyroParent.rotation;
					Quaternion quatGR = gyroParent.rotation * (gyroAttitude * initialGyroRotation);
					sMessage += "\nGlobalL: " + FormatQuat(quatGL) + ", GlobalR: " + FormatQuat(quatGR);
				}

				gyroInfoText.text = sMessage;
			}
		}

		// report location
		if(compassEnabled)
		{
			if(compassTransform)
			{
				Quaternion newRotation = Quaternion.Euler(0, 0, -Input.compass.trueHeading);
				compassTransform.rotation = Quaternion.Slerp(compassTransform.rotation, newRotation, 10f * Time.deltaTime);
			}

			if(compassInfoText)
			{
				string sMessage = "CompEnabled: " + Input.compass.enabled;
				sMessage += "\nHead: " + FormatHeading(Input.compass.magneticHeading) + 
					"\nTrue: " + FormatHeading(Input.compass.trueHeading) + 
					"\nStart: " + FormatHeading((startHeading)) + ", Comp: " + FormatHeading((startHeadingComp)) + ", Cam: " + FormatHeading((startHeadingCam)) + 
					", Gyro: " + FormatHeading((startHeadingGyro));
				
				float gyroComp = gyroTransform ? (gyroTransform.rotation.eulerAngles.y + 90f) : 0f;
				sMessage += "\nGyro: " + gyroComp + ", Time: " + Input.compass.timestamp;

				compassInfoText.text = sMessage;
			}
		}

		// get multi-ar manager's instance
		MultiARManager marManager = MultiARManager.Instance;

		// report ar-camera pose and point-cloud size
		if (arInfoText) 
		{
			if (!cameraTransform) 
			{
				Camera camera = marManager ? marManager.GetMainCamera() : null;
				cameraTransform = camera ? camera.transform : null;
			}

			string sMessage = string.Empty;
			if (cameraTransform) 
			{
				Vector3 pos = cameraTransform.position;
				Vector3 ori = cameraTransform.rotation.eulerAngles;

				sMessage = string.Format("Camera - Pos: ({0:F2}, {1:F2}, {2:F2}), Rot: ({3:F0}, {4:F0}, {5:F0})\n", pos.x, pos.y, pos.z, ori.x, ori.y, ori.z);
			}

			int pcLength = marManager ? marManager.GetPointCloudLength() : 0;
			sMessage += string.Format("PointCloud: {0} points", pcLength);

			arInfoText.text = sMessage;
		}

		// set start heading, when one is available
		if (!startHeadingSet && Input.compass.trueHeading != 0f && cameraTransform != null) 
		{
			startHeadingSet = true;
			startHeadingComp = Input.compass.trueHeading;
			startHeadingCam = cameraTransform.rotation.eulerAngles.y;

			startHeadingGyro = gyroTransform ? (gyroTransform.rotation.eulerAngles.y + 90f) : 0f;
			if (startHeadingGyro >= 360f)
				startHeadingGyro -= 360f;

			startHeading = startHeadingComp + startHeadingCam;
			if (startHeading < 0f)
				startHeading += 360f;
			if (startHeading >= 360f)
				startHeading -= 360f;
		}

		// show tracked plane transforms 
		if (marManager && gyroTransform) 
		{
//			Quaternion camToWorldRot = cameraTransform.rotation;  // Quaternion.Inverse(gyroTransform.rotation);
//			Vector3 vCompRotation = new Vector3(0, Input.compass.trueHeading, 0);
//			Quaternion worldToSceneRot = Quaternion.Euler(vCompRotation);
//
//			Matrix4x4 camToWorld = Matrix4x4.identity;
//			camToWorld.SetTRS(Vector3.zero, camToWorldRot, Vector3.one);

			// get tracked surfaces
			MultiARInterop.TrackedSurface[] trackedSurfaces = loadedSurfaces != null ?  loadedSurfaces : marManager.GetTrackedSurfaces(true);

//			gyroEnabled = false;
//			if (gyroInfoText) 
//			{
//				gyroInfoText.text = "TrackedSurfaces: " + trackedSurfaces.Length + ", alPlaneTrans: " + (alPlaneTrans != null ? alPlaneTrans.Count.ToString() : "null");
//			}

			for (int i = 0; i < trackedSurfaces.Length; i++) 
			{
				Transform surfaceTrans = GetSurfaceTransform(i);

				if (surfaceTrans) 
				{
					surfaceTrans.position = trackedSurfaces[i].position;  // camToWorld.MultiplyPoint3x4(trackedSurfaces[i].position);  // 
					surfaceTrans.rotation = trackedSurfaces[i].rotation;  // camToWorldRot * trackedSurfaces[i].rotation;  // 

//					if (gyroInfoText) 
//					{
//						gyroInfoText.text = "surfaceTrans-" + i + " @ " + surfaceTrans.position.ToString();
//					}
				}

			}

			// destroy the remaining surface transforms
			RemoveExtraSurfaceTransforms(trackedSurfaces.Length);
		}

	}


	void OnDestroy()
	{
		if (locationEnabled) 
		{
			Input.location.Stop();
			locationEnabled = false;
		}

		gyroEnabled = false;
		compassEnabled = false;
	}


	// formats quaternion
	private string FormatQuat(Quaternion quat)
	{
		Vector3 euler = quat.eulerAngles;
		return string.Format("({0:F0}, {1:F0}, {2:F0})", euler.x, euler.y, euler.z);
	}

	// formats angle
	private string FormatHeading(float head)
	{
		return string.Format("{0:F0}", head);
	}


	// returns the transform for i-th tracked plane
	private Transform GetSurfaceTransform(int i)
	{
		if (!planeTransformPrefab)
			return null;
		
		while (alPlaneTrans.Count < (i + 1)) 
		{
			GameObject planeTransObj = Instantiate(planeTransformPrefab);
			alPlaneTrans.Add(planeTransObj.transform);
		}

		return alPlaneTrans[i];
	}


	// destroys the extra plane-transforms
	private void RemoveExtraSurfaceTransforms(int planeCount)
	{
		while (alPlaneTrans.Count > planeCount) 
		{
			int iLast = alPlaneTrans.Count - 1;
			GameObject planeTransObj = alPlaneTrans[iLast].gameObject;

			alPlaneTrans.RemoveAt(iLast);
			Destroy(planeTransObj);
		}
	}



	// invoked by Save-button
	public void SaveButtonClicked()
	{
		try 
		{
			string sFilePath = GetPersitentDataPath("CameraPose.json");
			bool bSaved = SaveCameraPose(sFilePath);

			string sJpegPath = GetPersitentDataPath("CameraPose.jpg");
			SaveScreenShot(sJpegPath);

			// reuse compass-info to show path
			if (bSaved && locInfoText) 
			{
				Vector3 latLonM = GeoTools.LatLong2Meters(lastLoc.latitude, lastLoc.longitude, lastLoc.altitude);
				long latm = (long)((double)latLonM.x * 1000.0);
				long lonm = (long)((double)latLonM.y * 1000.0);

				locInfoText.text = "Saved: " + sFilePath +
					"\nLatm: " + latm + ", Lonm: " + lonm + ", Lat: " + lastLoc.latitude + ", Lon: " + lastLoc.longitude;
				locInfoStayTime = 5f;
			}
		} 
		catch (System.Exception ex) 
		{
			if (locInfoText) 
			{
				locInfoText.text = ex.Message + "\n" + ex.StackTrace;
				locInfoStayTime = 10f;
			}
		}
	}

	// invoked by Load-button
	public void LoadButtonClicked()
	{
		string sFilePath = GetPersitentDataPath("CameraPose.json");
		bool bLoaded = LoadCameraPose(sFilePath);

		if (bLoaded && locInfoText) 
		{
			locInfoText.text = "Loaded: " + sFilePath;
			locInfoStayTime = 5f;
		}
	}


	/// <summary>
	/// Gets path to subfolder of the persitent-data directory on the device.
	/// </summary>
	/// <returns>The path to subfolder of the persitent-data directory.</returns>
	/// <param name="path">Subfolder path.</param>
	public static string GetPersitentDataPath(string path)
	{
		if (path == string.Empty) 
		{
			return Application.persistentDataPath;
		}

		string sDirPath = Application.persistentDataPath;
		string sFileName = path;

		int iLastDS = path.LastIndexOf("/");
		if (iLastDS >= 0) 
		{
			sDirPath = sDirPath + "/" + path.Substring(0, iLastDS);
			sFileName = path.Substring(iLastDS + 1);
		}

		if (!Directory.Exists(sDirPath)) 
		{
			Directory.CreateDirectory(sDirPath);
		}

		return sDirPath + "/" + sFileName;
	}


	// saves the current screen shot
	public bool SaveScreenShot(string saveFilePath)
	{
		if (saveFilePath == string.Empty)
			return false;

		MultiARManager marManager = MultiARManager.Instance;
		Texture2D texScreenshot = MultiARInterop.MakeScreenShot(marManager != null ? marManager.GetMainCamera() : null);

		if (texScreenshot) 
		{
			byte[] btScreenShot = texScreenshot.EncodeToJPG();
			GameObject.Destroy(texScreenshot);

			File.WriteAllBytes(saveFilePath, btScreenShot);

			return true;
		}

		return false;
	}


	// saves camera pose, detected surfaces and point cloud
	public bool SaveCameraPose(string dataFilePath)
	{
		MultiARManager marManager = MultiARManager.Instance;
		if (!marManager)
			return false;
		
		JsonCameraPose data = new JsonCameraPose();

		if (locationEnabled) 
		{
			data.location = new Vector3(lastLoc.latitude, lastLoc.longitude, lastLoc.altitude);
			data.accuracy = new Vector3(lastLoc.horizontalAccuracy, lastLoc.verticalAccuracy, 0f);

			Vector3 latLonM = GeoTools.LatLong2Meters(lastLoc.latitude, lastLoc.longitude, lastLoc.altitude);
			data.latm = (long)((double)latLonM.x * 1000.0);
			data.lonm = (long)((double)latLonM.y * 1000.0);
		}

		if (gyroEnabled && gyro != null) 
		{
			data.attitude = gyro.attitude.eulerAngles;
			data.rotation = gyroTransform.rotation.eulerAngles;
		}

		if (compassEnabled && Input.compass != null) 
		{
			data.compHeading = Input.compass.magneticHeading;
			data.trueHeading = Input.compass.trueHeading;
		}

		data.startHeading = startHeadingGyro; // startHeading;

		if (cameraTransform) 
		{
			data.camPosition = cameraTransform.position;

			data.camRotation = cameraTransform.rotation.eulerAngles;
			data.camRotation.y += data.startHeading;
			data.camRotation = Quaternion.Euler(data.camRotation).eulerAngles;
		}

//		// construct scene to world matrix
//		Vector3 sceneToWorldRot = new Vector3(0f, -startHeading, 0f);
//		Matrix4x4 matSceneToWorld = Matrix4x4.identity;
//		matSceneToWorld.SetTRS(Vector3.zero, Quaternion.Euler(sceneToWorldRot), Vector3.one);

//		// original surfaces
//		data.surfacesOrig = new JsonTrackedSurfaces();
//		data.surfacesOrig.timestamp = marManager.GetTrackedSurfacesTimestamp();
//		data.surfacesOrig.surfaceCount = marManager.GetTrackedSurfacesCount();
//		data.surfacesOrig.surfaces = new JsonSurface[data.surfacesOrig.surfaceCount];

		// surfaces
		data.surfaces = new JsonTrackedSurfaces();
		data.surfaces.timestamp = marManager.GetTrackedSurfacesTimestamp();
		data.surfaces.surfaceCount = marManager.GetTrackedSurfacesCount();
		data.surfaces.surfaces = new JsonSurface[data.surfaces.surfaceCount];

		Quaternion compStartRot = Quaternion.Euler(0f, data.startHeading, 0f);
		MultiARInterop.TrackedSurface[] trackedSurfaces = marManager.GetTrackedSurfaces(true);

		for (int i = 0; i < data.surfaces.surfaceCount; i++) 
		{
//			// original surfaces
//			data.surfacesOrig.surfaces[i] = new JsonSurface();
//
//			data.surfacesOrig.surfaces[i].position = trackedSurfaces[i].position;
//			data.surfacesOrig.surfaces[i].rotation = trackedSurfaces[i].rotation.eulerAngles;
//			data.surfacesOrig.surfaces[i].bounds = trackedSurfaces[i].bounds;
//			data.surfacesOrig.surfaces[i].points = trackedSurfaces[i].points;
//			data.surfacesOrig.surfaces[i].triangles = trackedSurfaces[i].triangles;

			// transformed surfaces
			data.surfaces.surfaces[i] = new JsonSurface();

			Vector3 surfacePos = trackedSurfaces[i].position;
			data.surfaces.surfaces[i].position = compStartRot * surfacePos;

			Vector3 surfaceRot = trackedSurfaces[i].rotation.eulerAngles + compStartRot.eulerAngles;
			data.surfaces.surfaces[i].rotation = Quaternion.Euler(surfaceRot).eulerAngles;

			data.surfaces.surfaces[i].bounds = trackedSurfaces[i].bounds;
			data.surfaces.surfaces[i].points = trackedSurfaces[i].points;
			data.surfaces.surfaces[i].triangles = trackedSurfaces[i].triangles;
		}

//		// point cloud
//		data.pointCloud = new JsonPointCloud();
//		data.pointCloud.timestamp = marManager.GetPointCloudTimestamp();
//		data.pointCloud.pointCount = marManager.GetPointCloudLength();
//		data.pointCloud.points = marManager.GetPointCloudData();

		try 
		{
			// save json
			string sJsonText = JsonUtility.ToJson(data, true);
			File.WriteAllText(dataFilePath, sJsonText);

			Debug.Log("CameraPose (comp: " + (int)data.startHeading + ") saved to: " + dataFilePath);

			return true;
		} 
		catch (System.Exception ex) 
		{
			string sMessage = ex.Message + "\n" + ex.StackTrace;
			Debug.LogError(sMessage);
		}

		return false;
	}


	// loads camera pose, detected surfaces and point cloud
	public bool LoadCameraPose(string dataFilePath)
	{
		if(!File.Exists(dataFilePath))
			return false;

		// load json
		string sJsonText = File.ReadAllText(dataFilePath);
		JsonCameraPose data = JsonUtility.FromJson<JsonCameraPose>(sJsonText);

		if (data != null) 
		{
//			MultiARManager marManager = MultiARManager.Instance;
//			Camera mainCamera = marManager ? marManager.GetMainCamera() : null;
//
//			if (mainCamera) 
//			{
//				mainCamera.transform.position = data.camPosition;
//				mainCamera.transform.rotation = Quaternion.Euler(data.camRotation);
//			}

			// construct world to scene matrix
//			Vector3 worldToSceneRot = new Vector3(0f, data.startHeading, 0f);
//			Matrix4x4 matWorldToScene = Matrix4x4.identity;
//			matWorldToScene.SetTRS(Vector3.zero, Quaternion.Euler(worldToSceneRot), Vector3.one);

			//Quaternion compStartRot = Quaternion.Euler(0f, -startHeading, 0f);
			Quaternion compStartRot = Quaternion.Euler(0f, -startHeadingGyro, 0f);

			if (data.surfaces != null) 
			{
//				loadedSurfaces = new MultiARInterop.TrackedSurface[data.surfaces.surfaceCount];
//
//				for (int i = 0; i < data.surfaces.surfaceCount; i++) 
//				{
//					loadedSurfaces[i] = new MultiARInterop.TrackedSurface();
//
//					Vector3 surfacePos = data.surfaces.surfaces[i].position;
//					loadedSurfaces[i].position = compStartRot * surfacePos;
//
//					Vector3 surfaceRot = data.surfaces.surfaces[i].rotation + compStartRot.eulerAngles;
//					loadedSurfaces[i].rotation = Quaternion.Euler(surfaceRot);
//
//					loadedSurfaces[i].bounds = data.surfaces.surfaces[i].bounds;
//					loadedSurfaces[i].points = data.surfaces.surfaces[i].points;
//					loadedSurfaces[i].triangles = data.surfaces.surfaces[i].triangles;
//				}

				// destroy current overlay surfaces
				DestroyOverlaySurfaces();

				for (int i = 0; i < data.surfaces.surfaceCount; i++) 
				{
					GameObject overlaySurfaceObj = new GameObject();
					overlaySurfaceObj.name = "surface-" + i;

					GameObject overlayCubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
					overlayCubeObj.name = "surface-cube-" + i;
					overlayCubeObj.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);
					overlayCubeObj.transform.SetParent(overlaySurfaceObj.transform);

					OverlaySurfaceUpdater overlaySurface = overlaySurfaceObj.AddComponent<OverlaySurfaceUpdater>();
					overlaySurface.SetSurfaceMaterial(surfaceMaterial);
					overlaySurface.SetSurfaceCollider(true);

					Vector3 surfacePos = data.surfaces.surfaces[i].position;
					Quaternion surfaceRot = Quaternion.Euler(data.surfaces.surfaces[i].rotation);

					surfacePos = compStartRot * surfacePos;
					surfaceRot = Quaternion.Euler(surfaceRot.eulerAngles + compStartRot.eulerAngles);

					List<Vector3> meshVertices = new List<Vector3>(data.surfaces.surfaces[i].points);
					List<int> meshIndices = new List<int>(data.surfaces.surfaces[i].triangles);

					// update the surface mesh
					overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);
				}
			}

			Debug.Log("CameraPose (comp: " + (int)data.startHeading + ") loaded from: " + dataFilePath);

			return true;
		}

		return false;
	}

	// destroys the existing overlay surfaces
	private void DestroyOverlaySurfaces()
	{
		OverlaySurfaceUpdater[] overlaySurfaces = GameObject.FindObjectsOfType<OverlaySurfaceUpdater>();

		if (overlaySurfaces != null && overlaySurfaces.Length > 0) 
		{
			foreach (OverlaySurfaceUpdater ovlSurface in overlaySurfaces) 
			{
				Destroy(ovlSurface.gameObject);
			}
		}
	}

}
