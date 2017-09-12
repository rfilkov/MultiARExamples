using UnityEngine;

public class MultiARDirectionalLight : MonoBehaviour
{
	private Light lightComponent;
	private MultiARManager arManager;

	void Start()
	{
		lightComponent = GetComponent<Light>();
		arManager = MultiARManager.Instance;;
	}

	void Update()
	{
		if(lightComponent == null)
			return;
		
		if(arManager && arManager.applyARLight)
		{
			float intensity = arManager.GetLightIntensity();
			lightComponent.intensity = intensity;
		}
		else
		{
			if(lightComponent.intensity != 1f)
			{
				lightComponent.intensity = 1f;
			}
		}
	}

}