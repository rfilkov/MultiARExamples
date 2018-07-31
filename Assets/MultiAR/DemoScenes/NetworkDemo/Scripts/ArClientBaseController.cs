using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class ArClientBaseController : MonoBehaviour 
{
	[Tooltip("The name of the AR-game (used by client-server and broadcast messages).")]
	public string gameName = "ArGame";

//	[Tooltip("Port used for server broadcast discovery.")]
//	public int broadcastPort = 7779;

	[Tooltip("Host name or IP, where the game server is runing.")]
	public string serverHost = "0.0.0.0";

	[Tooltip("Port, where the game data server is listening.")]
	public int serverPort = 7777;

	[Tooltip("Try to reconnect after this amount of seconds.")]
	public float reconnectAfterSeconds = 2f;

	[Tooltip("Registered player prefab.")]
	public GameObject playerPrefab;

	[Tooltip("Registered spawn prefabs.")]
	public List<GameObject> spawnPrefabs = new List<GameObject>();

	[Tooltip("UI-Text to display status messages.")]
	public UnityEngine.UI.Text statusText;

	[Tooltip("Whether to show debug messages.")]
	public bool showDebugMessages;

	// singleton instance of this object
	protected static ArClientBaseController instance = null;

	// Anchor object to be saved or restored.
	protected GameObject worldAnchorObj = null;

	// Whether the world anchor needs to be set or not
	protected bool setAnchorAllowed = false;

	// Whether the saved world anchor can be used or not
	protected bool getAnchorAllowed = false;

	// reference to the network manager
	protected ClientNetworkManager netManager = null;

	// reference to the network client & discovery
	protected NetworkClient netClient = null;
	protected ClientNetworkDiscovery netDiscovery = null;

	// reference to the multi-ar manager
	protected MultiARManager marManager = null;


	// whether the client is connected to the server
	protected bool clientConnected = false;
	protected float disconnectedAt = 0f;
//	protected float dataReceivedAt = 0f;

	// saved anchor Id
	protected string worldAnchorId = string.Empty;

	// saved anchor data, if any
	protected byte[] worldAnchorData = null;


	/// <summary>
	/// Gets the singleton instance of the ar-client controller.
	/// </summary>
	/// <value>The instance.</value>
	public static ArClientBaseController Instance
	{
		get 
		{
			return instance;
		}
	}


	/// <summary>
	/// Determines whether setting world anchor is allowed.
	/// </summary>
	/// <returns><c>true</c> if setting anchor is allowed; otherwise, <c>false</c>.</returns>
	public bool IsSetAnchorAllowed()
	{
		return setAnchorAllowed;
	}


	/// <summary>
	/// Determines whether getting world anchor is allowed.
	/// </summary>
	/// <returns><c>true</c> if getting anchor is allowed; otherwise, <c>false</c>.</returns>
	public bool IsGetAchorAllowed()
	{
		return getAnchorAllowed;
	}


	/// <summary>
	/// Gets the anchor transform.
	/// </summary>
	/// <returns>The anchor transform.</returns>
	public Transform GetAnchorTransform()
	{
		if (worldAnchorObj) 
		{
			return worldAnchorObj.transform.parent ? worldAnchorObj.transform.parent : worldAnchorObj.transform;
		}

		return null;
	}


	/// <summary>
	/// Gets or sets the world anchor object.
	/// </summary>
	public GameObject WorldAnchorObj
	{
		get 
		{
			if (worldAnchorObj) 
			{
				return worldAnchorObj.transform.parent ? worldAnchorObj.transform.parent.gameObject : worldAnchorObj;
			}

			return null;
		}

		set
		{
			worldAnchorObj = value;
		}
	}


	void Awake()
	{
		instance = this;
	}


	protected virtual void Start () 
	{
		try 
		{
			//LogFilter.currentLogLevel = LogFilter.Debug;

			// get reference to the multi-ar manager
			marManager = MultiARManager.Instance;

			// setup network manager component
			netManager = GetComponent<ClientNetworkManager>();
			if(netManager == null)
			{
				netManager = gameObject.AddComponent<ClientNetworkManager>();
			}

			if(netManager != null)
			{
				netManager.arClient = this;

				if(playerPrefab != null)
				{
					netManager.playerPrefab = playerPrefab;
				}

				if(spawnPrefabs != null && spawnPrefabs.Count > 0)
				{
					netManager.spawnPrefabs.AddRange(spawnPrefabs);
				}
			}

			if(serverHost != "0.0.0.0" && !string.IsNullOrEmpty(serverHost))
			{
				// connect to the server
				ConnectToServer();
			}
			else
			{
				// start network discovery
				netDiscovery = gameObject.GetComponent<ClientNetworkDiscovery>();
				if(netDiscovery == null)
				{
					netDiscovery = gameObject.AddComponent<ClientNetworkDiscovery>();
				}

				if(netDiscovery != null)
				{
					netDiscovery.arClient = this;
					//netDiscovery.broadcastPort = broadcastPort;
					//netDiscovery.broadcastKey = serverPort;
					//netDiscovery.broadcastData = gameName;
					netDiscovery.showGUI = false;

					netDiscovery.Initialize();
					netDiscovery.StartAsClient();
				}
			}
		} 
		catch (System.Exception ex) 
		{
			Debug.LogError(ex.Message + "\n" + ex.StackTrace);

			if(statusText)
			{
				statusText.text = ex.Message;
			}
		}
	}


	protected virtual void OnDestroy()
	{
		clientConnected = false;
		disconnectedAt = 0f;
//		dataReceivedAt = 0f;

		if (netManager != null) 
		{
			netManager.StopClient();
		}
	}


	protected virtual void Update()
	{
		if (!clientConnected) 
		{
			if(statusText)
			{
				if(string.IsNullOrEmpty(serverHost) || serverHost == "0.0.0.0")
					statusText.text = "Looking for game server...";
				else if(disconnectedAt == 0f)
					statusText.text = "Connecting to game server: " + serverHost;
				else
					statusText.text = "Reconnecting to: " + serverHost;
			}

			if (disconnectedAt > 0f && (Time.realtimeSinceStartup - disconnectedAt) >= reconnectAfterSeconds) 
			{
				disconnectedAt = 0f;

				// try to reconnect
				ConnectToServer();
			}
		}
	}

	/// <summary>
	/// Tries to connect to the AR server.
	/// </summary>
	public void ConnectToServer()
	{
		if (/**netClient != null*/ netManager != null && serverHost != "0.0.0.0" && !string.IsNullOrEmpty(serverHost)) 
		{
			var config = new ConnectionConfig();
			config.AddChannel(QosType.ReliableSequenced);
			config.AddChannel(QosType.Unreliable);

			netManager.networkAddress = serverHost;
			netManager.networkPort = serverPort;
			netClient = netManager.StartClient(null, config);
		}
	}


	// handles network error message
	public void OnNetworkError(NetworkConnection conn, int errorCode)
	{
		int connId = conn.connectionId;

		string sErrorMessage = "NetError " + connId + ": " + (NetworkError)errorCode;
		LogErrorMessage(sErrorMessage);
	}


	// handles Connect-message
	public void OnClientConnect(NetworkConnection conn)
	{
		int connId = conn.connectionId;

		clientConnected = true;
		disconnectedAt = 0f;
//		dataReceivedAt = Time.realtimeSinceStartup;

		LogDebugMessage("Connected client " + connId + " to: " + conn.address);

		// register client handlers
		conn.RegisterHandler(NetMsgType.GetGameAnchorResponse, OnGetGameAnchorResponse);
		conn.RegisterHandler(NetMsgType.CheckHostAnchorResponse, OnCheckHostAnchorResponse);
		conn.RegisterHandler(NetMsgType.SetGameAnchorResponse, OnSetGameAnchorResponse);

		// send Get-game-anchor
		GetGameAnchorRequestMsg request = new GetGameAnchorRequestMsg
		{
			gameName = this.gameName
		};

		netClient.Send(NetMsgType.GetGameAnchorRequest, request);
	}


	// handles Disconnect-message
	public void OnClientDisconnect(NetworkConnection conn)
	{
		int connId = conn.connectionId;

		clientConnected = false;
		disconnectedAt = Time.realtimeSinceStartup;
//		dataReceivedAt = Time.realtimeSinceStartup;

		LogDebugMessage("Disconnected client " + connId + " from: " + conn.address);
	}


	private void OnGetGameAnchorResponse(NetworkMessage netMsg)
	{
		var response = netMsg.ReadMessage<GetGameAnchorResponseMsg>();
		int connId = netMsg.conn.connectionId;

		if (response.found && !string.IsNullOrEmpty(response.anchorId)) 
		{
			LogDebugMessage("GetGameAnchor " + connId + " found: " + response.anchorId);

			worldAnchorId = response.anchorId;
			worldAnchorData = response.anchorData;
			getAnchorAllowed = true;
		}
		else
		{
			LogDebugMessage("GetGameAnchor " + connId + ": not found.");

			// send Check-host-anchor
			CheckHostAnchorRequestMsg request = new CheckHostAnchorRequestMsg
			{
				gameName = this.gameName
			};

			netClient.Send(NetMsgType.CheckHostAnchorRequest, request);
		}
	}


	private void OnCheckHostAnchorResponse(NetworkMessage netMsg)
	{
		var response = netMsg.ReadMessage<CheckHostAnchorResponseMsg>();

		int connId = netMsg.conn.connectionId;
		LogDebugMessage("CheckHostAnchor " + connId + ": " + (response.granted ? "granted" : "not granted"));

		setAnchorAllowed = response.granted;

		//if (!response.granted) 
		{
			StartCoroutine(WaitAndCheckForAnchor());
		}
	}


	private IEnumerator WaitAndCheckForAnchor()
	{
		// wait some time
		yield return new WaitForSeconds(5f);

		if (worldAnchorObj == null) 
		{
			setAnchorAllowed = false;

			// re-send Get-game-anchor
			GetGameAnchorRequestMsg request = new GetGameAnchorRequestMsg
			{
				gameName = this.gameName
			};

			netClient.Send(NetMsgType.GetGameAnchorRequest, request);
		}
	}


	private void OnSetGameAnchorResponse(NetworkMessage netMsg)
	{
		var response = netMsg.ReadMessage<SetGameAnchorResponseMsg>();

		int connId = netMsg.conn.connectionId;
		LogDebugMessage("SetGameAnchorResponse " + connId + ": " + (response.confirmed ? "confirmed" : "not confirmed"));

		if (!response.confirmed) 
		{
			// send Get-game-anchor
			GetGameAnchorRequestMsg request = new GetGameAnchorRequestMsg
			{
				gameName = this.gameName
			};

			netClient.Send(NetMsgType.GetGameAnchorRequest, request);
		}
	}


	// logs the given message to console and the screen
	protected void LogMessage(string sMessage)
	{
		Debug.Log(sMessage);

		if(statusText)
		{
			statusText.text = sMessage;
		}
	}


	// logs the given error message to console and the screen
	protected void LogErrorMessage(string sMessage)
	{
		Debug.LogError(sMessage);

		if(statusText)
		{
			statusText.text = sMessage;
		}
	}


	// logs the given debug message to console and the screen
	protected void LogDebugMessage(string sMessage)
	{
		Debug.Log(sMessage);

		if(statusText && showDebugMessages)
		{
			statusText.text = sMessage;
		}
	}

}


