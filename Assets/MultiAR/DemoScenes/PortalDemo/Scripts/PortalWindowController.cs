using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalWindowController : MonoBehaviour 
{
	[Tooltip("Materials used by objects inside and outside the portal.")]
	public Material[] materials;

	// reference to the portal-window mesh renderer
	private MeshRenderer meshRenderer = null;

	private bool isInside = false;
	private bool isOutside = false;


	void Start () 
	{
		meshRenderer = GetComponent<MeshRenderer>();

		// the user is always outside at start
		OutsidePortal();
	}


	// invoked when the collision is triggered
	void OnTriggerStay(Collider col)
	{
		Camera mainCamera = MultiARManager.Instance.GetMainCamera();
		Vector3 playerPos = mainCamera.transform.position + (mainCamera.transform.forward * 0.05f);

		if (transform.InverseTransformPoint(playerPos).z >= 0f) 
		{
			// go inside
			if (isOutside) 
			{
				InsidePortal();
			}
		} 
		else 
		{
			// go outside
			if (isInside) 
			{
				OutsidePortal();
			}
		}
	}


	// the user is outside the portal
	void OutsidePortal()
	{
		Debug.Log("Outside portal.");

		isInside = false;
		isOutside = true;

		StartCoroutine(DelayChangeMat(3));
	}


	// the user is inside the portal
	void InsidePortal()
	{
		Debug.Log("Inside portal.");

		isOutside = false;
		isInside = true;

		StartCoroutine(DelayChangeMat(6));
	}


	// changes stencil-parameter of all specified materials
	IEnumerator DelayChangeMat(int stencilNum)
	{

		yield return new WaitForEndOfFrame();
		meshRenderer.enabled = false;

		foreach (Material mat in materials) 
		{
			mat.SetInt("_Stencil", stencilNum);
		}

		yield return new WaitForEndOfFrame();
		meshRenderer.enabled = true;
	}
	
}
