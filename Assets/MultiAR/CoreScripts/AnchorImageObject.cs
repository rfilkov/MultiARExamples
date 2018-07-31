using UnityEngine;


[System.Serializable]
public class AnchorImageObject //: UnityEngine.Object
{
	[Tooltip("2D-texture of the image achor.")]
	public Texture2D image;

	[Tooltip("Real width in meters of the image achor.")]
	public float width;

}