/// <summary>
/// ArClient's NetworkManager component
/// </summary>
public class ClientNetworkManager : NetworkManager
{

	public ArClientBaseController arClient;


	public override void OnClientConnect(NetworkConnection conn)
	{
		//Debug.Log ("OnClientConnect");
		base.OnClientConnect(conn);

		if (arClient != null) 
		{
			arClient.OnClientConnect(conn);
		}
	}


	public override void OnClientDisconnect(NetworkConnection conn)
	{
		//Debug.Log ("OnClientDisconnect");

		if (arClient != null) 
		{
			arClient.OnClientDisconnect(conn);
		}

		base.OnClientDisconnect(conn);
	}


	public override void OnClientError(NetworkConnection conn, int errorCode)
	{
		base.OnClientError(conn, errorCode);

		if (arClient != null) 
		{
			arClient.OnNetworkError(conn, errorCode);
		}
	}

}


/// <summary>
/// ArClient's NetworkDiscovery component
/// </summary>
public class ClientNetworkDiscovery : NetworkDiscovery
{

	public ArClientBaseController arClient;


	public override void OnReceivedBroadcast(string fromAddress, string data)
	{
		if (string.IsNullOrEmpty(data))
			return;

		// split the data
		string[] items = data.Split(':');
		if (items == null || items.Length < 3)
			return;

		if (arClient != null && items[0] == arClient.gameName && 
			(arClient.serverHost == "0.0.0.0" || string.IsNullOrEmpty(arClient.serverHost)))
		{
			Debug.Log("GotBroadcast: " + data);

			arClient.serverHost = items[1];
			arClient.serverPort = int.Parse(items [2]);
			//this.StopBroadcast();

			arClient.ConnectToServer();
		}
	}

}

