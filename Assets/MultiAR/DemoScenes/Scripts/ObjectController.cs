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
				// tap
				if(currentModel == null)
				{
					Debug.LogError("No model selected!");
					if(infoText)
					{
						infoText.text = "Please select a model.";
					}

					return;
				}

				Vector2 screenPos = Input.GetTouch(0).position;
				currentModel = GetModelHit(screenPos);

				if(infoText)
				{
					infoText.text = currentModel ? currentModel.gameObject.name : string.Empty;
				}

				MultiARInterop.TrackableHit hit;
				if(currentModel && arManager.RaycastScreenToWorld(screenPos, out hit))
				{
					// check for world anchor
					if(currentModel.parent == null)
					{
						arManager.AnchorGameObjectToWorld(currentModel.gameObject, hit.point, Quaternion.identity);
					}

					// set the new position
					SetCurrentModelWorldPos(hit.point);
				}
			}
		}

		// turn off the toggles, if the respective models are missing or inactive
		UpdateToggleStatus(toggle1, model1);
		UpdateToggleStatus(toggle2, model2);
		UpdateToggleStatus(toggle3, model3);
	}

	// returns the model hit, or current model if no other was hit
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
			model1.gameObject.SetActive(bOn);

			if(bOn)
			{
				currentModel = model1;
			}
		}
	}

	// invoked by the 2nd toggle
	public void Toggle2Selected(bool bOn)
	{
		if(model2)
		{
			model2.gameObject.SetActive(bOn);

			if(bOn)
			{
				currentModel = model2;
			}
		}
	}

	// invoked by the 3rd toggle
	public void Toggle3Selected(bool bOn)
	{
		if(model2)
		{
			model3.gameObject.SetActive(bOn);

			if(bOn)
			{
				currentModel = model3;
			}
		}
	}

}
