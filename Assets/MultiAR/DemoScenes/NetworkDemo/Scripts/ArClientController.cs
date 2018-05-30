using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class ArClientController : MonoBehaviour 
{
	[Tooltip("The name of the AR-game (used by client-server and broadcast messages).")]
	public string gameName = "ArGame";

	[Tooltip("Port used for server broadcast discovery.")]
	public int broadcastPort = 8889;

	[Tooltip("Host name or IP, where the game server is runing.")]
	public string serverHost = "0.0.0.0";

	[Tooltip("Port, where the game data server is listening.")]
	public int serverPort = 8888;

	[Tooltip("Try to reconnect after this amount of seconds.")]
	public float reconnectAfter = 2f;

	[Tooltip("UI-Text to display status messages.")]
	public UnityEngine.UI.Text statusText;

	// Anchor object to be saved or restored.
	[HideInInspector]
	public GameObject worldAnchorObj = null;

	// Whether the world anchor needs to be set or not
	[HideInInspector]
	public bool setAnchorAllowed = false;

	// Whether the saved world anchor can be used or not
	[HideInInspector]
	public bool getAnchorAllowed = false;


	// network client & discovery
	private NetworkClient netClient = null;
	private ArNetworkDiscovery netDiscovery = null;

	// reference to multi-ar manager
	private MultiARManager marManager = null;


	// whether the client is connected to the server
	private bool clientConnected = false;
	private float disconnectedAt = 0f;
	private float dataReceivedAt = 0f;

	// max-wait-time for network and cloud operations 
	private const float k_MaxWaitTime = 10f;

	// set-anchor timestamp
	private float setAnchorTillTime = 0f;

	// get-anchor timestamp
	private float getAnchorTillTime = 0f;

	// saved anchor Id
	private string worldAnchorId = string.Empty;


	void Start () 
	{
		try 
		{
			// get reference to the multi-ar manager
			marManager = MultiARManager.Instance;

			// create the network client
			LogFilter.currentLogLevel = LogFilter.Debug;
			netClient = new NetworkClient();

			netClient.RegisterHandler(MsgType.Error, OnNetworkError);
			netClient.RegisterHandler(MsgType.Connect, OnClientConnect);
			netClient.RegisterHandler(MsgType.Disconnect, OnClientDisconnect);

			netClient.RegisterHandler(NetMsgType.GetGameAnchorResponse, OnGetGameAnchorResponse);
			netClient.RegisterHandler(NetMsgType.CheckHostAnchorResponse, OnCheckHostAnchorResponse);

			if(serverHost != "0.0.0.0" && !string.IsNullOrEmpty(serverHost))
			{
				// connect to the server
				ConnectToServer();
			}
			else
			{
				// start network discovery
				netDiscovery = gameObject.AddComponent<ArNetworkDiscovery>();

				netDiscovery.arClient = this;
				netDiscovery.broadcastPort = broadcastPort;
				netDiscovery.broadcastKey = serverPort;
				netDiscovery.broadcastData = gameName;
				netDiscovery.showGUI = false;

				netDiscovery.Initialize();
				netDiscovery.StartAsClient();
			}
		} 
		catch (System.Exception ex) 
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);

			if(statusText)
			{
				statusText.text = ex.Message;
			}
		}
	}


	void OnDestroy()
	{
		clientConnected = false;
		disconnectedAt = Time.realtimeSinceStartup;
		dataReceivedAt = 0f;

		if (netDiscovery && netDiscovery.running) 
		{
			netDiscovery.StopBroadcast();
			netDiscovery = null;
		}

		if (netClient != null) 
		{
			//netClient.Disconnect();
			netClient.Shutdown();
			netClient = null;
		}
	}


	void Update () 
	{
		// check if the world anchor needs to be saved
		if (setAnchorAllowed && worldAnchorObj && marManager) 
		{
			if (setAnchorTillTime < Time.realtimeSinceStartup) 
			{
				setAnchorTillTime = Time.realtimeSinceStartup + k_MaxWaitTime;

				marManager.SaveWorldAnchor(worldAnchorObj, anchorId => 
					{
						Debug.Log("SaveWorldAnchor: " + anchorId);

						worldAnchorId = anchorId;
						getAnchorAllowed = !string.IsNullOrEmpty(anchorId);
						setAnchorAllowed = !getAnchorAllowed;
					});
			}
		}

		// check if the world anchor needs to be restored
		if (getAnchorAllowed && !worldAnchorObj && !string.IsNullOrEmpty(worldAnchorId) && marManager) 
		{
			if (getAnchorTillTime < Time.realtimeSinceStartup) 
			{
				getAnchorTillTime = Time.realtimeSinceStartup + k_MaxWaitTime;

				marManager.RestoreWorldAnchor(worldAnchorId, anchorObj =>
					{
						Debug.Log("RestoreWorldAnchor: " + worldAnchorId + ", got: " + (anchorObj != null));

						worldAnchorObj = anchorObj;
						getAnchorAllowed = anchorObj != null;
						setAnchorAllowed = false;

						if(!getAnchorAllowed)
						{
							// send Check-host-anchor
							CheckHostAnchorRequestMsg request = new CheckHostAnchorRequestMsg
							{
								gameName = this.gameName
							};

							netClient.Send(NetMsgType.CheckHostAnchorRequest, request);
						}
					});
			}
		}

	}


	/// <summary>
	/// Tries to connect to the AR server.
	/// </summary>
	public void ConnectToServer()
	{
		if (netClient != null && serverHost != "0.0.0.0" && !string.IsNullOrEmpty(serverHost)) 
		{
			var config = new ConnectionConfig();
			config.AddChannel(QosType.ReliableSequenced);
			config.AddChannel(QosType.Unreliable);

			netClient.Configure(config, 1);
			netClient.Connect(serverHost, serverPort);
		}
	}


	// handles network error message
	void OnNetworkError(NetworkMessage netMsg)
	{
		var errorMsg = netMsg.ReadMessage<ErrorMessage>();
		int connId = netMsg.conn.connectionId;

		string sErrorMessage = "NetError " + connId + ": " + (NetworkError)errorMsg.errorCode;
		Debug.LogError(sErrorMessage);

		if(statusText)
		{
			statusText.text = sErrorMessage;
		}
	}


	// handles Connect-message
	private void OnClientConnect(NetworkMessage netMsg)
	{
		int connId = netMsg.conn.connectionId;

		clientConnected = true;
		disconnectedAt = 0f;
		dataReceivedAt = Time.realtimeSinceStartup;

		LogMessage("Connected client " + connId + " IP: " + netMsg.conn.address);

		// send Get-game-anchor
		GetGameAnchorRequestMsg request = new GetGameAnchorRequestMsg
		{
			gameName = this.gameName
		};

		netClient.Send(NetMsgType.GetGameAnchorRequest, request);
	}


	// handles Disconnect-message
	private void OnClientDisconnect(NetworkMessage netMsg)
	{
		int connId = netMsg.conn.connectionId;

		clientConnected = false;
		disconnectedAt = Time.realtimeSinceStartup;
		dataReceivedAt = Time.realtimeSinceStartup;

		LogMessage("Disconnected client " + connId + " IP: " + netMsg.conn.address);
	}


	private void OnGetGameAnchorResponse(NetworkMessage netMsg)
	{
		var response = netMsg.ReadMessage<GetGameAnchorResponseMsg>();
		int connId = netMsg.conn.connectionId;

		if (response.found && !string.IsNullOrEmpty(response.anchorId)) 
		{
			LogMessage("GetGameAnchor " + connId + " found: " + response.anchorId);

			worldAnchorId = response.anchorId;
			getAnchorAllowed = true;
			setAnchorAllowed = false;
		}
		else
		{
			LogMessage("GetGameAnchor " + connId + ": not found.");

			getAnchorAllowed = false;
			setAnchorAllowed = false;

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
		LogMessage("CheckHostAnchor " + connId + ": " + (response.granted ? "granted" : "not granted"));

		getAnchorAllowed = false;
		setAnchorAllowed = response.granted;
	}


	// logs the given message to console and the screen
	private void LogMessage(string sMessage)
	{
		Debug.Log(sMessage);

		if(statusText)
		{
			statusText.text = sMessage;
		}
	}


}


/// <summary>
/// Ar network discovery client.
/// </summary>
public class ArNetworkDiscovery : NetworkDiscovery
{

	public ArClientController arClient;


	public override void OnReceivedBroadcast(string fromAddress, string data)
	{
		if (arClient != null && data == arClient.gameName && 
			(arClient.serverHost == "0.0.0.0" || string.IsNullOrEmpty(arClient.serverHost)))
		{
			arClient.serverHost = fromAddress;
			this.StopBroadcast();

			arClient.ConnectToServer();
		}
	}
}

