using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSampler : MonoBehaviour 
{

	private ParticleSystem ps;

	void Start () 
	{
		ps = GetComponent<ParticleSystem>();
		StartCoroutine(SampleParticleRoutine());
	}


	// starts particles at high speed for a 0.1s, and then slows them down
	IEnumerator SampleParticleRoutine()
	{
		var main = ps.main;
		main.simulationSpeed = 1000f;
		ps.Play();

		yield return new WaitForSeconds(0.1f);

		main.simulationSpeed = 0.05f;
	}

}
