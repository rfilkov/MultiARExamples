using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public class MyNetServer : MonoBehaviour 
{
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


	private ConnectionConfig serverConfig;
	private int serverChannelId;

	private string serverIpAddress = string.Empty;
	private HostTopology serverTopology;
	private int serverHostId = -1;
	private int broadcastHostId = -1;

	private int bufferSize = 0;
	private byte[] msgBuffer = null;
	private NetworkReader msgReader = null;

	private byte[] broadcastOutBuffer = null;

	private float fCurrentTime = 0f;

//	private Dictionary<int, HostConnection> dictConnection = new Dictionary<int, HostConnection>();
//	private List<int> alConnectionId = new List<int>();

	System.Type NetworkConnectionClass = typeof(MyNetConnection);
	List<MyNetConnection> alConnections = new List<MyNetConnection>();

	private Dictionary<short, NetworkMessageDelegate> msgHandlers = new Dictionary<short, NetworkMessageDelegate>();


	public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
	{
		if (msgHandlers.ContainsKey(msgType))
		{
			msgHandlers.Remove(msgType);
		}

		msgHandlers.Add(msgType, handler);
	}

	public void UnregisterHandler(short msgType)
	{
		if (msgHandlers.ContainsKey(msgType))
		{
			msgHandlers.Remove(msgType);
		}
	}

	public NetworkMessageDelegate GetHandler(short msgType)
	{
		if (msgHandlers.ContainsKey(msgType))
		{
			return msgHandlers[msgType];
		}

		return null;
	}

	public void ClearMessageHandlers()
	{
		msgHandlers.Clear();
	}


	void Awake () 
	{
		try 
		{
			NetworkTransport.Init();

			serverConfig = new ConnectionConfig();
			serverChannelId = serverConfig.AddChannel(QosType.StateUpdate);  // QosType.UnreliableFragmented
			serverConfig.MaxSentMessageQueueSize = 2048;  // 128 by default

			// start data server
			serverTopology = new HostTopology(serverConfig, maxConnections);

			if(!useWebSockets)
				serverHostId = NetworkTransport.AddHost(serverTopology, listenOnPort);
			else
				serverHostId = NetworkTransport.AddWebsocketHost(serverTopology, listenOnPort);

			if(serverHostId < 0)
			{
				throw new UnityException("AddHost failed for port " + listenOnPort);
			}

			bufferSize = NetworkMessage.MaxMessageSize;
			msgBuffer = new byte[bufferSize];
			msgReader = new NetworkReader(msgBuffer);

			// add broadcast host
			if(broadcastPort > 0 && !useWebSockets)
			{
				broadcastHostId = NetworkTransport.AddHost(serverTopology, 0);

				if(broadcastHostId < 0)
				{
					throw new UnityException("AddHost failed for broadcast discovery");
				}
			}

			// set broadcast data
			string sBroadcastData = string.Empty;

#if (!UNITY_WSA)
			serverIpAddress = Network.player.ipAddress;
#else
			serverIpAddress = "127.0.0.1";
#endif
			string sHostInfo = "Server: " + serverIpAddress + ":" + listenOnPort;;

			if(serverStatusText)
			{
				serverStatusText.text = sHostInfo;
			}

			// start broadcast discovery
			sBroadcastData = "MARServer:" + serverIpAddress + ":" + listenOnPort;

			if(broadcastHostId >= 0)
			{
				broadcastOutBuffer = System.Text.Encoding.UTF8.GetBytes(sBroadcastData);
				byte error = 0;

				if (!NetworkTransport.StartBroadcastDiscovery(broadcastHostId, broadcastPort, 8888, 1, 0, broadcastOutBuffer, broadcastOutBuffer.Length, 2000, out error))
				{
					throw new UnityException("Start broadcast discovery failed: " + (NetworkError)error);
				}
			}

			fCurrentTime = Time.time;

			if(connStatusText)
			{
				connStatusText.text = "Server running: 0 connection(s)";
			}
		} 
		catch (System.Exception ex) 
		{
			//LogErrorToConsole(ex.Message + "\n" + ex.StackTrace);

			if(connStatusText)
			{
				connStatusText.text = ex.Message;
			}
		}
	}


	void OnDestroy()
	{
		// clear connections
		//dictConnection.Clear();

		// stop broadcast
		if (broadcastHostId >= 0) 
		{
			NetworkTransport.StopBroadcastDiscovery();
			NetworkTransport.RemoveHost(broadcastHostId);
			broadcastHostId = -1;
		}

		// close the server port
		if (serverHostId >= 0) 
		{
			NetworkTransport.RemoveHost(serverHostId);
			serverHostId = -1;
		}

		// shitdown the transport layer
		NetworkTransport.Shutdown();
	}


	public void Update()
	{
		if (serverHostId == -1)
			return;

		int recvHostId;
		int connectionId;
		int channelId;
		int receivedSize;
		byte error;

		NetworkEventType networkEvent = NetworkEventType.Nothing;

//		if (m_RelaySlotId != -1)
//		{
//			networkEvent = NetworkTransport.ReceiveRelayEventFromHost(serverHostId, out error);
//			if (NetworkEventType.Nothing != networkEvent)
//			{
//				if (LogFilter.logDebug) { Debug.Log("NetGroup event:" + networkEvent); }
//			}
//			if (networkEvent == NetworkEventType.ConnectEvent)
//			{
//				if (LogFilter.logDebug) { Debug.Log("NetGroup server connected"); }
//			}
//			if (networkEvent == NetworkEventType.DisconnectEvent)
//			{
//				if (LogFilter.logDebug) { Debug.Log("NetGroup server disconnected"); }
//			}
//		}

		//do
		{
			//networkEvent = NetworkTransport.ReceiveFromHost(serverHostId, out connectionId, out channelId, messageBuffer, (int)messageBuffer.Length, out receivedSize, out error);
			//networkEvent = NetworkTransport.Receive(out recvHostId, out connectionId, out channelId, msgBuffer, bufferSize, out receivedSize, out error);
			networkEvent = NetworkTransport.Receive(out recvHostId, out connectionId, out channelId, msgBuffer, bufferSize, out receivedSize, out error);

			if (networkEvent != NetworkEventType.Nothing)
			{
				if (LogFilter.logDebug) { Debug.Log("Server event: host=" + recvHostId + " event=" + networkEvent + " error=" + error); }
			}

			switch (networkEvent)
			{
			case NetworkEventType.ConnectEvent:
				{
					//HandleConnect(connectionId, error);
					Debug.Log("Server-Connect");
					break;
				}

			case NetworkEventType.DataEvent:
				{
					//HandleData(connectionId, channelId, receivedSize, error);
					Debug.Log("Server-Data");
					break;
				}

			case NetworkEventType.DisconnectEvent:
				{
					//HandleDisconnect(connectionId, error);
					Debug.Log("Server-Disconnect");
					break;
				}

			case NetworkEventType.Nothing:
				break;

			default:
				if (LogFilter.logError) { Debug.LogError("Unknown network message type received: " + networkEvent); }
				break;
			}
		}
		//while (networkEvent != NetworkEventType.Nothing);

//		UpdateConnections();
	}

}
