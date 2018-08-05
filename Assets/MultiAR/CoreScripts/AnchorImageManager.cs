using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorImageManager : MonoBehaviour 
{

	[Tooltip("List of image achors. Use CreateDatabase-button below to convert these images into proper image-anchor database (for ARCore or ARKit).")]
	public List<AnchorImageObject> anchorImages = new List<AnchorImageObject>();

	//public AnchorImageObject anchorObj;

	//public string imageDatabaseName = "ImageDatabase";

	[HideInInspector]
	public UnityEngine.Object anchorImageDb;

}
