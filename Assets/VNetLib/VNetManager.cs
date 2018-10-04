using UnityEngine;
using System.Collections;
using VNetLib;
using System.Collections.Generic;

public class VNetManager : MonoBehaviour {

	public uint GUIDHeader = 0x52231234;
	public uint TransformUpdatesPerSecond = 5;

	public float TransformPredictionDotOffset = .1f;
	public float TransformPredictionSqDstOffset = .1f;

	public float PrefabSpawnDelay = .3f;
	public float CoroutineStartDelay = .2f;
	public float LookForHostTime = 4.0f;
	public float TransformUpdateDelay = .3f;

	public string MulticastAddress = "238.0.0.1";
	public string MulticastV6Address = "ff15::1";
	public bool UseIPv6 = false;


	[HideInInspector]
	public static VNetManager Inst;

    public enum ConnectionType { NotSpecified, Host, Client }
    public static ConnectionType NextGameConnectionType = ConnectionType.NotSpecified;

    // Connection state
    public bool NoConnection;
	public bool LocalIsClient;
	public bool LocalIsHost;
	public bool LocalHostIsScientist = true;

	public bool DebugPlayAsClient;

	private bool DoReconnectButton = false;

	// The network
	VNet vnet;
	VNetTransformManager vTransformMan;

	private float AttemptToConnectTimer;
	private ulong AttemptingToConnectGUID;
	private int AttemptingToConnectRole;

	public List<GameObject> DisableOnConnect;
	public List<GameObject> EnableOnConnect;

	public List<GameObject> Player1EnableOnConnect;
	public List<GameObject> Player1DisableOnConnect;
	public List<VNetTransform> Player1TakeControlOnConnect;

	public List<GameObject> Player2EnableOnConnect;
	public List<GameObject> Player2DisableOnConnect;
	public List<VNetTransform> Player2TakeControlOnConnect;


	// Use this for initialization
	void Awake () {
        Debug.Log("NextGameConnectionType: " + NextGameConnectionType);

		Inst = this;		
		vnet = new VNet();
		vnet.Init();


		vTransformMan = GetComponent<VNetTransformManager>();

		// This needs to update at least once a second
		if (TransformUpdatesPerSecond == 0)
			TransformUpdatesPerSecond = 1;

		NoConnection = true;
		LocalIsHost = false;
		LocalIsClient = false;



		StartAttemptToConnect();

	}

	void EnableDisableConnectionObjects(int player)
	{
		foreach (GameObject obj in DisableOnConnect)
			obj.SetActive(false);
		foreach (GameObject obj in EnableOnConnect)
			obj.SetActive(true);

		if (player == 1)
		{
			foreach (GameObject obj in Player1EnableOnConnect)
				obj.SetActive(true);
			foreach (GameObject obj in Player1DisableOnConnect)
				obj.SetActive(false);

			foreach (VNetTransform trans in Player1TakeControlOnConnect)
				trans.TakeLocalControl();
		}
		if (player == 2)
		{
			foreach (GameObject obj in Player2EnableOnConnect)
				obj.SetActive(true);
			foreach (GameObject obj in Player2DisableOnConnect)
				obj.SetActive(false);

			foreach (VNetTransform trans in Player2TakeControlOnConnect)
				trans.TakeLocalControl();
		}
	}

	void EndSession()
	{
		vnet.DisconnectAll();
		VNetTransformManager.Inst.Disconnect();
		NoConnection = true;
		LocalIsClient = false;
		LocalIsHost = false;
	}

	void StartAttemptToConnect()
	{
		vnet.DisconnectAll();
		AttemptToConnectTimer = LookForHostTime;
		AttemptingToConnectGUID = VNetCommon.NET_SESSION_INVALID_UID;
		NoConnection = true;
		LocalIsHost = false;
		LocalIsClient = false;
	}

	void UpdateAttemptToConnect()
	{
		if (LocalIsClient || LocalIsHost)
			return;

		if (DebugPlayAsClient)
		{
			LocalIsClient = true;
			EnableDisableConnectionObjects(2);
			return;
		}
		

		// We connected
		if (vnet.m_netSession.GetNumConnectedClients() > 0 &&
			vnet.m_netSession.GetSessionUID() == AttemptingToConnectGUID)
		{
			AttemptingToConnectGUID = VNetCommon.NET_SESSION_INVALID_UID;
			AttemptToConnectTimer = 0;
			LocalIsClient = true;
			NoConnection = false;
			EnableDisableConnectionObjects(AttemptingToConnectRole);
			return;
		}

		if (AttemptToConnectTimer > 0)
		{
			if (vnet.m_availableSessions.m_openSessions.Count > 0)
			{
				foreach (ulong key in vnet.m_availableSessions.m_openSessions.Keys)
				{
					AttemptingToConnectGUID = key;
					break;
				}

				// Figure out a valid role you can take
				VNetOpenSession sess = vnet.m_availableSessions.m_openSessions[AttemptingToConnectGUID];
				AttemptingToConnectRole = 3 - sess.host.role;

				vnet.m_netSession.AttemptToJoinSession(AttemptingToConnectGUID, AttemptingToConnectRole);
			}
			AttemptToConnectTimer -= VNetTimer.Inst.GetFrameTimeFloat();
		} 
		// Become the host of a new game since you failed to previously connect
		else if (AttemptingToConnectGUID == VNetCommon.NET_SESSION_INVALID_UID)
		{
			LocalIsHost = true;
			NoConnection = false;

			int hostRole = LocalHostIsScientist ? 1 : 2;
			EnableDisableConnectionObjects(hostRole);

			vnet.StartHosting(hostRole);
		}
	}


	public void RunCoroutineSynced(MonoBehaviour component, string coroutineName)
	{
		// Get out the network transform name
		VNetTransform trans = component.GetComponent<VNetTransform>();
		if (trans == null)
			throw new UnityException("Can't run synced coroutine on object without a VNetTransform!");

		VNetMessageRunCoroutine message = new VNetMessageRunCoroutine();
		message.networkID = trans.netIdentifier;
		message.componentType = component.GetType().ToString();
		message.coroutineName = coroutineName;
		message.netTimeStart = VNetSessionTime.Inst.GetServerTimePrecise() + VNetManager.Inst.CoroutineStartDelay;
		VNet.Inst.SendToLobby(message, true);


		// delay the start of the coroutine here, too
		StartCoroutine(VNetTransformManager.Inst.StartCoroutineDelayed((float)VNetManager.Inst.CoroutineStartDelay, component, coroutineName));

	}



	void OnDestroy()
	{
		Inst = null;
		vTransformMan = null;
	}

	// Update is called once per frame
	void LateUpdate () {
		vnet.Update();
		UpdateAttemptToConnect();


		if (DoReconnectButton)
		{
			DoReconnectButton = false;
			vnet.DisconnectAll();
			vnet.Init();
		}
				

	}

	public float GetTransformUpdateFrequency()
	{
		return 1.0f / TransformUpdatesPerSecond;
	}

}
