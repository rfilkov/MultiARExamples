using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Networking.NetworkSystem;

public class MyNetClient : NetworkClient 
{
	public int m_HostPort;


	private const int MaxEventsPerFrame = 500;

	private byte[] m_MsgBuffer = null;
	private NetworkReader m_MsgReader = null;

	private HostTopology m_HostTopology = null;
	private int m_ClientId = -1;
	private int m_ClientConnectionId = -1;

	private MyNetConnection m_myConnection = null;


	public MyNetClient()
		: base()
	{
		m_MsgBuffer = new byte[NetworkMessage.MaxMessageSize];
		m_MsgReader = new NetworkReader(m_MsgBuffer);
	}


	public void PrepareNetClient()
	{
		if (m_HostTopology == null)
		{
			var config = new ConnectionConfig();
			config.AddChannel(QosType.ReliableSequenced);
			config.AddChannel(QosType.Unreliable);
			config.UsePlatformSpecificProtocols = false;
			m_HostTopology = new HostTopology(config, 8);
		}

		m_ClientId = NetworkTransport.AddHost(m_HostTopology, m_HostPort);
	}


	public void UpdateNetClient()
	{
		if (m_AsyncConnect == ConnectState.None)
		{
			return;
		}

		switch (m_AsyncConnect)
		{
//		case ConnectState.None:
		case ConnectState.Resolving:
		case ConnectState.Disconnected:
			return;

		case ConnectState.Failed:
			GenerateError((int)NetworkError.DNSFailure);
			m_AsyncConnect = ConnectState.Disconnected;
			return;

		case ConnectState.Resolved:
			m_AsyncConnect = ConnectState.Connecting;
			ContinueConnect();
			return;

		case ConnectState.Connecting:
		case ConnectState.Connected:
			{
				break;
			}
		}

//		if (m_Connection != null)
//		{
//			if ((int)Time.time != m_StatResetTime)
//			{
//				m_Connection.ResetStats();
//				m_StatResetTime = (int)Time.time;
//			}
//		}

		int numEvents = 0;
		NetworkEventType networkEvent;
		do
		{
			int connectionId;
			int channelId;
			int receivedSize;
			byte error;

			networkEvent = NetworkTransport.ReceiveFromHost(m_ClientId, out connectionId, out channelId, m_MsgBuffer, (ushort)m_MsgBuffer.Length, out receivedSize, out error);
			//if (m_Connection != null) m_Connection.lastError = (NetworkError)error;

//			if (networkEvent != NetworkEventType.Nothing)
//			{
//				if (LogFilter.logDev) { Debug.Log("Client event: host=" + m_ClientId + " event=" + networkEvent + " error=" + error); }
//			}

			switch (networkEvent)
			{
			case NetworkEventType.ConnectEvent:

				if (LogFilter.logDebug) { Debug.Log("Client connected"); }

				if (error != 0)
				{
					GenerateError(error);
					return;
				}

				m_AsyncConnect = ConnectState.Connected;
				m_myConnection.InvokeHandlerNoData(MsgType.Connect);
				break;

			case NetworkEventType.DataEvent:
				if (error != 0)
				{
					GenerateError(error);
					return;
				}

//				#if UNITY_EDITOR
//				UnityEditor.NetworkDetailStats.IncrementStat(
//					UnityEditor.NetworkDetailStats.NetworkDirection.Incoming,
//					MsgType.LLAPIMsg, "msg", 1);
//				#endif

				m_MsgReader.SeekZero();
				m_myConnection.TransportReceive(m_MsgBuffer, receivedSize, channelId);
				break;

			case NetworkEventType.DisconnectEvent:
				if (LogFilter.logDebug) { Debug.Log("Client disconnected"); }

				m_AsyncConnect = ConnectState.Disconnected;

				if (error != 0)
				{
					if ((NetworkError)error != NetworkError.Timeout)
					{
						GenerateError(error);
					}
				}
//				ClientScene.HandleClientDisconnect(m_Connection);
				if (m_myConnection != null)
				{
					m_myConnection.InvokeHandlerNoData(MsgType.Disconnect);
				}
				break;

			case NetworkEventType.Nothing:
				break;

			default:
				if (LogFilter.logError) { Debug.LogError("Unknown network message type received: " + networkEvent); }
				break;
			}

			if (++numEvents >= MaxEventsPerFrame)
			{
				if (LogFilter.logDebug) { Debug.Log("EventsPerFrame got more than " + MaxEventsPerFrame); }
				break;
			}

			if (m_AsyncConnect == ConnectState.None)
			{
				break;
			}
		}
		while (networkEvent != NetworkEventType.Nothing);

		if (m_myConnection != null &&  m_AsyncConnect == ConnectState.Connected)
			m_myConnection.FlushChannels();
	}

	private void ContinueConnect()
	{
		byte error = 0;
		m_ClientConnectionId = NetworkTransport.Connect(m_ClientId, serverIp, serverPort, 0, out error);

		Type NetworkConnectionClass = typeof(MyNetConnection);
		m_myConnection = (MyNetConnection)Activator.CreateInstance(NetworkConnectionClass);
		m_Connection = m_myConnection;

		//m_Connection.SetHandlers(m_MessageHandlers);
		foreach (short msgType in handlers.Keys) 
		{
			NetworkMessageDelegate msgDelegate = handlers[msgType];
			m_myConnection.RegisterHandler(msgType, msgDelegate);
		}

		m_myConnection.Initialize(serverIp, m_ClientId, m_ClientConnectionId, m_HostTopology);
	}

	private void GenerateError(int error)
	{
		NetworkError netError = (NetworkError)error;
		if (LogFilter.logError) { Debug.LogError("Client Net Error: " + netError); }

		NetworkMessageDelegate msgDelegate = handlers.ContainsKey(MsgType.Error) ? handlers[MsgType.Error] : null;

		if (msgDelegate != null)
		{
			ErrorMessage msg = new ErrorMessage();
			msg.errorCode = error;

			// write the message to a local buffer
			byte[] errorBuffer = new byte[200];
			NetworkWriter writer = new NetworkWriter(errorBuffer);
			msg.Serialize(writer);

			// pass a reader (attached to local buffer) to handler
			NetworkReader reader = new NetworkReader(errorBuffer);

			NetworkMessage netMsg = new NetworkMessage();
			netMsg.msgType = MsgType.Error;
			netMsg.reader = reader;
			netMsg.conn = m_myConnection;
			netMsg.channelId = 0;
			msgDelegate(netMsg);
		}
	}

}
