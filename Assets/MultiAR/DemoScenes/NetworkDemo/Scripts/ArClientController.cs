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
	private GameObject worldAnchorObj = null;

	// Whether the world anchor needs to be set or not
	private bool setAnchorAllowed = false;

	// Whether the saved world anchor can be used or not
	private bool getAnchorAllowed = false;


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
			netClient.RegisterHandler(NetMsgType.SetGameAnchorResponse, OnSetGameAnchorResponse);

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
		if (setAnchorAllowed && !worldAnchorObj) 
		{
			if(statusText)
			{
				statusText.text = "Tap the floor to anchor the play area.";
			}
		}

		// check if the world anchor needs to be saved
		if (setAnchorAllowed && worldAnchorObj && marManager) 
		{
			if (setAnchorTillTime < Time.realtimeSinceStartup) 
			{
				setAnchorTillTime = Time.realtimeSinceStartup + k_MaxWaitTime;
				setAnchorAllowed = false;

				Debug.Log("Saving world anchor");
				if(statusText)
				{
					statusText.text = "Saving world anchor";
				}

				marManager.SaveWorldAnchor(worldAnchorObj, (anchorId, errorMessage) => 
					{
						worldAnchorId = anchorId;

						if(string.IsNullOrEmpty(errorMessage))
						{
							Debug.Log("World anchor saved: " + anchorId);
							if(statusText)
							{
								statusText.text = "World anchor saved: " + anchorId;
							}

							if(!string.IsNullOrEmpty(anchorId))
							{
								SetGameAnchorRequestMsg request = new SetGameAnchorRequestMsg
								{
									gameName = this.gameName,
									anchorId = worldAnchorId,
									anchorPos = worldAnchorObj.transform.position,
									anchorRot = worldAnchorObj.transform.rotation
								};

								netClient.Send(NetMsgType.SetGameAnchorRequest, request);
							}
						}
						else
						{
							Debug.Log("Error saving world anchor: " + errorMessage);
							if(statusText)
							{
								statusText.text = "Error saving world anchor: " + errorMessage;
							}
						}
					});
			}
		}

		// check if the world anchor needs to be restored
		if (getAnchorAllowed && !string.IsNullOrEmpty(worldAnchorId) && !worldAnchorObj && marManager) 
		{
			if (getAnchorTillTime < Time.realtimeSinceStartup) 
			{
				getAnchorTillTime = Time.realtimeSinceStartup + k_MaxWaitTime;
				getAnchorAllowed = false;

				Debug.Log("Restoring world anchor");
				if(statusText)
				{
					statusText.text = "Restoring world anchor";
				}

				marManager.RestoreWorldAnchor(worldAnchorId, (anchorObj, errorMessage) =>
					{
						worldAnchorObj = anchorObj;

						if(string.IsNullOrEmpty(errorMessage))
						{
							Debug.Log("World anchor restored: " + worldAnchorId);
							if(statusText)
							{
								statusText.text = "World anchor restored: " + worldAnchorId;
							}
						}
						else
						{
							Debug.Log("Error restoring world anchor: " + errorMessage);
							if(statusText)
							{
								statusText.text = "Error restoring world anchor: " + errorMessage;
							}

							// send Get-game-anchor
							GetGameAnchorRequestMsg request = new GetGameAnchorRequestMsg
							{
								gameName = this.gameName
							};

							netClient.Send(NetMsgType.GetGameAnchorRequest, request);
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
		}
		else
		{
			LogMessage("GetGameAnchor " + connId + ": not found.");

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

		setAnchorAllowed = response.granted;
	}


	private void OnSetGameAnchorResponse(NetworkMessage netMsg)
	{
		var response = netMsg.ReadMessage<SetGameAnchorResponseMsg>();

		int connId = netMsg.conn.connectionId;
		LogMessage("SetGameAnchorResponse " + connId + ": " + (response.confirmed ? "confirmed" : "not confirmed"));

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

