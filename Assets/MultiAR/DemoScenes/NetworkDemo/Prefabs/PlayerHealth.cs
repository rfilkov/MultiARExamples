using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public class PlayerHealth : NetworkBehaviour 
{

	public const int maxHealth = 100;

	public bool destroyOnDeath = false;

	[SyncVar(hook = "OnChangeHealth")]
	public int currentHealth = maxHealth;

	public RectTransform healthBar;


	public void TakeDamage(int amount)
	{
		if (!isServer)
		{
			return;
		}

		currentHealth -= amount;

		if (currentHealth <= 0)
		{
			if (destroyOnDeath)
			{
				Destroy(gameObject);
			} 
			else
			{
				Debug.Log("Dead!");

				RpcSetActive(false);
				StartCoroutine(RevivePlayerAfterTime());
			}
		}
	}


	void OnChangeHealth (int health)
	{
		if (healthBar) 
		{
			healthBar.sizeDelta = new Vector2(health, healthBar.sizeDelta.y);
		}
	}
	
	private IEnumerator RevivePlayerAfterTime()
	{
		yield return new WaitForSeconds(10f);

		currentHealth = maxHealth;

		// called on the Server, will be invoked on the Clients
		RpcSetActive(true);
	}

	[ClientRpc]
	void RpcSetActive(bool isActive)
	{
		if (isLocalPlayer)
		{
			gameObject.SetActive(isActive);
		}
	}

}
