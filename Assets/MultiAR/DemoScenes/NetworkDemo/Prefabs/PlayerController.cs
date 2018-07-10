using UnityEngine;
using UnityEngine.Networking;


public class PlayerController : NetworkBehaviour // MonoBehaviour
{

	public GameObject bulletPrefab;

	public Transform bulletSpawn;

	// reference to the Multi-AR manager
	private MultiARManager arManager = null;
	private Camera arMainCamera = null;

	// reference to the ar-client
	private ArClientBaseController arClient = null;


	void Start()
	{
		arManager = MultiARManager.Instance;
		arClient = ArClientBaseController.Instance;
	}


	void Update()
	{
		// check for local & active player
		if (!isLocalPlayer)
			return;
		if (!gameObject.activeSelf)
			return;

		if (!arMainCamera && arManager && arManager.IsInitialized()) 
		{
			arMainCamera = arManager.GetMainCamera();
		}

		if (arMainCamera) 
		{
			transform.position = arMainCamera.transform.position;
			transform.rotation = arMainCamera.transform.rotation;
		}

		// fire when clicked (world anchor must be present)
		if (arClient && arClient.WorldAnchorObj != null &&
			arManager && arManager.IsInitialized() && arManager.IsInputAvailable(true))
		{
			MultiARInterop.InputAction action = arManager.GetInputAction();

			if (action == MultiARInterop.InputAction.Click)
			{
				CmdFire();
			}
		}
	}


	[Command]
	void CmdFire()
	{
		// Create the Bullet from the Bullet Prefab
		var bullet = (GameObject)Instantiate(
			bulletPrefab,
			bulletSpawn.position,
			bulletSpawn.rotation);

		// Set the player-owner
		bullet.GetComponent<BulletScript>().playerOwner = gameObject;

		// Add velocity to the bullet
		bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 6;

		// Spawn the bullet on the Clients
		NetworkServer.Spawn(bullet);

		// Destroy the bullet after 2 seconds
		Destroy(bullet, 2.0f);
	}

}

