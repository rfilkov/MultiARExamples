using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadSceneWithDelay : MonoBehaviour 
{
	[Tooltip("Seconds to wait before loading next scene.")]
	public float waitSeconds = 20f;

	[Tooltip("Next scene build number.")]
	public int nextLevel = -1;

	[Tooltip("Whether to check for initialized MultiARManager or not.")]
	public bool validateArManager = true;

	[Tooltip("UI-Text used to display the debug messages.")]
	public UnityEngine.UI.Text debugText;

	private float timeToLoadLevel = 0f;
	private bool levelLoaded = false;


	void Start()
	{
		timeToLoadLevel = Time.realtimeSinceStartup + waitSeconds;

		if(validateArManager && debugText != null)
		{
			MultiARManager arManager = MultiARManager.Instance;

			if(arManager == null || !arManager.IsInitialized())
			{
				debugText.text = "MultiARManager is not initialized!";
				levelLoaded = true;
			}

//			if (arManager) 
//			{
//				Debug.Log("MainCamera: " + arManager.GetMainCamera ());
//			}
		}
	}

	
	void Update() 
	{
		if(!levelLoaded && nextLevel >= 0)
		{
			if(Time.realtimeSinceStartup >= timeToLoadLevel)
			{
				Debug.Log("Loading scene " + nextLevel);

				levelLoaded = true;
				SceneManager.LoadScene(nextLevel);
			}
			else
			{
				float timeRest = timeToLoadLevel - Time.realtimeSinceStartup;

				if(debugText != null)
				{
					debugText.text = string.Format("Time to the next level: {0:F0} s.", timeRest);
				}
			}
		}
	}
	
}
