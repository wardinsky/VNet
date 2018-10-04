using UnityEngine;
using System.Collections;
using VNetLib;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.IO;

public class VNetTransform : MonoBehaviour {

	//[HideInInspector]
	public ulong netIdentifier;
	public ulong controllingClient = VNetCommon.NET_CLIENT_INVALID_UID;

	// When clients try to control this, they will..
	public float localControlStartTime{ get; private set; }
	public bool isLocalControlled{ get; private set; }
	public bool isRemoteControlled{ get; private set; }
	float lastSendTime;
	bool usedGravity;
	bool isSleeping;

	TransformTrack m_clientPrev;
	TransformTrack m_clientPost;

	public class TransformTrack
	{
		public float netTime;
		public Vector3 pos;
		public Quaternion rot;
		public Vector3 vel;
		public Vector3 angularVel;
		public bool isSleeping;

		public void CopyFrom(TransformTrack prev)
		{
			netTime = prev.netTime;
			pos = prev.pos;
			rot = prev.rot;
			vel = prev.vel;
			angularVel = prev.angularVel;
			isSleeping = prev.isSleeping;
		}
	}

	public class TransformQueue
	{
		public TransformQueue()
		{
			m_queue = new Queue<TransformTrack>();
		}
		~TransformQueue()
		{
			m_queue.Clear();
		}
			
		public void ClearQueue()
		{
			m_queue.Clear();
		}
		public void AddDataPoint(TransformTrack track)
		{
			m_queue.Enqueue(track);
		}

		public void GetDataPoints(float time, out TransformTrack prev, out TransformTrack post)
		{
			prev = GetPrevPoint(time);
			post = GetPostPoint(time);
		}

		private TransformTrack GetPrevPoint(float time)
		{
			float bestTime = -1;
			TransformTrack best = null;
			foreach (TransformTrack t in m_queue)
			{
				if (t.netTime > bestTime && t.netTime <= time)
				{
					bestTime = t.netTime;
					best = t;
				}
			}
			if (best != null)
			{
				while (m_queue.Peek().netTime < bestTime)
					m_queue.Dequeue();
			}

			return best;					
		}

		private TransformTrack GetPostPoint(float time)
		{
			float bestTime = float.MaxValue;
			TransformTrack best = null;
			foreach (TransformTrack t in m_queue)
			{
				if (t.netTime < bestTime && t.netTime >= time)
				{
					bestTime = t.netTime;
					best = t;
				}
			}
			return best;
		}

		Queue<TransformTrack> m_queue;
	}

	TransformQueue m_transformQueue;
	// for RigidBody syncing
	Rigidbody rigidBody;

	void Start()
	{
		if (netIdentifier == 0)
			throw new UnityException("VNetTransform must have a Net Identifier!");
		
		VNetTransformManager.Inst.RegisterTransform(this);
		rigidBody = GetComponent<Rigidbody>();
		m_transformQueue = new TransformQueue();

		m_clientPrev = new TransformTrack();
		m_clientPost = new TransformTrack();


		if (rigidBody)
		{
			usedGravity = rigidBody.useGravity;
		}
		isSleeping = false;

	}

	void OnDestroy()
	{
        if (VNetTransformManager.Inst != null) {
            VNetTransformManager.Inst.RemoveTransform(this);
        }
	}

	// Update is called once per frame
	void Update ()
	{
		if (isRemoteControlled)
		{
			UpdateRemoteControl();
			return;
		}
		if (isLocalControlled)
		{
			// See where we expect the values to be...
			float serverTime = VNetSessionTime.Inst.GetServerTime();
			float t = (serverTime - m_clientPrev.netTime) / (m_clientPost.netTime - m_clientPrev.netTime);
			Vector3 localPos = Vector3.LerpUnclamped(m_clientPrev.pos, m_clientPost.pos, t);

			// compare this to current values
			float expectedOffSq = (localPos - transform.localPosition).sqrMagnitude;
			bool updateRemote = false;
			if (expectedOffSq > VNetManager.Inst.TransformPredictionSqDstOffset)
			{
				updateRemote = true;
			}

			if (!updateRemote)
			{
				Quaternion localRot = Quaternion.SlerpUnclamped(m_clientPrev.rot, m_clientPost.rot, t);
				Vector3 forw = localRot * Vector3.forward;
				Vector3 comp = transform.localRotation * Vector3.forward;

				if (Vector3.Dot(forw, comp) < VNetManager.Inst.TransformPredictionDotOffset)
				{
					updateRemote = true;
				}
			}
			if (updateRemote)
			{
				lastSendTime = serverTime;

				// Create and send a message
				VNetMessageNetTransformData data = new VNetMessageNetTransformData();
				data.transformUID = netIdentifier;
				data.time = serverTime;
				data.localPosition = transform.localPosition;
				data.localRotation = transform.localRotation;

				if (rigidBody != null)
				{
					data.rbSleeping = rigidBody.IsSleeping();
					data.rbVelocity = rigidBody.velocity;
					data.rbRotation = rigidBody.angularVelocity;
				}

				// This is now a reliable message
				VNet.Inst.SendToLobby(data, true);


				// Update the post and prev
				TransformTrack temp = m_clientPrev;
				m_clientPrev = m_clientPost;
				m_clientPost = temp;
				m_clientPost.netTime = lastSendTime;
				m_clientPost.pos = transform.localPosition;
				m_clientPost.rot = transform.localRotation;
				if (rigidBody != null)
				{
					data.rbSleeping = rigidBody.IsSleeping();
					data.rbVelocity = rigidBody.velocity;
					data.rbRotation = rigidBody.angularVelocity;
				}
			}
		}
	}



