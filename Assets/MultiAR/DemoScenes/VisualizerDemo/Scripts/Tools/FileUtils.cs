using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FileUtils : MonoBehaviour 
{

	/// <summary>
	/// Gets path to subfolder of the persitent-data directory on the device.
	/// </summary>
	/// <returns>The path to subfolder of the persitent-data directory.</returns>
	/// <param name="path">Subfolder path.</param>
	public static string GetPersitentDataPath(string path)
	{
		if (path == string.Empty) 
		{
			return Application.persistentDataPath;
		}

		string sDirPath = Application.persistentDataPath;
		string sFileName = path;

		int iLastDS = path.LastIndexOf("/");
		if (iLastDS >= 0) 
		{
			sDirPath = sDirPath + "/" + path.Substring(0, iLastDS);
			sFileName = path.Substring(iLastDS + 1);
		}

		if (!Directory.Exists(sDirPath)) 
		{
			Directory.CreateDirectory(sDirPath);
		}

		return sDirPath + "/" + sFileName;
	}

}
