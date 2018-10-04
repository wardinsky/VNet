using System;
using System.IO;

namespace VNetLib
	{

	public class VNetMessage
	{
	    public VNetMessage(VNetStringChksum typeName)
	    {
	        __typeName = typeName;
            __size = 0;
	    }

	    public VNetStringChksum GetMessageType() { return __typeName; }
	    public Int32 GetReliableIndex() { return m_reliableIndex; }
	    public void SetReliableIndex(Int32 index) { m_reliableIndex = index; }
        
	    public virtual VNetMessage Clone()
	    {
	        var bc = CreateInstanceForClone();
	        bc.__typeName = __typeName;
	        bc.__size = __size;
	        bc.m_reliableIndex = m_reliableIndex;
	        return bc;
	    }
	  
	    protected virtual VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessage("");
	    }

	    public virtual void ToBytes(BinaryWriter writer)
	    {
	        writer.Write(__typeName.checksum);
	        writer.Write(__size);
	        writer.Write(m_reliableIndex);
	    }

	    public virtual void FromBytes(BinaryReader reader)
	    {
	        __typeName = new VNetStringChksum(reader.ReadUInt32());
	        __size = reader.ReadInt32();
	        m_reliableIndex = reader.ReadInt32();
	    }

	    public VNetPacket _packet;
	    public VNetClient _client;

	    // Message Data Starts here...
	    public VNetStringChksum __typeName;
	    public Int32 __size;

	    private Int32 m_reliableIndex;
	}



	public class VNetMessagePacketAck : VNetMessage
	{
	    public VNetMessagePacketAck() : base("VNetMessagePacketAck") { }
	    public override VNetMessage Clone()
	    {
	        var clone = base.Clone();
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessagePacketAck();
	    }
	}

	public class VNetMessagePingClient : VNetMessage
	{
	    public VNetMessagePingClient() : base("VNetMessagePingClient") { }
	    public double timeSent;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(timeSent);
	    }

	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        timeSent = reader.ReadDouble();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessagePingClient clone = (VNetMessagePingClient)base.Clone();
	        clone.timeSent = timeSent;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessagePingClient();
	    }
	}

	public class VNetMessagePongClient : VNetMessage
	{
	    public VNetMessagePongClient() : base("VNetMessagePongClient") { }
	    public double timeSent;

	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(timeSent);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        timeSent = reader.ReadDouble();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessagePongClient clone = (VNetMessagePongClient)base.Clone();
	        clone.timeSent = timeSent;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessagePongClient();
	    }
	}

	public class VNetMessageTimeRequest : VNetMessage
	{
	    public VNetMessageTimeRequest() : base("VNetMessageTimeRequest") { }
	    public double currentTime;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(currentTime);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        currentTime = reader.ReadDouble();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageTimeRequest clone = (VNetMessageTimeRequest)base.Clone();
	        clone.currentTime = currentTime;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageTimeRequest();
	    }
	}

	public class VNetMessageTimeReturn : VNetMessage
	{
	    public VNetMessageTimeReturn() : base("VNetMessageTimeReturn") { }
	    public double clientTime;
	    public double serverTime;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(clientTime);
	        writer.Write(serverTime);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        clientTime = reader.ReadDouble();
	        serverTime = reader.ReadDouble();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageTimeReturn clone = (VNetMessageTimeReturn)base.Clone();
	        clone.clientTime = clientTime;
	        clone.serverTime = serverTime;
	        return clone;
	    }

	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageTimeReturn();
	    }
	}

	public class VNetMessageJoinSession : VNetMessage
	{
	    public VNetMessageJoinSession() : base("VNetMessageJoinSession") { }
	    public UInt64 sessionUID;
		public Int32 role;
	    public string userName;

	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(sessionUID);
			writer.Write(role);
	        writer.Write(userName.Length);
            foreach (char c in userName)
                writer.Write((ushort)c);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        sessionUID = reader.ReadUInt64();
			role = reader.ReadInt32();
	        int len = reader.ReadInt32();
            userName = "";
            for (int i = 0; i < len; i++)
            {
                char c = (char)reader.ReadChar();
                userName += c;
            }   
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageJoinSession clone = (VNetMessageJoinSession)base.Clone();
	        clone.sessionUID = sessionUID;
			clone.role = role;
	        clone.userName = userName;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageJoinSession();
	    }
	}

	public class VNetMessageSessionAvailable : VNetMessage
	{
	    public VNetMessageSessionAvailable() : base("VNetMessageSessionAvaliable")
	    {
	        client = new VNetSimpleClientData[VNetCommon.NET_MAX_CLIENTS - 1];

	    }
	    public UInt64 UID;
	    public Int32 sessionAvaliable;
	    public VNetSimpleClientData host;
	    public SByte numClients;
	    public VNetSimpleClientData[] client;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);

	        writer.Write(UID);
	        writer.Write(sessionAvaliable);
            writer.Write(numClients);
            
            host.ToBytes(writer);
	        foreach (VNetSimpleClientData c in client)
	        {
				if (c != null)
	            	c.ToBytes(writer);
	        }
	    }

	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);

	        UID = reader.ReadUInt64();
	        sessionAvaliable = reader.ReadInt32();
	        numClients = reader.ReadSByte();

            host = new VNetSimpleClientData();
            host.FromBytes(reader);
            for (int i = 0; i < numClients; i++)
			{
				if (client [i] == null)
					client [i] = new VNetSimpleClientData();
				client [i].FromBytes(reader);
			}

	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageSessionAvailable clone = (VNetMessageSessionAvailable)base.Clone();

	        clone.UID = UID;
	        clone.sessionAvaliable = sessionAvaliable;
	        clone.numClients = numClients;

	        // copy client data
	        clone.host = VNetSimpleClientData.ComponentCopy(host);
	        for (int i = 0; i < clone.client.Length; i++)
	            clone.client[i] = VNetSimpleClientData.ComponentCopy(client[i]);

	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageSessionAvailable();
	    }

	}

	public class VNetMessageNewClient : VNetMessage
	{
	    public VNetMessageNewClient() : 
            base("VNetMessageNewClient")
	    {
            
	    }
	    public VNetSimpleClientData clientData;
	    public UInt64 sessionUID;
	    public Int32 role;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        clientData.ToBytes(writer);
	        writer.Write(sessionUID);
	        writer.Write(role);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        clientData.FromBytes(reader);
	        sessionUID = reader.ReadUInt64();
	        role = reader.ReadInt32();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageNewClient clone = (VNetMessageNewClient)base.Clone();
	        clone.clientData = VNetSimpleClientData.ComponentCopy(clientData);
	        clone.sessionUID = sessionUID;
	        clone.role = role;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageNewClient();
	    }
	}

	public class VNetMessageAcceptClient : VNetMessage
	{
	    public VNetMessageAcceptClient() : base("VNetMessageAcceptClient") { }
	    public UInt64 sessionUID;
	    public UInt64 clientUID;
		public Int32 role;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(sessionUID);
	        writer.Write(clientUID);
			writer.Write(role);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        sessionUID = reader.ReadUInt64();
	        clientUID = reader.ReadUInt64();
			role = reader.ReadInt32();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageAcceptClient clone = (VNetMessageAcceptClient)base.Clone();
	        clone.sessionUID = sessionUID;
	        clone.clientUID = clientUID;
			clone.role = role;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageAcceptClient();
	    }
	}
	       
	public class VNetMessageLeaveSession : VNetMessage
	{
	    public VNetMessageLeaveSession() : base("VNetMessageLeaveSession") { }
	    public UInt64 sessionUID;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(sessionUID);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        sessionUID = reader.ReadUInt64();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageLeaveSession clone = (VNetMessageLeaveSession)base.Clone();
	        clone.sessionUID = sessionUID;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageLeaveSession();
	    }
	}

	public class VNetMessageLeaveSessionConfirm : VNetMessage
	{
	    public VNetMessageLeaveSessionConfirm() : base("VNetMessageLeaveSessionConfirm") { }
	    public UInt64 clientUID;
	    public override void ToBytes(BinaryWriter writer)
	    {
	        base.ToBytes(writer);
	        writer.Write(clientUID);
	    }
	    public override void FromBytes(BinaryReader reader)
	    {
	        base.FromBytes(reader);
	        clientUID = reader.ReadUInt64();
	    }
	    public override VNetMessage Clone()
	    {
	        VNetMessageLeaveSessionConfirm clone = (VNetMessageLeaveSessionConfirm)base.Clone();
	        clone.clientUID = clientUID;
	        return clone;
	    }
	    protected override VNetMessage CreateInstanceForClone()
	    {
	        return new VNetMessageLeaveSessionConfirm();
	    }
	}
}
