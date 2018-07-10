using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArClientCloudAnchorController : ArClientBaseController 
{

	// max-wait-time for network and cloud operations 
	protected const float k_MaxWaitTime = 10f;

	// set-anchor timestamp
	protected float setAnchorTillTime = 0f;

	// get-anchor timestamp
	protected float getAnchorTillTime = 0f;


	protected override void Update () 
	{
		base.Update();
		if (!clientConnected)
			return;
		
		if (setAnchorAllowed && !worldAnchorObj) 
		{
			if(statusText)
			{
				statusText.text = "Tap the floor to anchor the play area.";
			}
		}

		// check if the world anchor needs to be saved
		if (setAnchorAllowed && worldAnchorObj && 
			marManager && netClient != null) 
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

							GameObject gameAnchorGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
							gameAnchorGo.name = "GameAnchor-" + anchorId;
							Transform gameAnchorTransform = gameAnchorGo.transform;

							gameAnchorTransform.SetParent(worldAnchorObj.transform);
							gameAnchorTransform.localPosition = Vector3.zero;
							gameAnchorTransform.localRotation = Quaternion.identity;
							gameAnchorTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

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
							worldAnchorObj = null;
						}
					});
			}
		}

		// check if the world anchor needs to be restored
		if (getAnchorAllowed && !string.IsNullOrEmpty(worldAnchorId) && !worldAnchorObj && 
			marManager && netClient != null) 
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

							GameObject gameAnchorGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
							gameAnchorGo.name = "GameAnchor-" + worldAnchorId;
							Transform gameAnchorTransform = gameAnchorGo.transform;

							gameAnchorTransform.SetParent(worldAnchorObj.transform);
							gameAnchorTransform.localPosition = Vector3.zero;
							gameAnchorTransform.localRotation = Quaternion.identity;
							gameAnchorTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

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
