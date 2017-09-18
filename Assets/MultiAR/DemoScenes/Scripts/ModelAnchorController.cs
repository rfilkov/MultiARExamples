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

	[Tooltip("UI-Text to show information messages.")]
	public Text infoText;


	// reference to the MultiARManager
	private MultiARManager arManager;
	// attached anchorId, or empty string
	private string anchorId;
	// internal action or not
	private bool bIntAction = false;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;

		if(infoText)
		{
			infoText.text = "Please select a model.";
		}

		// select the activity toggle at start
		if(modelActiveToggle)
		{
			modelActiveToggle.isOn = true;
		}
	}
	
	void Update () 
	{
		// check for tap
		if (Input.touchCount > 0 && arManager && arManager.IsInitialized())
		{
			// don't consoder taps over the UI
			if(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
				return;
			
			if (Input.GetTouch(0).phase == TouchPhase.Began || Input.GetTouch(0).phase == TouchPhase.Moved)
			{
				if(modelTransform)
				{
					// raycast world
					Vector2 screenPos = Input.GetTouch(0).position;

					MultiARInterop.TrackableHit hit;
					if(modelTransform.gameObject.activeInHierarchy && 
						arManager.RaycastScreenToWorld(screenPos, out hit))
					{
						// anchor the model to the hit point
						if(anchorActiveToggle.isOn && modelTransform.parent == null)
						{
							anchorId = arManager.AnchorGameObjectToWorld(modelTransform.gameObject, hit);
							SetAnchorTransformPosition();
						}

						// set the new position
						SetModelWorldPos(hit.point);
					}
				}
			}
		}

		// update the model-active and anchor-active transforms
		UpdateModelToggle(modelActiveToggle, modelTransform);
		UpdateModelToggle(anchorActiveToggle, anchorTransform);

		if(infoText)
		{
			string sMsg = (modelTransform && modelTransform.gameObject.activeSelf ? 
				modelTransform.gameObject.name + " at " + modelTransform.transform.position + ", " : string.Empty);
			sMsg += (anchorTransform && anchorTransform.gameObject.activeSelf ? 
				anchorTransform.gameObject.name + " at " + anchorTransform.transform.position + "\n" : string.Empty);
			sMsg += !string.IsNullOrEmpty(anchorId) ? "Anchor: " + anchorId : "No anchor";

			infoText.text = sMsg;
		}
	}

	// returns the model hit by the screen ray, or current model if no other was hit
	private Transform GetModelHit(Vector2 screenPos)
	{
		Camera mainCamera = arManager.GetMainCamera();

		if(mainCamera)
		{
			Ray ray = mainCamera.ScreenPointToRay(screenPos);

			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit))
			{
				if(rayHit.transform == modelTransform)
				{
					return modelTransform;
				}
			}
		}

		return null;
	}

	// sets the anchor-transform position to match the model's anchor
	private bool SetAnchorTransformPosition()
	{
		if(anchorTransform && arManager && modelTransform && modelTransform.parent && 
			modelTransform.parent.gameObject.activeInHierarchy)
		{
			Debug.Log("Attaching anchor transform to: " + anchorId);

			// activate the anchor transform if needed
			anchorId = arManager.AttachObjectToAnchor(anchorTransform.gameObject, anchorId, true, true);

			if(anchorId != string.Empty)
				anchorTransform.localPosition = Vector3.zero; // place it at anchor's position
			else
				anchorTransform.gameObject.SetActive(false); // no anchor - deactivate the transform
			
			Debug.Log("AnchorTransform set at " + anchorTransform.position);

			// set the toggle status
			if(anchorActiveToggle)
			{
				bIntAction = true;
				anchorActiveToggle.isOn = anchorTransform.gameObject.activeSelf;
				bIntAction = false;
			}

			return true;
		}

		return false;
	}

	// removes the anchor and deactivates anchor-transform
	private bool RemoveAnchorTransform()
	{
		if (!anchorTransform)
			return false;
		
		// remove the anchor
		if(anchorTransform.parent != null && anchorId != string.Empty)
		{
			Debug.Log("Detaching anchor transform from: " + anchorId);

			// remove the parent (anchor) and deactivate the anchor transform
			anchorTransform.gameObject.SetActive(false);
			anchorId = arManager.DetachObjectFromAnchor(anchorTransform.gameObject, anchorId, false, false);

			return true;
		}

		return false;
	}

	// positions the controlled model in the world
	private bool SetModelWorldPos(Vector3 vNewPos)
	{
		Camera arCamera = arManager.GetMainCamera();

		if(modelTransform && modelTransform.gameObject.activeSelf && arCamera)
		{
			// set position and look at the camera
			modelTransform.position = vNewPos;
			modelTransform.LookAt(arCamera.transform);

			// avoid rotation around x
			Vector3 objRotation = modelTransform.rotation.eulerAngles;
			modelTransform.rotation = Quaternion.Euler(0f, objRotation.y, objRotation.z);

			return true;
		}

		return false;
	}

	// update the toggle, depending on the model activity
	private void UpdateModelToggle(Toggle toggle, Transform model)
	{
		if(toggle)
		{
			if(model != null && toggle.isOn != model.gameObject.activeInHierarchy)
			{
				bIntAction = true;
				toggle.isOn = model.gameObject.activeInHierarchy;
				bIntAction = false;
			}
		}
	}

	// invoked by the model toggle
	public void ModelToggleSelected(bool bOn)
	{
		if(bIntAction)
			return;

		if(!bOn)
		{
			// remove anchor transform, if any
			RemoveAnchorTransform();
		}
		
		if(modelTransform)
		{
			// activate or deactivate model
			modelTransform.gameObject.SetActive(bOn);
		}
	}

	// invoked by the anchor
	public void AnchorToggleSelected(bool bOn)
	{
		if(bIntAction)
			return;
		
		if(anchorTransform && arManager)
		{
			if(bOn)
			{
				// activate the model, if needed
				if(!modelTransform.gameObject.activeSelf)
				{
					modelTransform.gameObject.SetActive(true);
				}

				// create the anchor at model's position
				if(modelTransform.gameObject.activeSelf && modelTransform.parent == null)
				{
					anchorId = arManager.AnchorGameObjectToWorld(modelTransform.gameObject, modelTransform.position, Quaternion.identity);
					SetAnchorTransformPosition();
				}
			}
			else
			{
				// remove the anchor and deactivate the anchor transform
				RemoveAnchorTransform();
			}
		}
	}

}
