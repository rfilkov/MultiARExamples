using UnityEngine;


[System.Serializable]
public class JsonArScene 
{

	public double timestamp;

	public Vector3 location;

	public long latm, lonm, altm;

	public Vector3 gyroAttitude;

	public Vector3 gyroRotation;

	public float startHeading;

	public Vector3 camPosition;

	public Vector3 camRotation;

	public JsonTrackedSurfaces surfaces;

}
