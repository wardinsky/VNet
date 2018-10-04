using System;
using System.Net.NetworkInformation;

namespace VNetLib
{

	public class VNetPlatform
	{
	    public static Int16 GetEndianValue()
	    {
	        return 1;
	    }

	    public static void FillLocalUsername(ref string name)
	    {
	        name = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
	    }

	    public static UInt64 GetUID()
	    {
	        foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
	        {
	            if (nic.OperationalStatus != OperationalStatus.Up)
	                continue;

	            byte[] macBytes = nic.GetPhysicalAddress().GetAddressBytes();
				byte[] full = new byte[8];
				for (int i = 0; i < macBytes.Length; i++)
					full [i] = macBytes [i];
				return BitConverter.ToUInt64(full, 0);
	        }
	        return UInt64.MaxValue;
	    }
	}
}