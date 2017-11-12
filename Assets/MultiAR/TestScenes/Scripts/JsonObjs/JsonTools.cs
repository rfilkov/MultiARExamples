using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class JsonTools : MonoBehaviour 
{

	// saves camera pose, detected surfaces and point cloud
	private void SaveCameraPose(string dataFilePath)
	{
		JsonCameraPose data = new JsonCameraPose();

		try 
		{
			// save json
			string sJsonText = JsonUtility.ToJson(data);
			File.WriteAllText(dataFilePath, sJsonText);

			Debug.Log("CameraPose saved to: " + dataFilePath);
		} 
		catch (System.Exception ex) 
		{
			string sMessage = ex.Message + "\n" + ex.StackTrace;
			Debug.LogError(sMessage);
		}
	}


	// loads camera pose, detected surfaces and point cloud
	private void LoadUserData(string dataFilePath)
	{
		if(!File.Exists(dataFilePath))
			return;

		// load json
		string sJsonText = File.ReadAllText(dataFilePath);
		JsonCameraPose data = JsonUtility.FromJson<JsonCameraPose>(sJsonText);

		if (data != null) 
		{

			Debug.Log("CameraPose loaded from: " + dataFilePath);
		}
	}

}
