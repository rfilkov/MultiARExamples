using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;


public class ArServerController : MonoBehaviour 
{
	[Tooltip("The name of the AR-game (used by client-server and broadcast messages).")]
	public string gameName = "ArGame";

	[Tooltip("Port to be used for incoming connections.")]
	public int listenOnPort = 8888;

	[Tooltip("Port used for server broadcast discovery.")]
	public int broadcastPort = 8889;

	[Tooltip("Maximum number of allowed connections.")]
	public int maxConnections = 8;

	[Tooltip("Whether the server should use websockets or not.")]
	public bool useWebSockets = false;

	[Tooltip("UI-Text to display connection status messages.")]
	public Text connStatusText;

	[Tooltip("UI-Text to display server status messages.")]
	public Text serverStatusText;

	[Tooltip("UI-Text to display server console.")]
	public Text consoleMessages;


	// network broadcast component
	private NetworkDiscovery netDiscovery = null;

	// api key needed for cloud anchor hosting or resolving
	//private string cloudApiKey = string.Empty;

	// cloud anchor Id
	private string gameCloudAnchorId = string.Empty;
	private Transform gameAnchorTransform = null;
	private float gameAnchorTimestamp = 0f;

	// id of the hosting-requesting cloud-anchor client 
	private int hostingClientId = -1;
	private float hostingClientTimestamp = 0f;

	// client transforms
	private Dictionary<int, NetClientData> dictClientTrans = new Dictionary<int, NetClientData>();


	void Start () 
	{
		try 
		{
			LogFilter.currentLogLevel = 0; // dev // LogFilter.Debug;

			// configure the network server
			var config = new ConnectionConfig();
			config.AddChannel(QosType.ReliableSequenced);
			config.AddChannel(QosType.Unreliable);

			NetworkServer.Configure(config, maxConnections);
			NetworkServer.useWebSockets = useWebSockets;

			// start the server and register handlers
			string serverHost = Network.player.ipAddress;
			NetworkServer.Listen(listenOnPort);

			NetworkServer.RegisterHandler(MsgType.Error, OnNetworkError);
			NetworkServer.RegisterHandler(MsgType.Connect, OnServerConnect);
			NetworkServer.RegisterHandler(MsgType.Disconnect, OnServerDisconnect);

			NetworkServer.RegisterHandler(NetMsgType.GetGameAnchorRequest, OnGetGameAnchorRequest);
			NetworkServer.RegisterHandler(NetMsgType.CheckHostAnchorRequest, OnCheckHostAnchorRequest);
			NetworkServer.RegisterHandler(NetMsgType.SetGameAnchorRequest, OnSetGameAnchorRequest);
			NetworkServer.RegisterHandler(NetMsgType.SetClientPoseRequest, OnSetClientPoseRequest);

			// get the broadcasting component
			netDiscovery = GetComponent<NetworkDiscovery>();

			if(netDiscovery != null)
			{
				netDiscovery.broadcastPort = broadcastPort;
				netDiscovery.broadcastKey = listenOnPort;
				netDiscovery.broadcastData = gameName;

				netDiscovery.Initialize();
				netDiscovery.StartAsServer();
			}

			string sMessage = gameName + "-Server started on " + serverHost + ":" + listenOnPort;
			Debug.Log(sMessage);

			if(serverStatusText)
			{
				serverStatusText.text = sMessage;
			}

		} 
		catch (System.Exception ex) 
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);

