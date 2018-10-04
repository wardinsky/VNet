using System;
using System.Net;
using System.Collections.Generic;

namespace VNetLib
	{
	public class VNetClient
	{
	    public VNetClient()
		{
	        m_clientPing = new VNetClientPing(this);
	        m_clientBandwidth = new VNetClientBandwidth();
	        m_staleReliableMessages = new Queue<VNetMessage>();
	        m_newReliableMessages = new Queue<VNetMessage>();
	        Disconnect();
	    }
	    ~VNetClient()
	    {
	        Disconnect();
	    }

	    public void Disconnect()
	    {
	        m_ipEndpoint = null;
	        m_UID = 0;

	        m_isFrozen = false;
	        SetActive(false);

	        m_clientBandwidth.Reset();
	        m_packetOutQueue = new VNetPacket[VNetCommon.NET_PACKET_QUEUE_SIZE];
			for (int i = 0; i < m_packetOutQueue.Length; i++)
				m_packetOutQueue [i] = new VNetPacket();
	        m_packetQueueIndex = 0;
	        ResetTimeToSend();

	        // RUDP stuff
	        m_nextReliablePacketIndex = 1;
	        m_lastReliableRecvdIndex = 0;
	        m_lastAckedPacketIndex = -1;

	        m_staleReliableMessages.Clear();
	        m_newReliableMessages.Clear();
	    }

	    public void Update()
	    {
	        // Calculate bandwidth
	        m_clientBandwidth.Update();

	        // Freeze if necessary
	        m_isFrozen = VNetCommon.NET_DEBUG_FREEZE_WITH_HEARTBEAT &&
	            ((VNetTimer.Inst.GetSystemTimeNow() - m_lastCommunicationTime) > VNetCommon.NET_DEBUG_FREEZE_HEARTBEAT_WAIT_TIME);

	        m_nextSendTime -= VNetTimer.Inst.GetFrameTimeFloat();
	        bool sendPacket = m_nextSendTime < 0;
	        if (m_playerIndex == -1)
	        {
	            if (sendPacket)
	                SendPacketToMulti();
	            return;
	        }

	        // Calculate internal client stuff
	        m_clientPing.Update();
	        if (sendPacket)
	        {
	            SendPacketToClient();
	        }
	    }

	    // Identification
	    public void SetClientData(VNetSimpleClientData clientData, int clientID)
	    {
	        if (clientData == null)
	        {
	            SetClientRaw(0, null);
	            SetPlayerIndex(clientID);
	            return;
	        }

	        SetClientRaw(clientData.uid, new IPEndPoint(clientData.ip, clientData.port));
	        SetName(clientData.name);
	        SetPlayerIndex(clientID);
	    }

	    public void SetClientRaw(UInt64 uid, IPEndPoint ipEndpoint)
	    {
			if (ipEndpoint != null)
				m_ipEndpoint = new IPEndPoint(ipEndpoint.Address, ipEndpoint.Port);
			else
				m_ipEndpoint = null;
	        m_UID = uid;
	        SetActive(true);
	    }

	    public void SetName(string name)
	    {
	        m_name = name;
	    }

	    public IPEndPoint GetIPEndpoint()
	    {
	        return m_ipEndpoint;
	    }
	    public IPAddress GetIP()
	    {
	        return m_ipEndpoint.Address;
	    }
	    public Int32 GetPort()
	    {
	        return m_ipEndpoint.Port;
	    }

	    // Send a message
	    public void SendNetMessage(VNetMessage message, bool isReliable)
	    {
	        message.SetReliableIndex(-1);
	        if (isReliable)
	        {
	            message.SetReliableIndex(m_nextReliablePacketIndex);
	            m_nextReliablePacketIndex++;

	            if (m_newReliableMessages.Count >= VNetCommon.NET_CLIENT_RELIABLE_QUEUE)
	                return;

	            VNetMessage copy = AllocateMessageCopy(message);
	            m_newReliableMessages.Enqueue(copy);
	        }
	        PackMessage(message);
	    }

	    // Identity Information
	    public string GetName()
	    {
	        return m_name;
	    }

	    public Int32 GetPlayerID()
	    {
	        return m_playerIndex;
	    }

		public Int32 GetRole()
		{
			return m_role;
		}

	    public UInt64 GetUID()
	    {
	        return m_UID;
	    }

	    public Int32 GetAveragePingMS()
	    {
	        return m_clientPing.GetAverageMS();
	    }

	    public Int32 GetNumUnackedMessages()
	    {
	        return m_staleReliableMessages.Count + m_newReliableMessages.Count;
	    }

	    // maintenance
	    public void SetActive(bool active)
	    {
	        m_isActive = active;
	        if (active == false)
	            m_playerIndex = 0;
	        m_lastCommunicationTime = VNetTimer.Inst.GetSystemTimeNow() + 5.0;
	    }

	    public bool GetIsActive()
	    {
	        return m_isActive;
	    }

	    public double GetTimeSinceLastCommunication()
	    {
	        return VNetTimer.Inst.GetSystemTimeNow() - m_lastCommunicationTime;
	    }

	    public bool IsFrozen()
	    {
	        return m_isFrozen;
	    }

	    // Management of timing and reliablility
	    public void OnDataReceived(VNetPacket inPacket)
	    {
	        PurgeReliableQueue(inPacket.header.lastReliablePacketRecvd);
	        m_lastCommunicationTime = VNetTimer.Inst.GetSystemTimeNow();
	    }

	    public bool ReliableMessageCheck(VNetMessage message)
	    {
	        if (message.GetReliableIndex() == -1)
	            return false;
	        if (m_lastReliableRecvdIndex != message.GetReliableIndex() - 1)
	            return true;
	        m_lastReliableRecvdIndex++;
	        return false;
	    }

	    public int GetNextReliableIndex()
	    {
	        return m_lastReliableRecvdIndex + 1;
	    }

	    // Utilities
	    public bool CheckForTimeout()
	    {
	        if (GetTimeSinceLastCommunication() > VNetCommon.NET_CLIENT_TIMEOUT_SECONDS)
	        {
	            if (!VNetCommon.NET_DEBUG_FREEZE_WITH_HEARTBEAT)
	            {
	                return true;
	            }
	        }
            return false;
	    }

	    public void ResetTimeToSend()
	    {
	        m_nextSendTime = VNetCommon.NET_CLIENT_SEND_FRAMETIME;
	    }

	    public void SetOutgoingPacketHeader(VNetPacket packet)
	    {
	        packet.IP_Port = m_ipEndpoint;
	        packet.header.clientUID = VNet.Inst.GetUID();
	        packet.header.sessionUID = VNetSession.Inst.GetSessionUID();
			packet.header.identHeader = VNetManager.Inst.GUIDHeader;
	        packet.header.lastReliablePacketRecvd = m_lastReliableRecvdIndex;
	        packet.header.parentEndianess = VNetPlatform.GetEndianValue();
	    }

	    public void SendPacketToClient()
	    {
	        // Add reliable messages
	        PackReliableMessages();

	        // for all packets to go
	        for (int i = 0; i <= m_packetQueueIndex; i++)
	        {
	            if (i == VNetCommon.NET_PACKET_QUEUE_SIZE) break;

	            VNetPacket packet = m_packetOutQueue[i];
	            if (packet.header.numMessages == 0)
	                return;

	            // end a packet if it's the last one this frame
	            if (i == m_packetQueueIndex)
	                packet.EndPacket();

	            SetOutgoingPacketHeader(packet);

	            VNet.Inst.SendOnClientLink(packet);
	            m_clientBandwidth.AddToBandwidth(packet.header.dataLength + VNetPacketHeader.SizeOf(), packet.header.origLength + VNetPacketHeader.SizeOf());
	            packet.Clear();
	        }

	        m_packetQueueIndex = 0;
	        ResetTimeToSend();
	    }

	    public void SendPacketToMulti()
	    {
	        for (int i = 0; i <= m_packetQueueIndex; i++)
	        {
	            if (i == VNetCommon.NET_PACKET_QUEUE_SIZE) break;
	            VNetPacket packet = m_packetOutQueue[i];
	            if (packet.header.numMessages == 0)
	                return;

	            packet.header.clientUID = VNet.Inst.GetUID();
	            packet.header.lastReliablePacketRecvd = -1;
	            packet.header.sessionUID = VNetSession.Inst.GetSessionUID();
				packet.header.identHeader = VNetManager.Inst.GUIDHeader;
	            packet.header.parentEndianess = VNetPlatform.GetEndianValue();

	            // End a packet if it is the last one this frame
	            if (i == m_packetQueueIndex)
	                packet.EndPacket();

	            VNet.Inst.SendOnMultiLink(packet);
	            m_clientBandwidth.AddToBandwidth(packet.header.dataLength + VNetPacketHeader.SizeOf(), packet.header.origLength + VNetPacketHeader.SizeOf());
	            packet.Clear();
	        }
	        m_packetQueueIndex = 0;
	        ResetTimeToSend();
	    }
	    

	    public void SetPlayerIndex(Int32 p)
	    {
	        m_playerIndex = p;
	    }
		public void SetRole(int role)
		{
			m_role = role;
		}


	    //////////////////////////////////////////////////////
	    VNetClientBandwidth m_clientBandwidth;
	    VNetClientPing m_clientPing;

	    // Time
	    float m_nextSendTime;
	    double m_lastCommunicationTime;

	    // Ident
	    UInt64 m_UID;
	    IPEndPoint m_ipEndpoint;
	    string m_name;
	    bool m_isActive;
	    bool m_isFrozen;
	    Int32 m_playerIndex;
		Int32 m_role;

	    // Incoming data
	    Int32 m_lastAckedPacketIndex;
	    Int32 m_lastReliableRecvdIndex;

	    // Outgoing
	    Int32 m_nextReliablePacketIndex;
	    Queue<VNetMessage> m_staleReliableMessages;
	    Queue<VNetMessage> m_newReliableMessages;

	    // Utilities
	    bool PackMessage(VNetMessage message)
	    {
	        // already sent too many
	        if (m_packetQueueIndex == VNetCommon.NET_PACKET_QUEUE_SIZE)
	            return false;

	        // Packe message
	        VNetPacket packet = m_packetOutQueue[m_packetQueueIndex];
	        if (packet.AddNetMessage(message))
	        {
	            packet.header.numMessages++;
	            return true;
	        }
	        packet.EndPacket();

	        // do the next one
	        m_packetQueueIndex++;
            
	        return PackMessage(message);
	    }

	    void PackReliableMessages()
	    {
	        if (m_packetQueueIndex >= VNetCommon.NET_PACKET_QUEUE_SIZE)
	            return;

	        // Fit as many as possible
	        foreach (VNetMessage message in m_staleReliableMessages)
	        {
	            if (!PackMessage(message))
	                return;
	        }

	        // Add all new messages to the end
	        while (m_newReliableMessages.Count > 0)
	        {
	            VNetMessage message = m_newReliableMessages.Dequeue();
	            m_staleReliableMessages.Enqueue(message);
	        }

	    }

	    VNetMessage AllocateMessageCopy(VNetMessage message)
	    {
	        return message.Clone();
	    }

	    VNetPacket[] m_packetOutQueue;
	    Int32 m_packetQueueIndex;

	    void PurgeReliableQueue(Int32 lastReliableIndex)
	    {
	        bool needToAck = false;
	        if (lastReliableIndex > m_lastAckedPacketIndex)
	        {
	            m_lastAckedPacketIndex = lastReliableIndex;
	            needToAck = true;
	        }

	        // Free messages waiting to be acked
	        while (m_staleReliableMessages.Count > 0 &&
	            m_staleReliableMessages.Peek().GetReliableIndex() <= m_lastAckedPacketIndex)        
	            m_staleReliableMessages.Dequeue();
	        
	        while (m_newReliableMessages.Count > 0 &&
	            m_newReliableMessages.Peek().GetReliableIndex() <= m_lastAckedPacketIndex)
	            m_newReliableMessages.Dequeue();

	        // If we received a reliable message, ack it
	        if (needToAck)
	        {
	            VNetMessagePacketAck packetAckMessage = new VNetMessagePacketAck();
	            SendNetMessage(packetAckMessage, false);
	        }

	    }
	    
	    

	}
}