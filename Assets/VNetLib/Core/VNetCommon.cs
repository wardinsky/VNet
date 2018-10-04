using System;

namespace VNetLib
	{

	public class VNetSimpleClientData
	{
	    static public VNetSimpleClientData ComponentCopy(VNetSimpleClientData rhs)
	    {
			if (rhs == null)
				return null;
			
	        VNetSimpleClientData data = new VNetSimpleClientData();
	        data.uid = rhs.uid;
	        data.ip = rhs.ip;
	        data.port = rhs.port;
	        data.active = rhs.active;
	        data.name = rhs.name;
			data.role = rhs.role;
	        return data;
	    }

        public void ToBytes(System.IO.BinaryWriter writer)
	    {
	        writer.Write(uid);            

			if (ip == null)
			{
				writer.Write(0);
                writer.Write(0);
			}
			else
			{
	        	writer.Write(ip.GetAddressBytes().Length);
	        	writer.Write(ip.GetAddressBytes());
			}

	        writer.Write(port);
	        writer.Write(active);

			writer.Write(role);
	        writer.Write(name.Length);
            foreach (char c in name)
                writer.Write((short)c);
	    }
	    public void FromBytes(System.IO.BinaryReader reader)
	    {
	        uid = reader.ReadUInt64();

	        int len = reader.ReadInt32();
			if (len != 0)
			{
				byte[] ipbytes = reader.ReadBytes(len);
				ip = new System.Net.IPAddress(ipbytes);
			} else
			{
                len = reader.ReadInt32(); // read 4 more bytes
				ip = null;
			}
	        port = reader.ReadInt32();
	        active = reader.ReadByte();
			role = reader.ReadInt32();
	        len = reader.ReadInt32();
            name = "";
            for (int i = 0; i < len; i++)
            {
                char c = (char)reader.ReadInt16();
                name += c;
            }   

	    }



	    public UInt64 uid;
	    public System.Net.IPAddress ip;
	    public Int32 port;
	    public byte active;
	    public string name;
		public int role;


	};


	public class VNetCommon
	{
		static public bool SHOW_LOGS = true;
	    static public ulong NET_CLIENT_INVALID_UID = ulong.MaxValue;

	    static public int NET_CLIENT_MAX_NAME_LENGTH = 32;
	    static public int NET_CLIENT_RELIABLE_QUEUE = 32;
	    static public float NET_CLIENT_CONNECTION_TIME = 1.0f;
	    static public float NET_CLIENT_SEND_FRAMETIME = .05f;
	    static public float NET_CLIENT_TIMEOUT_SECONDS = 20.0f;
	    static public int NET_MAX_CLIENTS = 8;

	    static public int NET_CLIENT_BANDWIDTH_COUNTER_QUEUE_LENGTH = 4;
	    static public float NET_CLIENT_PING_WAIT_TIME = .65f;
	    static public int NET_CLIENT_PING_QUEUE_LENGTH = 8;

	    static public ulong NET_SESSION_INVALID_UID = ulong.MaxValue;
	    static public float NET_SESSION_STALE_TIME = 5.0f;

	    static public ushort NET_PACKET_MAX_LENGTH = 1024;
	    //static public uint NET_PACKET_IDENT_HEADER = (0x52254321);
	    static public uint NET_PACKET_QUEUE_SIZE = 32;

	    static public ushort CLIENT_LINK_LOCAL_PORT = 2946;
	    static public ushort CLIENT_LINK_TV_TIMEOUT = 500;
	    static public ushort MULTI_LINK_TTL = 8;
	    static public string MULTI_LINK_IP = "238.0.0.1";
	    static public ushort MULTI_LINK_PORT = 6241;

	    static public float NET_HOST_LOBBY_SEND_TIME = 1.0f;
	    static public ushort NET_TIME_NUM_SYNCS = 5;
	    static public float NET_TIME_SYNC_WAIT_TIME = 1.0f;

	    static public bool NET_DEBUG_FREEZE_WITH_HEARTBEAT = false;
	    static public float NET_DEBUG_FREEZE_HEARTBEAT_WAIT_TIME = .65f;



	}
}