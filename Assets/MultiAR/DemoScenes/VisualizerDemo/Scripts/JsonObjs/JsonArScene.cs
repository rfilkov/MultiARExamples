using UnityEngine;


[System.Serializable]
public class JsonArScene 
{

	public int saverVer;

	public string sceneDesc;

	public double timestamp;

	public bool locEnabled;

	public Vector3 location;

	public long latm, lonm, altm;

	public bool gyroEnabled;

	public Vector3 gyroAttitude;

	public Vector3 gyroRotation;

	public float startHeading;

	public Vector3 camPosition;

	public Vector3 camRotation;

	public JsonSurfaceSet surfaceSet;

}
