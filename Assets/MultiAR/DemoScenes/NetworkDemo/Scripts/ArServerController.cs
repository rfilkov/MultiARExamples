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

//	[Tooltip("Port used for server broadcast discovery.")]
//	public int broadcastPort = 8889;

	[Tooltip("Maximum number of allowed connections.")]
	public int maxConnections = 8;

	[Tooltip("Whether the server should use websockets or not.")]
	public bool useWebSockets = false;

	[Tooltip("Registered player prefab.")]
	public GameObject playerPrefab;

	[Tooltip("UI-Text to display connection status messages.")]
	public Text connStatusText;

	[Tooltip("UI-Text to display server status messages.")]
	public Text serverStatusText;

	[Tooltip("UI-Text to display server console.")]
	public Text consoleMessages;

	// reference to the network components
	private ServerNetworkManager netManager = null;
	private NetworkDiscovery netDiscovery = null;

	// api key needed for cloud anchor hosting or resolving
	//private string cloudApiKey = string.Empty;

	// timeout constants in seconds
	private const float GameAnchorTimeout = 24 * 3600f;  // how long to keep the anchor
	private const float AnchorHostingTimeout = 60f;  // how long to wait for anchor hosting

	// cloud anchor Id
	private string gameCloudAnchorId = string.Empty;
	private Transform gameAnchorTransform = null;
	private float gameAnchorTimestamp = 0f;

	// game anchor data, if any
	private byte[] gameAnchorData = null;

	// id of the hosting-requesting cloud-anchor client 
	private int hostingClientId = -1;
	private float hostingClientTimestamp = 0f;


	/// <summary>
	/// Gets the anchor transform.
	/// </summary>
	/// <returns>The anchor transform.</returns>
	public Transform GetAnchorTransform()
	{
		if (gameAnchorTransform) 
		{
			return gameAnchorTransform.parent ? gameAnchorTransform.parent : gameAnchorTransform;
		}

		return null;
	}


	void Start () 
	{
		try 
		{
			// setup network manager component
			netManager = GetComponent<ServerNetworkManager>();
			if(netManager == null)
			{
				netManager = gameObject.AddComponent<ServerNetworkManager>();
			}

			// start the server
			if(netManager != null)
			{
				netManager.arServer = this;

				netManager.networkPort = listenOnPort;
				netManager.useWebSockets = useWebSockets;

				if(playerPrefab != null)
				{
					netManager.playerPrefab = playerPrefab;
				}

				// configure the network server
				var config = new ConnectionConfig();
				config.AddChannel(QosType.ReliableSequenced);
				config.AddChannel(QosType.Unreliable);

				netManager.StartServer(config, maxConnections);
			}

			NetworkServer.RegisterHandler(NetMsgType.GetGameAnchorRequest, OnGetGameAnchorRequest);
			NetworkServer.RegisterHandler(NetMsgType.CheckHostAnchorRequest, OnCheckHostAnchorRequest);
			NetworkServer.RegisterHandler(NetMsgType.SetGameAnchorRequest, OnSetGameAnchorRequest);
			NetworkServer.RegisterHandler(NetMsgType.HandleSyncTransform, ArSyncTransform.HandleSyncTransform);

			// get server ip address
#if !UNITY_WSA
			string serverHost = Network.player.ipAddress;
#else
			string serverHost = "127.0.0.1";
#endif

			// setup network discovery component
			netDiscovery = GetComponent<NetworkDiscovery>();
			if(netDiscovery == null)
			{
				netDiscovery = gameObject.AddComponent<NetworkDiscovery>();
			}

			if(netDiscovery != null)
			{
				//netDiscovery.broadcastPort = broadcastPort;
				//netDiscovery.broadcastKey = listenOnPort;
				netDiscovery.broadcastData = gameName + ":" + serverHost + ":" + listenOnPort;
				netDiscovery.showGUI = false;

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
		// shutdown the server and disconnect all clients
		if (netManager) 
		{
			netManager.StopServer();
		}

		string sMessage = gameName + "-Server stopped.";
		Debug.Log(sMessage);
	}


	void Update () 
	{
		// check for waiting too long for anchor hosting
		if (hostingClientId >= 0 && hostingClientTimestamp > 0f &&
			Time.realtimeSinceStartup > (hostingClientTimestamp + AnchorHostingTimeout)) 
		{
			//hostingClientId = -1;
			hostingClientTimestamp = 0f;

			LogToConsole("Hosting client timed out.");
		}

		// check for anchor timeout
		if (!string.IsNullOrEmpty(gameCloudAnchorId) && Time.time > (gameAnchorTimestamp + GameAnchorTimeout)) 
		{
			gameCloudAnchorId = string.Empty;
			gameAnchorTransform = null;
			gameAnchorTimestamp = 0f;

			LogToConsole("Game anchor timed out.");
		}
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
			//apiKey = cloudApiKey,
			anchorData = gameAnchorData
		};

		NetworkServer.SendToClient(netMsg.conn.connectionId, NetMsgType.GetGameAnchorResponse, response);

		int connId = netMsg.conn.connectionId;
		LogToConsole("GetGameAnchor received from client " + connId + ", anchorId: " + gameCloudAnchorId);
	}


	// handles CheckHostAnchorRequest
	private void OnCheckHostAnchorRequest(NetworkMessage netMsg)
	{
		var request = netMsg.ReadMessage<CheckHostAnchorRequestMsg>();
		if (request == null || request.gameName != gameName)
			return;

		bool requestGranted = string.IsNullOrEmpty(gameCloudAnchorId) && (hostingClientTimestamp == 0f ||
			Time.realtimeSinceStartup > (hostingClientTimestamp + AnchorHostingTimeout));
		
		if (!requestGranted) 
		{
			// check for last-granted client
			if (string.IsNullOrEmpty (gameCloudAnchorId) && (hostingClientId == netMsg.conn.connectionId)) 
			{
				requestGranted = true;
			}
		}

		if (requestGranted) 
		{
			// save last-granted client & time
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

		bool requestConfirmed = !string.IsNullOrEmpty(request.anchorId) && 
			hostingClientId == netMsg.conn.connectionId;

		if (requestConfirmed) 
		{
			hostingClientId = -1;

			gameCloudAnchorId = request.anchorId;
			gameAnchorTimestamp = Time.realtimeSinceStartup;

			gameAnchorData = request.anchorData;

			GameObject gameAnchorGo = new GameObject("GameAnchor-" + gameCloudAnchorId);
			gameAnchorTransform = gameAnchorGo.transform;

			gameAnchorTransform.position = request.anchorPos;
			gameAnchorTransform.rotation = request.anchorRot;

			GameObject anchoredSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			anchoredSphere.transform.SetParent(gameAnchorTransform);

			anchoredSphere.transform.localPosition = Vector3.zero;
			anchoredSphere.transform.localRotation = Quaternion.identity;
			anchoredSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
		}

		SetGameAnchorResponseMsg response = new SetGameAnchorResponseMsg
		{
			confirmed = requestConfirmed,
		};

		NetworkServer.SendToClient(netMsg.conn.connectionId, NetMsgType.SetGameAnchorResponse, response);

		int connId = netMsg.conn.connectionId;
		LogToConsole("SetGameAnchor received from client " + connId + ", anchorId: " + request.anchorId);
	}


	// logs message to the console
	public void LogToConsole(string sMessage)
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
	public void LogErrorToConsole(string sMessage)
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


/// <summary>
/// ArServer's NetworkManager component
/// </summary>
public class ServerNetworkManager : NetworkManager
{

	public ArServerController arServer;


	public override void OnServerConnect(NetworkConnection conn)
	{
		base.OnServerConnect(conn);

		if (arServer != null) 
		{
			int connId = conn.connectionId;
			arServer.LogToConsole("Connected client " + connId + ", IP: " + conn.address);
		}
	}


	public override void OnServerDisconnect(NetworkConnection conn)
	{
		if (arServer != null) 
		{
			int connId = conn.connectionId;
			arServer.LogToConsole("Disconnected client " + connId);
		}

		base.OnServerDisconnect(conn);
	}


	public override void OnServerError(NetworkConnection conn, int errorCode)
	{
		base.OnServerError(conn, errorCode);

		if (arServer != null) 
		{
			int connId = conn.connectionId;
			arServer.LogErrorToConsole("NetError " + connId + " detected: " + (NetworkError)errorCode);
		}
	}

}

