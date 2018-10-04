using System;

namespace VNetLib
	{
	public class VNetVerifier
	{
	    public static bool VerifyPacket(VNetPacket packet)
	    {
	        if (packet.length < VNetPacketHeader.SizeOf())
	            return false;

            // RJW Endianess... todo

            // Get out the packet header
            packet.header.ReadBytes(packet.data);

	        // sizes don't match
	        if (packet.length != packet.header.dataLength + VNetPacketHeader.SizeOf())
	            return false;

	        if (!IsPacketHeaderValid(packet.header))
	            return false;

	        return true;
	    }
	    private static bool IsPacketHeaderValid(VNetPacketHeader header)
	    {
	        // client packet
			if (header.identHeader != VNetManager.Inst.GUIDHeader)
	            return false;

	        // packet from me
	        if (header.clientUID == VNet.Inst.GetUID())
	            return false;

	        if (header.sessionUID != VNetCommon.NET_SESSION_INVALID_UID)
	        {
	            // Get the current session sessionID
	            UInt64 currentSession = VNetSession.Inst.GetSessionUID();
	            if (currentSession == VNetCommon.NET_SESSION_INVALID_UID)
	                return true;

	            if (header.sessionUID == currentSession)
	                return true;

	            // not this session
	            return false;
	        }

	        // it was a valid multicast packet
	        return true;
	    }
	}
}
