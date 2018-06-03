using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARBaseInterface : MonoBehaviour  
{

	public virtual bool AnchorGameObject(GameObject gameObj, GameObject anchorObj)
	{
		return false;
	}
	
	public virtual bool PauseSession()
	{
		return false;
	}


	public virtual void ResumeSession()
	{
	}


	public virtual void SaveWorldAnchor(GameObject gameObj, AnchorSavedDelegate anchorSaved)
	{
		if (anchorSaved != null) 
		{
			anchorSaved(string.Empty, "SaveAnchorNotSupported");
		}
	}


	public virtual void RestoreWorldAnchor(string anchorId, AnchorRestoredDelegate anchorRestored)
	{
		if (anchorRestored != null) 
		{
			anchorRestored(null, "RestoreAnchorNotSupported");
		}
	}

}
