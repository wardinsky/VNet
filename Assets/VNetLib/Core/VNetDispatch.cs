using System;
using System.Collections.Generic;
using System.IO;

namespace VNetLib
	{

	public delegate void Del<T>(T item);

	public class VNetDispatchRegistryBase
	{
	    public virtual VNetMessage ConvertDataToMessage(VNetPacket packet) { return null; }
	    public virtual void Callback(VNetMessage message) { }

	    public int GetNumCallbacks() { return m_numCallbacks; }
	    protected int m_numCallbacks;
	}

	public class VNetDispatchRegistry<T> : VNetDispatchRegistryBase where T : VNetMessage, new()
	{
	    public static VNetMessage ConvertPacketToMessage(VNetPacket packet)
	    {
	        UInt32 size = packet.m_nextMessageLength;

	        // allocate and setup the vtable
	        VNetMessage newMessage = new T();
	        byte[] message = new byte[size];
	        packet.ReadNextMessage(message);

	        using (MemoryStream m = new MemoryStream(message))
	        {
	            using (BinaryReader reader = new BinaryReader(m))
	            {
	                newMessage.FromBytes(reader);
	            }
	        }
	        newMessage._packet = packet;
	        return newMessage;
	    }

	    public override VNetMessage ConvertDataToMessage(VNetPacket packet)
	    {
	        return ConvertPacketToMessage(packet);
	    }
	    
	    public static void RegisterCB(Dictionary<UInt32, VNetDispatchRegistryBase> registry, Del<T> cb)
	    {
	        if (me == null)
	        {
	            me = new VNetDispatchRegistry<T>();
	            T message = new T();
	            registry.Add(message.GetMessageType().checksum, me);    
	        }
	        me.myDelegates += cb;
	        me.m_numCallbacks++;
	    }

	    public static void UnregisterCB(Dictionary<UInt32, VNetDispatchRegistryBase> registry, Del<T> cb)
	    {
	        if (me == null)
	            return;

	        me.myDelegates -= cb;
	        me.m_numCallbacks--;
	        if (me.myDelegates == null)
	        {
	            T message = new T();
	            UInt32 chk = message.GetMessageType().checksum;
	            registry.Remove(chk);
	            me = null;
	        }
	    }

	    public override void Callback(VNetMessage message)
	    {
	        myDelegates(message as T);
	    }

	    static VNetDispatchRegistry<T> me;
	    public Del<T> myDelegates;
	}

	public class VNetDispatch
	{
	    static VNetDispatch Inst;

	    public VNetDispatch()
	    {
	        Inst = this;
	        m_register = new Dictionary<uint, VNetDispatchRegistryBase>();
	    }
	    ~VNetDispatch()
	    {
	        Inst = null;
	    }

	    
	    
	    public static void RegisterListenerInst<T>(Del<T> delegateFunction)
	        where T : VNetMessage, new()
	    {
	        VNetDispatchRegistry<T>.RegisterCB(Inst.m_register, delegateFunction);
	    }

	    public static void UnregisterListenerInst<T>(Del<T> delegateFunction)
	        where T : VNetMessage, new()
	    {
	        VNetDispatchRegistry<T>.UnregisterCB(Inst.m_register, delegateFunction);
	    }

	    public void HandlePacketIn(VNetPacket packet, VNetClient client)
	    {
	        int numMessagesToRead = packet.header.numMessages;

	        packet.StartPacketRead();

	        Dictionary<int, VNetMessage> reliableIn = new Dictionary<int, VNetMessage>();

	        while (numMessagesToRead-- > 0)
	        {
	            if (packet.UpdateNextMessageHeaders() == false)
	                break;

	            if (m_register.ContainsKey(packet.m_nextMessageType.checksum) == false)
	                packet.SkipNextMessage();
	            else
	            {
	                VNetDispatchRegistryBase reg = m_register[packet.m_nextMessageType.checksum];
	                if (reg.GetNumCallbacks() == 0)
	                    packet.SkipNextMessage();
	                else
	                {
	                    VNetMessage message = reg.ConvertDataToMessage(packet);
	                    message._client = client;
	                    message._packet = packet;
                        message.__typeName = new VNetStringChksum(packet.m_nextMessageType.checksum);


	                    if (message.GetReliableIndex() != -1)
	                    {
                            if (reliableIn.ContainsKey(message.GetReliableIndex()))
                                continue;
	                        reliableIn.Add(message.GetReliableIndex(), message);
	                        continue;
	                    }

	                    reg.Callback(message);
	                }
	            }
	        }
	        if (client != null)
	        {
	            while (reliableIn.ContainsKey(client.GetNextReliableIndex()))
	            {
	                VNetMessage message = reliableIn[client.GetNextReliableIndex()];
	                client.ReliableMessageCheck(message);

	                VNetDispatchRegistryBase reg = m_register[message.GetMessageType().checksum];
	                reg.Callback(message);
	            }
	        }

	        reliableIn.Clear();

	    }


	    Dictionary<UInt32, VNetDispatchRegistryBase> m_register;

	}
}