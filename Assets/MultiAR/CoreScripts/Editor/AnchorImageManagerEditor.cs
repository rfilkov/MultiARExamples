using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


[CustomEditor(typeof(AnchorImageManager))]
public class AnchorImageManagerEditor : Editor 
{

	SerializedProperty anchorImages;
	//SerializedProperty anchorObj;
	SerializedProperty anchorImageDb;

	private const string SaveResourcePath = "Assets/MultiAR/Resources";
	private const string ArCoreImageDatabase = "ArCoreImageDatabase.asset";
	private const string ArKitImageDatabase = "ArKitImageDatabase.asset";


	void OnEnable()
	{
		anchorImages = serializedObject.FindProperty("anchorImages");
		//anchorObj = serializedObject.FindProperty("anchorObj");
		anchorImageDb = serializedObject.FindProperty("anchorImageDb");
	}


	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		EditorGUILayout.PropertyField(anchorImages, true);
		//EditorGUILayout.PropertyField(anchorObj);
		serializedObject.ApplyModifiedProperties();

		var buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.margin = new RectOffset(10, 10, 13, 0);

		if (GUILayout.Button ("Create Database", buttonStyle)) 
		{
			// create the image database
			int numImages = anchorImages.arraySize;
			List<SerializedProperty> anchorImageObjs = new List<SerializedProperty>();

			for (int i = 0; i < numImages; i++) 
			{
				SerializedProperty anchorImageProp = anchorImages.GetArrayElementAtIndex(i);
				//if(anchorImageProp.objectReferenceValue != null)
					anchorImageObjs.Add(anchorImageProp);
			}

			UnityEngine.Object arImageDb = CreateArImageDatabase(anchorImageObjs);
			serializedObject.Update();
			anchorImageDb.objectReferenceValue = arImageDb;
			serializedObject.ApplyModifiedProperties();

			anchorImageObjs.Clear();
		}

		string sImageDbInfo = GetArImageDatabaseInfo();
		EditorGUILayout.LabelField(sImageDbInfo);
	}


	// creates augmented image database
	private UnityEngine.Object CreateArImageDatabase(List<SerializedProperty> anchorImageProps)
	{
		if (!Directory.Exists(SaveResourcePath))
		{
			Directory.CreateDirectory(SaveResourcePath);
			AssetDatabase.Refresh();
		}

#if UNITY_ANDROID
		var imageDatabase = ScriptableObject.CreateInstance<GoogleARCore.AugmentedImageDatabase>();

		for (int i = 0; i < anchorImageProps.Count; i++) 
		{
			SerializedProperty property = anchorImageProps[i];

			SerializedProperty imageProp = property.FindPropertyRelative("image");
			SerializedProperty widthProp = property.FindPropertyRelative("width");
			if(imageProp == null || imageProp.objectReferenceValue == null)
				continue;

			Object imageObj = imageProp.objectReferenceValue;
			string assetPath = AssetDatabase.GetAssetPath(imageObj);

			var fileName = Path.GetFileName(assetPath);
			var imageName = fileName.Replace(Path.GetExtension(fileName), string.Empty);

			GoogleARCore.AugmentedImageDatabaseEntry newEntry = new GoogleARCore.AugmentedImageDatabaseEntry(imageName,
				                                                    AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath),
                                                                    widthProp.floatValue);
			imageDatabase.Add(newEntry);
		}

		string saveImageDbPath = Path.Combine(SaveResourcePath, ArCoreImageDatabase);
		if (File.Exists(saveImageDbPath)) 
		{
			AssetDatabase.DeleteAsset(saveImageDbPath);
		}
			
		//saveDbPath = AssetDatabase.GenerateUniqueAssetPath(saveDbPath);
		AssetDatabase.CreateAsset(imageDatabase, saveImageDbPath);

//		// build the database
//		string sError = string.Empty;
//		newDatabase.BuildIfNeeded(out sError);
//
//		if (!string.IsNullOrEmpty(sError))
//		{
//			Debug.LogError(sError);
//		}

		return imageDatabase;
#elif UNITY_IOS
		var imageDatabase = ScriptableObject.CreateInstance<ARReferenceImagesSet>();

		imageDatabase.resourceGroupName = "ArKitImageDatabase";
		imageDatabase.referenceImages = new ARReferenceImage[anchorImageProps.Count];

		for (int i = 0; i < anchorImageProps.Count; i++) 
		{
			SerializedProperty property = anchorImageProps[i];

			SerializedProperty imageProp = property.FindPropertyRelative("image");
			SerializedProperty widthProp = property.FindPropertyRelative("width");
			if(imageProp == null || imageProp.objectReferenceValue == null)
				continue;

			Object imageObj = imageProp.objectReferenceValue;
			Texture2D imageTex = imageObj as Texture2D;

			if(imageTex != null)
			{
				var imageRef = ScriptableObject.CreateInstance<ARReferenceImage>();
				imageRef.imageTexture = imageTex;
				imageRef.imageName = imageTex.name;
				imageRef.physicalSize = widthProp.floatValue;

				string saveImageRefPath = Path.Combine(SaveResourcePath, "ArKitImageRef" + i + ".asset");
				if (File.Exists(saveImageRefPath)) 
				{
					AssetDatabase.DeleteAsset(saveImageRefPath);
				}

				AssetDatabase.CreateAsset(imageRef, saveImageRefPath);

				imageDatabase.referenceImages[i] = AssetDatabase.LoadAssetAtPath<ARReferenceImage>(saveImageRefPath);
			}
		}

		string saveImageDbPath = Path.Combine(SaveResourcePath, ArKitImageDatabase);
		if (File.Exists(saveImageDbPath)) 
		{
			AssetDatabase.DeleteAsset(saveImageDbPath);
		}

		AssetDatabase.CreateAsset(imageDatabase, saveImageDbPath);

		return imageDatabase;
#else
		return null;
#endif
	}


	// gets augmented image database info (path & update time)
	private string GetArImageDatabaseInfo()
	{
		string imageDbPath = string.Empty;

#if UNITY_ANDROID
		imageDbPath = Path.Combine(SaveResourcePath, ArCoreImageDatabase);
#elif UNITY_IOS
		imageDbPath = Path.Combine(SaveResourcePath, ArKitImageDatabase);
#endif

		if (imageDbPath != string.Empty) 
		{
			if (File.Exists(imageDbPath)) 
			{
				return "Last updated on: " + File.GetLastWriteTime(imageDbPath) + 
					"\n" + imageDbPath;
			} 
			else 
			{
				return "Image database not created yet.\n" + imageDbPath;
			}
		}

		return "Set platform to Android or iOS!";
	}

}
