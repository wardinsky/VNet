using System;

namespace VNetLib
{
		
	public class VNetUtils
	{
	    static public UInt64 GenerateUID()
	    {
	        double currentTime = VNetTimer.Inst.GetSystemTimeNow();
	        UInt64 clientID = VNet.Inst.GetUID();
	        double seed = (new Random()).NextDouble();

	        UInt64 timeRe = BitConverter.ToUInt64(BitConverter.GetBytes(currentTime), 0);
	        UInt64 seedRe = BitConverter.ToUInt64(BitConverter.GetBytes(seed), 0);

	        return (clientID ^ timeRe) + seedRe;
	    }

		static public UInt64 GenerateUIDInEditor()
		{
			VNetTimer timer = new VNetTimer();
			double currentTime = timer.GetSystemTimeNow();
			timer = null;

			UInt64 clientID = VNetPlatform.GetUID();
			double seed = (new Random()).NextDouble();


			UInt64 timeRe = BitConverter.ToUInt64(BitConverter.GetBytes(currentTime), 0);
			UInt64 seedRe = BitConverter.ToUInt64(BitConverter.GetBytes(seed), 0);

			return (clientID ^ timeRe) + seedRe;
		}
	}
}
