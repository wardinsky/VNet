using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class VNetVOIPPlayback : MonoBehaviour {

	AudioSource source;
	AudioClip audioClip;

	int lastChunkStart = -1;

	// Use this for initialization
	void Start () {
		audioClip = AudioClip.Create("VOIP", 8192, 1, 8192, false);
		source = GetComponent<AudioSource>();

		VNetLib.VNetDispatch.RegisterListenerInst<VNetMessageVOIPData>(this.OnAudioDataIn);

	}

	void OnDestroy()
	{
		VNetLib.VNetDispatch.UnregisterListenerInst<VNetMessageVOIPData>(this.OnAudioDataIn);
	}

	void OnAudioDataIn(VNetMessageVOIPData voip)
	{
		if (voip.offsetIndex + voip.blockLength > 8192)
		{
			source.clip = audioClip;
			source.Play();
		}		
		audioClip.SetData(voip.voiceData, voip.offsetIndex);
	}
}
