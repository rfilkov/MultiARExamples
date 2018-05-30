using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


/// <summary>
/// Network Message Types.
/// </summary>
public struct NetMsgType
{
	
	public const short GetGameAnchorRequest = 1001;

	public const short GetGameAnchorResponse = 1002;

	public const short CheckHostAnchorRequest = 1003;

	public const short CheckHostAnchorResponse = 1004;

	public const short SetGameAnchorRequest = 1005;

	public const short SetGameAnchorResponse = 1006;

	public const short SetClientPoseRequest = 1007;

	//public const short SetClientPoseResponse = 1008;

}


public struct NetClientData
{
	public string ipAddress;
	public float timestamp;
	public Transform transform;
	public Pose clientPose;
	public Pose localPose;
}


/// <summary>
/// Get-game-anchor request message.
/// </summary>
public class GetGameAnchorRequestMsg : MessageBase
{
	public string gameName;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(gameName);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		gameName = reader.ReadString();
	}
}


/// <summary>
/// Get-game-anchor response message.
/// </summary>
public class GetGameAnchorResponseMsg : MessageBase
{
	public bool found;
	public string anchorId;
	//public string apiKey;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(found);
		writer.Write(anchorId);
		//writer.Write(apiKey);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		found = reader.ReadBoolean();
		anchorId = reader.ReadString();
		//apiKey = reader.ReadString();
	}
}


/// <summary>
/// Check-host-anchor request message.
/// </summary>
public class CheckHostAnchorRequestMsg : MessageBase
{
	public string gameName;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(gameName);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		gameName = reader.ReadString();
	}
}


/// <summary>
/// Check-host-anchor response message.
/// </summary>
public class CheckHostAnchorResponseMsg : MessageBase
{
	public bool granted;
	//public string apiKey;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(granted);
		//writer.Write(apiKey);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		granted = reader.ReadBoolean();
		//apiKey = reader.ReadString();
	}
}


/// <summary>
/// Set-game-anchor request message.
/// </summary>
public class SetGameAnchorRequestMsg : MessageBase
{
	public string gameName;
	public string anchorId;
	public Vector3 anchorPos;
	public Quaternion anchorRot;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);

		writer.Write(gameName);
		writer.Write(anchorId);
		writer.Write(anchorPos);
		writer.Write(anchorRot);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);

		gameName = reader.ReadString();
		anchorId = reader.ReadString();
		anchorPos = reader.ReadVector3();
		anchorRot = reader.ReadQuaternion();
	}
}


/// <summary>
/// Set-game-anchor response message.
/// </summary>
public class SetGameAnchorResponseMsg : MessageBase
{
	public bool confirmed;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(confirmed);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		confirmed = reader.ReadBoolean();
	}
}


/// <summary>
/// Set-client-pose request message.
/// </summary>
public class SetClientPoseRequestMsg : MessageBase
{
	public Vector3 clientPos;
	public Quaternion clientRot;
	public Vector3 localPos;
	public Quaternion localRot;

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);

		writer.Write(clientPos);
		writer.Write(clientRot);
		writer.Write(localPos);
		writer.Write(localRot);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);

		clientPos = reader.ReadVector3();
		clientRot = reader.ReadQuaternion();
		localPos = reader.ReadVector3();
		localRot = reader.ReadQuaternion();
	}
}


///// <summary>
///// Set-client-pose response message.
///// </summary>
//public class SetClientPoseResponseMsg : MessageBase
//{
//	public bool confirmed;
//
//	public override void Serialize(NetworkWriter writer)
//	{
//		base.Serialize(writer);
//		writer.Write(confirmed);
//	}
//
//	public override void Deserialize(NetworkReader reader)
//	{
//		base.Deserialize(reader);
//		confirmed = reader.ReadBoolean();
//	}
//}


