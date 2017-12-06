using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SceneSaverLoader : MonoBehaviour 
{

	public Text locInfoText;

	public Text gyroInfoText;

	public Text arInfoText;

	public GameObject planeTransformPrefab;

	public Material surfaceMaterial;


	private bool locationEnabled = false;
	private bool gyroEnabled = false;

	private LocationInfo lastLoc;
	private Gyroscope gyro;

	private Quaternion gyroParentRot = Quaternion.identity;
	private Quaternion initialGyroRot = Quaternion.identity;

	private Quaternion gyroAttitude = Quaternion.identity;
	private Quaternion gyroRotation = Quaternion.identity;

	private Vector3 camPosition = Vector3.zero;
	private Quaternion camRotation = Quaternion.identity;

	private float startHeadingGyro = 0f;
	private bool startHeadingSet = false;

	private MultiARInterop.TrackedSurface[] loadedSurfaces = null;


	void Start () 
	{
		// start location services
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

			gyroParentRot = Quaternion.Euler(90f, 90f, 0f);
			initialGyroRot = Quaternion.Euler(0f, 0f, 180f);
		}
		else
		{
			if (gyroInfoText) 
			{
				gyroInfoText.text = "Gyroscope is not supported.";
			}
		}
	}

	
	void Update () 
	{
		// report location position
		if (locationEnabled && locInfoText) 
		{
			lastLoc = Input.location.lastData;

			if (locInfoText) 
			{
				string sMessage = "LocStatus: " + Input.location.status.ToString() + ", Enabled: " + Input.location.isEnabledByUser;
				sMessage += "\nLat: " + lastLoc.latitude + ", Long: " + lastLoc.longitude + ", Alt: " + lastLoc.altitude;

				locInfoText.text = sMessage;
			}
		}

		// report gyro rotation
		if (gyroEnabled) 
		{
			gyroAttitude = gyro.attitude;
			gyroRotation = gyroParentRot * (gyroAttitude * initialGyroRot);

			if (gyroInfoText) 
			{
				string sMessage = "GyroEnabled: " + gyro.enabled + 
					"\nAtt: " + FormatQuat(gyroAttitude) + ", Rot: " + FormatQuat(gyroRotation);

				gyroInfoText.text = sMessage;
			}
		}

		// get multi-ar manager's instance
		MultiARManager marManager = MultiARManager.Instance;
		Camera mainCamera = marManager ? marManager.GetMainCamera() : null;

		// report ar-camera pose and point-cloud size
		if (mainCamera) 
		{
			camPosition = mainCamera.transform.position;
			camRotation = mainCamera.transform.rotation;

			if (arInfoText) 
			{
				string sMessage = string.Format("Camera - Pos: {0}, Rot: {1}\n", camPosition, FormatQuat(camRotation));
				arInfoText.text = sMessage;
			}
		}

		// set start heading, when one is available
		if (!startHeadingSet && gyroEnabled && gyroRotation != Quaternion.identity) 
		{
			startHeadingGyro = (gyroRotation.eulerAngles.y + 90f);
			if (startHeadingGyro >= 360f)
				startHeadingGyro -= 360f;

			startHeadingSet = true;
		}

	}


	void OnDestroy()
	{
		if (locationEnabled) 
		{
			Input.location.Stop();
			locationEnabled = false;
		}

		if(gyroEnabled)
		{
			gyro.enabled = false;
			gyroEnabled = false;
		}
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


	// invoked by Save-button
	public void SaveButtonClicked()
	{
		try 
		{
			string sFilePath = FileUtils.GetPersitentDataPath("SavedScene.json");
			bool bSaved = SaveCameraPose(sFilePath);

			string sJpegPath = FileUtils.GetPersitentDataPath("SavedScene.jpg");
			SaveScreenShot(sJpegPath);

			// reuse compass-info to show path
			if (bSaved && locInfoText) 
			{
				locInfoText.text = "Saved: " + sFilePath;
			}
		} 
		catch (System.Exception ex) 
		{
			if (locInfoText) 
			{
				locInfoText.text = ex.Message + "\n" + ex.StackTrace;
			}
		}
	}

	// invoked by Load-button
	public void LoadButtonClicked()
	{
		string sFilePath = FileUtils.GetPersitentDataPath("SavedScene.json");
		bool bLoaded = LoadCameraPose(sFilePath);

		if (bLoaded && locInfoText) 
		{
			locInfoText.text = "Loaded: " + sFilePath;
		}
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
		
		JsonArScene data = new JsonArScene();
		data.timestamp = marManager.GetTrackedSurfacesTimestamp();

		if (locationEnabled) 
		{
			data.location = new Vector3(lastLoc.latitude, lastLoc.longitude, lastLoc.altitude);

			Vector3 latLonM = GeoUtils.LatLong2Meters(lastLoc.latitude, lastLoc.longitude, lastLoc.altitude);
			data.latm = (long)((double)latLonM.x * 1000.0);
			data.lonm = (long)((double)latLonM.y * 1000.0);
		}

		data.gyroAttitude = gyroAttitude.eulerAngles;
		data.gyroRotation = gyroRotation.eulerAngles;

		data.startHeading = startHeadingGyro;;

		data.camPosition = camPosition;

		data.camRotation = camRotation.eulerAngles;
		data.camRotation.y += data.startHeading;
		data.camRotation = Quaternion.Euler(data.camRotation).eulerAngles;

		// surfaces
		data.surfaces = new JsonTrackedSurfaces();
		data.surfaces.timestamp = marManager.GetTrackedSurfacesTimestamp();
		data.surfaces.surfaceCount = marManager.GetTrackedSurfacesCount();
		data.surfaces.surfaces = new JsonSurface[data.surfaces.surfaceCount];

		Quaternion compStartRot = Quaternion.Euler(0f, data.startHeading, 0f);
		MultiARInterop.TrackedSurface[] trackedSurfaces = marManager.GetTrackedSurfaces(true);

		for (int i = 0; i < data.surfaces.surfaceCount; i++) 
		{
			// transformed surfaces
			data.surfaces.surfaces[i] = new JsonSurface();

			Vector3 surfacePos = trackedSurfaces[i].position;
			data.surfaces.surfaces[i].position = compStartRot * surfacePos;

			Vector3 surfaceRot = trackedSurfaces[i].rotation.eulerAngles + compStartRot.eulerAngles;
			data.surfaces.surfaces[i].rotation = Quaternion.Euler(surfaceRot).eulerAngles;

			data.surfaces.surfaces[i].bounds = trackedSurfaces[i].bounds;
			data.surfaces.surfaces[i].points = trackedSurfaces[i].points;
			data.surfaces.surfaces[i].indices = trackedSurfaces[i].triangles;
		}

		try 
		{
			// save json
			string sJsonText = JsonUtility.ToJson(data, true);
			File.WriteAllText(dataFilePath, sJsonText);

			Debug.Log("AR-Scene (head: " + (int)data.startHeading + ") saved to: " + dataFilePath);

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
			Quaternion compStartRot = Quaternion.Euler(0f, -startHeadingGyro, 0f);

			if (data.surfaces != null) 
			{
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
					overlaySurface.SetSurfaceCollider(true, null);

					Vector3 surfacePos = data.surfaces.surfaces[i].position;
					Quaternion surfaceRot = Quaternion.Euler(data.surfaces.surfaces[i].rotation);

					surfacePos = compStartRot * surfacePos;
					surfaceRot = Quaternion.Euler(surfaceRot.eulerAngles + compStartRot.eulerAngles);

					List<Vector3> meshVertices = new List<Vector3>(data.surfaces.surfaces[i].points);
					List<int> meshIndices = new List<int>(data.surfaces.surfaces[i].indices);

					// update the surface mesh
					overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);
				}
			}

			Debug.Log("AR-Scene (head: " + (int)data.startHeading + ") loaded from: " + dataFilePath);

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
