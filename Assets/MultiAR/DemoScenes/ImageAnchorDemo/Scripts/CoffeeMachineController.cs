using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleARCore;
using System.Text;


public class CoffeeMachineController : MonoBehaviour 
{

	public List<GameObject> coffeeCupModels = new List<GameObject>();
	public List<Vector3> coffeeCupOffsets = new List<Vector3>();

	public List<string> coffeeCupTitles = new List<string>();
	public List<float> coffeeCupPrices = new List<float>();

	public RectTransform confirmationPanel;

	public Button confirmYesButton;
	public Button confirmNoButton;
	public Button confirmOkButton;

	public Text infoText;
	public Text debugText;


//	private List<AugmentedImage> allDetectedImages = new List<AugmentedImage>();
	private string imageAnchorName = string.Empty;
	private GameObject imageAnchor = null;
	private GameObject cupAnchor = null;

	private List<Transform> cupTrans = new List<Transform>();
	private List<Canvas> cupCanvas = new List<Canvas>();
	private float fTargetError = 0f;

	private Transform cupTransSelected = null;
	private int cupIndexSelected = -1;
	private float cupSelectionTime = 0f;
	private bool isProcessing = false;

	private Text confText = null;
	private string confMessage = string.Empty;


	void Start()
	{
		if (confirmationPanel) 
		{
			confText = confirmationPanel.gameObject.GetComponentInChildren<Text>();

			if (confText) 
			{
				confMessage = confText.text;
			}
		}
	}


	void Update () 
	{
		// check for image anchor, to create or destroy the coffee cups
		CheckForImageAnchor();

		// adjust the cup anchor as needed
		if (cupAnchor != null && imageAnchorName != string.Empty && imageAnchor != null) 
		{
			cupAnchor.transform.position = Vector3.Lerp(cupAnchor.transform.position, imageAnchor.transform.position, Time.deltaTime * 3f);
			//cupAnchor.transform.rotation = Quaternion.Slerp(cupAnchor.transform.rotation, imageAnchor.transform.rotation, Time.deltaTime * 3f);
		}

		// check for user interaction, to select a coffee cup
		CheckForUserInteraction();
	}


	// called when the Yes-button gets clicked
	public void ConfirmationYesButtonClicked()
	{
		// pay for the coffee and get it
		StartCoroutine(PayAndGetCoffee());
	}


	// called when the Yes-button gets clicked
	public void ConfirmationNoButtonClicked()
	{
		// hide confirmation panel
		if (confirmationPanel) 
		{
			confirmationPanel.gameObject.SetActive(false);
		}

		// clear selection
		ClearCupSelection();
	}


	// pay for and get the coffee
	private IEnumerator PayAndGetCoffee()
	{
		isProcessing = true;

		// hide confirmation panel
		if (confirmationPanel) 
		{
			confirmationPanel.gameObject.SetActive(false);
		}

		Debug.Log(string.Format("Pay now ${0:F2} and get {1}", coffeeCupPrices[cupIndexSelected], coffeeCupTitles[cupIndexSelected]));
		yield return null;

		// invoke the REST API (remove WaitForSeconds below)
		// ...

		yield return new WaitForSeconds(2f);

		// clear selection
		ClearCupSelection();

		isProcessing = false;
	}


