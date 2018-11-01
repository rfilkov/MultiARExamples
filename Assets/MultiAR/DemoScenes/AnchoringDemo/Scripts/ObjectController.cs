using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectController : MonoBehaviour 
{
	[Tooltip("Toggle for Model1.")]
	public Toggle toggle1;

	[Tooltip("Toggle for Model2.")]
	public Toggle toggle2;

	[Tooltip("Toggle for Model3.")]
	public Toggle toggle3;

	[Tooltip("Transform of Model1, if any.")]
	public Transform model1;

	[Tooltip("Transform of Model2, if any.")]
	public Transform model2;

	[Tooltip("Transform of Model3, if any.")]
	public Transform model3;

	[Tooltip("Whether the virtual model should rotate at the AR-camera or not.")]
	public bool modelLookingAtCamera = true;

	[Tooltip("Whether the virtual model should be vertical, or orthogonal to the surface.")]
	public bool verticalModel = false;

	[Tooltip("UI-Text to show information messages.")]
	public Text infoText;

	// reference to the MultiARManager
	private MultiARManager arManager;

	// currently selected model
	private Transform currentModel = null;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;

		if(infoText)
		{
			infoText.text = "Please select a model.";
		}

		// select the 1st toggle at start
		if(toggle1)
		{
			if (model1) 
			{
				model1.position = new Vector3(0f, 0f, -10f);
			}

			toggle1.isOn = true;
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

			if (action == MultiARInterop.InputAction.Click || action == MultiARInterop.InputAction.Grip)
			{
				// check if there is a model selected
				if(currentModel == null)
				{
					Debug.LogError("No model selected!");
					return;
				}

				// update the currently selected model
				currentModel = GetModelHit();

				// raycast world
				MultiARInterop.TrackableHit hit;
				if(currentModel && arManager.RaycastToWorld(true, out hit))
				{
					// anchor the model if needed
					if(currentModel.parent == null)
					{
						arManager.AnchorGameObjectToWorld(currentModel.gameObject, hit);
					}

					// set the new position of the model
					SetModelWorldPos(hit.point, !verticalModel ? hit.rotation : Quaternion.identity);
				}
			}
		}

		// update the info
		if(infoText)
		{
			infoText.text = currentModel ? "Selected: " + currentModel.gameObject.name : "No model selected";
		}

		// turn off the toggles, if the respective models are not active
		UpdateToggleStatus(toggle1, model1);
		UpdateToggleStatus(toggle2, model2);
		UpdateToggleStatus(toggle3, model3);
	}

	// returns the model hit by the input ray, or current model if no other was hit
	private Transform GetModelHit()
	{
		MultiARInterop.TrackableHit[] hits;
		if(arManager.RaycastAllToScene(true, out hits))
		{
			MultiARInterop.TrackableHit hit = hits[0];
			RaycastHit rayHit = (RaycastHit)hit.psObject;

			// check for hitting the same model
			if (currentModel != null) 
			{
				for (int i = hits.Length - 1; i >= 0; i--) 
				{
					hit = hits[i];
					rayHit = (RaycastHit)hit.psObject;

					if (rayHit.transform == currentModel) 
					{
						return currentModel;
					}
				}
			}

			// check for any of the models
			if(rayHit.transform == model1)
			{
				return model1;
			}
			else if(rayHit.transform == model2)
			{
				return model2;
			}
			else if(rayHit.transform == model3)
			{
				return model3;
			}
		}

		return currentModel;
	}

	// sets the world position of the current model
	private bool SetModelWorldPos(Vector3 vNewPos, Quaternion qNewRot)
	{
		if(currentModel)
		{
			// set position and look at the camera
			currentModel.position = vNewPos;
			currentModel.rotation = qNewRot;

			if (modelLookingAtCamera) 
			{
				Camera arCamera = arManager.GetMainCamera();
				MultiARInterop.TurnObjectToCamera(currentModel.gameObject, arCamera, currentModel.position, currentModel.up);
			}

			return true;
		}

		return false;
	}

	// removes the model anchor
	private bool RemoveModelAnchor(Transform model)
	{
		// remove the anchor
		if(model && arManager)
		{
			// get the anchorId
			string anchorId = arManager.GetObjectAnchorId(model.gameObject);

			if(anchorId != string.Empty)
			{
				arManager.RemoveGameObjectAnchor(anchorId, false);
				return true;
			}
		}

		return false;
	}

	// turn off the toggle, of model is missing or inactive
	private void UpdateToggleStatus(Toggle toggle, Transform model)
	{
		if(toggle && toggle.isOn)
		{
			if(model == null || !model.gameObject.activeSelf)
			{
				toggle.isOn = false;
			}
		}
	}

	// returns the 1st active model
	private Transform GetActiveModel()
	{
		if(model1 && model1.gameObject.activeSelf)
			return model1;
		else if(model2 && model2.gameObject.activeSelf)
			return model2;
		else if(model3 && model3.gameObject.activeSelf)
			return model3;

		// no model is currently selected
		return null;
	}

	// invoked by the 1st toggle
	public void Toggle1Selected(bool bOn)
	{
		if(model1)
		{
			if(!bOn)
			{
				// remove the world anchor
				RemoveModelAnchor(model1);
			}

			// activate or deactivate the object
			model1.gameObject.SetActive(bOn);

			if(bOn)
			{
				// make it currently selected
				currentModel = model1;
			}
			else if(currentModel == model1)
			{
				// if it was selected, reset the selection
				currentModel = GetActiveModel();
			}
		}
	}

	// invoked by the 2nd toggle
	public void Toggle2Selected(bool bOn)
	{
		if(model2)
		{
			if(!bOn)
			{
				// remove the world anchor
				RemoveModelAnchor(model2);
			}

			// activate or deactivate the object
			model2.gameObject.SetActive(bOn);

			if(bOn)
			{
				// make it currently selected
				currentModel = model2;
			}
			else if(currentModel == model2)
			{
				// if it was selected, reset the selection
				currentModel = GetActiveModel();
			}
		}
	}

	// invoked by the 3rd toggle
	public void Toggle3Selected(bool bOn)
	{
		if(model3)
		{
			if(!bOn)
			{
				// remove the world anchor
				RemoveModelAnchor(model3);
			}

			// activate or deactivate the object
			model3.gameObject.SetActive(bOn);

			if(bOn)
			{
				// make it currently selected
				currentModel = model3;
			}
			else if(currentModel == model3)
			{
				// if it was selected, reset the selection
				currentModel = GetActiveModel();
			}
		}
	}

}
