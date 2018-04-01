using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalController : MonoBehaviour 
{

	public Material[] materials;
	public MeshRenderer meshRenderer;

	private bool isInside = false;
	private bool isOutside = false;


	void Start () 
	{
		// the user is always outside at start
		isOutside = true;
		OutsidePortal();
	}


	// invoked when the collision is triggered
	void OnTriggerStay(Collider col)
	{
		Camera mainCamera = MultiARManager.Instance.GetMainCamera();
		Vector3 playerPos = mainCamera.transform.position + (mainCamera.transform.forward * mainCamera.nearClipPlane * 4f);

		if (transform.InverseTransformPoint(playerPos).z <= 0f) 
		{
			// go inside
			if (isOutside) 
			{
				isOutside = false;
				isInside = true;
				InsidePortal();
			}
		} 
		else 
		{
			// go outside
			if (isInside) 
			{
				isInside = false;
				isOutside = true;
				OutsidePortal();
			}
		}
	}


	// the user is outside the portal
	void OutsidePortal()
	{
		StartCoroutine(DelayChangeMat(3));
	}


	// the user is inside the portal
	void InsidePortal()
	{
		StartCoroutine(DelayChangeMat(6));
	}


	// changes stencil number of all materials
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
