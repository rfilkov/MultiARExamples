using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalOpener : MonoBehaviour 
{
	[Tooltip("The portal prefab.")]
	public GameObject portalPrefab;

	[Tooltip("Whether the portal should be vertical, or orthogonal to the surface.")]
	public bool verticalPortal = false;

	[Tooltip("Whether the portal should rotate at the AR-camera or not.")]
	public bool portalLookingAtCamera = false;

	[Tooltip("Vertical offset of the portal object to the hit point.")]
	public float verticalOffset = 0f;

	[Tooltip("Name of the animation to be played, when the portal is created.")]
	public string playAnimation = string.Empty;

	[Tooltip("Camera tigger box-collider dimensions. If left to zero, no box-collider will be created.")]
	public Vector3 cameraBoxCollider = Vector3.zero;



	// reference to the MultiARManager
	private MultiARManager arManager;

	// referece to the portal object and animator
	private GameObject portalObj;
	private Animator animator;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;
	}
	
	// Update is called once per frame
	void Update () 
	{
		// check for tap
		if (portalPrefab && arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				// raycast to world
				MultiARInterop.TrackableHit hit;
				if(arManager.RaycastToWorld(true, out hit))
				{
					// create the portal object, if needed
					if (!portalObj) 
					{
						portalObj = Instantiate(portalPrefab);
					}

					// set its position and rotation
					portalObj.transform.position = hit.point;
					portalObj.transform.rotation = !verticalPortal ? hit.rotation : Quaternion.identity;

					// look at the camera
					if(portalLookingAtCamera)
					{
						Camera arCamera = arManager.GetMainCamera();
						MultiARInterop.TurnObjectToCamera(portalObj, arCamera, hit.point, hit.normal);
					}

					// remove object anchor, if it was anchored before
					string anchorId = arManager.GetObjectAnchorId(portalObj);
					if (anchorId != string.Empty) 
					{
						arManager.RemoveGameObjectAnchor(anchorId, true);
					}

					// anchor it to the new world position
					arManager.AnchorGameObjectToWorld(portalObj, hit);

					// apply the vertical offset
					if (verticalOffset != 0f) 
					{
						Vector3 objPos = portalObj.transform.position;
						//objPos.y += verticalOffset;
						objPos += portalObj.transform.up * verticalOffset;
						portalObj.transform.position = objPos;
					}

					// play portal-open animation
					if (playAnimation != string.Empty) 
					{
						// get reference to the portal animator
						if (!animator) 
						{
							animator = portalObj.GetComponent<Animator>();
						}

						if (animator) 
						{
							animator.Play(playAnimation, 0, 0f);
						}
					}

					// create camera rigidbody (no gravity) & box-collider, if needed
					if (cameraBoxCollider != Vector3.zero) 
					{
						Camera arCamera = arManager.GetMainCamera();

						Rigidbody camRigidbody = arCamera.gameObject.GetComponent<Rigidbody>();
						if (camRigidbody == null) 
						{
							camRigidbody = arCamera.gameObject.AddComponent<Rigidbody>();
							camRigidbody.useGravity = false;
						}

						BoxCollider camBoxCollider = arCamera.gameObject.GetComponent<BoxCollider>();
						if (camBoxCollider == null) 
						{
							camBoxCollider = arCamera.gameObject.AddComponent<BoxCollider>();
							camBoxCollider.size = cameraBoxCollider;
							camBoxCollider.isTrigger = true;
						}
					}

				}
			}

		}
	}

}
