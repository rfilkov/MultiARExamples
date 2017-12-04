using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class TestSceneLoad : MonoBehaviour 
{

	public string filePath;

	public bool fixRotateSurfTrans = false;

	public Material surfaceMaterial;

	public Transform cameraTransform;

	public UnityEngine.UI.Text compAngleText;

	public UnityEngine.UI.Text cameraAngleText;

	public UnityEngine.UI.Slider cameraAngleSlider;


	private float startCompAngle = 0f;


	void Start () 
	{
		if (!cameraTransform) 
		{
			cameraTransform = Camera.main ? Camera.main.transform : null;
		}

		// load the predefined scene
		LoadSceneButton_Clicked();

		// create my mesh
		//CreateMesh();
	}


	void Update () 
	{
		
	}


	public void CompAngleSlider_ValueChanged(float value)
	{
		startCompAngle = value;

		if (compAngleText) 
		{
			compAngleText.text = string.Format("{0:F0}", value);
		}
	}


	public void CameraAngleSlider_ValueChanged(float value)
	{
		if (cameraAngleText) 
		{
			cameraAngleText.text = string.Format("{0:F0}", value);
		}

		if (cameraTransform) 
		{
			Vector3 cameraRot = cameraTransform.rotation.eulerAngles;
			cameraRot.y = value;
			cameraTransform.rotation = Quaternion.Euler(cameraRot);
		}
	}


	public void LoadSceneButton_Clicked()
	{
		if (filePath != string.Empty) 
		{
			JsonCameraPose poseData = LoadCameraPose(filePath);
			if (poseData != null) 
			{
				DestroyOverlaySurfaces();
				SetupPoseScene(poseData, fixRotateSurfTrans);
			}
		}
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


	// loads camera pose file path
	public JsonCameraPose LoadCameraPose(string dataFilePath)
	{
		JsonCameraPose data = null;
		if(!File.Exists(dataFilePath))
			return data;

		try 
		{
			// load json
			string sJsonText = File.ReadAllText(dataFilePath);
			data = JsonUtility.FromJson<JsonCameraPose>(sJsonText);
		} 
		catch (System.Exception ex) 
		{
			string sMessage = ex.Message + "\n" + ex.StackTrace;
			Debug.LogError(sMessage);
		}

		return data;
	}


	// sets up the camera-pose scene
	public void SetupPoseScene(JsonCameraPose poseData, bool rotateSurfacePosRot)
	{
		if (cameraTransform) 
		{
			cameraTransform.position = poseData.camPosition;
			cameraTransform.rotation = Quaternion.Euler(poseData.camRotation);

			if (cameraAngleSlider) 
			{
				cameraAngleSlider.value = poseData.camRotation.y;
			}
		}

		// construct world to scene matrix
		Vector3 worldToSceneRot = new Vector3(0f, poseData.startHeading, 0f);
		Matrix4x4 matWorldToScene = Matrix4x4.identity;
		matWorldToScene.SetTRS(Vector3.zero, Quaternion.Euler(worldToSceneRot), Vector3.one);

		Quaternion compStartRot = Quaternion.Euler(0f, -startCompAngle, 0f);

		if (poseData.surfaces != null) 
		{
			for (int i = 0; i < poseData.surfaces.surfaceCount; i++) 
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

				Vector3 surfacePos = poseData.surfaces.surfaces[i].position;
				Quaternion surfaceRot = Quaternion.Euler(poseData.surfaces.surfaces[i].rotation);

				if (rotateSurfacePosRot) 
				{
					surfacePos = matWorldToScene.MultiplyPoint3x4(surfacePos);
					surfaceRot = Quaternion.Euler(surfaceRot.eulerAngles + worldToSceneRot);
				}

				surfacePos = compStartRot * surfacePos;
				surfaceRot = Quaternion.Euler(surfaceRot.eulerAngles + compStartRot.eulerAngles);

				List<Vector3> meshVertices = new List<Vector3>(poseData.surfaces.surfaces[i].points);
				List<int> meshIndices = new List<int>(poseData.surfaces.surfaces[i].triangles);

//				Quaternion invRot = Quaternion.Inverse(surfaceRot);
//				for (int v = 0; v < meshVertices.Count; v++) 
//				{
//					meshVertices[v] = invRot * meshVertices[v];
//				}

//				if (absoluteMeshCoords) 
//				{
//					surfacePos = Vector3.zero;
//					surfaceRot = Quaternion.identity;
//				}

				// update the surface mesh
				overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);

//				overlayCubeObj.transform.position = surfacePos;
//				overlayCubeObj.transform.rotation = surfaceRot;
			}
		}
	}


	// create a quad mesh
	private void CreateMesh()
	{
		// surface position & rotation
		Vector3 surfacePos = new Vector3(0.5f, 1.0f, 1.5f);  // Vector3.zero; // 
		Quaternion surfaceRot = Quaternion.Euler(0, 30, 0); // Quaternion.identity; // 

		//Quaternion surfaceRotInv = Quaternion.Inverse(surfaceRot);
		//Matrix4x4 matTransform = Matrix4x4.TRS(-surfacePos, Quaternion.Inverse(surfaceRot), Vector3.one);

		List<Vector3> meshVertices = new List<Vector3>();
		meshVertices.Add(new Vector3(-0.5f, 0, 0.5f));
		meshVertices.Add(new Vector3(0.5f, 0, 0.5f));
		meshVertices.Add(new Vector3(0.5f, 0, -0.5f));
		meshVertices.Add(new Vector3(-0.5f, 0, -0.5f));

		// estimate mesh triangles
		int verticeLength = meshVertices.Count;
		List<int> meshIndices = new List<int>();

		for (int i = 1; i < (verticeLength - 1); i++)
		{
			//meshVertices[i] = matTransform.MultiplyPoint3x4(meshVertices[i]);
			//meshVertices[i] = surfaceRotInv * meshVertices[i];
			//meshVertices[i] -= surfacePos;

			meshIndices.Add(0);
			meshIndices.Add(i);
			meshIndices.Add(i + 1);
		}

		GameObject overlaySurfaceObj = new GameObject();
		overlaySurfaceObj.name = "my-surface";

		GameObject overlayCubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
		overlayCubeObj.name = "my-surface-cube-";
		overlayCubeObj.transform.localScale = new Vector3(0.1f, 0.2f, 0.3f);
		overlayCubeObj.transform.SetParent(overlaySurfaceObj.transform);

		OverlaySurfaceUpdater overlaySurface = overlaySurfaceObj.AddComponent<OverlaySurfaceUpdater>();
		overlaySurface.SetSurfaceMaterial(surfaceMaterial);
		overlaySurface.SetSurfaceCollider(true, null);

		// update the surface mesh
		overlaySurface.UpdateSurfaceMesh(surfacePos, surfaceRot, meshVertices, meshIndices);

	}

}
