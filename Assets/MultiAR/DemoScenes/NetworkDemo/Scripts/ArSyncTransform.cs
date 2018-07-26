using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;


[DisallowMultipleComponent]
public class ArSyncTransform : NetworkBehaviour
{
	[HideInInspector]
	public Transform anchorTransform;

	public float m_SendInterval = 0.1f;
	public float m_MovementTheshold = 0.001f;

	//private float m_LastClientSyncTime; // last time client received a sync from server
	private float m_LastClientSendTime; // last time client send a sync to server

	private Vector3 m_PrevPosition;
	private Quaternion m_PrevRotation;

	private const float k_LocalMovementThreshold = 0.00001f;
	private const float k_LocalRotationThreshold = 0.00001f;

	private NetworkWriter m_LocalTransformWriter;

	private ArServerController m_arServer = null;
	private ArClientBaseController m_arClient = null;

	private List<Renderer> m_renderers = new List<Renderer>();
	private bool m_transformVisible = true;


	void Awake()
	{
		m_PrevPosition = transform.position;
		m_PrevRotation = transform.rotation;

		// cache these to avoid per-frame allocations.
		if (localPlayerAuthority)
		{
			m_LocalTransformWriter = new NetworkWriter();
		}

		Renderer objRend = gameObject.GetComponent<Renderer>();
		if (objRend) 
		{
			m_renderers.Add(objRend);
		}

		Renderer[] childRends = gameObject.GetComponentsInChildren<Renderer>();
		if (childRends != null && childRends.Length > 0) 
		{
			m_renderers.AddRange(childRends);
		}
	}

	public override void OnStartServer()
	{
		//m_LastClientSyncTime = 0;
		m_arServer = FindObjectOfType<ArServerController>();
	}

	public override void OnStartClient()
	{
		m_arClient = ArClientBaseController.Instance;
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		if (initialState)
		{
			// always write initial state, no dirty bits
		}
		else if (syncVarDirtyBits == 0)
		{
			writer.WritePackedUInt32(0);
			return false;
		}
		else
		{
			// dirty bits
			writer.WritePackedUInt32(1);
		}

		SerializeTransform(writer);

		return true;
	}

	private void SerializeTransform(NetworkWriter writer)
	{
		if (anchorTransform == null) 
		{
			SetAnchorTransform();
		}

		//Vector3 locArPosition = anchorTransform ? transform.position - anchorTransform.position : transform.position;
		Vector3 locArPosition = anchorTransform ? anchorTransform.InverseTransformPoint(transform.position) : transform.position;
		Quaternion locArRotation = anchorTransform ? Quaternion.Inverse(anchorTransform.rotation) * transform.rotation : transform.rotation;

		// position
		writer.Write(locArPosition);

		// rotation
		Vector3 rotAngles = locArRotation.eulerAngles;
		writer.Write(rotAngles.x);
		writer.Write(rotAngles.y);
		writer.Write(rotAngles.z);

		// set prev values
		m_PrevPosition = transform.position;
		m_PrevRotation = transform.rotation;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (isServer && NetworkServer.localClientActive)
			return;

		if (!initialState)
		{
			if (reader.ReadPackedUInt32() == 0)
				return;
		}

		UnserializeTransform(reader, initialState);

		MakeVisible(anchorTransform != null);

		//m_LastClientSyncTime = Time.time;
	}

	private void UnserializeTransform(NetworkReader reader, bool initialState)
	{
		Vector3 locArPosition = reader.ReadVector3();
			
		Vector3 rotAngles = Vector3.zero;
		rotAngles.Set(reader.ReadSingle(),reader.ReadSingle(), reader.ReadSingle());

		Quaternion locArRotation = Quaternion.identity;
		locArRotation.eulerAngles = rotAngles;

		if (hasAuthority)
		{
			return;
		}

		if (anchorTransform == null) 
		{
			SetAnchorTransform();
		}

		// position
		//transform.position = anchorTransform ? anchorTransform.position + locArPosition : locArPosition;
		transform.position = anchorTransform ? anchorTransform.TransformPoint(locArPosition) : locArPosition;

		// rotation
		transform.rotation = anchorTransform ? anchorTransform.rotation * locArRotation : locArRotation;
	}

