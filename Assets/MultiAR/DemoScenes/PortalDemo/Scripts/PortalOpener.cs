using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalOpener : MonoBehaviour 
{
	[Tooltip("The portal prefab.")]
	public GameObject portalPrefab;

	[Tooltip("Whether the portal should be vertical, or orthogonal to the surface.")]
	public bool verticalPortal = false;

	[Tooltip("Vertical offset of the object to the hit point.")]
	public float portalVerticalOffset = 0f;

	[Tooltip("Name of the portal opening animation.")]
	public string portalOpenAnimation = string.Empty;


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

					// get reference to the portal animator
					if (!animator) 
					{
						animator = portalObj.GetComponent<Animator>();
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
					Vector3 objPos = portalObj.transform.position;
					//objPos.y += verticalOffset;
					objPos += portalObj.transform.up * portalVerticalOffset;
					portalObj.transform.position = objPos;

					// play portal-open animation
					if (animator && portalOpenAnimation != string.Empty) 
					{
						animator.Play(portalOpenAnimation, 0, 0f);
					}
				}
			}

		}
	}

}
