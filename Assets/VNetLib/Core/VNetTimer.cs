using System;
using System.Runtime.InteropServices;

namespace VNetLib
{
		
	public class VNetTimer
	{
	    static public VNetTimer Inst;

        public VNetTimer() {
            Inst = this;
            Load();
        }

        ~VNetTimer() {
            Inst = null;
        }

        [DllImport("KERNEL32")]
	    private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

	    [DllImport("Kernel32.dll")]
	    private static extern bool QueryPerformanceFrequency(out long lpFrequency);

	    public void Load()
	    {
	        perfCountArray = new long[30];
	           
	        QueryPerformanceCounter(out currentPerformanceCount);
	        for (int i = 0; i < 8; i++)
	            perfCountArray[i] = currentPerformanceCount;
	        firstPerformanceCount = currentPerformanceCount;

	        // initialize the performance frequency
	        QueryPerformanceFrequency(out performanceFrequency);
	        
	        m_timeSinceGameStart = 0;
	        m_timeSinceGameStartFloat = 0;

	        Update();
	    }

	    public void Update()
	    {
	        for (int i = 1; i < 8; i++)
	        {
	            perfCountArray[i - 1] = perfCountArray[i];
	        }
	        QueryPerformanceCounter(out currentPerformanceCount);
	        perfCountArray[7] = currentPerformanceCount;

	        performanceSum = 0;
	        for (int i = 1; i < 8; i++)
	        {
	            performanceSum += perfCountArray[i] - perfCountArray[i - 1];
	        }

	        m_frameTime = ((double)(performanceSum)) / (((double)performanceFrequency) * 7.0);
	        if (m_frameTime < 0)
	            m_frameTime = 0;
	        m_timeSinceGameStart += m_frameTime;

	        m_timeSinceGameStart += m_frameTime;
	        m_timeSinceGameStartFloat = (float)m_timeSinceGameStart;

	    }
	    
	    public double GetElapsedTime()
	    {
	        QueryPerformanceCounter(out elapsedPerformanceCount);
	        return (double)(elapsedPerformanceCount - firstPerformanceCount) / (double)(performanceFrequency);
	    }

	    public double GetSystemTimeNow()
	    {
	        return (double)(currentPerformanceCount) / (double)(performanceFrequency);
	    }

	    public double GetFrameTime()
	    {
	        return m_frameTime;
	    }

	    public float GetFrameTimeFloat()
	    {
	        return (float)m_frameTime;
	    }

	    public float GetMultiplier()
	    {
	        return GetFrameTimeFloat() * 60.0f;
	    }

	    public long GetCurrentCount()
	    {
	        QueryPerformanceCounter(out perfCount);
	        return perfCount;
	    }

	    public long GetPerformanceFreq()
	    {
	        return performanceFrequency;
	    }

	    public double GetTimeSinceGameStart() { return m_timeSinceGameStart; }
	    public float GetTimeSinceGameStartFloat() { return m_timeSinceGameStartFloat; }
	    long performanceSum;
	    long[] perfCountArray;
	    long currentPerformanceCount;
	    long performanceFrequency;
	    long firstPerformanceCount;
	    long elapsedPerformanceCount;
	    long perfCount;

	    double m_frameTime;
	    double m_timeSinceGameStart;
	    float m_timeSinceGameStartFloat;

	}

}
