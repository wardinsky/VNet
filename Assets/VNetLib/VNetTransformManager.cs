using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VNetLib;
using System.IO;
using System;

public class VNetTransformManager : MonoBehaviour {

	public static VNetTransformManager Inst;

	public void Awake()
	{
		Inst = this;
		m_networkTransforms = new Dictionary<ulong, VNetTransform>();
		m_spawnPrefabs = new Dictionary<ulong, VNetSpawnPrefab>();
		VNetDispatch.RegisterListenerInst<VNetMessageTransformControl>(this.OnTransformControlMessage);
		VNetDispatch.RegisterListenerInst<VNetMessageNetTransformData>(this.OnTransformDataMessage);
		VNetDispatch.RegisterListenerInst<VNetMessageSpawnPrefab>(this.OnSpawnPrefabMessage);
		VNetDispatch.RegisterListenerInst<VNetMessageRunCoroutine>(this.OnRunCoroutineMessage);

		VNetSession.Inst.ClientAddedCallback += OnNewClient;
		VNetSession.Inst.ClientRemovedCallback += OnRemoveClient;

	}
	void OnDestroy()
	{
		VNetDispatch.UnregisterListenerInst<VNetMessageTransformControl>(this.OnTransformControlMessage);
		VNetDispatch.UnregisterListenerInst<VNetMessageNetTransformData>(this.OnTransformDataMessage);
		VNetDispatch.UnregisterListenerInst<VNetMessageSpawnPrefab>(this.OnSpawnPrefabMessage);
		VNetDispatch.UnregisterListenerInst<VNetMessageRunCoroutine>(this.OnRunCoroutineMessage);

		VNetSession.Inst.ClientAddedCallback -= OnNewClient;
		VNetSession.Inst.ClientRemovedCallback -= OnRemoveClient;

        Inst = null;
    }

    public void Disconnect()
	{
		foreach (VNetTransform trans in m_networkTransforms.Values)
			trans.StopControl();
	}

	public void RegisterPrefab(VNetSpawnPrefab prefab)
	{
		m_spawnPrefabs.Add(prefab.netIdentifier, prefab);
	}
	public void RemovePrefab(VNetSpawnPrefab prefab)
	{
		m_spawnPrefabs.Remove(prefab.netIdentifier);
	}

	// Add a network transform to this list. must have a unique ID
	public void RegisterTransform(VNetTransform transform)
	{
		m_networkTransforms.Add(transform.netIdentifier, transform);
	}

	// Remove a network transform from the list of all registered transforms (and children)
	public void RemoveTransform(VNetTransform transform)
	{
		m_networkTransforms.Remove(transform.netIdentifier);		
	}

	public void OnTransformDataMessage(VNetMessageNetTransformData message)
	{
		// Get out the transform from the remote control stack
		if (!m_networkTransforms.ContainsKey(message.transformUID))
			return;
		VNetTransform trans = m_networkTransforms[message.transformUID];
		trans.AddRemoteDataPoint(message);
	}

	// Tell a transform that it's control was changed
	public void OnTransformControlMessage(VNetMessageTransformControl message)
	{
		if (m_networkTransforms.ContainsKey(message.transformUID) == false)
			return;

		VNetTransform trans = m_networkTransforms [message.transformUID];
		if (message.clientUID == VNetCommon.NET_CLIENT_INVALID_UID &&
		    message._client.GetUID() == trans.controllingClient)
		{
			trans.RemoteRevokeControl();
			return;
		}

		trans.RemoteTakeControl(message.requestTime, message.clientUID);			
	}

	public void OnSpawnPrefabMessage(VNetMessageSpawnPrefab message)
	{
		if (m_spawnPrefabs.ContainsKey(message.transformUID) == false)
			return;

		VNetSpawnPrefab pref = m_spawnPrefabs [message.transformUID];
		pref.SpawnNetPrefab(message);
	}

	public void OnRunCoroutineMessage(VNetMessageRunCoroutine message)
	{		// Get out the object
		if (m_networkTransforms.ContainsKey(message.networkID) == false)
			return;

		double currentTime = VNetSessionTime.Inst.GetServerTimePrecise();
		VNetTransform trans = m_networkTransforms [message.networkID];
		MonoBehaviour comp = trans.GetComponent(message.componentType) as MonoBehaviour;

		StartCoroutine(StartCoroutineDelayed((float)(message.netTimeStart - currentTime), comp, message.coroutineName));

	}
	public IEnumerator StartCoroutineDelayed(float wait, MonoBehaviour component, string coroutineName)
	{
		yield return new WaitForSeconds(wait);
		component.StartCoroutine(coroutineName);
	}

	public void OnNewClient(VNetClient clientUID)
	{
        int numSynced = 0;
        VNetMessageTransformControl controlMessage = new VNetMessageTransformControl();
        foreach (VNetTransform trans in m_networkTransforms.Values) {
            if (trans.controllingClient != VNet.Inst.GetUID())
                continue;

            controlMessage.transformUID = trans.netIdentifier;
            controlMessage.clientUID = VNet.Inst.GetUID();
            controlMessage.requestTime = trans.localControlStartTime;
            numSynced++;
            clientUID.SendNetMessage(controlMessage, true);
        }

        Debug.Log("Syncing " + numSynced + " VNetTransforms for new client!");
    }

	public void OnRemoveClient(VNetClient clientUID)
	{
		foreach (VNetTransform trans in m_networkTransforms.Values)
		{
			if (trans.isRemoteControlled && trans.controllingClient == clientUID.GetUID())
				trans.RemoteRevokeControl();
		}
	}


	// List of transforms registered
	Dictionary<ulong, VNetTransform> m_networkTransforms;
	Dictionary<ulong, VNetSpawnPrefab> m_spawnPrefabs;

}

// Messages for vnet transforms

public class VNetMessageTransformControl : VNetMessage
{
	public VNetMessageTransformControl() : base("VNetMessageTransformControl") { }
	public ulong transformUID;
	public ulong clientUID;
	public float requestTime;

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(transformUID);
		writer.Write(clientUID);
		writer.Write(requestTime);
	}

	public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
		transformUID = reader.ReadUInt64();
		clientUID = reader.ReadUInt64();
		requestTime = reader.ReadSingle();
	}
	public override VNetMessage Clone()
	{
		VNetMessageTransformControl clone = (VNetMessageTransformControl)base.Clone();
		clone.transformUID = transformUID;
		clone.clientUID = clientUID;
		clone.requestTime = requestTime;
		return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetMessageTransformControl();
	}
}