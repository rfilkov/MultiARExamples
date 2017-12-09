using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class StandaloneSceneLoader : MonoBehaviour 
{

	public string sceneFilePath;

	public Material surfaceMaterial;

	public Transform cameraTransform;

	public bool useSavedHeading = true;

	public float startHeadingGyro = 0f;

	public bool displaySavedInfos = false;

	public Text locationInfoText;

	public Text gyroInfoText;

	public Text cameraInfoText;

	public Text sceneInfoText;

	// list of loaded surfaces
	private List<OverlaySurfaceUpdater> alLoadedSurfaces = new List<OverlaySurfaceUpdater>();

	// whether the coroutine is currently running
	private bool routineRunning = false;


	void Start () 
	{
		if (!cameraTransform) 
		{
			cameraTransform = Camera.main ? Camera.main.transform : null;
		}

		// load the predefined scene
		routineRunning = true;
		StartCoroutine(LoadButtonClicked());
	}


	void Update () 
	{
		
	}


	// invoked by Load-button
	public IEnumerator LoadButtonClicked()
	{
		// load scene
		string sFilePath = sceneFilePath; // FileUtils.GetPersitentDataPath("SavedSceneSurfaces.json");
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
			if (useSavedHeading) 
			{
				startHeadingGyro = data.startHeading;
			}

			Quaternion compStartRot = Quaternion.Euler(0f, -startHeadingGyro, 0f);

			if (data.surfaceSet != null) 
			{
				// destroy currently loaded surfaces
				DestroyLoadedSurfaces();

				for (int i = 0; i < data.surfaceSet.surfaceCount; i++) 
				{
					GameObject overlaySurfaceObj = new GameObject();
					overlaySurfaceObj.name = "surface-" + i;
					overlaySurfaceObj.transform.SetParent(transform);

					OverlaySurfaceUpdater overlaySurface = overlaySurfaceObj.AddComponent<OverlaySurfaceUpdater>();
					overlaySurface.SetSurfaceMaterial(surfaceMaterial);
					overlaySurface.SetSurfaceCollider(true, null);

					Vector3 surfacePos = data.surfaceSet.surfaces[i].position;
					surfacePos = compStartRot * surfacePos;

					Quaternion surfaceRot = Quaternion.Euler(data.surfaceSet.surfaces[i].rotation);

					List<Vector3> meshVertices = new List<Vector3>(data.surfaceSet.surfaces[i].vertices);
					List<int> meshIndices = new List<int>(data.surfaceSet.surfaces[i].indices);

					// update the surface mesh
					overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);

					alLoadedSurfaces.Add(overlaySurface);
				}
			}

			if (cameraTransform) 
			{
				cameraTransform.position = data.camPosition;
				cameraTransform.rotation = Quaternion.Euler(data.camRotation);
			}

			if (displaySavedInfos) 
			{
				if (locationInfoText) 
				{
					if (data.locEnabled) 
					{
						string sMessage = "Lat: " + data.location.x + ", Lon: " + data.location.y + ", Alt: " + data.location.z;
						locationInfoText.text = sMessage;
					} 
					else 
					{
						locationInfoText.text = "Location service is not supported.";
					}
				}

				if (gyroInfoText) 
				{
					if (data.gyroEnabled) 
					{
						string sMessage = "Att: " + data.gyroAttitude + ", Rot: " + data.gyroRotation + ", Head: " + FormatHeading(data.startHeading);
						gyroInfoText.text = sMessage;
					} 
					else 
					{
						gyroInfoText.text = "Gyroscope is not supported.";
					}
				}

				if (cameraInfoText) 
				{
					string sMessage = string.Format("Camera - Pos: {0}, Rot: {1}\n", data.camPosition, data.camRotation);
					cameraInfoText.text = sMessage;
				}
			}

			Debug.Log("AR-Scene (head: " + (int)data.startHeading + ") loaded from: " + dataFilePath);
		}

		return data;
	}

	// formats angle
	private string FormatHeading(float head)
	{
		return string.Format("{0:F0}", head);
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
