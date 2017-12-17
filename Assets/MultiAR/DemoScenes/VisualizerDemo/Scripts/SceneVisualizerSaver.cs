using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SceneVisualizerSaver : MonoBehaviour 
{

	public Text locationInfoText;

	public Text gyroInfoText;

	public Text sceneInfoText;


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

	private float compHeading = 0f;
	private float startHeading = 0f;
	private bool startHeadingSet = false;

	// reference to ar-manager
	private MultiARManager arManager = null;

	// whether the coroutine is currently running
	private bool routineRunning = false;


	void Start () 
	{
		// get reference to ar-manager
		arManager = MultiARManager.Instance;

		// start location services
		if (SystemInfo.supportsLocationService && Input.location.isEnabledByUser) 
		{
			locationEnabled = true;
			Input.location.Start(1f, 0.1f);
			Input.compass.enabled = true;
		}
		else
		{
			if (locationInfoText) 
			{
				locationInfoText.text = "Location service not supported or enabled.";
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
				gyroInfoText.text = "Gyroscope not supported.";
			}
		}
	}

	
	void Update () 
	{
		// report location position
		if (locationEnabled && Input.location.status == LocationServiceStatus.Running) 
		{
			lastLoc = Input.location.lastData;

			compHeading = Input.compass.enabled ? Input.compass.trueHeading : 0f;

			if (locationInfoText) 
			{
				string sMessage = "LocStatus: " + Input.location.status.ToString() + ", Enabled: " + Input.location.isEnabledByUser;
				sMessage += "\nLat: " + lastLoc.latitude + ", Lon: " + lastLoc.longitude + ", Alt: " + lastLoc.altitude;
				sMessage += "\nHeading: " + FormatHeading(compHeading) + ", Start: " + FormatHeading(startHeading);

				locationInfoText.text = sMessage;
			}
		}
		else
		{
			string sMessage = "LocStatus: " + Input.location.status.ToString() + ", Enabled: " + Input.location.isEnabledByUser;
			locationInfoText.text = sMessage;
		}

		// report gyro rotation
		string sGyroMessage = string.Empty;

		if (gyroEnabled) 
		{
			gyroAttitude = gyro.attitude;
			gyroRotation = gyroParentRot * (gyroAttitude * initialGyroRot);

			sGyroMessage = "GyroEnabled: " + gyro.enabled + 
				"\nAtt: " + FormatQuat(gyroAttitude) + ", Rot: " + FormatQuat(gyroRotation);
		}

		// get the main camera
		Camera mainCamera = arManager ? arManager.GetMainCamera() : null;

		// report ar-camera pose and point-cloud size
		if (mainCamera) 
		{
			camPosition = mainCamera.transform.position;
			camRotation = mainCamera.transform.rotation;

			sGyroMessage += string.Format("\nCamPos: {0}, CamRot: {1}\n", camPosition, FormatQuat(camRotation));
		}

		if (gyroInfoText) 
		{
			gyroInfoText.text = sGyroMessage;
		}

		// set start heading, when one is available
		//if (!startHeadingSet && mainCamera && gyroEnabled && gyroAttitude != Quaternion.identity)
		if (!startHeadingSet && mainCamera && locationEnabled && compHeading != 0f)
		{
			Debug.Log("Set heading with gyroRot: " + gyroRotation.eulerAngles  + ", gyroAtt: " + gyroAttitude.eulerAngles + ", compHead: " + compHeading);
			
			//startHeading = (gyroRotation.eulerAngles.y + 90f);
			startHeading = compHeading;

//			if (startHeading >= 360f)
//				startHeading -= 360f;

			startHeadingSet = true;
		}

		// check for click
		if (arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();
			float navMagnitude = action == MultiARInterop.InputAction.Grip ? arManager.GetInputNavCoordinates().magnitude : 0f;

			if (action == MultiARInterop.InputAction.Grip && navMagnitude >= 0.1f && !routineRunning)
			{
				routineRunning = true;
				StartCoroutine(SaveButtonClicked());
			}
		}

	}


	void OnDestroy()
	{
		if (locationEnabled) 
		{
			Input.location.Stop();
			Input.compass.enabled = false;
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
	public IEnumerator SaveButtonClicked()
	{
		// save scene
		string sFilePath = FileUtils.GetPersitentDataPath("SavedSceneSurfaces.json");
		bool bSaved = SaveArScene(sFilePath);

		// save image
		if (bSaved) 
		{
			string sJpegPath = FileUtils.GetPersitentDataPath("SavedSceneImage.jpg");
			SaveScreenShot(sJpegPath);
		}

		yield return null;

		// show result
		if (bSaved && sceneInfoText) 
		{
			MultiARManager marManager = MultiARManager.Instance;
			sceneInfoText.text = "Saved " + (marManager ? marManager.GetTrackedSurfacesCount() : 0) + 
				" surfaces (head:" + (int)startHeading + ") to file: " + sFilePath;

			// wait for some time
			yield return new WaitForSeconds(10f);

			// clear the info
			sceneInfoText.text = "Slide to save the scene into local file.";
		}

		routineRunning = false;
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


	// saves AR scene parameters and the detected surfaces
	public bool SaveArScene(string dataFilePath)
	{
		MultiARManager marManager = MultiARManager.Instance;
		if (!marManager || marManager.GetTrackedSurfacesCount() == 0)
			return false;
		
		JsonArScene data = new JsonArScene();

		data.saverVer = 1;
		data.sceneDesc = string.Empty;
		data.timestamp = marManager.GetTrackedSurfacesTimestamp();

		if (locationEnabled && Input.location.status == LocationServiceStatus.Running) 
		{
			data.scenePos = new JsonScenePos();

			data.scenePos.lat = lastLoc.latitude;
			data.scenePos.lon = lastLoc.longitude;
			data.scenePos.alt = lastLoc.altitude;

			Vector3 latLonM = GeoUtils.LatLong2Meters(lastLoc.latitude, lastLoc.longitude, lastLoc.altitude);
			data.scenePos.latm = (long)((double)latLonM.x * 1000.0);
			data.scenePos.lonm = (long)((double)latLonM.y * 1000.0);
			data.scenePos.altm = (long)latLonM.z;
		}


		if (gyroEnabled) 
		{
			data.sceneRot = new JsonSceneRot();

			data.sceneRot.gyroAtt = gyroAttitude.eulerAngles;
			data.sceneRot.gyroRot = gyroRotation.eulerAngles;
		}

		data.compHeading = compHeading;
		data.startHeading = startHeading;
		Quaternion compStartRot = Quaternion.Euler(0f, startHeading, 0f);

		data.sceneCam = new JsonSceneCam();
		data.sceneCam.camPos = compStartRot * camPosition;

		data.sceneCam.camRot = camRotation.eulerAngles + compStartRot.eulerAngles;
		data.sceneCam.camRot = Quaternion.Euler(data.sceneCam.camRot).eulerAngles;

		// surfaces
		data.surfaceSet = new JsonSurfaceSet();
		data.surfaceSet.timestamp = marManager.GetTrackedSurfacesTimestamp();
		data.surfaceSet.surfaceCount = marManager.GetTrackedSurfacesCount();
		data.surfaceSet.surfaces = new JsonSurface[data.surfaceSet.surfaceCount];

		MultiARInterop.TrackedSurface[] trackedSurfaces = marManager.GetTrackedSurfaces(true);
		for (int i = 0; i < data.surfaceSet.surfaceCount; i++) 
		{
			// transformed surfaces
			data.surfaceSet.surfaces[i] = new JsonSurface();

			Vector3 surfacePos = trackedSurfaces[i].position;
			data.surfaceSet.surfaces[i].position = compStartRot * surfacePos;

//			data.surfaceSet.surfaces[i].rotation = trackedSurfaces[i].rotation.eulerAngles;
			Vector3 surfaceRot = trackedSurfaces[i].rotation.eulerAngles + compStartRot.eulerAngles;
			data.surfaceSet.surfaces[i].rotation = Quaternion.Euler(surfaceRot).eulerAngles;

			data.surfaceSet.surfaces[i].bounds = trackedSurfaces[i].bounds;
			data.surfaceSet.surfaces[i].vertices = trackedSurfaces[i].points;
			data.surfaceSet.surfaces[i].indices = trackedSurfaces[i].triangles;
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

}
