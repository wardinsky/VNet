using UnityEngine;
using System.Collections;

public class ShootFromCamera : MonoBehaviour {

	VNetSpawnPrefab prefabSpawner;
	bool waiting;


	// Use this for initialization
	void Start () {
		prefabSpawner = GetComponent<VNetSpawnPrefab>();
		waiting = false;
	}
	
	// Update is called once per frame
	void Update () {
	
		if (Input.GetKeyDown(KeyCode.Space))
		{
			prefabSpawner.SpawnPrefab(Camera.main.transform.position, Camera.main.transform.rotation, true);
			waiting = true;
		}

		GameObject inst = prefabSpawner.PrefabInstance;
		if (inst && waiting)
		{
			waiting = false;
			Rigidbody rb = inst.GetComponent<Rigidbody>();
			rb.velocity = Camera.main.transform.forward * 15;
			rb.angularVelocity = Random.insideUnitSphere * 5;
		}

	}
}
