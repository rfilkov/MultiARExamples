using UnityEngine;


[System.Serializable]
public class JsonCameraPose 
{

	public double timestamp;

	public Vector3 location;

	public Vector3 accuracy;

	public Vector3 attitude;

	public Vector3 rotation;

	public float compHeading;

	public float trueHeading;

	public float startHeading;

	public Vector3 camPosition;

	public Vector3 camRotation;

	public JsonTrackedSurfaces surfacesOrig;

	public JsonTrackedSurfaces surfaces;

	public JsonPointCloud pointCloud;

}
