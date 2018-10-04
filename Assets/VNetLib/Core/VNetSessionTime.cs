using System;

namespace VNetLib
{
		
	public class VNetSessionTime
	{
	    public static VNetSessionTime Inst;

	    public VNetSessionTime()
	    {
	        Inst = this;
	        Reset();	        
	    }

	    ~VNetSessionTime() {
            Inst = null;
        }

	    public void Update(VNetClient host)
	    {
	        if (host != null && m_timeSyncsRemaining > 0)
	        {
	            m_nextTimeSync -= VNetTimer.Inst.GetFrameTimeFloat();
	            if (m_nextTimeSync <= 0)
	            {
	                m_nextTimeSync = VNetCommon.NET_TIME_SYNC_WAIT_TIME;
	                VNetMessageTimeRequest request = new VNetMessageTimeRequest();
	                request.currentTime = VNetTimer.Inst.GetSystemTimeNow();
	                host.SendNetMessage(request, false);
	            }
	        }
	    }

	    public void Reset()
	    {
	        m_serverLatencies = new double[VNetCommon.NET_TIME_NUM_SYNCS];
	        m_startTime = VNetTimer.Inst.GetSystemTimeNow();
	        m_serverTimeDifference = -m_startTime;
	        m_nextTimeSync = 0;
	        m_timeSyncsRemaining = VNetCommon.NET_TIME_NUM_SYNCS;
	    }

	    public float GetServerTime()
	    {
	        return (float)GetServerTimePrecise();
	    }

	    public double GetServerTimePrecise()
	    {
	        double currentTime = VNetTimer.Inst.GetSystemTimeNow();
	        return currentTime + m_serverTimeDifference;
	    }

	    public void OnTimeReturn(VNetMessageTimeReturn timeReturn)
	    {
	        double curTime = VNetTimer.Inst.GetSystemTimeNow();
	        double halfLatency = (curTime - timeReturn.clientTime) / 2;
	        double difference = timeReturn.serverTime - curTime + halfLatency;

	        if (m_timeSyncsRemaining == VNetCommon.NET_TIME_NUM_SYNCS)
	            m_serverTimeDifference = difference;

	        // Pool it
	        m_serverLatencies[VNetCommon.NET_TIME_NUM_SYNCS - m_timeSyncsRemaining] = difference;
	        m_timeSyncsRemaining--;

	        if (m_timeSyncsRemaining == 0)
	        {
	            for (int i = 0; i < VNetCommon.NET_TIME_NUM_SYNCS; i++)
	            {
	                if (m_serverLatencies[i] < m_serverTimeDifference)
	                    m_serverTimeDifference = m_serverLatencies[i];
	            }
	        }

	    }

	    public void OnTimeRequest(VNetMessageTimeRequest timeRequest)
	    {
	        VNetMessageTimeReturn nmtr = new VNetMessageTimeReturn();
	        nmtr.clientTime = timeRequest.currentTime;
	        nmtr.serverTime = VNetSessionTime.Inst.GetServerTimePrecise();
	        timeRequest._client.SendNetMessage(nmtr, false);
	    }

	    int m_timeSyncsRemaining;
	    float m_nextTimeSync;

	    double m_startTime;
	    double m_serverTimeDifference;
	    double[] m_serverLatencies;


	}
}