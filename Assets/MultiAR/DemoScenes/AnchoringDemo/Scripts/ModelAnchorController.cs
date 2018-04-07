using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModelAnchorController : MonoBehaviour 
{
	[Tooltip("Transform of the controlled model.")]
	public Transform modelTransform;

	[Tooltip("Transform of the anchor object.")]
	public Transform anchorTransform;

	[Tooltip("Toggle to show if the model is active or not.")]
	public Toggle modelActiveToggle;

	[Tooltip("Toggle to show if the model is anchored or not.")]
	public Toggle anchorActiveToggle;

	[Tooltip("Whether the virtual model should rotate at the AR-camera or not.")]
	public bool modelLookingAtCamera = true;

	[Tooltip("Whether the virtual model should be vertical, or orthogonal to the surface.")]
	public bool verticalModel = false;

	[Tooltip("UI-Text to show information messages.")]
	public Text infoText;


	// reference to the MultiARManager
	private MultiARManager arManager;
	// attached anchorId, or empty string
	private string anchorId;
	// internal action or not
	private bool bIntAction = false;

	// whether the session is paused
	private bool bSessionPaused = false;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;

		// enable the model toggle at start - make the model transform visible
		if(modelTransform)
		{
			modelTransform.gameObject.SetActive(true);
			modelTransform.position = new Vector3(0f, 0f, -10f);
		}
	}
	
	void Update () 
	{
//		// don't consider taps over the UI
//		if(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
//			return;

		// check for tap
		if (arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if(action == MultiARInterop.InputAction.Click || action == MultiARInterop.InputAction.Grip)
			{
				if(modelTransform && modelTransform.gameObject.activeSelf)
				{
					// raycast world
					//Vector2 screenPos = Input.GetTouch(0).position;
					MultiARInterop.TrackableHit hit;

					if(arManager.RaycastToWorld(true, out hit))
					{
						// set model's new position
						SetModelWorldPos(hit.point, !verticalModel ? hit.rotation : Quaternion.identity);
					}
				}
			}
		}

		// check if anchorId is still valid
		if(arManager && anchorId != string.Empty && !arManager.IsValidAnchorId(anchorId))
		{
			anchorId = string.Empty;
		}

		// update the model-active and anchor-active transforms
		UpdateModelToggle(modelActiveToggle, modelTransform);
		UpdateModelToggle(anchorActiveToggle, anchorTransform);

		// update the info-text
		if(infoText)
		{
			string sMsg = string.Empty;

			if (!bSessionPaused) 
			{
				sMsg = (modelTransform && modelTransform.gameObject.activeSelf ? 
					"Model at " + modelTransform.transform.position + ", " : "No model, ");
				sMsg += (anchorTransform && anchorTransform.gameObject.activeSelf ? 
					"Anchor at " + anchorTransform.transform.position : "No anchor");
				sMsg += !string.IsNullOrEmpty(anchorId) ? "\nAnchorId: " + anchorId : string.Empty;
			}
			else
			{
				sMsg = "AR-Session is paused.";
			}

			infoText.text = sMsg;
		}
	}

	// sets the world position of the current model
	private bool SetModelWorldPos(Vector3 vNewPos, Quaternion qNewRot)
	{
		if(modelTransform)
		{
			// activate model if needed
			if(!modelTransform.gameObject.activeSelf)
			{
				modelTransform.gameObject.SetActive(true);
			}

			// set position and look at the camera
			modelTransform.position = vNewPos;
			modelTransform.rotation = qNewRot;

			if (modelLookingAtCamera) 
			{
				Camera arCamera = arManager.GetMainCamera();
				MultiARInterop.TurnObjectToCamera(modelTransform.gameObject, arCamera, modelTransform.position, modelTransform.up);
			}

			return true;
		}

		return false;
	}

	// update the toggle, depending on the model activity
	private void UpdateModelToggle(Toggle toggle, Transform model)
	{
		if(toggle)
		{
			if(model != null && toggle.isOn != model.gameObject.activeSelf)
			{
				bIntAction = true;
				toggle.isOn = model.gameObject.activeInHierarchy;
				bIntAction = false;
			}
		}
	}

	// invoked by the model-toggle
	public void ModelToggleSelected(bool bOn)
	{
		if(bIntAction || !modelTransform || !arManager)
			return;

		if(bOn)
		{
			if(anchorTransform && anchorTransform.gameObject.activeSelf)
			{
				// set model at anchor's position
				SetModelWorldPos(anchorTransform.position, !verticalModel ? anchorTransform.rotation : Quaternion.identity);
			}
			else
			{
				// raycast from center of the screen
				//Vector2 screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
				MultiARInterop.TrackableHit hit;
				if(arManager.RaycastToWorld(false, out hit))
				{
					// set model position
					SetModelWorldPos(hit.point, !verticalModel ? hit.rotation : Quaternion.identity);
				}
			}

			// attach to existing anchor, if needed
			if(arManager.IsValidAnchorId(anchorId))
			{
				arManager.AttachObjectToAnchor(modelTransform.gameObject, anchorId, false, false);
			}
		}
		else
		{
			// detach from the anchor, if needed
			if(arManager.IsValidAnchorId(anchorId))
			{
				anchorId = arManager.DetachObjectFromAnchor(modelTransform.gameObject, anchorId, false, false);
			}

			// deactivate the model if needed
			if(modelTransform.gameObject.activeSelf)
			{
				modelTransform.gameObject.SetActive(false);
			}
		}
	}

	// invoked by the anchor-toggle
	public void AnchorToggleSelected(bool bOn)
	{
		if(bIntAction || !anchorTransform || !arManager)
			return;
		
		if(bOn)
		{
			// activate the anchor transform, if needed
			if(!anchorTransform.gameObject.activeSelf)
			{
				anchorTransform.gameObject.SetActive(true);
			}

			// create the anchor if needed
			if(!arManager.IsValidAnchorId(anchorId))
			{
				if(modelTransform && modelTransform.gameObject.activeSelf)
				{
					// create the world anchor at model's position
					anchorId = arManager.AnchorGameObjectToWorld(anchorTransform.gameObject, modelTransform.position, Quaternion.identity);
				}
				else
				{
					// raycast center of the screen
					//Vector2 screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
					MultiARInterop.TrackableHit hit;
					if(arManager.RaycastToWorld(false, out hit))
					{
						// create the world anchor at hit's position
						anchorId = arManager.AnchorGameObjectToWorld(anchorTransform.gameObject, hit);
					}
				}

				// attach the model to the same anchor
				if(arManager.IsValidAnchorId(anchorId) && modelTransform && modelTransform.gameObject.activeSelf)
				{
					arManager.AttachObjectToAnchor(modelTransform.gameObject, anchorId, false, false);
				}
			}
		}
		else
		{
			// remove the anchor as needed
			if(arManager.IsValidAnchorId(anchorId))
			{
				// create the world anchor
				if(arManager.RemoveGameObjectAnchor(anchorId, true))
				{
					anchorId = string.Empty;
				}
			}

			// deactivate the anchor transform, if needed
			if(anchorTransform.gameObject.activeSelf)
			{
				anchorTransform.gameObject.SetActive(false);
			}
		}
	}

	// invoked by the pause-toggle
	public void PauseToggleSelected(bool bOn)
	{
		if (!arManager)
			return;

		if (bOn) 
		{
			bSessionPaused = arManager.PauseSession ();
		}
		else
		{
			if (bSessionPaused) 
			{
				arManager.ResumeSession();
				bSessionPaused = false;
			}
		}
	}

}