	private void MakeVisible(bool isVisible)
	{
		if (m_transformVisible == isVisible)
			return;

		m_transformVisible = isVisible;
		foreach (Renderer rend in m_renderers) 
		{
			rend.enabled = isVisible;
		}
	}

	private void SetAnchorTransform()
	{
		if (m_arServer) 
		{
			anchorTransform = m_arServer.GetAnchorTransform();
		}
		else if (m_arClient) 
		{
			anchorTransform = m_arClient.GetAnchorTransform();
		}
	}

	void FixedUpdate()
	{
		if (isServer)
		{
			FixedUpdateServer();
		}
	}

	void FixedUpdateServer()
	{
		if (syncVarDirtyBits != 0)
			return;

		// dont run if network isn't active
		if (!NetworkServer.active)
			return;

		// dont run if we haven't been spawned yet
		if (!isServer)
			return;

		// dont' auto-dirty if no send interval
		if (m_SendInterval == 0)
			return;

		float distance = (transform.position - m_PrevPosition).magnitude;
		if (distance < m_MovementTheshold)
		{
			distance = Quaternion.Angle(m_PrevRotation, transform.rotation);
			if (distance < m_MovementTheshold)
			{
				return;
			}
		}

		// This will cause transform to be sent
		SetDirtyBit(1);
	}

	void Update()
	{
		if (!hasAuthority)
			return;

		if (!localPlayerAuthority)
			return;

		if (NetworkServer.active)
			return;

		if ((Time.time - m_LastClientSendTime) > m_SendInterval)
		{
			SendSyncTransform();
			m_LastClientSendTime = Time.time;
		}
	}

	bool HasMoved()
	{
		float diff = 0;

		// check if position has changed
		diff = (transform.position - m_PrevPosition).magnitude;
		if (diff > k_LocalMovementThreshold)
		{
			return true;
		}

		// check if rotation has changed
		diff = Quaternion.Angle(transform.rotation, m_PrevRotation);
		if (diff > k_LocalRotationThreshold)
		{
			return true;
		}

		return false;
	}

	[Client]
	void SendSyncTransform()
	{
		if (!HasMoved() || ClientScene.readyConnection == null)
		{
			return;
		}

		m_LocalTransformWriter.StartMessage(NetMsgType.HandleSyncTransform);
		m_LocalTransformWriter.Write(netId);

		SerializeTransform(m_LocalTransformWriter);

		m_PrevPosition = transform.position;
		m_PrevRotation = transform.rotation;

		m_LocalTransformWriter.FinishMessage();
		ClientScene.readyConnection.SendWriter(m_LocalTransformWriter, GetNetworkChannel());
	}

	static public void HandleSyncTransform(NetworkMessage netMsg)
	{
		NetworkInstanceId netId = netMsg.reader.ReadNetworkId();

		GameObject foundObj = NetworkServer.FindLocalObject(netId);
		if (foundObj == null)
		{
			if (LogFilter.logError) { Debug.LogError("HandleSyncTransform - NetObject that doesn't exist"); }
			return;
		}

		ArSyncTransform foundSync = foundObj.GetComponent<ArSyncTransform>();
		if (foundSync == null)
		{
			if (LogFilter.logError) { Debug.LogError("HandleSyncTransform - ArSyncTransform component doesn't exist"); }
			return;
		}

		if (!foundSync.localPlayerAuthority)
		{
			if (LogFilter.logError) { Debug.LogError("HandleSyncTransform - No localPlayerAuthority."); }
			return;
		}

		if (netMsg.conn.clientOwnedObjects == null)
		{
			if (LogFilter.logError) { Debug.LogError("HandleSyncTransform - object not owned by the client."); }
			return;
		}

		if (netMsg.conn.clientOwnedObjects.Contains(netId))
		{
			foundSync.UnserializeTransform(netMsg.reader, false);

			//foundSync.m_LastClientSyncTime = Time.time;
			return;
		}

		if (LogFilter.logWarn) { Debug.LogWarning("HandleTransform netId:" + netId + " is not for a valid player"); }
	}

	public override int GetNetworkChannel()
	{
		return Channels.DefaultUnreliable;
	}

	public override float GetNetworkSendInterval()
	{
		return m_SendInterval;
	}

	public override void OnStartAuthority()
	{
		// must reset this timer, or the server will continue to send target position instead of current position
		//m_LastClientSyncTime = 0;
	}

}

