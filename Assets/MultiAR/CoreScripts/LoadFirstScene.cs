using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadFirstScene : MonoBehaviour 
{
	private bool levelLoaded = false;
	
	
	void Update() 
	{
		MultiARManager arManager = MultiARManager.Instance;
		ARPlatformInterface arInterface = arManager ? arManager.GetARInterface() : null;
		
		if(!levelLoaded && arManager && arManager.IsInitialized() &&
			arInterface != null && arInterface.IsInitialized())
		{
			Debug.Log("MultiARManager initialized. Loading 1st scene...");

			levelLoaded = true;
			SceneManager.LoadScene(1);
		}
	}
	
}
