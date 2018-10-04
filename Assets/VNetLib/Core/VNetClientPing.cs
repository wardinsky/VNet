using System;

namespace VNetLib
	{
	public class VNetClientPing
	{
	    public VNetClientPing(VNetClient client)
	    {
	        m_client = client;
	        m_times = new double[VNetCommon.NET_CLIENT_PING_QUEUE_LENGTH];
	        Reset();

	        VNetDispatch.RegisterListenerInst<VNetMessagePingClient>(OnPing);
	        VNetDispatch.RegisterListenerInst<VNetMessagePongClient>(OnPong);

	    }

	    ~VNetClientPing()
	    {
	        VNetDispatch.UnregisterListenerInst<VNetMessagePingClient>(OnPing);
	        VNetDispatch.UnregisterListenerInst<VNetMessagePongClient>(OnPong);
	    }

	    public void Reset()
	    {
	        m_timeQueueIndex = 0;
	        m_timeAverage = 0;
	        m_timeBest = UInt32.MaxValue;
	        m_delay = VNetCommon.NET_CLIENT_PING_WAIT_TIME;
	        for (int p = 0; p < m_times.Length; p++)
	            m_times[p] = 0;


	    }

	    public void Update()
	    {
	        m_delay -= VNetTimer.Inst.GetFrameTimeFloat();
	        if (m_delay > 0)
	            return;

	        // Reset the timer
	        m_delay += VNetCommon.NET_CLIENT_PING_WAIT_TIME;

	        // Send the message
	        VNetMessagePingClient ping = new VNetMessagePingClient();
	        ping.timeSent = VNetTimer.Inst.GetSystemTimeNow();
	        m_client.SendNetMessage(ping, false);        
	    }

	    // Listener
	    public void OnPing(VNetMessagePingClient ping)
	    {
	        if (ping._client.GetUID() != m_client.GetUID())
	            return;

            Console.WriteLine(ping._client.GetName() + " - ping");

            // Send a pong response
            VNetMessagePongClient pong = new VNetMessagePongClient();
	        pong.timeSent = ping.timeSent;
	        m_client.SendNetMessage(pong, false);
	    }

	    public void OnPong(VNetMessagePongClient pong)
	    {
	        if (pong._client.GetUID() != m_client.GetUID())
	            return;

            Console.WriteLine(pong._client.GetName() + " - pong");

	        m_delay = VNetCommon.NET_CLIENT_PING_WAIT_TIME;

	        double timeDif = VNetTimer.Inst.GetSystemTimeNow() - pong.timeSent;
	        m_times[m_timeQueueIndex] = timeDif;
	        m_timeQueueIndex = (m_timeQueueIndex + 1) % m_times.Length;
	        m_timeAverage = 0;
	        for (int i = 0; i < m_times.Length; i++)
	        {
	            m_timeAverage += m_times[i];
	        }
	        m_timeAverage /= m_times.Length;

	        if (timeDif < m_timeBest)
	        {
	            m_timeBest = timeDif;
	        }
	    }

	    public Int32 GetAverageMS()
	    {
	        return (int)(m_timeAverage * 1000);
	    }

	    ////////////////////////////////////
	    VNetClient m_client;

	    // ping data
	    double m_timeBest;
	    double m_timeAverage;
	    double[] m_times;
	    Int32 m_timeQueueIndex;
	    float m_delay;



	}
}