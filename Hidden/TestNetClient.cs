using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;


public class TestNetClient : MonoBehaviour 
{

	public string m_NetworkAddress = "localhost";
	public int m_NetworkPort = 8888;
	public int m_HostPort = 8887;

	public bool isNetworkActive;
	public MyNetClient client;

	private ErrorMessage s_ErrorMessage = new ErrorMessage();


	void Start () 
	{
		StartClient(null, m_HostPort);
	}
	

	void OnDestroy()
	{
		StopClient();
	}


	void Update () 
	{
		if (client != null) 
		{
			client.UpdateNetClient();
		}
	}



	public NetworkClient StartClient(ConnectionConfig config, int hostPort)
	{
		LogFilter.currentLogLevel = 0;

//		InitializeSingleton();
//
//		matchInfo = info;
//		if (m_RunInBackground)
			Application.runInBackground = true;

		isNetworkActive = true;

//		if (m_GlobalConfig != null)
//		{
//			NetworkTransport.Init(m_GlobalConfig);
//		}

		client = new MyNetClient();
		client.hostPort = hostPort;

//		config = new ConnectionConfig();
//		byte clientChannelId = config.AddChannel(QosType.StateUpdate);  // QosType.UnreliableFragmented
//
//		// add client host
//		HostTopology topology = new HostTopology(config, 1);
//		client.Configure(topology);

//		if (config != null)
//		{
//			if ((config.UsePlatformSpecificProtocols) && (UnityEngine.Application.platform != RuntimePlatform.PS4) && (UnityEngine.Application.platform != RuntimePlatform.PSP2))
//				throw new System.ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
//
//			client.Configure(config, 1);
//		}

//		else
//		{
//			if (m_CustomConfig && m_ConnectionConfig != null)
//			{
//				m_ConnectionConfig.Channels.Clear();
//				for (int i = 0; i < m_Channels.Count; i++)
//				{
//					m_ConnectionConfig.AddChannel(m_Channels[i]);
//				}
//				if ((m_ConnectionConfig.UsePlatformSpecificProtocols) && (UnityEngine.Application.platform != RuntimePlatform.PS4) && (UnityEngine.Application.platform != RuntimePlatform.PSP2))
//					throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
//				client.Configure(m_ConnectionConfig, m_MaxConnections);
//			}
//		}

		RegisterClientMessages(client);

//		if (matchInfo != null)
//		{
//			if (LogFilter.logDebug) { Debug.Log("NetworkManager StartClient match: " + matchInfo); }
//			client.Connect(matchInfo);
//		}
//		else if (m_EndPoint != null)
//		{
//			if (LogFilter.logDebug) { Debug.Log("NetworkManager StartClient using provided SecureTunnel"); }
//			client.Connect(m_EndPoint);
//		}
//		else
		{
			if (string.IsNullOrEmpty(m_NetworkAddress))
			{
				if (LogFilter.logError) { Debug.LogError("Must set the Network Address field in the manager"); }
				return null;
			}
			if (LogFilter.logDebug) { Debug.Log("NetworkManager StartClient address:" + m_NetworkAddress + " port:" + m_NetworkPort); }

//			if (m_UseSimulator)
//			{
//				client.ConnectWithSimulator(m_NetworkAddress, m_NetworkPort, m_SimulatedLatency, m_PacketLossPercentage);
//			}
//			else
			{
				client.Connect(m_NetworkAddress, m_NetworkPort);
				client.PrepareNetClient();
			}
		}

//		OnStartClient(client);
//		s_Address = m_NetworkAddress;

		return client;
	}


	public void StopClient()
	{
//		OnStopClient();

		if (LogFilter.logDebug) { Debug.Log("NetworkManager StopClient"); }
		isNetworkActive = false;
		if (client != null)
		{
			// only shutdown this client, not ALL clients.
			client.Disconnect();
			client.Shutdown();
			client = null;
		}

//		StopMatchMaker();

		ClientScene.DestroyAllClientObjects();

//		if (!string.IsNullOrEmpty(m_OfflineScene))
//		{
//			ClientChangeScene(m_OfflineScene, false);
//		}
//		CleanupNetworkIdentities();
	}


	internal void RegisterClientMessages(NetworkClient client)
	{
		client.RegisterHandler(MsgType.Connect, OnClientConnectInternal);
		client.RegisterHandler(MsgType.Disconnect, OnClientDisconnectInternal);
//		client.RegisterHandler(MsgType.NotReady, OnClientNotReadyMessageInternal);
		client.RegisterHandler(MsgType.Error, OnClientErrorInternal);
//		client.RegisterHandler(MsgType.Scene, OnClientSceneInternal);

//		if (m_PlayerPrefab != null)
//		{
//			ClientScene.RegisterPrefab(m_PlayerPrefab);
//		}
//		for (int i = 0; i < m_SpawnPrefabs.Count; i++)
//		{
//			var prefab = m_SpawnPrefabs[i];
//			if (prefab != null)
//			{
//				ClientScene.RegisterPrefab(prefab);
//			}
//		}
	}


	// ----------------------------- Client Internal Message Handlers  --------------------------------

	internal void OnClientConnectInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnClientConnectInternal"); }

//		netMsg.conn.SetMaxDelay(0.01f);

//		string loadedSceneName = SceneManager.GetSceneAt(0).name;
//		if (string.IsNullOrEmpty(m_OnlineScene) || (m_OnlineScene == m_OfflineScene) || (loadedSceneName == m_OnlineScene))
//		{
//			m_ClientLoadedScene = false;
//			OnClientConnect(netMsg.conn);
//		}
//		else
//		{
//			// will wait for scene id to come from the server.
//			s_ClientReadyConnection = netMsg.conn;
//		}
	}

	internal void OnClientDisconnectInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnClientDisconnectInternal"); }

//		if (!string.IsNullOrEmpty(m_OfflineScene))
//		{
//			ClientChangeScene(m_OfflineScene, false);
//		}
//
//		// If we have a valid connection here drop the client in the matchmaker before shutting down below
//		if (matchMaker != null && matchInfo != null && matchInfo.networkId != NetworkID.Invalid && matchInfo.nodeId != NodeID.Invalid)
//		{
//			matchMaker.DropConnection(matchInfo.networkId, matchInfo.nodeId, matchInfo.domain, OnDropConnection);
//		}
//
//		OnClientDisconnect(netMsg.conn);
	}

	internal void OnClientNotReadyMessageInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnClientNotReadyMessageInternal"); }

//		ClientScene.SetNotReady();
//		OnClientNotReady(netMsg.conn);

		// NOTE: s_ClientReadyConnection is not set here! don't want OnClientConnect to be invoked again after scene changes.
	}

	internal void OnClientErrorInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnClientErrorInternal"); }

		netMsg.ReadMessage(s_ErrorMessage);
//		OnClientError(netMsg.conn, s_ErrorMessage.errorCode);
	}

	internal void OnClientSceneInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnClientSceneInternal"); }

//		string newSceneName = netMsg.reader.ReadString();

//		if (IsClientConnected() && !NetworkServer.active)
//		{
//			ClientChangeScene(newSceneName, true);
//		}
	}

}
