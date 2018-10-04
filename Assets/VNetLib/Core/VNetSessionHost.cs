using System;

namespace VNetLib
{
		
	public class VNetSessionHost
	{
	    public VNetSessionHost()
	    {
	        m_lookingForClientsTimer = 0;
	    }

	    ~VNetSessionHost()
	    {
	        Disconnect();
	    }


	    public void Update()
	    {
	        if (LocalIsHost() == false)
	            return;

	        UpdateLookingForClients();
	    }
	    public void Disconnect()
	    {
			SetHostInfo(VNetCommon.NET_CLIENT_INVALID_UID, -1);
	    }

	    // Functions
	    public void CheckForHostLeft(VNetClient client)
	    {	        
			if (client.GetUID() == m_hostUID)
			{
				if (VNetCommon.SHOW_LOGS)
					UnityEngine.Debug.Log("VNet: The host left the session.");
				
				// In the future, become the host based on some internal value
				Disconnect();
				VNetSession.Inst.DisconnectAll();
			}
	    }

	    public bool LocalIsHost()
	    {
	        return m_hostUID == VNet.Inst.GetUID();
	    }

	    public UInt64 GetHostUID() { return m_hostUID; }

		public void SetHostInfo(UInt64 clientID, int hostRole)
	    {
	        if (LocalIsHost())
	        {
	            VNetDispatch.UnregisterListenerInst<VNetMessageJoinSession>(OnClientJoinRequest);
	        }
	        m_hostUID = clientID;
			m_hostRole = hostRole;
	        if (LocalIsHost())
	        {
	            m_lookingForClientsTimer = 0;
	            VNetDispatch.RegisterListenerInst<VNetMessageJoinSession>(OnClientJoinRequest);
	        }
	    }

	    public void OnClientJoinRequest(VNetMessageJoinSession joinRequest)
	    {
	        // If i'm not the host, ignore this
	        if (LocalIsHost() == false)
	            return;

	        // If this is for a separate session, ignore
	        UInt64 sessionUID = VNetSession.Inst.GetSessionUID();
	        if (joinRequest.sessionUID != sessionUID)
	            return;

	        // Could be a dup, ignore if if that's the case
	        if (VNetSession.Inst.GetClientByUID(joinRequest._packet.header.clientUID) != null)
	            return;

	        // Add this client
	        VNetMessageNewClient nmc = new VNetMessageNewClient();
            nmc.clientData = new VNetSimpleClientData();               
	        nmc.clientData.active = 1;
	        nmc.clientData.ip = joinRequest._packet.IP_Port.Address;
	        nmc.clientData.port = joinRequest._packet.IP_Port.Port;
	        nmc.clientData.uid = joinRequest._packet.header.clientUID;
	        nmc.clientData.name = joinRequest.userName;
			nmc.clientData.role = joinRequest.role;

	        nmc.sessionUID = sessionUID;
	        VNet.Inst.SendToLobby(nmc, true);

	        // Add the client to the local list
	        VNetClient client = VNetSession.Inst.AddClient(joinRequest._packet.header.clientUID, joinRequest._packet.IP_Port);
	        client.SetName(joinRequest.userName);
			client.SetRole(joinRequest.role);

	        // Accept this client
	        VNetMessageAcceptClient ac = new VNetMessageAcceptClient();
	        ac.clientUID = client.GetUID();
	        ac.sessionUID = sessionUID;
			ac.role = joinRequest.role;
	        client.SendNetMessage(ac, true);
	    }

	    void UpdateLookingForClients()
	    {
	        // see if we should send a message
	        m_lookingForClientsTimer -= VNetTimer.Inst.GetFrameTimeFloat();
	        if (m_lookingForClientsTimer > 0.0f)
	            return;

	        m_lookingForClientsTimer = VNetCommon.NET_HOST_LOBBY_SEND_TIME;

	        // Send out a game message
	        VNetMessageSessionAvailable session = new VNetMessageSessionAvailable();
	        session.UID = VNetSession.Inst.GetSessionUID();
	        session.sessionAvaliable = 1;
			session.host = new VNetSimpleClientData();
	        session.host.active = 1;
	        session.host.uid = VNet.Inst.GetUID();
	        session.host.ip = null;
	        session.host.port = 0;
			session.host.role = m_hostRole;
	        VNetPlatform.FillLocalUsername(ref session.host.name);

	        session.numClients = (SByte)VNetSession.Inst.GetNumConnectedClients();

	        int i = 0;
	        foreach (VNetClient client in VNetSession.Inst.m_clientsByUID.Values)
	        {
                session.client[i] = new VNetLib.VNetSimpleClientData();                    
	            session.client[i].active = 0;
	            if (client.GetIsActive())
	            {
	                session.client[i].active = 1;
	                session.client[i].ip = client.GetIP();
	                session.client[i].port = client.GetPort();
	                session.client[i].name = client.GetName();
					session.client[i].role = client.GetRole();
	            }
	        }

	        VNet.Inst.SendToGlobal(session);

	    }

	    float m_lookingForClientsTimer;
	    UInt64 m_hostUID;
		int m_hostRole;

	}
}