    public bool TakeLocalControl() {
        if (isLocalControlled)
            return true;

        if (isRemoteControlled)
            return false;

        localControlStartTime = VNetSessionTime.Inst.GetServerTime();
        isLocalControlled = true;

        TransformTrack track = new TransformTrack();
        track.netTime = VNetSessionTime.Inst.GetServerTime() - VNetManager.Inst.TransformUpdateDelay * 2;
        track.pos = transform.localPosition;
        track.rot = transform.localRotation;
        if (rigidBody) {
            track.angularVel = rigidBody.angularVelocity;
            track.vel = rigidBody.velocity;
            track.isSleeping = rigidBody.IsSleeping();
        }
        m_clientPrev = track;

        track = new TransformTrack();
        track.netTime = VNetSessionTime.Inst.GetServerTime() - VNetManager.Inst.TransformUpdateDelay;
        track.pos = transform.localPosition;
        track.rot = transform.localRotation;
        if (rigidBody) {
            track.angularVel = rigidBody.angularVelocity;
            track.vel = rigidBody.velocity;
            track.isSleeping = rigidBody.IsSleeping();
        }
        m_clientPost = track;

        VNetMessageTransformControl controlMessage = new VNetMessageTransformControl();
		controlMessage.transformUID = netIdentifier;
		controlMessage.clientUID = VNet.Inst.GetUID();
		controlMessage.requestTime = localControlStartTime;

		controllingClient = VNet.Inst.GetUID();

		VNet.Inst.SendToLobby(controlMessage, true);
		return true;
	}

	public void StopControl()
	{
		isLocalControlled = false;
		isRemoteControlled = false;
		localControlStartTime = 0;
		controllingClient = VNetCommon.NET_CLIENT_INVALID_UID;
	}

	public void RevokeLocalControl()
	{
		if (!isLocalControlled || isRemoteControlled)
			return;

		VNetMessageTransformControl controlMessage = new VNetMessageTransformControl();
		controlMessage.transformUID = netIdentifier;
		controlMessage.clientUID = VNetCommon.NET_CLIENT_INVALID_UID;
		controlMessage.requestTime = VNetSessionTime.Inst.GetServerTime();

		VNet.Inst.SendToLobby(controlMessage, true);

		isLocalControlled = false;
		localControlStartTime = 0;
		controllingClient = VNetCommon.NET_CLIENT_INVALID_UID;
	}

	public bool RemoteTakeControl(float time, ulong client)
	{
		bool pastTime = localControlStartTime > time;
		if (isLocalControlled == false || pastTime)
		{
			isRemoteControlled = true;
			controllingClient = client;
			if (pastTime)
			{
				localControlStartTime = 0;
				isLocalControlled = false;
			}

			TransformTrack track = new TransformTrack();
			track.netTime = VNetSessionTime.Inst.GetServerTime() - VNetManager.Inst.TransformUpdateDelay * 2;
			track.pos = transform.localPosition;
			track.rot = transform.localRotation;
			if (rigidBody)
			{
				track.angularVel = rigidBody.angularVelocity;
				track.vel = rigidBody.velocity;
				track.isSleeping = rigidBody.IsSleeping();
			}
			m_transformQueue.AddDataPoint(track);

			track = new TransformTrack();
			track.netTime = VNetSessionTime.Inst.GetServerTime() - VNetManager.Inst.TransformUpdateDelay;
			track.pos = transform.localPosition;
			track.rot = transform.localRotation;
			if (rigidBody)
			{
				track.angularVel = rigidBody.angularVelocity;
				track.vel = rigidBody.velocity;
				track.isSleeping = rigidBody.IsSleeping();
			}
			m_transformQueue.AddDataPoint(track);

			return true;
		}


		return false;
	}

