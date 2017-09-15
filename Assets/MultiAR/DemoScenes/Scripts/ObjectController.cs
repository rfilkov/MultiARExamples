﻿using System.Collections;
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
			toggle1.isOn = true;
		}
	}
	
	void Update () 
	{
		// check for tap
		if (Input.touchCount > 0 && arManager && arManager.IsInitialized())
		{
			if (Input.GetTouch(0).phase == TouchPhase.Began || Input.GetTouch(0).phase == TouchPhase.Moved)
			{
				// check if there is a model selected
				if(currentModel == null)
				{
					Debug.LogError("No model selected!");
					if(infoText)
					{
						infoText.text = "Please select a model.";
					}

					return;
				}

				// update the currently selected model
				Vector2 screenPos = Input.GetTouch(0).position;
				currentModel = GetModelHit(screenPos);

				MultiARInterop.TrackableHit hit;
				if(currentModel && arManager.RaycastScreenToWorld(screenPos, out hit))
				{
					// anchor the model if needed
					if(currentModel.parent == null)
					{
						arManager.AnchorGameObjectToWorld(currentModel.gameObject, hit);
					}

					// set the new position
					SetCurrentModelWorldPos(hit.point);
				}
			}
		}

		// update the info
		if(infoText)
		{
			infoText.text = currentModel ? "Selected: " + currentModel.gameObject.name : string.Empty;
		}

		// turn off the toggles, if the respective models are not active
		UpdateToggleStatus(toggle1, model1);
		UpdateToggleStatus(toggle2, model2);
		UpdateToggleStatus(toggle3, model3);
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
		}

		return currentModel;
	}

	// sets the world position of the current model
	private bool SetCurrentModelWorldPos(Vector3 vNewPos)
	{
		Camera arCamera = arManager.GetMainCamera();

		if(currentModel && arCamera)
		{
			// set position and look at the camera
			currentModel.position = vNewPos;
			currentModel.LookAt(arCamera.transform);

			// avoid rotation around x
			Vector3 objRotation = currentModel.rotation.eulerAngles;
			currentModel.rotation = Quaternion.Euler(0f, objRotation.y, objRotation.z);

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
				arManager.RemoveGameObjectAnchor(anchorId);
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
				// if it was selected, clear the selection
				currentModel = null;
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
				// if it was selected, clear the selection
				currentModel = null;
			}
		}
	}

	// invoked by the 3rd toggle
	public void Toggle3Selected(bool bOn)
	{
		if(model2)
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
				// if it was selected, clear the selection
				currentModel = null;
			}
		}
	}

}