			if(serverStatusText)
			{
				serverStatusText.text = ex.Message;
			}
		}
	}


	void OnDestroy()
	{
		// clear client data
		dictClientTrans.Clear();

		// shutdown the server and disconnect all clients
		NetworkServer.Shutdown();

		if (netDiscovery) 
		{
			netDiscovery.StopBroadcast();
		}
	}


	void Update () 
	{
		
	}


	// handles network error message
	void OnNetworkError(NetworkMessage netMsg)
	{
		var errorMsg = netMsg.ReadMessage<ErrorMessage>();
		int connId = netMsg.conn.connectionId;

		LogErrorToConsole("NetError " + connId + " detected: " + (NetworkError)errorMsg.errorCode);
	}


	// handles Connect-message
	private void OnServerConnect(NetworkMessage netMsg)
	{
		int connId = netMsg.conn.connectionId;
		bool connFound = dictClientTrans.ContainsKey(connId);

		NetClientData clientData = new NetClientData();
		clientData.ipAddress = netMsg.conn.address;
		clientData.timestamp = Time.realtimeSinceStartup;
		clientData.transform = null;

		dictClientTrans [connId] = clientData;

		LogToConsole((!connFound ? "Connected" : "Reconnected") + " client " + connId + " IP: " + netMsg.conn.address);
	}


	// handles Disconnect-message
	private void OnServerDisconnect(NetworkMessage netMsg)
	{
		int connId = netMsg.conn.connectionId;

		if (dictClientTrans.ContainsKey(connId)) 
		{
			dictClientTrans.Remove(connId);
		}

		LogToConsole("Disconnected client " + connId + " IP: " + netMsg.conn.address);
	}


	// handles GetGameAnchorRequestMsg
	private void OnGetGameAnchorRequest(NetworkMessage netMsg)
	{
		var request = netMsg.ReadMessage<GetGameAnchorRequestMsg>();
		if (request == null || request.gameName != gameName)
			return;

		GetGameAnchorResponseMsg response = new GetGameAnchorResponseMsg
		{
			found = !string.IsNullOrEmpty(gameCloudAnchorId),
			anchorId = gameCloudAnchorId,
			//apiKey = cloudApiKey
		};

		NetworkServer.SendToClient(netMsg.conn.connectionId, NetMsgType.GetGameAnchorResponse, response);

		int connId = netMsg.conn.connectionId;
		LogToConsole("GetGameAnchor received from client " + connId);
	}


	// handles CheckHostAnchorRequest
	private void OnCheckHostAnchorRequest(NetworkMessage netMsg)
	{
		var request = netMsg.ReadMessage<CheckHostAnchorRequestMsg>();
		if (request == null || request.gameName != gameName)
			return;

		bool requestGranted = string.IsNullOrEmpty(gameCloudAnchorId) && hostingClientId < 0;
		if (requestGranted) 
		{
			hostingClientId = netMsg.conn.connectionId;
			hostingClientTimestamp = Time.realtimeSinceStartup;
		}

		CheckHostAnchorResponseMsg response = new CheckHostAnchorResponseMsg
		{
			granted = requestGranted,
			//apiKey = cloudApiKey
		};

		NetworkServer.SendToClient(netMsg.conn.connectionId, NetMsgType.CheckHostAnchorResponse, response);

		int connId = netMsg.conn.connectionId;
		LogToConsole("CheckHostAnchor received from client " + connId);
	}


	// handles SetGameAnchorRequest
	private void OnSetGameAnchorRequest(NetworkMessage netMsg)
	{
		var request = netMsg.ReadMessage<SetGameAnchorRequestMsg>();
		if (request == null || request.gameName != gameName)
			return;

		bool requestConfirmed = string.IsNullOrEmpty(gameCloudAnchorId) && hostingClientId == netMsg.conn.connectionId;

		if (requestConfirmed) 
		{
			gameCloudAnchorId = request.anchorId;
			gameAnchorTimestamp = Time.realtimeSinceStartup;

			GameObject gameAnchorGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			gameAnchorGo.name = "GameAnchor-" + gameCloudAnchorId;
			gameAnchorTransform = gameAnchorGo.transform;

			gameAnchorTransform.position = request.anchorPos;
			gameAnchorTransform.rotation = request.anchorRot;
			gameAnchorTransform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}

		SetGameAnchorResponseMsg response = new SetGameAnchorResponseMsg
		{
			confirmed = requestConfirmed,
		};

		NetworkServer.SendToClient(netMsg.conn.connectionId, NetMsgType.SetGameAnchorResponse, response);

		int connId = netMsg.conn.connectionId;
		LogToConsole("SetGameAnchor received from client " + connId + ", anchorId: " + request.anchorId);
	}


	// handles SetClientPoseRequest
	private void OnSetClientPoseRequest(NetworkMessage netMsg)
	{
		var request = netMsg.ReadMessage<SetClientPoseRequestMsg>();
		if (request == null)
			return;

		int connId = netMsg.conn.connectionId;
		bool clientFound = dictClientTrans.ContainsKey(connId);

		if (clientFound) 
		{
			NetClientData clientData = dictClientTrans[connId];

			if (clientData.transform == null) 
			{
				GameObject clientGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);

				clientGo.name = "Client-" + connId;
				clientGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

				clientGo.transform.position = Vector3.zero;
				clientGo.transform.rotation = Quaternion.identity;

				clientData.transform = clientGo.transform;
			}

			if (clientData.transform.parent == null && gameAnchorTransform != null) 
			{
				clientData.transform.parent = gameAnchorTransform;
			}

			clientData.transform.localPosition = request.clientPos;
			clientData.transform.localRotation = request.clientRot;

			clientData.clientPose.position = request.clientPos;
			clientData.clientPose.rotation = request.clientRot;

			clientData.localPose.position = request.localPos;
			clientData.localPose.rotation = request.localRot;

			dictClientTrans[connId] = clientData;
		}

//		SetClientPoseResponseMsg response = new SetClientPoseResponseMsg
//		{
//			confirmed = clientFound,
//		};
//
//		NetworkServer.SendToClient(netMsg.conn.connectionId, NetMsgType.SetClientPoseResponse, response);

		//LogToConsole("SetClientPose received from client " + connId);
	}


	// logs message to the console
	private void LogToConsole(string sMessage)
	{
		Debug.Log(sMessage);

		if (consoleMessages) 
		{
			consoleMessages.text += "\r\n" + sMessage;

			// scroll to end
			ScrollRect scrollRect = consoleMessages.gameObject.GetComponentInParent<ScrollRect>();
			if (scrollRect) 
			{
				Canvas.ForceUpdateCanvases();
				scrollRect.verticalScrollbar.value = 0f;
				Canvas.ForceUpdateCanvases();		
			}
		}
	}


	// logs error message to the console
	private void LogErrorToConsole(string sMessage)
	{
		Debug.LogError(sMessage);

		if (consoleMessages) 
		{
			consoleMessages.text += "\r\n" + sMessage;

			// scroll to end
			ScrollRect scrollRect = consoleMessages.gameObject.GetComponentInParent<ScrollRect>();
			if (scrollRect) 
			{
				Canvas.ForceUpdateCanvases();
				scrollRect.verticalScrollbar.value = 0f;
				Canvas.ForceUpdateCanvases();		
			}
		}
	}


	// logs error message to the console
	private void LogErrorToConsole(System.Exception ex)
	{
		LogErrorToConsole(ex.Message + "\n" + ex.StackTrace);
	}

}
