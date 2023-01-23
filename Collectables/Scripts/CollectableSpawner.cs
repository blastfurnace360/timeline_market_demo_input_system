using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GlobalInput;

public class CollectableSpawner : MonoBehaviour
{
	public GameObject[] collectablesToSpawn;
	public int xRange;
	public int zRange;
	public int yHeight;
	
	private bool coolDown = false;
	private float cooldownTimer = 0.0f;
	private float cooldownPeriod = 0.5f;

	void Update ()
	{
		if (coolDown && cooldownTimer > 0) cooldownTimer -= Time.deltaTime;
		if (coolDown && cooldownTimer <= 0) coolDown = false;
		
		if (!coolDown && InputManager.instance.spawnCollectableButtonDown) {
			SpawnObject();
			coolDown = true;
			cooldownTimer = cooldownPeriod;
		}
	}

	void SpawnObject ()
	{
		int randomCollectable = Random.Range (0, collectablesToSpawn.Length);
		int xPosition = Random.Range (-xRange, xRange);
		int zPosition = Random.Range (-zRange, zRange);
		Vector3 randomPosition = new Vector3 (xPosition, yHeight, zPosition);
		Instantiate (collectablesToSpawn [randomCollectable], transform.position + randomPosition, transform.rotation);
	}
}
