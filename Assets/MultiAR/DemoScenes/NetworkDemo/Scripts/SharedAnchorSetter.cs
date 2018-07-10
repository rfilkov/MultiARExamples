using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharedAnchorSetter : MonoBehaviour 
{
	// reference to MultiARManager
	private MultiARManager arManager;
	private ArClientBaseController arClient;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;
		arClient = ArClientBaseController.Instance;
	}
	
	void Update () 
	{
		if (!arClient)
			return;
		
		// check for click
		if (arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true) &&
			arClient && arClient.IsSetAnchorAllowed() && arClient.WorldAnchorObj == null)
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				MultiARInterop.TrackableHit hit;

				if(arManager.RaycastToWorld(true, out hit))
				{
					// instantiate the world anchor object
					GameObject worldAnchor = new GameObject("WorldAnchor");
					worldAnchor.transform.position = hit.point;
					worldAnchor.transform.rotation = hit.rotation;  // Quaternion.identity

					arManager.AnchorGameObjectToWorld(worldAnchor, hit);

					arClient.WorldAnchorObj = worldAnchor;
				}
			}
		}

	}


}