	// checks for image anchor, to create or destroy the coffee cups
	private void CheckForImageAnchor()
	{
		MultiARManager marManager = MultiARManager.Instance;

		// Check that motion tracking is tracking.
		StringBuilder sbMessage = new StringBuilder();

		if (!marManager || marManager.GetCameraTrackingState() != MultiARInterop.CameraTrackingState.NormalTracking)
		{
			sbMessage.AppendLine("Camera - Not tracking.");
			sbMessage.AppendLine("detectedImage: " + (imageAnchorName != string.Empty ? imageAnchorName : "-"));

			ShowDebugText(sbMessage.ToString());
			return;
		}

		// Get updated augmented images for this frame.
		List<string> alImageAnchors = marManager.GetTrackedImageAnchorNames();
		sbMessage.AppendLine(alImageAnchors.Count.ToString () + " anchor images found: ");

//		for (int i = 0; i < alImageAnchors.Count; i++) 
//		{
//			sbMessage.Append(i).Append (" - ").Append (alImageAnchors[i]).AppendLine();
//		}

		sbMessage.AppendLine("detectedImage: " + (imageAnchorName != string.Empty ? imageAnchorName : "-"));
		sbMessage.AppendLine("imageAnchor: " + (imageAnchor != null ? imageAnchor.name : "none"));

		ShowDebugText(sbMessage.ToString());

		if (infoText) 
		{
			infoText.text = "Found image anchor: " + (imageAnchorName != string.Empty ? imageAnchorName : "-");

			if(cupIndexSelected >= 0)
				infoText.text = string.Format("Selected: {0}", coffeeCupTitles[cupIndexSelected]);
		}

        // check for found image anchor
        //string foundImageAnchor = marManager.GetVisibleImageAnchorName();
        string foundImageAnchor = marManager.GetNearestImageAnchorName();

        if (imageAnchorName == string.Empty)
		{
			imageAnchorName = foundImageAnchor;

			if (imageAnchorName != string.Empty)
			{
				imageAnchor = marManager.GetTrackedImageAnchorByName(imageAnchorName);
				Camera mainCamera = marManager.GetMainCamera();

				if (imageAnchor != null && mainCamera != null) 
				{
					// create coffee cups-parent (used to smooth anchor jittering)
					cupAnchor = new GameObject("CupAnchor"); // GameObject.CreatePrimitive(PrimitiveType.Cube);

					cupAnchor.transform.position = imageAnchor.transform.position;
					Vector3 vAnchorDir = imageAnchor.transform.position - mainCamera.transform.position;
					cupAnchor.transform.rotation = Quaternion.LookRotation(vAnchorDir, Vector3.up); // imageAnchor.transform.rotation;

//					// create anchor-pointer object (cube)
//					GameObject anchorPoiterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
//					anchorPoiterObj.transform.parent = imageAnchor.transform;
//
//					anchorPoiterObj.transform.localPosition = Vector3.zero;
//					anchorPoiterObj.transform.localRotation = Quaternion.identity;
//					anchorPoiterObj.transform.localScale = new Vector3(0.05f, 0.1f, 0.15f);

					// create cup models
					StartCoroutine(CreateAndPlaceCups());
				}
			}
		} 
		else if(imageAnchorName != foundImageAnchor)
		{
			// destroy the coffee cups
			DestroyCups();
		}
	}


	// checks for user interaction, to select a coffee cup
	private void CheckForUserInteraction()
	{
		// check for touch user interaction
		if (imageAnchor != null && cupTransSelected == null && !isProcessing && 
			confirmationPanel && confirmationPanel.gameObject.activeSelf == false  && 
			Time.time >= cupSelectionTime && Input.touchCount > 0) 
		{
			Touch touch = Input.GetTouch(0);

			if (Input.touchCount == 1 || touch.phase != TouchPhase.Began) 
			{
				Vector3 screenPos = new Vector3(touch.position.x, touch.position.y, 0f);
				Ray screenRay = Camera.main.ScreenPointToRay(screenPos);

				RaycastHit rayHit;
				if (Physics.Raycast(screenRay, out rayHit)) 
				{
					// check for hit
					cupIndexSelected = -1;

					for (int i = 0; i < cupTrans.Count; i++) 
					{
						if (rayHit.collider.transform == cupTrans[i]) 
						{
							// a cup is hit
							cupTransSelected = cupTrans[i];
							cupIndexSelected = i;

							Debug.Log("Selected cup index: " + i);
							break;
						}
					}
				}

				// if cup gets selected
				if (cupTransSelected != null) 
				{
					// hide non-selected cups
					for (int i = 0; i < cupTrans.Count; i++) 
					{
						if (i != cupIndexSelected) 
						{
							cupTrans[i].gameObject.SetActive(false);
							cupCanvas[i].gameObject.SetActive(false);
							//Debug.Log(cupCanvas[i].gameObject.name + "'s canvas was set non-active.");
						}
					}

					// show confirmation dialog and respective buttons
					if (confirmationPanel && confText && confirmYesButton && confirmNoButton && confirmOkButton) 
					{
						confirmationPanel.gameObject.SetActive(true);

						if (coffeeCupPrices[cupIndexSelected] > 0f) 
						{
							confText.text = string.Format(confMessage, coffeeCupPrices[cupIndexSelected], coffeeCupTitles[cupIndexSelected]);

							confirmYesButton.gameObject.SetActive(true);
							confirmNoButton.gameObject.SetActive(true);
							confirmOkButton.gameObject.SetActive(false);
						} 
						else 
						{
							// price = 0 means 'not available' 
							confText.text = string.Format("{0} is currently not available. Sorry.", coffeeCupTitles[cupIndexSelected]);;

							confirmYesButton.gameObject.SetActive(false);
							confirmNoButton.gameObject.SetActive(false);
							confirmOkButton.gameObject.SetActive(true);
						}
					}
				}
			}
		}
	}


