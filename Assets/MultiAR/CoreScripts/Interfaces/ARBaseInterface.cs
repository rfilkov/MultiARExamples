using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARBaseInterface : MonoBehaviour  
{

	public virtual bool PauseSession()
	{
		return false;
	}

	public virtual void ResumeSession()
	{
	}

}
