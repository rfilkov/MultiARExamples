using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewSceneCleaner : MonoBehaviour 
{

	void Start () 
	{
		// get reference to ar-manager
		MultiARManager arManager = MultiARManager.Instance;

		if(arManager)
		{
			// destroys all anchors and anchored objects
			arManager.DestroyAllAnchors();

			// refresh scene references (graphic raycasters, etc.)
			arManager.RefreshSceneReferences();

			// destroy second main camera, if any
			Camera arCamObject = arManager.GetMainCamera();
			GameObject[] mainCamObjects = GameObject.FindGameObjectsWithTag("MainCamera");

			for(int i = mainCamObjects.Length - 1; i >= 0; i--)
			{
				GameObject mainCamObj = mainCamObjects[i];
				if(arCamObject != null && arCamObject.gameObject != mainCamObj) 
				{
					Destroy(mainCamObj);
				}
			}
		}
	}
	
}
