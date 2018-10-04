using System;
using System.Collections.Generic;
using System.Net;

namespace VNetLib
{
		

	public enum VNetSessionState
	{
	    Disconnected,
	    Connecting,
	    InSession,
	    Disconnecting,
	}

	public class VNetSession
	{
	    public static VNetSession Inst;

	    public VNetSession()
	    {
	        Inst = this;
			m_clientsByUID = new Dictionary<ulong, VNetClient>();
	        m_netTime = new VNetSessionTime();
	        m_netHost = new VNetSessionHost();

	        m_sessionState = VNetSessionState.Disconnected;
	        m_sessionUID = VNetCommon.NET_SESSION_INVALID_UID;
	    }
	    ~VNetSession()
	    {
	        DisconnectAll();
	        Inst = null;
	    }

	    public void DisconnectAll()
	    {
	        m_netHost.Disconnect();
	        m_sessionState = VNetSessionState.Disconnected;
	        m_sessionUID = VNetCommon.NET_SESSION_INVALID_UID;
	    }

	    public void Update()
	    {
	        m_netHost.Update();
	        switch (m_sessionState)
	        {
	            case VNetSessionState.Disconnected:
	                break;
	            case VNetSessionState.Disconnecting:
	                UpdateLeavingSession();
	                break;
	            case VNetSessionState.Connecting:
	                UpdateJoiningSession();
	                break;
	            case VNetSessionState.InSession:
	                UpdateConnection();
	                break;
	        }
	    }
	    public void UpdateClients()
	    {
            List<VNetClient> disconnectedClients = new List<VNetClient>();

	        foreach (KeyValuePair<UInt64,VNetClient> kvp in m_clientsByUID)
	        {
	            VNetClient client = kvp.Value;
	            client.Update();
                if (client.CheckForTimeout())
                    disconnectedClients.Add(client);
	        }

            foreach (VNetClient client in disconnectedClients)
                m_clientsByUID.Remove(client.GetUID());
	    }

	    // Listeners
	    public void OnAcceptClientJoinRequest(VNetMessageAcceptClient accept)
	    {
	        // Not us
	        if (accept.clientUID != VNet.Inst.GetUID())
	            return;

	        // Different session?
	        if (accept.sessionUID != m_attemptingToJoinSession.sessionUID)
	            return;

	        // We aren't trying to join..
	        if (m_sessionState != VNetSessionState.Connecting)
	            return;

	        // joined
	        m_sessionState = VNetSessionState.InSession;

	        // Add the host
	        VNetSimpleClientData sc = m_attemptingToJoinSession.host;
			m_netHost.SetHostInfo(sc.uid, sc.role);

	        VNetClient hostClient = GetClientByUID(sc.uid);
	        hostClient.SetName(m_attemptingToJoinSession.host.name);
			hostClient.SetRole(sc.role);

	        // add remaining clients
	        for (SByte i = 0; i < m_attemptingToJoinSession.numClients; i++)
	        {
	            sc = m_attemptingToJoinSession.clients[i];

	            // skip yourself
	            if (sc.uid == VNet.Inst.GetUID())
	                continue;

	            VNetClient client = AddClient(sc.uid, new IPEndPoint(sc.ip, sc.port));
	            client.SetName(sc.name);
				client.SetRole(sc.role);
	        }

			if (VNetCommon.SHOW_LOGS)
				UnityEngine.Debug.Log("VNet: Joined session hosted by " + hostClient.GetName());
	    }
	    public void OnNewClient(VNetMessageNewClient newClient)
	    {
	        // make sure this client doesn't already exist
	        if (GetClientByUID(newClient.clientData.uid) != null)
	            return;

	        VNetClient client = AddClient(newClient.clientData.uid, new IPEndPoint(newClient.clientData.ip, newClient.clientData.port) );
	        client.SetName(newClient.clientData.name);
			client.SetRole(newClient.role);
	    }
	    public void OnClientsWantsToLeave(VNetMessageLeaveSession leaveRequest)
	    {
	        VNetMessageLeaveSessionConfirm confirm = new VNetMessageLeaveSessionConfirm();
	        VNetClient client = leaveRequest._client;
	        confirm.clientUID = client.GetUID();
	        client.SendNetMessage(confirm, false);
	        client.SendPacketToClient();
	        RemoveClient(client);
	    }
	    public void OnClientLeaveConfirm(VNetMessageLeaveSessionConfirm leaveConfirm)
	    {
	        RemoveClient(leaveConfirm._client);
	    }

	    // Client 
	    public VNetClient GetClientByUID(UInt64 clientID)
	    {
	        if (m_clientsByUID.ContainsKey(clientID))
	            return m_clientsByUID[clientID];
	        else
	            return null;
	    }

		public delegate void OnClientDelegate(VNetClient client);
		public OnClientDelegate ClientAddedCallback;
		public OnClientDelegate ClientRemovedCallback;

	    public VNetClient AddClient(UInt64 clientID, IPEndPoint endpoint)
	    {
	        if (GetClientByUID(clientID) != null)
	        {
	            return null;
	        }

	        // create the client
	        VNetClient sockClient = new VNetClient();
	        sockClient.SetClientRaw(clientID, endpoint);

	        // Add to the list
	        m_clientsByUID.Add(clientID, sockClient);

			// Call add-client so systems can propogate this
			ClientAddedCallback(sockClient);

			if (VNetCommon.SHOW_LOGS)
				UnityEngine.Debug.Log("VNet: Adding client #" + clientID.ToString() );
	        return sockClient;

	    }

