using System;

namespace VNetLib
	{

	public class VNetClientBandwidth
	{
	    public VNetClientBandwidth()
	    {
	        Reset();
	    }

	    ~VNetClientBandwidth() { }

	    public void Reset()
	    {
	        m_totalBytesOut = 0;
	        m_timer = 0;
	        m_queueIndex = 0;
	        m_averageBandwidth = 0;
	        m_compressionRate = 0;
	        m_bytesSent = new long[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH + 1];
	        m_uncompressedBytes = new long[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH + 1];
	    }

	    public void Update()
	    {
	        m_timer -= VNetTimer.Inst.GetFrameTimeFloat();
	        if (m_timer > 0)
	            return;

	        m_timer += 1.0f;

	        m_bytesSent[m_queueIndex] = m_bytesSent[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH];
	        m_bytesSent[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH] = 0;

	        m_uncompressedBytes[m_queueIndex] = m_uncompressedBytes[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH];
	        m_uncompressedBytes[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH] = 0;

	        m_queueIndex = (m_queueIndex + 1) % VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH;

	        // Calculate average and compression
	        Int64 bandwidth = 0;
	        Int64 uncompressedSize = 0;
	        for (int i = 0; i < VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH; i++)
	        {
	            bandwidth += m_bytesSent[i];
	            uncompressedSize += m_uncompressedBytes[i];
	        }
	        m_averageBandwidth = bandwidth / (float)VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH;

	        m_compressionRate = 0;
	        float orig = uncompressedSize / (float)VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH;
	        if (orig != 0)
	            m_compressionRate = 1.0f - (m_averageBandwidth / orig);
	    }

	    public void AddToBandwidth(Int32 sentLength, Int32 origLength)
	    {
	        m_totalBytesOut += sentLength;
	        m_bytesSent[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH] += sentLength;
	        m_uncompressedBytes[VNetCommon.NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH] += origLength;
	    }

	    public float GetAverageBandwidth() { return m_averageBandwidth; }
	    public float GetBandwidthCompressionRate() { return m_compressionRate; }

	    Int64 m_totalBytesOut;
	    Int64[] m_bytesSent;
	    Int64[] m_uncompressedBytes;
	    Int32 m_queueIndex;

	    float m_averageBandwidth;
	    float m_compressionRate;
	    float m_timer;

	}
}