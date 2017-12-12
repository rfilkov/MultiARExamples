using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class SceneVisualizerLoader : MonoBehaviour 
{

	public Text locationInfoText;

	public Text gyroInfoText;

	public Text cameraInfoText;

	public Text sceneInfoText;

	public Material surfaceMaterial;

	public bool applyLocationDistance = false;


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

	// reference to ar-manager
	private MultiARManager arManager = null;

	// list of loaded surfaces
	private List<OverlaySurfaceUpdater> alLoadedSurfaces = new List<OverlaySurfaceUpdater>();

	// whether the coroutine is currently running
	private bool routineRunning = false;


	void Start () 
	{
		// get reference to ar-manager
		arManager = MultiARManager.Instance;

		// start location services
		if (SystemInfo.supportsLocationService) 
		{
			locationEnabled = true;
			Input.location.Start(1f, 0.1f);
		}
		else
		{
			if (locationInfoText) 
			{
				locationInfoText.text = "Location service is not supported.";
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
		if (locationEnabled && locationInfoText) 
		{
			lastLoc = Input.location.lastData;

			if (locationInfoText) 
			{
				string sMessage = "LocStatus: " + Input.location.status.ToString() + ", Enabled: " + Input.location.isEnabledByUser;
				sMessage += "\nLat: " + lastLoc.latitude + ", Lon: " + lastLoc.longitude + ", Alt: " + lastLoc.altitude;

				locationInfoText.text = sMessage;
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
					"\nAtt: " + FormatQuat(gyroAttitude) + ", Rot: " + FormatQuat(gyroRotation) + ", Head: " + startHeadingGyro;

				gyroInfoText.text = sMessage;
			}
		}

		// get the main camera
		Camera mainCamera = arManager ? arManager.GetMainCamera() : null;

		// report ar-camera pose and point-cloud size
		if (mainCamera) 
		{
			camPosition = mainCamera.transform.position;
			camRotation = mainCamera.transform.rotation;

			if (cameraInfoText) 
			{
				string sMessage = string.Format("Camera - Pos: {0}, Rot: {1}\n", camPosition, FormatQuat(camRotation));
				cameraInfoText.text = sMessage;
			}
		}

		// set start heading, when one is available
		if (!startHeadingSet && mainCamera && gyroEnabled && gyroRotation.eulerAngles.y != 0f) 
		{
			startHeadingGyro = (gyroRotation.eulerAngles.y + 90f);
			if (startHeadingGyro >= 360f)
				startHeadingGyro -= 360f;

			startHeadingSet = true;
		}

		// check for click
		if (arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();
			//float navMagnitude = action == MultiARInterop.InputAction.Grip ? arManager.GetInputNavCoordinates().magnitude : 0f;

			if (action == MultiARInterop.InputAction.Grip && !routineRunning)
			{
				routineRunning = true;
				StartCoroutine(LoadButtonClicked());
			}
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


	// invoked by Load-button
	public IEnumerator LoadButtonClicked()
	{
		// load scene
		string sFilePath = FileUtils.GetPersitentDataPath("SavedSceneSurfaces.json");
		JsonArScene arScene = LoadArScene(sFilePath);

		yield return null;

		// show result
		if (arScene != null && sceneInfoText) 
		{
			sceneInfoText.text = "Loaded " + alLoadedSurfaces.Count + " surfaces (head:" + (int)arScene.startHeading + ") from file: " + sFilePath;

			// wait for some time
			yield return new WaitForSeconds(5f);

			// clear the info
			sceneInfoText.text = string.Empty;
		}

		routineRunning = false;
	}


	// loads AR scene and the detected surfaces
	public JsonArScene LoadArScene(string dataFilePath)
	{
		if(!File.Exists(dataFilePath))
			return null;

		// load json
		string sJsonText = File.ReadAllText(dataFilePath);
		JsonArScene data = JsonUtility.FromJson<JsonArScene>(sJsonText);

		if (data != null) 
		{
			Quaternion compStartRot = Quaternion.Euler(0f, -startHeadingGyro, 0f);

			Vector3 camOffset = Vector3.zero;
			if (applyLocationDistance && locationEnabled && data.scenePos != null) 
			{
				Vector3 locSaved = GeoUtils.LatLong2Meters(data.scenePos.lat, data.scenePos.lon, data.scenePos.alt);
				Vector3 locCamera = GeoUtils.LatLong2Meters(lastLoc.latitude, lastLoc.longitude, lastLoc.altitude);

				camOffset = locSaved - locCamera;
				camOffset = new Vector3(camOffset.y, camOffset.z, camOffset.x);  // x=lon; y=alt; z=lat
			}

			if (data.surfaceSet != null) 
			{
				// destroy currently loaded surfaces
				DestroyLoadedSurfaces();

				for (int i = 0; i < data.surfaceSet.surfaceCount; i++) 
				{
					GameObject overlaySurfaceObj = new GameObject();
					overlaySurfaceObj.name = "surface-" + i;
					overlaySurfaceObj.transform.SetParent(transform);

//					GameObject overlayCubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
//					overlayCubeObj.name = "surface-cube-" + i;
//					overlayCubeObj.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);
//					overlayCubeObj.transform.SetParent(overlaySurfaceObj.transform);

					OverlaySurfaceUpdater overlaySurface = overlaySurfaceObj.AddComponent<OverlaySurfaceUpdater>();
					overlaySurface.SetSurfaceMaterial(surfaceMaterial);
					overlaySurface.SetSurfaceCollider(true, null);

					Vector3 surfacePos = data.surfaceSet.surfaces[i].position;
					surfacePos = compStartRot * surfacePos;

					if (applyLocationDistance) 
					{
						surfacePos += camOffset;
					}

					Quaternion surfaceRot = Quaternion.Euler(data.surfaceSet.surfaces[i].rotation);
					surfaceRot = Quaternion.Euler(surfaceRot.eulerAngles + compStartRot.eulerAngles); //

					List<Vector3> meshVertices = new List<Vector3>(data.surfaceSet.surfaces[i].vertices);
					List<int> meshIndices = new List<int>(data.surfaceSet.surfaces[i].indices);

					// update the surface mesh
					overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);

//					// find the nearest currently tracked surface
//					MultiARInterop.TrackedSurface[] alTrackedSurf = arManager ? arManager.GetTrackedSurfaces(false) : null;
//
//					MultiARInterop.TrackedSurface nearestSurf;
//					float nearestDist = float.MaxValue;
//					bool foundNearestSurf = false;
//
//					if (alTrackedSurf != null && alTrackedSurf.Length > 0) 
//					{
//						for (int s = 0; s < alTrackedSurf.Length; s++) 
//						{
//							MultiARInterop.TrackedSurface trackedSurf = alTrackedSurf[s];
//
//
//						}
//					}

					alLoadedSurfaces.Add(overlaySurface);
				}
			}

			Debug.Log("AR-Scene (head: " + (int)data.startHeading + ") loaded from: " + dataFilePath);
		}

		return data;
	}


	/// <summary>
	/// Destroys the existing loaded surfaces.
	/// </summary>
	public void DestroyLoadedSurfaces()
	{
		if (alLoadedSurfaces.Count > 0) 
		{
			for(int i = alLoadedSurfaces.Count - 1; i >= 0; i--) 
			{
				OverlaySurfaceUpdater loadedSurface = alLoadedSurfaces[i];
				alLoadedSurfaces.RemoveAt(i);

				Destroy(loadedSurface.gameObject);
			}
		}
	}

}
