using UnityEngine;
using System.Collections;

public class VNetTextMeshToNetTime : MonoBehaviour {

	TextMesh textMesh;

	void Start()
	{
		textMesh = GetComponent<TextMesh>();
	}
	// Update is called once per frame
	void Update () {

		textMesh.text = VNetLib.VNetSessionTime.Inst.GetServerTime().ToString();

		VNetManager.Inst.RunCoroutineSynced(this, "ChangeTextColor");
	}

	IEnumerator ChangeTextColor()
	{
		textMesh.color = Color.red;
		yield return null;
	}
}
