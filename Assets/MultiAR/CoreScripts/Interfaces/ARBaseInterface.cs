using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARBaseInterface : MonoBehaviour  
{

    // memory buffer management
    protected const int MemBufferLength = 65535;

    protected byte[] memBuffer = null;
    protected int memBufferOfs = 0;

	// image anchors
	protected Dictionary<string, GameObject> dictImageAnchors = new Dictionary<string, GameObject>();
	protected List<string> alImageAnchorNames = new List<string>();


	public virtual bool IsTracking()
	{
		return false;
	}

    protected void InitMemBuffer(int bufLen)
    {
        memBuffer = new byte[bufLen];
        memBufferOfs = 0;
    }

    protected void WriteMemBuffer(byte[] btData)
    {
        if (btData == null)
            throw new System.Exception("btData is null.");
        if (memBuffer == null)
            throw new System.Exception("memBuffer not initialized.");
        if ((btData.Length + memBufferOfs) > memBuffer.Length)
            throw new System.Exception("btData doesn't fit memBuffer. bufLen: " + memBuffer.Length + ", bufOfs: " + memBufferOfs + ", dataLen: " + btData.Length);

        System.Buffer.BlockCopy(btData, 0, memBuffer, memBufferOfs, btData.Length);
    }

    protected byte[] GetMemBuffer()
    {
        return memBuffer;
    }

    protected void ClearMemBuffer()
    {
        memBufferOfs = 0;
    }

    protected void FreeMemBuffer()
    {
        memBuffer = null;
        memBufferOfs = 0;
    }


	public virtual bool AnchorGameObject(GameObject gameObj, GameObject anchorObj)
	{
		return false;
	}
	
	public virtual bool PauseSession()
	{
		return false;
	}


	public virtual void ResumeSession()
	{
	}


	public virtual void SaveWorldAnchor(GameObject gameObj, AnchorSavedDelegate anchorSaved)
	{
		if (anchorSaved != null) 
		{
			anchorSaved(string.Empty, "SaveAnchorNotSupported");
		}
	}


    public virtual byte[] GetSavedAnchorData()
    {
        return memBuffer;
    }


    public virtual void SetSavedAnchorData(byte[] btData)
    {
        if(btData != null)
        {
            InitMemBuffer(btData.Length);
            WriteMemBuffer(btData);
        }
        else
        {
            memBuffer = null;
            memBufferOfs = 0;
        }
    }


    public virtual void RestoreWorldAnchor(string anchorId, AnchorRestoredDelegate anchorRestored)
	{
		if (anchorRestored != null) 
		{
			anchorRestored(null, "RestoreAnchorNotSupported");
		}
	}


	public virtual void InitImageAnchorsTracking(AnchorImageManager imageManager)
	{
	}


	public virtual void EnableImageAnchorsTracking()
	{
	}


	public virtual void DisableImageAnchorsTracking()
	{
	}


	public List<string> GetTrackedImageAnchorNames()
	{
		return alImageAnchorNames;
	}


	public string GetFirstTrackedImageAnchorName()
	{
		return alImageAnchorNames.Count > 0 ? alImageAnchorNames[0] : string.Empty;;
	}


	public GameObject GetTrackedImageAnchorByName(string imageAnchorName)
	{
		if (dictImageAnchors.ContainsKey(imageAnchorName))
			return dictImageAnchors[imageAnchorName];
		else
			return null;
	}


    /// <summary>
    /// Gets the background (reality) texture
    /// </summary>
    /// <param name="arData">AR data</param>
    /// <returns>The background texture, or null</returns>
    public virtual Texture GetBackgroundTex(MultiARInterop.MultiARData arData)
    {
        return arData != null ? arData.backgroundTex : null;
    }


    /// <summary>
    /// Sets or clears fixed background texture size
    /// </summary>
    /// <param name="arData">AR data</param>
    /// <param name="isFixedSize">Whether the background texture has fixed size</param>
    /// <param name="fixedSizeW">Fixed size width</param>
    /// <param name="fixedSizeH">Fixed size height</param>
    public void SetFixedBackTexSize(MultiARInterop.MultiARData arData, bool isFixedSize, int fixedSizeW, int fixedSizeH)
    {
        if(arData != null)
        {
            arData.isFixedBackTexSize = isFixedSize;
            arData.fixedBackTexW = fixedSizeW;
            arData.fixedBackTexH = fixedSizeH;
        }
    }


    /// <summary>
    /// Gets reference to the background render texture. Creates or recreates it, if needed.
    /// </summary>
    /// <param name="arData">AR data</param>
    /// <returns>The background render texture, or null</returns>
    protected RenderTexture GetBackgroundTexureRef(MultiARInterop.MultiARData arData)
    {
        if (arData != null)
        {
            int currentScreenW = arData.isFixedBackTexSize ? arData.fixedBackTexW : Screen.width;
            int currentScreenH = arData.isFixedBackTexSize ? arData.fixedBackTexH : Screen.height;

            if (arData.backgroundTex == null || arData.backScreenW != currentScreenW || arData.backScreenH != currentScreenH)
            {
                if(arData.backgroundTex != null)
                {
                    arData.backgroundTex.Release();
                    arData.backgroundTex = null;
                }

                arData.backScreenW = currentScreenW;
                arData.backScreenH = currentScreenH;

                arData.backgroundTex = new RenderTexture(arData.backScreenW, arData.backScreenH, 0);
                arData.backTexTime = 0.0;
            }

            return arData.backgroundTex;
        }

        return null;
    }

}
