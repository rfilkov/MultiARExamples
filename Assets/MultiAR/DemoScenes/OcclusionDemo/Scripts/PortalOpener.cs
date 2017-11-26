using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalOpener : MonoBehaviour 
{
	[Tooltip("The object to be placed there, where the user's input hits surface.")]
	public GameObject portalObj;

	[Tooltip("Vertical offset of the object to the hit point.")]
	public float verticalOffset = 0f;

	[Tooltip("Name of the portal opening animation.")]
	public string portalOpenAnimation = string.Empty;


	// reference to the MultiARManager
	private MultiARManager arManager;
	// referece to the portal animator
	private Animator animator;


	void Start () 
	{
		// get reference to MultiARManager
		arManager = MultiARManager.Instance;

		// get reference to the portal animator
		if (portalObj) 
		{
			animator = portalObj.GetComponent<Animator>();
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		// check for tap
		if (portalObj && arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				// activate the object if needed
				if (!portalObj.activeSelf) 
				{
					portalObj.SetActive(true);
				}

				// raycast world
				MultiARInterop.TrackableHit hit;
				if(arManager.RaycastToWorld(true, out hit))
				{
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
					objPos.y += verticalOffset;
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
