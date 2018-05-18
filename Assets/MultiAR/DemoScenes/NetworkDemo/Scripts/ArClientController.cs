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

	// network client & discovery
	private NetworkClient netClient = null;
	ArNetworkDiscovery netDiscovery = null;


	// whether the client is connected to the server
	private bool clientConnected = false;
	private float disconnectedAt = 0f;
	private float dataReceivedAt = 0f;


	// Game-Anchor-Found
	public delegate void GameAnchorFoundDelegate(string anchorId, string apiKey);
	public GameAnchorFoundDelegate gameAnchorFoundCallback = null;


	void Start () 
	{
		try 
		{
			netClient = new NetworkClient();

			netClient.RegisterHandler(MsgType.Error, OnNetworkError);
			netClient.RegisterHandler(MsgType.Connect, OnClientConnect);
			netClient.RegisterHandler(MsgType.Disconnect, OnClientDisconnect);

			netClient.RegisterHandler(NetMsgType.GetGameAnchorResponse, OnGetGameAnchorResponse);

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
				//netDiscovery.broadcastData = gameName;

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
		
	}


	/// <summary>
	/// Tries to connect to the AR server.
	/// </summary>
	public void ConnectToServer()
	{
		if (netClient != null && serverHost != "0.0.0.0" && !string.IsNullOrEmpty(serverHost)) 
		{
			netClient.Connect(serverHost, serverPort);
		}
	}


	// handles network error message
	void OnNetworkError(NetworkMessage netMsg)
	{
		var errorMsg = netMsg.ReadMessage<ErrorMessage>();
		int connId = netMsg.conn.connectionId;

		string sErrorMessage = "NetError " + connId + " detected: " + (NetworkError)errorMsg.errorCode;
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

		if (response.found) 
		{
			int connId = netMsg.conn.connectionId;
			LogMessage("GetGameAnchor " + connId + ", anchorId: " + response.anchorId);

			if (gameAnchorFoundCallback != null) 
			{
				gameAnchorFoundCallback(response.anchorId, response.apiKey);
			}
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

