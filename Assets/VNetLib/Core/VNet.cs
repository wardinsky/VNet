using System;
using System.Net;
using System.Collections.Generic;
using UnityEngine;

namespace VNetLib
{

	public class VNet
	{
	    public static VNet Inst;
	    public VNet()
	    {
			Inst = this;
			m_netDispatch = new VNetDispatch();
	        m_netTimer = new VNetTimer();

			// Initialize client and multi links
			m_clientLink = UnityEngine.Transform.FindObjectOfType<VNetSockClientLink>();
			if (m_clientLink == null)
			{
				GameObject newObj = new GameObject("VNetSockets");
				GameObject.DontDestroyOnLoad(newObj);
				m_clientLink = newObj.AddComponent<VNetSockClientLink>();
				m_multiLink = newObj.AddComponent<VNetSockMultiLink>();
			} else
			{
				m_multiLink = m_clientLink.GetComponent<VNetSockMultiLink>();
			}

	       // m_clientLink = new VNetSockClientLink();
	       // m_multiLink = new VNetSockMultiLink();

	        m_multicastClient = new VNetClient();
	        m_availableSessions = new VNetAvailableSessions();
	        m_netSession = new VNetSession();



	        RegisterListeners();
	    }

	    ~VNet()
	    {
	        UnregisterListeners();
	        DisconnectAll();

	        // net_shutdown();

	        Inst = null;
	    }

	    public void Init()
	    {
	        // net_startup();
	        UpdateUID();

	        m_isInitialized = m_multiLink.Initialize();
	        if (m_isInitialized)
	            m_isInitialized = m_clientLink.Initialize();

	        m_multicastClient.Disconnect();
	        m_multicastClient.SetActive(true);
	        m_multicastClient.SetClientData(null, -1);
	    }
	    public void Update()
	    {
	        if (m_isInitialized == false)
	            return;

	        m_netTimer.Update();
	        m_netSession.Update();

	        // Read packet data from the net
	        VNetPacket inPacket = new VNetPacket();
	        while (m_multiLink.Recv(inPacket))
	        {
	            if (VNetVerifier.VerifyPacket(inPacket))
	                m_netDispatch.HandlePacketIn(inPacket, null);
	        }

	        while (m_clientLink.Recv(inPacket))
	        {
	            if (VNetVerifier.VerifyPacket(inPacket))
	            {
	                VNetClient client = m_netSession.ResolveClientFromPacket(inPacket);
	                if (client == null)
	                    continue;

	                // reliable, packet stuff
	                client.OnDataReceived(inPacket);

	                // dispatch
	                m_netDispatch.HandlePacketIn(inPacket, client);
	            }
	        }

	        // Update clients
	        m_multicastClient.Update();
	        m_netSession.UpdateClients();

	    }

	    public void DisconnectAll()
	    {
	        m_availableSessions.Clear();
	        m_netSession.DisconnectAll();
	    }

	    public void StartHosting(int hostRole)
	    {
	        m_netSession.StartHosting(hostRole);
	    }
	    
	    public bool IsInitialized() { return m_isInitialized; }
	    public bool IsHost()
	    {
	        return m_netSession.HostIsLocalPlayer();
	    }

	    public void SendToLobby(VNetMessage message, bool reliable)
	    {
	        m_netSession.SendToAllClients(message, reliable);
	    }

	    public void SendToGlobal(VNetMessage message)
	    {
	        m_multicastClient.SendNetMessage(message, false);
	    }

	    public UInt64 GetUID() { return m_UID; }

	    VNetDispatch m_netDispatch;

	    public VNetAvailableSessions m_availableSessions;
	    public VNetSession m_netSession;
	    public VNetTimer m_netTimer;

	    // Identification
	    UInt64 m_UID;
	    bool m_isInitialized;

	    // Client and multi links
	    VNetSockClientLink m_clientLink;
	    VNetSockMultiLink m_multiLink;
	    VNetClient m_multicastClient;
	    
	    public void SendOnClientLink(VNetPacket packet)
	    {
	        m_clientLink.Send(packet);
	    }

	    public void SendOnMultiLink(VNetPacket packet)
	    {
	        m_multiLink.Send(packet);
	    }

	    void UpdateUID()
	    {
	        m_UID = VNetPlatform.GetUID();
	    }

	    void RegisterListeners()
	    {
	        // Net session messages
	        VNetDispatch.RegisterListenerInst<VNetMessageAcceptClient>(m_netSession.OnAcceptClientJoinRequest);
	        VNetDispatch.RegisterListenerInst<VNetMessageNewClient>(m_netSession.OnNewClient);
	        VNetDispatch.RegisterListenerInst<VNetMessageLeaveSession>(m_netSession.OnClientsWantsToLeave);
	        VNetDispatch.RegisterListenerInst<VNetMessageLeaveSessionConfirm>(m_netSession.OnClientLeaveConfirm);

	        // Net time
	        VNetDispatch.RegisterListenerInst<VNetMessageTimeRequest>(m_netSession.m_netTime.OnTimeRequest);
	        VNetDispatch.RegisterListenerInst<VNetMessageTimeReturn>(m_netSession.m_netTime.OnTimeReturn);

	        // New games
	        VNetDispatch.RegisterListenerInst<VNetMessageSessionAvailable>(m_availableSessions.AddOrUpdateSession);
	    }

	    void UnregisterListeners()
	    {
	        // Net session messages
	        VNetDispatch.UnregisterListenerInst<VNetMessageAcceptClient>(m_netSession.OnAcceptClientJoinRequest);
	        VNetDispatch.UnregisterListenerInst<VNetMessageNewClient>(m_netSession.OnNewClient);
	        VNetDispatch.UnregisterListenerInst<VNetMessageLeaveSession>(m_netSession.OnClientsWantsToLeave);
	        VNetDispatch.UnregisterListenerInst<VNetMessageLeaveSessionConfirm>(m_netSession.OnClientLeaveConfirm);

	        // Net time
	        VNetDispatch.UnregisterListenerInst<VNetMessageTimeRequest>(m_netSession.m_netTime.OnTimeRequest);
	        VNetDispatch.UnregisterListenerInst<VNetMessageTimeReturn>(m_netSession.m_netTime.OnTimeReturn);

	        // New games
	        VNetDispatch.UnregisterListenerInst<VNetMessageSessionAvailable>(m_availableSessions.AddOrUpdateSession);
	    }



	}
}