using System.Diagnostics.CodeAnalysis;
using GoogleARCore;
using UnityEditor;
using UnityEditor.Build;


public class AugmentedImageDatabasePreprocessBuild : IPreprocessBuild
{
    public int callbackOrder
    {
        get
        {
            return 0;
        }
    }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
		var refImageGuids = AssetDatabase.FindAssets("t:ARReferenceImage");
		foreach (var refImageGuid in refImageGuids)
        {
			var refImage = AssetDatabase.LoadAssetAtPath<ARReferenceImage>(
				AssetDatabase.GUIDToAssetPath(refImageGuid));
			EditorUtility.SetDirty(refImage);
        }

		AssetDatabase.SaveAssets();

		var refImageSetGuids = AssetDatabase.FindAssets("t:ARReferenceImagesSet");
		foreach (var refImageSetGuid in refImageSetGuids)
		{
			var refImageSet = AssetDatabase.LoadAssetAtPath<ARReferenceImagesSet>(
				AssetDatabase.GUIDToAssetPath(refImageSetGuid));
			EditorUtility.SetDirty(refImageSet);
		}

		AssetDatabase.SaveAssets();
	}
}