	public void RemoteRevokeControl()
	{
		if (!isRemoteControlled)
			return;

		if (rigidBody)
		{
			rigidBody.useGravity = usedGravity;
		}
		isRemoteControlled = false;
		controllingClient = VNetCommon.NET_CLIENT_INVALID_UID;
	}


	public void AddRemoteDataPoint(VNetMessageNetTransformData message)
	{
		TransformTrack temp = new TransformTrack();
		temp.angularVel = message.rbRotation;
		temp.vel = message.rbVelocity;
		temp.pos = message.localPosition;
		temp.rot = message.localRotation;
		temp.isSleeping = message.rbSleeping;
		temp.netTime = message.time;

		m_transformQueue.AddDataPoint(temp);
	}

	void LerpRemote(TransformTrack a, TransformTrack b, float t)
	{
		if (a == null || b == null)
			return;
		transform.localPosition = Vector3.LerpUnclamped(a.pos, b.pos, t);
		transform.localRotation = Quaternion.LerpUnclamped(a.rot, b.rot, t);

		if (rigidBody != null)
		{
			rigidBody.velocity = Vector3.LerpUnclamped(a.vel, b.vel, t);
			rigidBody.angularVelocity = Vector3.LerpUnclamped(a.angularVel, b.angularVel, t);
		}
	}

	void UpdateRemoteControl()
	{
		float currentTime = VNetSessionTime.Inst.GetServerTime() - VNetManager.Inst.TransformUpdateDelay;
		TransformTrack prev;
		TransformTrack post;
		m_transformQueue.GetDataPoints(currentTime, out prev, out post);

		if (prev == null)
			LerpRemote(post, post, 0);
		else if (post == null)
			LerpRemote(prev, prev, 0);
		else if (post != null && prev != null)
		{
            float t = (currentTime - prev.netTime);
            if (post.netTime == prev.netTime)
                t = 1;
            else
                t /= (post.netTime - prev.netTime);
            LerpRemote(prev, post, t);
		}
	}
}


[CustomEditor( typeof (VNetTransform))]
class VNetTransformInspector : Editor
{
	public override void OnInspectorGUI()
	{
		VNetTransform me = (target as VNetTransform);
	
		GUIStyle style = new GUIStyle(GUI.skin.button);
		style.normal.textColor = Color.red;
		if (me.netIdentifier == 0 &&
			GUILayout.Button("Generate Net UID", style))
		{
			me.netIdentifier = VNetUtils.GenerateUIDInEditor();
		}

		if (me.isLocalControlled == false &&
			me.isRemoteControlled == false &&
			GUILayout.Button("Take Local Control"))
		{
			me.TakeLocalControl();
		}

		if (me.isLocalControlled &&
		    GUILayout.Button("Revoke Local Control"))
		{
			me.RevokeLocalControl();
		}


		base.OnInspectorGUI();
	}
}
	

public class VNetMessageNetTransformData : VNetMessage
{
	public VNetMessageNetTransformData() : base("VNetMessageNetTransformData") { }

	public ulong transformUID;
	public float time;
	public Vector3 localPosition;
	public Quaternion localRotation;
	public Vector3 rbVelocity;
	public Vector3 rbRotation;
	public bool rbSleeping;

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

		writer.Write(rbVelocity.x);
		writer.Write(rbVelocity.y);
		writer.Write(rbVelocity.z);

		writer.Write(rbRotation.x);
		writer.Write(rbRotation.y);
		writer.Write(rbRotation.z);
		writer.Write(rbSleeping);

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

		x = reader.ReadSingle();
		y = reader.ReadSingle();
		z = reader.ReadSingle();
		rbVelocity = new Vector3(x, y, z);

		x = reader.ReadSingle();
		y = reader.ReadSingle();
		z = reader.ReadSingle();
		rbRotation = new Vector3(x, y, z);

		rbSleeping = reader.ReadBoolean();

	}
	public override VNetMessage Clone()
	{
		VNetMessageNetTransformData clone = (VNetMessageNetTransformData)base.Clone();
		clone.transformUID = transformUID;
		clone.time = time;
		clone.localPosition = localPosition;
		clone.localRotation = localRotation;
		clone.rbVelocity = rbVelocity;
		clone.rbRotation = rbRotation;
		clone.rbSleeping = rbSleeping;
		return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetMessageNetTransformData();
	}

}