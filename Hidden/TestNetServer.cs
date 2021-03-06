﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;


public class TestNetServer : MonoBehaviour 
{


	public bool isNetworkActive;


	// static message objects to avoid runtime-allocations
	static AddPlayerMessage s_AddPlayerMessage = new AddPlayerMessage();
	static RemovePlayerMessage s_RemovePlayerMessage = new RemovePlayerMessage();
	static ErrorMessage s_ErrorMessage = new ErrorMessage();


	private MyNetServer networkServer = null;


	void Start () 
	{
		var config = new ConnectionConfig();
		//config.AddChannel(QosType.ReliableSequenced);
		//config.AddChannel(QosType.Unreliable);
		config.AddChannel(QosType.StateUpdate);
		config.MaxSentMessageQueueSize = 2048;

		StartServer(config, 8);
	}

	void OnDestroy()
	{
		StopServer();
	}


	void Update () 
	{
		if (networkServer != null) 
		{
			networkServer.Update();
		}
	}


	bool StartServer(ConnectionConfig config, int maxConnections)
	{
//		InitializeSingleton();
//
//		OnStartServer();
		LogFilter.currentLogLevel = 0;

		//if (m_RunInBackground)
			Application.runInBackground = true;

		networkServer = GetComponent<MyNetServer>(); // new MyNetServer(); // new NetworkServerSimple();

			{
//				networkServer.useWebSockets = false;
//				HostTopology hostTopology = null;
//
//				if (config != null)
//				{
//					hostTopology = new HostTopology(config, maxConnections);
//					networkServer.Configure(hostTopology);
//				}
//
//				if (!networkServer.Listen(8888, hostTopology))
//				{
//					if (LogFilter.logError) { Debug.LogError("StartServer listen failed."); }
//					return false;
//				}
			}

		// this must be after Listen(), since that registers the default message handlers
		RegisterServerMessages();

		if (LogFilter.logDebug) { Debug.Log("NetworkManager StartServer port:" + networkServer.listenOnPort); }
		isNetworkActive = true;

//		// Only change scene if the requested online scene is not blank, and is not already loaded
//		string loadedSceneName = SceneManager.GetSceneAt(0).name;
//		if (!string.IsNullOrEmpty(m_OnlineScene) && m_OnlineScene != loadedSceneName && m_OnlineScene != m_OfflineScene)
//		{
//			ServerChangeScene(m_OnlineScene);
//		}
//		else
//		{
//			NetworkServer.SpawnObjects();
//		}
		return true;
	}


	public void StopServer()
	{
		if (networkServer == null)
			return;

//		OnStopServer();

		if (LogFilter.logDebug) { Debug.Log("NetworkManager StopServer"); }
		isNetworkActive = false;

		//NetworkServer.Shutdown();
//		networkServer.DisconnectAllConnections();
//		networkServer.Stop();



//		StopMatchMaker();
//		if (!string.IsNullOrEmpty(m_OfflineScene))
//		{
//			ServerChangeScene(m_OfflineScene);
//		}
//		CleanupNetworkIdentities();
	}


//	void CleanupNetworkIdentities()
//	{
//		foreach (NetworkIdentity netId in Resources.FindObjectsOfTypeAll<NetworkIdentity>())
//		{
//			netId..MarkForReset();
//		}
//	}


	internal void RegisterServerMessages()
	{
		networkServer.RegisterHandler(MsgType.Connect, OnServerConnectInternal);
		networkServer.RegisterHandler(MsgType.Disconnect, OnServerDisconnectInternal);
//		networkServer.RegisterHandler(MsgType.Ready, OnServerReadyMessageInternal);
//		networkServer.RegisterHandler(MsgType.AddPlayer, OnServerAddPlayerMessageInternal);
//		networkServer.RegisterHandler(MsgType.RemovePlayer, OnServerRemovePlayerMessageInternal);
		networkServer.RegisterHandler(MsgType.Error, OnServerErrorInternal);
	}


	// ----------------------------- Server Internal Message Handlers  --------------------------------

	internal void OnServerConnectInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnServerConnectInternal"); }

//		netMsg.conn.SetMaxDelay(0.01f);

//		if (m_MaxBufferedPackets != ChannelBuffer.MaxBufferedPackets)
//		{
//			for (int channelId = 0; channelId < NetworkServer.numChannels; channelId++)
//			{
//				netMsg.conn.SetChannelOption(channelId, ChannelOption.MaxPendingBuffers, m_MaxBufferedPackets);
//			}
//		}
//
//		if (!m_AllowFragmentation)
//		{
//			for (int channelId = 0; channelId < NetworkServer.numChannels; channelId++)
//			{
//				netMsg.conn.SetChannelOption(channelId, ChannelOption.AllowFragmentation, 0);
//			}
//		}
//
//		if (networkSceneName != "" && networkSceneName != m_OfflineScene)
//		{
//			StringMessage msg = new StringMessage(networkSceneName);
//			netMsg.conn.Send(MsgType.Scene, msg);
//		}
//
//		#if ENABLE_UNET_HOST_MIGRATION
//		if (m_MigrationManager != null)
//		{
//		m_MigrationManager.SendPeerInfo();
//		}
//		#endif
//		OnServerConnect(netMsg.conn);
	}

	internal void OnServerDisconnectInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnServerDisconnectInternal"); }

//		#if ENABLE_UNET_HOST_MIGRATION
//		if (m_MigrationManager != null)
//		{
//		m_MigrationManager.SendPeerInfo();
//		}
//		#endif
//		OnServerDisconnect(netMsg.conn);
	}

	internal void OnServerReadyMessageInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnServerReadyMessageInternal"); }

//		OnServerReady(netMsg.conn);
	}

	internal void OnServerAddPlayerMessageInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnServerAddPlayerMessageInternal"); }

		netMsg.ReadMessage(s_AddPlayerMessage);

//		if (s_AddPlayerMessage.msgSize != 0)
//		{
//			var reader = new NetworkReader(s_AddPlayerMessage.msgData);
//			OnServerAddPlayer(netMsg.conn, s_AddPlayerMessage.playerControllerId, reader);
//		}
//		else
//		{
//			OnServerAddPlayer(netMsg.conn, s_AddPlayerMessage.playerControllerId);
//		}
//
//		#if ENABLE_UNET_HOST_MIGRATION
//		if (m_MigrationManager != null)
//		{
//		m_MigrationManager.SendPeerInfo();
//		}
//		#endif
	}

	internal void OnServerRemovePlayerMessageInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnServerRemovePlayerMessageInternal"); }

		netMsg.ReadMessage(s_RemovePlayerMessage);

//		PlayerController player;
//		netMsg.conn.GetPlayerController(s_RemovePlayerMessage.playerControllerId, out player);
//		OnServerRemovePlayer(netMsg.conn, player);
//		netMsg.conn.RemovePlayerController(s_RemovePlayerMessage.playerControllerId);
//
//		#if ENABLE_UNET_HOST_MIGRATION
//		if (m_MigrationManager != null)
//		{
//		m_MigrationManager.SendPeerInfo();
//		}
//		#endif
	}

	internal void OnServerErrorInternal(NetworkMessage netMsg)
	{
		if (LogFilter.logDebug) { Debug.Log("NetworkManager:OnServerErrorInternal"); }

		netMsg.ReadMessage(s_ErrorMessage);
//		OnServerError(netMsg.conn, s_ErrorMessage.errorCode);
	}


	// this is invoked by the UnityEngine
	static internal void UNetStaticUpdate()
	{
//		NetworkServer.Update();
//		NetworkClient.UpdateClients();
//		NetworkManager.UpdateScene();
	}

}
