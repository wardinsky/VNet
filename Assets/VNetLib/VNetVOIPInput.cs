using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;
using System;
using System.Collections.Generic;

public class VNetVOIPInput : MonoBehaviour {

	AudioClip m_audioClip;
	bool isActive;

	float[] m_samples;
	float[] m_activeSamples;

	public int numDifferent;
	public int firstDifferent;
	public int lastDifferent;

	public int currentVolume;

	VNetMessageVOIPData m_dataMessage;

	void Start()
	{
		if (Microphone.devices.Length == 0)
		{
			isActive = false;
			return;
		}

		m_audioClip = Microphone.Start(Microphone.devices[0], true, 1, 8192);

		m_samples = new float[8192];
		m_activeSamples = new float[8192];

		m_audioClip.GetData(m_samples, 0);
		lastDifferent = 0;
		firstDifferent = 8191;

		m_dataMessage = new VNetMessageVOIPData();
		m_dataMessage.voiceData = new float[200];

		isActive = true;
	}


	void Update()
	{
		if (!isActive)
		{
			return;
		}

		m_audioClip.GetData(m_activeSamples, 0);
		numDifferent = 0;


				
			


		firstDifferent = -1;
		lastDifferent = -1;
		for (int i = 0; i < m_activeSamples.Length; i++)
		{
			if (m_activeSamples [i] != m_samples [i])
			{
				numDifferent++;
				if (firstDifferent == -1)
					firstDifferent = i;
				lastDifferent = i;

				m_samples [i] = m_activeSamples [i];
			}
		}

		if (firstDifferent == -1)
			return;

		// Round first different to nearest multiple of 20
	

		PackClip();
	}

	void PackClip()
	{

		int adjustedLast = lastDifferent;
		if (firstDifferent > lastDifferent)
			adjustedLast = lastDifferent + 8192;
		int numSamples = adjustedLast - firstDifferent;

		int curOffset = 0;
		while (numSamples > 0)
		{
			int remaining = Mathf.Min(200, numSamples);
			if (firstDifferent + remaining < adjustedLast)
			{
				
				m_dataMessage.offsetIndex = firstDifferent;
				m_dataMessage.blockLength = remaining;
				for (int i = 0; i < remaining; i++)
					m_dataMessage.voiceData [i] = m_activeSamples [(firstDifferent + i)%8192];

				// Send it
				VNet.Inst.SendToLobby(m_dataMessage, false);					
			
				firstDifferent = (firstDifferent + remaining) % 8192;
			}
			numSamples -= remaining;
		}

	}
}




public class VNetMessageVOIPData : VNetMessage
{
	public VNetMessageVOIPData() : base("VNetMessageVOIPData") { }
	public float recordTime;
	public int offsetIndex;
	public int blockLength;
	public float[] voiceData;

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(recordTime);
		writer.Write(offsetIndex);
		writer.Write(blockLength);
		writer.Write(voiceData.Length);
		for (int i = 0; i < voiceData.Length; i++)
			writer.Write(voiceData[i]);
	}

	public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
		recordTime = reader.ReadSingle();
		offsetIndex = reader.ReadInt32();
		blockLength = reader.ReadInt32();
		int len = reader.ReadInt32();
		voiceData = new float[len];
		for (int i = 0; i < len; i++)
			voiceData[i] = reader.ReadSingle();
	}
	public override VNetMessage Clone()
	{
		VNetMessageVOIPData clone = (VNetMessageVOIPData)base.Clone();
		clone.recordTime = recordTime;
		clone.offsetIndex = offsetIndex;
		clone.blockLength = blockLength;
		clone.voiceData = new float[voiceData.Length];
		for (int i = 0; i < voiceData.Length; i++)
			clone.voiceData [i] = voiceData [i];
		return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetMessageVOIPData();
	}
}
