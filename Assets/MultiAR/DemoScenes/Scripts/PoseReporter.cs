using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseReporter : MonoBehaviour 
{

	public UnityEngine.UI.Text locationText;
	public UnityEngine.UI.Text debugText;

	private MultiARManager marManager;
	private Transform cameraTransform;

	private bool locServiceStarted = false;
	private double lastLocTimestamp = 0.0;


	IEnumerator Start()
	{
		// get reference to multi-ar-manager
		marManager = MultiARManager.Instance;

		// Start service before querying location
		locServiceStarted = false;
		Input.location.Start(1f, 0.1f);

		if (locationText) 
		{
			locationText.text = "Location service is starting...";
		}

//		// First, check if user has location service enabled
//		if (!Input.location.isEnabledByUser) 
//		{
//			if (locationText) 
//			{
//				locationText.text = "Location not enabled by the user.";
//			}
//
//			yield break;
//		}

		// Wait until service initializes
		int maxWait = 20;
		while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
		{
			yield return new WaitForSeconds(1);
			maxWait--;
		}

		// Service didn't initialize in 20 seconds
		if (maxWait < 1)
		{
			if (locationText) 
			{
				locationText.text = "Timed out.";
			}

			yield break;
		}

		// Connection has failed
		if (Input.location.status == LocationServiceStatus.Failed)
		{
			if (locationText) 
			{
				locationText.text = "Location service cannot be started.";
			}

			yield break;
		}

		// service is started
		locServiceStarted = true;
		if (locationText) 
		{
			locationText.text = "Location service successfully started!";
		}
	}

	void OnDestroy()
	{
		// Stop service if there is no need to query location updates continuously
		if (locServiceStarted) 
		{
			Input.location.Stop();
		}
	}

	void Update () 
	{
		if (!cameraTransform) 
		{
			Camera camera = marManager ? marManager.GetMainCamera() : null;
			cameraTransform = camera ? camera.transform : null;
		}

		if (debugText) 
		{
			string sMessage = string.Empty;
			Vector3 pos = Vector3.zero;
			Vector3 ori = Vector3.zero;

			if (cameraTransform) 
			{
				pos = cameraTransform.position;
				ori = cameraTransform.rotation.eulerAngles;

				sMessage += string.Format("Camera - Pos: ({0:F2}, {1:F2}, {2:F2}), Rot: ({3:F0}, {4:F0}, {5:F0})\n", pos.x, pos.y, pos.z, ori.x, ori.y, ori.z);
			}

			pos = transform.position;
			ori = transform.rotation.eulerAngles;

			sMessage += string.Format("TrPose - Pos: ({0:F2}, {1:F2}, {2:F2}), Rot: ({3:F0}, {4:F0}, {5:F0})\n", pos.x, pos.y, pos.z, ori.x, ori.y, ori.z);

			if (Input.location.status == LocationServiceStatus.Running && Input.location.lastData.timestamp != lastLocTimestamp) 
			{
				LocationInfo locLast = Input.location.lastData;
				lastLocTimestamp = locLast.timestamp;

				string sLocMessage = string.Format("Lat: {0:F8}, Long: {1:F8}, Alt: {2:F8} @ {3:F3}\n", locLast.latitude, locLast.longitude, locLast.altitude, locLast.timestamp);
				sMessage += sLocMessage;

				if (locationText) 
				{
					locationText.text = sLocMessage;
				}
			}

			int pcLength = marManager ? marManager.GetPointCloudLength() : 0;
			sMessage += string.Format("PointCloud: {0} points", pcLength);

			debugText.text = sMessage;
		}
	}
}
