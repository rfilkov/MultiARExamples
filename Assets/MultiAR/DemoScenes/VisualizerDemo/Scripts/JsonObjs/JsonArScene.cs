using UnityEngine;


[System.Serializable]
public class JsonArScene 
{

	public int saverVer;

	public string sceneDesc;

	public double timestamp;

	public JsonScenePos scenePos;

	public JsonSceneRot sceneRot;

	public float compHeading;

	public float startHeading;

	public JsonSceneCam sceneCam;

	public JsonSurfaceSet surfaceSet;

}
