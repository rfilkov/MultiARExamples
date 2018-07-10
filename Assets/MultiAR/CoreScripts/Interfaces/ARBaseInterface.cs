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

}
