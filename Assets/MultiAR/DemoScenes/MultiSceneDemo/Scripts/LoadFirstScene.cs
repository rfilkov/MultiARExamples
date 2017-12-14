using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadFirstScene : MonoBehaviour 
{
	private bool levelLoaded = false;
	
	
	void Update() 
	{
		MultiARManager arManager = MultiARManager.Instance;
		
		if(!levelLoaded && arManager && arManager.IsInitialized())
		{
			Debug.Log("MultiARManager initialized. Loading 1st scene...");

			levelLoaded = true;
			SceneManager.LoadScene(1);
		}
	}
	
}