	    public bool RemoveClient(VNetClient client)
	    {
	        if (client == null)
	            return false;
			if (VNetCommon.SHOW_LOGS)
				UnityEngine.Debug.Log("VNet: Removing client " + client.GetName());

			// Callback when clietns are removed
			ClientRemovedCallback(client);

	        // Check if we need to do a host migration
	        m_netHost.CheckForHostLeft(client);

	        // Remove from the client list and delete
	        m_clientsByUID.Remove(client.GetUID());

	        // disconnect it
	        client.Disconnect();

	        return true;

	    }

	    public Int32 GetNumConnectedClients()
	    {
	        return m_clientsByUID.Count;
	    }

	    public void JoiningTimedOut()
	    {
	        m_sessionState = VNetSessionState.Disconnected;
	        m_attemptingToJoinSession = null;

	        // Probably should do a callback here
	        // TODO callback to say we timed out
	    }

	    // Host related
	    public UInt64 GetHostUID()
	    {
	        return m_netHost.GetHostUID();
	    }

	    public bool HostIsLocalPlayer()
	    {
	        return m_netHost.LocalIsHost();
	    }

	    // Session
		public void StartHosting(int hostRole)
	    {
	        m_sessionUID = VNetUtils.GenerateUID();
			m_netHost.SetHostInfo(VNet.Inst.GetUID(), hostRole);

			if (VNetCommon.SHOW_LOGS)
				UnityEngine.Debug.Log("VNet: Hosting");
	    }

		public void AttemptToJoinSession(UInt64 sessionUID, Int32 role)
	    {
	        m_attemptingToJoinSession = VNet.Inst.m_availableSessions.GetSession(sessionUID);
	        m_sessionState = VNetSessionState.Connecting;
	        m_attemptingToJoinTimer = 0;
	        m_sessionUID = sessionUID;
			m_attemptingToJoinRole = role;

            // Add the host as a client
            VNetClient host = AddClient(m_attemptingToJoinSession.host.uid, new IPEndPoint(m_attemptingToJoinSession.host.ip, m_attemptingToJoinSession.host.port));
			host.SetRole(m_attemptingToJoinSession.host.role);

			if (VNetCommon.SHOW_LOGS)
				UnityEngine.Debug.Log("VNet: Attempting to join session #" + sessionUID.ToString());

	    }

	    public void LeaveSession()
	    {
	        VNetMessageLeaveSession nmlg = new VNetMessageLeaveSession();
	        nmlg.sessionUID = m_sessionUID;
	        VNet.Inst.SendToLobby(nmlg, true);

	        m_sessionState = VNetSessionState.Disconnecting;
			if (VNetCommon.SHOW_LOGS)
				UnityEngine.Debug.Log("VNet: Left session #" + m_sessionUID);
	    }

	    public UInt64 GetSessionUID() { return m_sessionUID;  }
	    public VNetSessionState GetSessionState() { return m_sessionState; }

	    // Encapsulated classes
	    public VNetSessionTime m_netTime;
	    VNetSessionHost m_netHost;

	    public Dictionary<UInt64, VNetClient> m_clientsByUID;

	    // Update lobbies
	    void UpdateConnection()
	    {
			if (HostIsLocalPlayer() == false)
			{
				if (m_clientsByUID.ContainsKey(m_netHost.GetHostUID()))
				{
					m_netTime.Update(m_clientsByUID [m_netHost.GetHostUID()]);
				} 
				else
				{
					// we disconnected...
					UnityEngine.Debug.Log("VNetSession... not connected to host.");
				}
			}
	    }

	    void UpdateJoiningSession()
	    {
	        VNet.Inst.m_availableSessions.RemoveStaleSessions();
	        if (m_sessionUID == VNetCommon.NET_SESSION_INVALID_UID)
	            return;

	        // Potentially check for timeout
	        m_attemptingToJoinTimer -= VNetTimer.Inst.GetFrameTimeFloat();
	        if (m_attemptingToJoinTimer > 0)
	            return;
	        m_attemptingToJoinTimer = VNetCommon.NET_CLIENT_CONNECTION_TIME;
            if (m_attemptingToJoinSession == null)
                return;

	        // send a message to join
	        VNetMessageJoinSession jsm = new VNetMessageJoinSession();
	        jsm.sessionUID = m_attemptingToJoinSession.sessionUID;
			jsm.role = m_attemptingToJoinRole;
	        VNetPlatform.FillLocalUsername(ref jsm.userName);

	        // Send
	        VNet.Inst.SendToGlobal(jsm);


	    }

	    void UpdateLeavingSession()
	    {
	        if (m_clientsByUID.Count > 0)
	            return;

	        DisconnectAll();
	    }

	    // Current net state
	    VNetSessionState m_sessionState;
	    UInt64 m_sessionUID;
	    public VNetClient ResolveClientFromPacket(VNetPacket packet)
	    {
	        return GetClientByUID(packet.header.clientUID);            
	    }

	    VNetOpenSession m_attemptingToJoinSession;
	    float m_attemptingToJoinTimer;
		Int32 m_attemptingToJoinRole;

	    public void SendToAllClients(VNetMessage message, bool reliable)
	    {
	        foreach (VNetClient client in m_clientsByUID.Values)
	        {
	            client.SendNetMessage(message, reliable);
	        }
	    }
	}
}