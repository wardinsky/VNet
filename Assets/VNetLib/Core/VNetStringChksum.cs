using System;

namespace VNetLib
	{

	public class VNetStringChksum
	{
	    public VNetStringChksum()
	    {
	        checksum = 0;
	    }
	    public VNetStringChksum(string str)
	    {
	        checksum = CalculateStringChecksum(str);
	    }
	    public VNetStringChksum(uint chk)
	    {
	        checksum = chk;
	    }

	    public static implicit operator VNetStringChksum(string str)
	    {
	        return new VNetStringChksum(str);
	    }

	    public uint checksum { get; private set; }

	   static public bool operator != (VNetStringChksum lhs, VNetStringChksum rhs)
	    {
	        return lhs.checksum != rhs.checksum;
	    }
	    static public bool operator == (VNetStringChksum lhs, VNetStringChksum rhs)
	    {
	        return lhs.checksum == rhs.checksum;
	    }
	    public override bool Equals(object obj)
	    {
	        return this == (obj as VNetStringChksum);
	    }
	    public override int GetHashCode()
	    {
	        return base.GetHashCode();
	    }
	    private uint CalculateStringChecksum(string str)
	    {
	        int ptr = 0;
	        checksum = 0;
	        float intermediary = 0;

	        while (ptr < str.Length)
	        {
	            checksum += str[ptr];
	            intermediary = (float)str[ptr++];
	            intermediary *= BitConverter.ToUInt32(BitConverter.GetBytes(intermediary), 0) % 155323;
	            checksum *= BitConverter.ToUInt32(BitConverter.GetBytes(intermediary), 0) % 0xD5F288 + (checksum >> 16);
	        }
	        return checksum;
	    }
	        
	}

}
