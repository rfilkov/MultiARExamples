using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArClientCloudAnchorController : ArClientBaseController 
{

	[Tooltip("Material used to mark the cloud anchor")]
	public Material cloudAnchorMaterial;

	// max-wait-time for network and cloud operations 
	protected const float k_MaxWaitTime = 10f;

	// set-anchor timestamp
	protected float setAnchorTillTime = 0f;

	// get-anchor timestamp
	protected float getAnchorTillTime = 0f;

	// shared game anchor mark (the cube)
	private GameObject gameAnchorGo = null;


	protected override void Update () 
	{
		base.Update();
		if (netClient == null || !clientConnected)
			return;
		if (marManager == null || !marManager.IsTracking())
			return;
		
		if (setAnchorAllowed && !worldAnchorObj) 
		{
			if(statusText)
			{
				statusText.text = "Tap the floor to anchor the play area.";
			}
		}

		// if there is no world anchor set yet, check for click
		if (setAnchorAllowed && worldAnchorObj == null && marManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = marManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				MultiARInterop.TrackableHit hit;

				if(marManager.RaycastToWorld(true, out hit))
				{
					// create the world anchor object
					worldAnchorObj = new GameObject("SharedWorldAnchor");
					worldAnchorObj.transform.position = hit.point;
					worldAnchorObj.transform.rotation = hit.rotation;  // Quaternion.identity

					marManager.AnchorGameObjectToWorld(worldAnchorObj, hit);

					gameAnchorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
					gameAnchorGo.name = "GameAnchor";

					Transform gameAnchorTransform = gameAnchorGo.transform;
					gameAnchorTransform.SetParent(worldAnchorObj.transform);

					gameAnchorTransform.localPosition = Vector3.zero;
					gameAnchorTransform.localRotation = Quaternion.identity;
					gameAnchorTransform.localScale = new Vector3(0.1f, 0.2f, 0.3f);
				}
			}
		}

		// check if the world anchor needs to be saved
		if (setAnchorAllowed && worldAnchorObj != null) 
		{
			if (setAnchorTillTime < Time.realtimeSinceStartup) 
			{
				setAnchorTillTime = Time.realtimeSinceStartup + k_MaxWaitTime;
				setAnchorAllowed = false;

				LogMessage("Saving world anchor...");

				marManager.SaveWorldAnchor(worldAnchorObj, (anchorId, errorMessage) => 
					{
						worldAnchorId = anchorId;

						if(string.IsNullOrEmpty(errorMessage))
						{
							LogMessage("World anchor saved: " + anchorId);

							if(gameAnchorGo != null)
							{
								gameAnchorGo.name = "GameAnchor-" + anchorId;
								Renderer renderer = gameAnchorGo.GetComponent<Renderer>();

								if(renderer != null && cloudAnchorMaterial != null)
								{
									renderer.material = cloudAnchorMaterial;
								}
							}

							if(!string.IsNullOrEmpty(anchorId))
							{
								SetGameAnchorRequestMsg request = new SetGameAnchorRequestMsg
								{
									gameName = this.gameName,
									anchorId = worldAnchorId,
									anchorPos = worldAnchorObj.transform.position,
									anchorRot = worldAnchorObj.transform.rotation,
									anchorData = marManager.GetSavedAnchorData()
								};

								netClient.Send(NetMsgType.SetGameAnchorRequest, request);

								if(statusText)
								{
									statusText.text = "Tap to shoot.";
								}
							}
						}
						else
						{
							LogErrorMessage("Error saving world anchor: " + errorMessage);

							// allow new world anchor setting
							worldAnchorId = string.Empty;

							Destroy(worldAnchorObj);
							worldAnchorObj = null;
						}
					});
			}
		}

		// check if the world anchor needs to be restored
		if (getAnchorAllowed && !string.IsNullOrEmpty(worldAnchorId) && worldAnchorObj == null) 
		{
			if (getAnchorTillTime < Time.realtimeSinceStartup) 
			{
				getAnchorTillTime = Time.realtimeSinceStartup + k_MaxWaitTime;
				getAnchorAllowed = false;

				LogMessage("Restoring world anchor...");

				marManager.SetSavedAnchorData(worldAnchorData);
				marManager.RestoreWorldAnchor(worldAnchorId, (anchorObj, errorMessage) =>
					{
						worldAnchorObj = anchorObj;

						if(string.IsNullOrEmpty(errorMessage))
						{
							LogMessage("World anchor restored: " + worldAnchorId);

							gameAnchorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
							gameAnchorGo.name = "GameAnchor-" + worldAnchorId;

							Transform gameAnchorTransform = gameAnchorGo.transform;
							gameAnchorTransform.SetParent(worldAnchorObj.transform);

							gameAnchorTransform.localPosition = Vector3.zero;
							gameAnchorTransform.localRotation = Quaternion.identity;
							gameAnchorTransform.localScale = new Vector3(0.1f, 0.2f, 0.3f);

							Renderer renderer = gameAnchorGo.GetComponent<Renderer>();
							if(renderer != null && cloudAnchorMaterial != null)
							{
								renderer.material = cloudAnchorMaterial;
							}

							if(statusText)
							{
								statusText.text = "Tap to shoot.";
							}
						}
						else
						{
							LogErrorMessage("Error restoring world anchor: " + errorMessage);

							// send Get-game-anchor
							GetGameAnchorRequestMsg request = new GetGameAnchorRequestMsg
							{
								gameName = this.gameName
							};

							netClient.Send(NetMsgType.GetGameAnchorRequest, request);
						}
					});
			}
		}
	}

}
