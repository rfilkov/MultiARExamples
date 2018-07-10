using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


[CustomEditor(typeof(AnchorImageManager))]
public class AnchorImageManagerEditor : Editor 
{

	SerializedProperty anchorImages;


	void OnEnable()
	{
		anchorImages = serializedObject.FindProperty("anchorImages");
	}


	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(anchorImages, true);
		serializedObject.ApplyModifiedProperties();

		//EditorGUILayout.
		var buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.margin = new RectOffset(10, 10, 13, 0);

		if (GUILayout.Button ("Create Database", buttonStyle)) 
		{
			// create the image database
			int numImages = anchorImages.arraySize;
			List<Object> anchorImageObjs = new List<Object>();

			for (int i = 0; i < numImages; i++) 
			{
				SerializedProperty anchorImageElem = anchorImages.GetArrayElementAtIndex(i);
				if(anchorImageElem.objectReferenceValue != null)
					anchorImageObjs.Add(anchorImageElem.objectReferenceValue);
			}

			CreateArCoreDatabase(anchorImageObjs);
		}
	}


	// creates augmented image database for AR-Core
	private void CreateArCoreDatabase(List<Object> anchorImageObjs)
	{
		var newDatabase = ScriptableObject.CreateInstance<GoogleARCore.AugmentedImageDatabase>();

		for (int i = 0; i < anchorImageObjs.Count; i++) 
		{
			Object imageObj = anchorImageObjs[i];
			string assetPath = AssetDatabase.GetAssetPath(imageObj);

			var fileName = Path.GetFileName(assetPath);
			var imageName = fileName.Replace(Path.GetExtension(fileName), string.Empty);

			GoogleARCore.AugmentedImageDatabaseEntry newEntry = new GoogleARCore.AugmentedImageDatabaseEntry(imageName,
				                                                    AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath));
			newDatabase.Add(newEntry);
		}

		string saveDbPath = "Assets/Resources"; // Application.streamingAssetsPath;
		if (!Directory.Exists(saveDbPath))
		{
			Directory.CreateDirectory(saveDbPath);
			AssetDatabase.Refresh();
		}

		saveDbPath = Path.Combine(saveDbPath, "ArCoreImageDatabase.asset");
		if (File.Exists(saveDbPath)) 
		{
			AssetDatabase.DeleteAsset(saveDbPath);
		}
			
		//saveDbPath = AssetDatabase.GenerateUniqueAssetPath(saveDbPath);
		AssetDatabase.CreateAsset(newDatabase, saveDbPath);

		// build the database
		string sError = string.Empty;
		newDatabase.BuildIfNeeded(out sError);

		if (!string.IsNullOrEmpty(sError))
		{
			Debug.LogError(sError);
		}
	}

}
