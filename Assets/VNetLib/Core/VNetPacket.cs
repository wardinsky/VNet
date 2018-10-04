using System;
using System.Runtime.InteropServices;

namespace VNetLib
	{
	public struct VNetPacketHeader
	{
	    static public Int32 SizeOf() { return 4 + 8 + 8 + 4 + 2 + 2 + 2 + 1 + 1; }
	    public UInt32 identHeader;
	    public UInt64 sessionUID;
	    public UInt64 clientUID;

	    // RUDP header
	    public Int32 lastReliablePacketRecvd;

	    // messages inside
	    public Int16 dataLength;
	    public Int16 origLength;
	    public Int16 parentEndianess;
	    public SByte numMessages;
	    public Byte compression;

	    public void FillBytes(byte[] buffer)
	    {
            using (System.IO.MemoryStream m = new System.IO.MemoryStream(buffer, true))
            {
                using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(m))
                {
                    writer.Write(identHeader);
                    writer.Write(sessionUID);
                    writer.Write(clientUID);
                    writer.Write(lastReliablePacketRecvd);
                    writer.Write(dataLength);
                    writer.Write(origLength);
                    writer.Write(parentEndianess);
                    writer.Write(numMessages);
                    writer.Write(compression);

                    writer.Close();
                }
                m.Close();
            }
	    }

	    public void ReadBytes(byte[] buffer)
	    {
	        identHeader = BitConverter.ToUInt32(buffer, 0);
	        sessionUID = BitConverter.ToUInt64(buffer, 4);
	        clientUID = BitConverter.ToUInt64(buffer, 12);

	        lastReliablePacketRecvd = BitConverter.ToInt32(buffer, 20);

	        dataLength = BitConverter.ToInt16(buffer, 24);
	        origLength = BitConverter.ToInt16(buffer, 26);
	        parentEndianess = BitConverter.ToInt16(buffer, 28);
	        numMessages = (SByte)buffer[30];
	        compression = (Byte)buffer[31];        
	    }
	}

	public class VNetPacket
	{
	    public VNetPacket()
	    {
	        int sizeofVnetPacket = VNetPacketHeader.SizeOf();
	        data = new byte[VNetCommon.NET_PACKET_MAX_LENGTH];
	        m_cursor = sizeofVnetPacket;
	    }

	    public void Clear()
	    {
	        m_cursor = VNetPacketHeader.SizeOf();

	        length = 0;
	        IP_Port = null;

	        // Compression data
	        m_currentRun = 0;
	        m_previousValue = 0x100;
	        m_farRunValue = 0;

	        // clear the header
	        header.sessionUID = 0;
	        header.clientUID = 0;
	        header.lastReliablePacketRecvd = 0;
	        header.dataLength = 0;
	        header.origLength = 0;
	        header.parentEndianess = 1;
	        header.numMessages = 0;
	        header.compression = 0;

	    }

	    public byte[] GetBytes()
	    {
	        header.FillBytes(data);
	        return data;
	    }

	    public void ReadBytes(byte[] packet, int len)
	    {
	        for (int i = 0; i < len; i++)
	        {
	            data[i] = packet[i];
	        }
	        // Get out the header
	        header.ReadBytes(data);
	    }

	    public Int32 GetLength()
	    {
	        return m_cursor;
	    }

	    public VNetPacketHeader header;
	    public Byte[] data;

	    // Nothing below this is sent
	    public Int16 length;

	    // Identity
	    public System.Net.IPEndPoint IP_Port;

	    public VNetStringChksum m_nextMessageType;
	    public UInt32 m_nextMessageLength;

	    // Packet Creation
	    int m_cursor;

	    // Compression
	    Byte m_currentRun;
	    UInt16 m_previousValue;
	    Byte m_farRunValue;
	    Byte m_currentValue;
	    Byte m_readState;

	    public bool AddNetMessage(VNetMessage message)
	    {
	        byte[] src = null;
	        using (System.IO.MemoryStream m = new System.IO.MemoryStream())
	        {
	            using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(m))
	            {
	                message.ToBytes(writer);
	            }
	            src = m.ToArray();
	        }
            
            message.__size = src.Length;
            header.origLength += (short)message.__size;

            int len = src.Length;

            // Carefully add the size of this message
            byte[] lengthBytes = BitConverter.GetBytes(src.Length);
            for (int i = 0; i < 4; i++)
                src[4 + i] = lengthBytes[i];

            if (m_cursor == VNetPacketHeader.SizeOf() &&
                header.numMessages != 0)
            {
                throw new Exception();
            }

            // Compress it
            int newCursor = m_cursor;
            if (!CompressBuffer(src, data, ref newCursor))
            {
                return false;
            }

            // Sanity check...
            CheckCompression(src, data, m_cursor, message.__size, newCursor - m_cursor);


            m_cursor = newCursor;
            header.dataLength = (short)(m_cursor - VNetPacketHeader.SizeOf());

            return true;

	    }

        static public void CheckCompression(byte[] src, byte[] dest, int destOffset, int origLength, int compLength)
        {
            int readState = 0;
            int cursor = 0;
            byte currentValue = 0;
            ushort prevValue = 0x100;
            byte curRun = 0;
            int readIndex = 0;

            while (readIndex < origLength)
            {
                switch (readState)
                {
                    case 0:
                        currentValue = dest[destOffset + cursor++];
                        if (currentValue != prevValue)
                            readState = 1;
                        else
                            readState = 2;
                        continue;
                    case 1:
                        prevValue = currentValue;
                        readState = 0;
                        break;
                    case 2:
                        curRun = dest[destOffset + cursor++];
                        readState = 3;
                        break;
                    case 3:
                        if (curRun == 0)
                        {
                            readState = 0;
                            prevValue = 0x100;
                            continue;
                        }
                        curRun--;
                        break;
                    default:
                        throw new Exception();
                }

                if (currentValue != src[readIndex])
                {
                    throw new Exception();
                }
                readIndex++;
            }

        }

        public bool CompressBuffer(byte[] src, byte[] dest, ref int destOffset)
        {
            int len = src.Length;

            int srcCursor = 0;
            UInt16 curVal = 0;
            Byte curRun = 0;
            Byte farRun = 0;
            UInt16 lastPrev = 0x100;
            int maxTail = dest.Length - 3;

            while (len-- > 0)
            {
                curVal = src[srcCursor++];
                if (curVal != lastPrev)
                {
                    // Print out the previous run
                    if (curRun > 0)
                    {
                        if (lastPrev == 0x100)
                            dest[destOffset++] = farRun;
                        else
                            dest[destOffset++] = (Byte)lastPrev;
                        dest[destOffset++] = (Byte)(curRun - 1);
                        curRun = 0;
                    }
                    dest[destOffset++] = (Byte)curVal;
                    lastPrev = curVal;

                    if (destOffset > maxTail)
                    {
                        return false;
                    }
                }
                else // Run
                {
                    curRun++;
                    if (curRun > 254)
                    {
                        farRun = (Byte)lastPrev;
                        lastPrev = 0x100;
                    }
                }
            }
            if (curRun > 0)
            {
                dest[destOffset++] = (Byte)lastPrev;
                dest[destOffset++] = (Byte)(curRun - 1);
            }
            
            return true;
        }

	    public void EndPacket()
	    {
            length = (short)m_cursor;
	        header.dataLength = (short)(m_cursor - VNetPacketHeader.SizeOf());
	    }

	    // Decompression
	    public void StartPacketRead()
	    {
	        m_nextMessageLength = 0;
	        m_nextMessageType = new VNetStringChksum(0);
	        m_cursor = VNetPacketHeader.SizeOf();

	        m_currentRun = 0;
	        m_currentValue = 0;
	        m_previousValue = 0x100;
	        m_readState = 0;


	    }

	    public bool UpdateNextMessageHeaders()
	    {
	        byte[] buffer = new byte[8];
	        DecompressToBuffer(buffer, 8, 0);

	        UInt32 type = BitConverter.ToUInt32(buffer, 0);
	        m_nextMessageType = new VNetStringChksum(type);
	        m_nextMessageLength = BitConverter.ToUInt32(buffer, 4);

            if (m_nextMessageType.checksum == 0 || m_nextMessageLength == 0)
                return false;
	        return true;

	    }

	    public void ReadNextMessage(byte[] buffer)
	    {
	        DecompressToBuffer(buffer, (ushort)(m_nextMessageLength - 8), 8);
	    }
	    public void SkipNextMessage()
	    {
	        byte[] buffer = new byte[m_nextMessageLength];
	        ReadNextMessage(buffer);
	    }
	    public void DecompressToBuffer(Byte[] buffer, UInt16 length, UInt16 offset)
	    {
	        for (int i = 0; i < length; i++)
	        {
	            buffer[i + offset] = DecompressNextByte();
	        }
	    }

	    public Byte DecompressNextByte()
	    {
            if (m_cursor > header.dataLength + VNetPacketHeader.SizeOf())
            {
                return 0;
            }
	        switch (m_readState)
	        {
	            case 0:
	                m_currentValue = data[m_cursor++];
	                if (m_currentValue != m_previousValue)
	                    m_readState = 1;
	                else
	                    m_readState = 2;
	                return DecompressNextByte();
	            case 1:
	                m_previousValue = m_currentValue;
	                m_readState = 0;
	                return m_currentValue;
	            case 2:
	                m_currentRun = data[m_cursor++];
	                m_readState = 3;
	                return m_currentValue;
	            case 3:
	                if (m_currentRun == 0)
	                {
	                    m_readState = 0;
	                    m_previousValue = 0x100;
	                    return DecompressNextByte();
	                }
	                m_currentRun--;
	                return m_currentValue;
	            default:
	                return 0;
	        }
	    }

	}
}
