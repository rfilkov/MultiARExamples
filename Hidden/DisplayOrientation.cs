using GoogleARCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayOrientation : MonoBehaviour
{

    public Text orientInfoText;

    public RawImage backTextureImage;

    private GoogleARCore.ARCoreBackgroundRenderer arCodeRenderer = null;
    private Material backgroundMat = null;

    private RenderTexture backRT = null;


    // Use this for initialization
    void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
		if(orientInfoText)
        {
            orientInfoText.text = Screen.orientation.ToString();
        }

        if (backgroundMat == null)
        {
            arCodeRenderer = FindObjectOfType<GoogleARCore.ARCoreBackgroundRenderer>();

            if (arCodeRenderer)
            {
                backgroundMat = arCodeRenderer.BackgroundMaterial;
            }
        }

        if (backRT == null && backTextureImage)
        {
            RectTransform imageRect = backTextureImage.rectTransform;
            backRT = new RenderTexture((int)imageRect.rect.width, (int)imageRect.rect.height, 0);
        }

        if (backgroundMat && backRT)
        {
            Graphics.Blit(null, backRT, backgroundMat);
        }

        if (backTextureImage)
        {
            //backTextureImage.texture = Frame.CameraImage.Texture;
            backTextureImage.texture = backRT;
        }
    }

}