	// creates and places the coffee cups in the scene
	private IEnumerator CreateAndPlaceCups()
	{
		cupTrans.Clear();

		for (int i = 0; i < coffeeCupModels.Count; i++) 
		{
			GameObject cupModel = coffeeCupModels[i];
			if (cupModel == null)
				continue;

			// create the cup object
			GameObject cupObj = GameObject.Instantiate(cupModel);
			cupObj.transform.SetParent(cupAnchor.transform);
			cupObj.name = cupModel.name;

			cupObj.transform.localPosition = Vector3.zero;
			cupObj.transform.localRotation = Quaternion.identity;
			//cupObj.transform.localScale = Vector3.one * 0.1f;

			cupTrans.Add(cupObj.transform);

			// disable the cup canvas
			Canvas cupCompCanvas = cupObj.GetComponentInChildren<Canvas>();
			if (cupCompCanvas) 
			{
				cupCanvas.Add(cupCompCanvas);
				cupCompCanvas.gameObject.SetActive(false);
			}
		}

		yield return null;

		// wait while the cups reach their target positions
		fTargetError = 1000f;

		while (fTargetError >= 0.2f) 
		{
			fTargetError = 0f;

			for (int i = 0; i < cupTrans.Count; i++) 
			{
				Vector3 vTargetPos = coffeeCupOffsets[i];
				cupTrans[i].localPosition = Vector3.Lerp(cupTrans[i].localPosition, vTargetPos, Time.deltaTime * 3f);
				fTargetError += Vector3.Distance(cupTrans[i].localPosition, vTargetPos);
			}

			yield return null;
		}

		// enable UI canvases
		for (int i = 0; i < cupCanvas.Count; i++) 
		{
			cupCanvas[i].gameObject.SetActive(true);
			//Debug.Log(cupCanvas[i].gameObject.name + "'s canvas was set active.");
		}
	}


	// destroys the coffee cups and the anchor
	private void DestroyCups()
	{
		// destroy the image anchor
		if (imageAnchor != null) 
		{
			GameObject.Destroy(imageAnchor.gameObject);
			imageAnchor = null;
		}

		// destroy cup anchor
		if (cupAnchor != null) 
		{
			GameObject.Destroy(cupAnchor);
			cupAnchor = null;
		}

		// clear anchor name
		imageAnchorName = string.Empty;

		cupTrans.Clear();
		cupCanvas.Clear();

		cupTransSelected = null;
		cupIndexSelected = -1;
	}


	// clear cup selection
	private void ClearCupSelection()
	{
		cupTransSelected = null;
		cupIndexSelected = -1;
		cupSelectionTime = Time.time + 0.5f;  // delay next selection time

		// enable UI canvases
		for (int i = 0; i < cupTrans.Count; i++) 
		{
			cupTrans[i].gameObject.SetActive(true);
			cupCanvas[i].gameObject.SetActive(true);
			//Debug.Log(cupCanvas[i].gameObject.name + "'s canvas was set active.");
		}
	}


	// shows debug message, if enabled
	private void ShowDebugText(string sMessage)
	{
		if (debugText) 
		{
			debugText.text = sMessage;
		}
	}

}
