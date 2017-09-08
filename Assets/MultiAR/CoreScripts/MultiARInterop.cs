using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiARInterop 
{
	/// <summary>
	/// AR platform enum.
	/// </summary>
	public enum ARPlatform : int
	{
		None = 0,
		ArKit = 1,
		ArCore = 2,
		WindowsMR = 3
	}


	/// <summary>
	/// Contains information about a raycast hit against a physical object
	/// </summary>
	public struct TrackableHit
	{
		public Vector3 point;
		public float distance;

		public Plane plane;

		public string anchorId;
		public GameObject anchor;
	}

	/// <summary>
	/// Contains the most actual AR-data
	/// </summary>
	public class MultiARData
	{
		// point cloud data from the last frame
		public Vector3[] pointCloudData = new Vector3[0];
		public int pointCloudLength = 0;
		public double pointCloudTimestamp = 0.0;
	}

	/// <summary>
	/// Gets the main camera in the scene
	/// </summary>
	/// <returns>The main camera.</returns>
	public static Camera GetMainCamera()
	{
		return Camera.main;
	}


	/// <summary>
	/// Gets the directional light object in the scene
	/// </summary>
	/// <returns>The directional light.</returns>
	public static Light GetDirectionalLight()
	{
		Light[] objLights = GameObject.FindObjectsOfType<Light>();

		foreach(Light objLight in objLights)
		{
			if(objLight.type == LightType.Directional)
			{
				return objLight;
			}
		}

		return null;
	}

}
