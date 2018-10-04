using System;
using System.Collections.Generic;

namespace VNetLib
	{

	public class VNetOpenSession
	{
	    public VNetOpenSession()
	    {
	        clients = new VNetSimpleClientData[VNetCommon.NET_MAX_CLIENTS - 1];

	    }

	    // GameID
	    public UInt64 sessionUID;
	    public Int32 isAvailable;
	    public SByte numClients;

	    public VNetSimpleClientData host;
	    public VNetSimpleClientData[] clients;
	    
	    public double discoveredTime;

	    public void UpdateData(VNetMessageSessionAvailable netMessage)
	    {
	        host = VNetSimpleClientData.ComponentCopy(netMessage.host);
	        host.ip = netMessage._packet.IP_Port.Address;
	        host.port = netMessage._packet.IP_Port.Port;

	        numClients = netMessage.numClients;

	        for (int i = 0; i < numClients; i++)
	        {
	            clients[i] = VNetSimpleClientData.ComponentCopy(netMessage.client[i]);
	        }

	        sessionUID = netMessage.UID;
	        discoveredTime = VNetTimer.Inst.GetSystemTimeNow();


	    }


	}


	public class VNetAvailableSessions
	{
	    public VNetAvailableSessions()
	    {
	        m_openSessions = new Dictionary<ulong, VNetOpenSession>();
	    }

	    ~VNetAvailableSessions() { }
	    public void Clear()
	    {
	        m_openSessions.Clear();
	    }

	    public VNetOpenSession GetSession(UInt64 sessionUID)
	    {
	        if (m_openSessions.ContainsKey(sessionUID))
	            return m_openSessions[sessionUID];
	        return null;
	    }

	    public void RemoveStaleSessions()
	    {
	        double currentTime = VNetTimer.Inst.GetSystemTimeNow();
	        foreach (VNetOpenSession session in m_openSessions.Values)
	        {
	            if (currentTime - session.discoveredTime > VNetCommon.NET_SESSION_STALE_TIME)
	            {
	                if (session.sessionUID == VNetSession.Inst.GetSessionUID())
	                {
	                    VNetSession.Inst.JoiningTimedOut();
	                    Clear();
	                    return;
	                }

	                // otherwise, remove this session
	                m_openSessions.Remove(session.sessionUID);
	            }
	        }
	    }

	    public void AddOrUpdateSession(VNetMessageSessionAvailable session)
	    {
	        if (VNetSession.Inst.GetSessionState() == VNetSessionState.InSession)
	            return;

	        // Not found
	        if (m_openSessions.ContainsKey(session.UID) == false)
	        {
	            VNetOpenSession newSession = new VNetOpenSession();
	            newSession.UpdateData(session);
	            m_openSessions.Add(session.UID, newSession);
	        }
	        else
	        {
	            m_openSessions[session.UID].UpdateData(session);
	        }
	    }

	    public Dictionary<UInt64, VNetOpenSession> m_openSessions;
	}
}