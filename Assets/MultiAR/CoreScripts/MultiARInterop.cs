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
	/// Tracking state for AR frames.
	/// </summary>
	public enum CameraTrackingState : int
	{
		Unknown = -1,
		NotInitialized = 0,
		LimitedTracking = 1,
		NormalTracking = 2
	}

	/// <summary>
	/// Input action types.
	/// </summary>
	public enum InputAction : int
	{
		None = 0,
		Click = 1,
		Grip = 2,
		Release = 3,
		SpeechCommand = 11,
		CustomCommand = 19
	}

	/// <summary>
	/// Contains information about a raycast hit against a physical object
	/// </summary>
	public struct TrackableHit
	{
		public Vector3 rayPos;
		public Vector3 rayDir;

		public Vector3 point;
		public Vector3 normal;
		public float distance;

		public object psObject;
		//public Plane plane;
	}

	/// <summary>
	/// Contains information about the currently tracked surfaces
	/// </summary>
	public struct TrackedSurface
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 bounds;

		public Vector3[] points;
		public int[] triangles;
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

		// anchored objects
		public Dictionary<string, List<GameObject>> allAnchorsDict = new Dictionary<string, List<GameObject>>();
	}

	/// <summary>
	/// Maximum point count used for the point-cloud mesh
	/// </summary>
	public const int MAX_POINT_COUNT = 61440;

	/// <summary>
	/// Maximum raycast distance.
	/// </summary>
	public const float MAX_RAYCAST_DIST = 20f;

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


	public static void ShowCursor(Transform cursorTrans, TrackableHit target, float surfDistance, float defDistance, float smoothFactor)
	{
		// the forward vector is looking back
		Vector3 lookForward = -target.rayDir;

		Vector3 targetPosition = Vector3.zero;
		Quaternion targetRotation = Quaternion.identity;

		if (target.point != Vector3.zero)
		{
			targetPosition = target.point + (lookForward * surfDistance);

			Vector3 lookRotation = Vector3.Slerp(target.normal, lookForward, 0.5f);
			targetRotation = Quaternion.LookRotation((lookRotation != lookForward ? lookRotation : Vector3.zero), Vector3.up);
		}
		else
		{
			targetPosition = target.rayPos + target.rayDir * defDistance;
			targetRotation = lookForward.sqrMagnitude > 0f ? Quaternion.LookRotation(lookForward, Vector3.up) : cursorTrans.rotation;
		}

		cursorTrans.position = Vector3.Lerp(cursorTrans.position, targetPosition, smoothFactor * Time.deltaTime);
		cursorTrans.rotation = Quaternion.Lerp(cursorTrans.rotation, targetRotation, smoothFactor * Time.deltaTime);
	}


}
