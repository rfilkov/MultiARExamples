using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;


public class MyNetConnection : NetworkConnection 
{

	private Dictionary<short, NetworkMessageDelegate> m_MessageHandlersDict = new Dictionary<short, NetworkMessageDelegate>();
	//private NetworkMessageHandlers m_MessageHandlers;

	private NetworkMessage m_MessageInfo = new NetworkMessage();
	private NetworkMessage m_NetMsg = new NetworkMessage();

	new public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
	{
		m_MessageHandlersDict[msgType] = handler;
	}


	new public void UnregisterHandler(short msgType)
	{
		if (m_MessageHandlersDict.ContainsKey(msgType)) 
		{
			m_MessageHandlersDict.Remove(msgType);
		}
	}

	new public bool CheckHandler(short msgType)
	{
		return m_MessageHandlersDict.ContainsKey(msgType);
	}

	new public bool InvokeHandlerNoData(short msgType)
	{
		return InvokeHandler(msgType, null, 0);
	}

	new public bool InvokeHandler(short msgType, NetworkReader reader, int channelId)
	{
		if (m_MessageHandlersDict.ContainsKey(msgType))
		{
			m_MessageInfo.msgType = msgType;
			m_MessageInfo.conn = this;
			m_MessageInfo.reader = reader;
			m_MessageInfo.channelId = channelId;

			NetworkMessageDelegate msgDelegate = m_MessageHandlersDict[msgType];
			if (msgDelegate == null)
			{
				if (LogFilter.logError) { Debug.LogError("NetworkConnection InvokeHandler no handler for " + msgType); }
				return false;
			}

			msgDelegate(m_MessageInfo);
			return true;
		}

		return false;
	}

	new public bool InvokeHandler(NetworkMessage netMsg)
	{
		if (m_MessageHandlersDict.ContainsKey(netMsg.msgType))
		{
			NetworkMessageDelegate msgDelegate = m_MessageHandlersDict[netMsg.msgType];
			msgDelegate(netMsg);
			return true;
		}
		return false;
	}

	public virtual void TransportReceive(byte[] bytes, int numBytes, int channelId)
	{
		HandleBytes(bytes, numBytes, channelId);
	}

	protected void HandleBytes(
		byte[] buffer,
		int receivedSize,
		int channelId)
	{
		// build the stream form the buffer passed in
		NetworkReader reader = new NetworkReader(buffer);

		HandleReader(reader, receivedSize, channelId);
	}

	new protected void HandleReader(
		NetworkReader reader,
		int receivedSize,
		int channelId)
	{
		// read until size is reached.
		// NOTE: stream.Capacity is 1300, NOT the size of the available data
		while (reader.Position < receivedSize)
		{
			// the reader passed to user code has a copy of bytes from the real stream. user code never touches the real stream.
			// this ensures it can never get out of sync if user code reads less or more than the real amount.
			ushort sz = reader.ReadUInt16();
			short msgType = reader.ReadInt16();

			// create a reader just for this message
			byte[] msgBuffer = reader.ReadBytes(sz);
			NetworkReader msgReader = new NetworkReader(msgBuffer);

			if (logNetworkMessages)
			{
				StringBuilder msg = new StringBuilder();
				for (int i = 0; i < sz; i++)
				{
					msg.AppendFormat("{0:X2}", msgBuffer[i]);
//					if (i > k_MaxMessageLogSize) break;
				}
				Debug.Log("ConnectionRecv con:" + connectionId + " bytes:" + sz + " msgId:" + msgType + " " + msg);
			}

			NetworkMessageDelegate msgDelegate = null;
			if (m_MessageHandlersDict.ContainsKey(msgType))
			{
				msgDelegate = m_MessageHandlersDict[msgType];
			}
			if (msgDelegate != null)
			{
				m_NetMsg.msgType = msgType;
				m_NetMsg.reader = msgReader;
				m_NetMsg.conn = this;
				m_NetMsg.channelId = channelId;
				msgDelegate(m_NetMsg);
				lastMessageTime = Time.time;
			}
			else
			{
				//NOTE: this throws away the rest of the buffer. Need moar error codes
				if (LogFilter.logError) { Debug.LogError("Unknown message ID " + msgType + " connId:" + connectionId); }
				break;
			}
		}
	}

}
