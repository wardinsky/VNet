using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;
using UnityEditor;

public class VNetSpawnPrefab : MonoBehaviour
{	
	public GameObject m_prefab;
	public GameObject PrefabInstance { get; private set; }

	private ulong spawnedPrefabID;
	private float netSpawnAtTime;
	private Vector3 spawnPos;
	private Quaternion spawnRot;
	private ulong spawnOwnerID;
	private ulong spawnObjectGUID;

	[HideInInspector]
	public ulong netIdentifier;

	void Start()
	{
		if (netIdentifier == 0)
			throw new UnityException("VNetSpawnPrefab must have a net identifier!");
		
		PrefabInstance = null;
		netSpawnAtTime = 0;
		enabled = false;
		VNetTransformManager.Inst.RegisterPrefab(this);
	}
	void OnDestroy()
	{
        if (VNetTransformManager.Inst != null) {
            VNetTransformManager.Inst.RemovePrefab(this);
        }
	}


	public void SpawnPrefab(Vector3 pos, Quaternion rotation, bool takeLocalControl)
	{
		// Start this...
		enabled = true;

		PrefabInstance = null;

		netSpawnAtTime = VNetSessionTime.Inst.GetServerTime() + VNetManager.Inst.PrefabSpawnDelay;
		spawnPos = pos;
		spawnRot = rotation;
		spawnOwnerID = takeLocalControl ? VNet.Inst.GetUID() : VNetCommon.NET_CLIENT_INVALID_UID;
		spawnObjectGUID = VNetUtils.GenerateUID();

		// create a message and send it out 

		VNetMessageSpawnPrefab spawn = new VNetMessageSpawnPrefab();
		spawn.transformUID = netIdentifier;
		spawn.localControlUID = spawnOwnerID;
		spawn.time = netSpawnAtTime;
		spawn.localPosition = pos;
		spawn.localRotation = rotation;
		spawn.objectGUID = spawnObjectGUID;
		VNet.Inst.SendToLobby(spawn, true);
	}
	public void SpawnNetPrefab(VNetMessageSpawnPrefab message)
	{
		netSpawnAtTime = message.time;
		spawnPos = message.localPosition;
		spawnRot = message.localRotation;
		spawnOwnerID = message.localControlUID;
		spawnObjectGUID = message.objectGUID;
		enabled = true;
	}


	public void Update()
	{
		if (netSpawnAtTime == 0)
			return;

		if (netSpawnAtTime > VNetSessionTime.Inst.GetServerTime())
			return;

		netSpawnAtTime = 0;
		enabled = false;

		// Spawn the prefab
		PrefabInstance = GameObject.Instantiate(m_prefab);
		PrefabInstance.transform.position = spawnPos;
		PrefabInstance.transform.rotation = spawnRot;
		VNetTransform netTrans = PrefabInstance.GetComponent<VNetTransform>();
		if (netTrans)
		{
			netTrans.netIdentifier = spawnObjectGUID;
			if (spawnOwnerID != VNetCommon.NET_CLIENT_INVALID_UID)
			{
				if (spawnOwnerID == VNet.Inst.GetUID())
					netTrans.TakeLocalControl();
				else
					netTrans.RemoteTakeControl(netSpawnAtTime, spawnOwnerID);
		
			}
		}
	}

}
[CustomEditor( typeof (VNetSpawnPrefab))]
class VNetSpawnPrefabInspector : Editor
{
	public override void OnInspectorGUI()
	{
		VNetSpawnPrefab me = (target as VNetSpawnPrefab);

		GUIStyle style = new GUIStyle(GUI.skin.button);
		style.normal.textColor = Color.red;
		if (me.netIdentifier == 0 &&
			GUILayout.Button("Generate Net UID", style))
		{
			me.netIdentifier = VNetUtils.GenerateUIDInEditor();
		}

		base.OnInspectorGUI();
	}
}



public class VNetMessageSpawnPrefab : VNetMessage
{
	public VNetMessageSpawnPrefab() : base("VNetMessageSpawnPrefab") { }

	public ulong transformUID;
	public float time;
	public Vector3 localPosition;
	public Quaternion localRotation;
	public ulong localControlUID;
	public ulong objectGUID;
	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(transformUID);
		writer.Write(time);
		writer.Write(localPosition.x);
		writer.Write(localPosition.y);
		writer.Write(localPosition.z);

		writer.Write(localRotation.x);
		writer.Write(localRotation.y);
		writer.Write(localRotation.z);
		writer.Write(localRotation.w);

		writer.Write(localControlUID);
		writer.Write(objectGUID);


	}

	public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
		transformUID = reader.ReadUInt64();
		time = reader.ReadSingle();

		float x = reader.ReadSingle();
		float y = reader.ReadSingle();
		float z = reader.ReadSingle();
		localPosition = new Vector3(x, y, z);

		x = reader.ReadSingle();
		y = reader.ReadSingle();
		z = reader.ReadSingle();
		float w = reader.ReadSingle();
		localRotation = new Quaternion(x, y, z, w);

		localControlUID = reader.ReadUInt64();
		objectGUID = reader.ReadUInt64();

	}
	public override VNetMessage Clone()
	{
		VNetMessageSpawnPrefab clone = (VNetMessageSpawnPrefab)base.Clone();
		clone.transformUID = transformUID;
		clone.time = time;
		clone.localPosition = localPosition;
		clone.localRotation = localRotation;
		clone.localControlUID = localControlUID;
		clone.objectGUID = objectGUID;
		return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetMessageSpawnPrefab();
	}

}
