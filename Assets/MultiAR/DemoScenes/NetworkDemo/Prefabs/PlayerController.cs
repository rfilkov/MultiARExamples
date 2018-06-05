using UnityEngine;
using UnityEngine.Networking;


public class PlayerController : NetworkBehaviour // MonoBehaviour
{

	public GameObject bulletPrefab;

	public Transform bulletSpawn;

	// reference to the Multi-AR manager
	private MultiARManager arManager;
	private Camera arMainCamera;


	void Start()
	{
		arManager = MultiARManager.Instance;
	}


	void Update()
	{
		// check for local player
		if (!isLocalPlayer)
		{
			return;
		}

//		var x = Input.GetAxis("Horizontal") * Time.deltaTime * 150.0f;
//		var z = Input.GetAxis("Vertical") * Time.deltaTime * 3.0f;
//
//		transform.Rotate(0, x, 0);
//		transform.Translate(0, 0, z);

		if (!arMainCamera && arManager) 
		{
			arMainCamera = arManager.GetMainCamera();
		}

		if (arMainCamera) 
		{
			transform.position = arMainCamera.transform.position;
			transform.rotation = arMainCamera.transform.rotation;
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			CmdFire();
		}
	}


	[Command]
	void CmdFire()
	{
		// Create the Bullet from the Bullet Prefab
		var bullet = (GameObject)Instantiate (
			bulletPrefab,
			bulletSpawn.position,
			bulletSpawn.rotation);

		// Add velocity to the bullet
		bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 6;

		// Spawn the bullet on the Clients
		NetworkServer.Spawn(bullet);

		// Destroy the bullet after 2 seconds
		Destroy(bullet, 2.0f);
	}


	public override void OnStartLocalPlayer()
	{
		GetComponent<MeshRenderer>().material.color = Color.blue;
	}

}

