using UnityEngine;
using System.Collections;

using VNetLib;

public class VNetLibTest : MonoBehaviour {

	VNet vnet;


	// Use this for initialization
	void Start () {
		vnet = new VNet();
		vnet.Init();
		vnet.StartHosting(0);
	}
	
	// Update is called once per frame
	void Update () {
		vnet.Update();
	}
}
