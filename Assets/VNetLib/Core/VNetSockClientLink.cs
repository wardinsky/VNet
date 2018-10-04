using System;
using System.Net;
using System.Net.Sockets;

namespace VNetLib
{
	

	public class VNetSockClientLink : UnityEngine.MonoBehaviour
	{

		public void OnDestroy()
		{
			Shutdown();
		}

	    public bool Initialize()
	    {
			if (m_isInitialized)
				return true;
			
	        m_isInitialized = InitializeSend();
	        if (m_isInitialized)
	            m_isInitialized = InitializeReceive();
	        return m_isInitialized;
	    }
	    public bool Shutdown()
	    {
	        if (m_isInitialized == false)
	            return false;

			m_clientSocket.Close();
			m_serverSocket.Close();
	        m_isInitialized = false;
	        return true;
	        
	    }

	    public bool Send(VNetPacket outPacket)
	    {
	        if (m_isInitialized == false)
	            return false;
	        int ret = -1;
	        int dataLength = outPacket.header.dataLength + VNetPacketHeader.SizeOf();
            IPEndPoint endpoint = new IPEndPoint(outPacket.IP_Port.Address, VNetCommon.CLIENT_LINK_LOCAL_PORT);
			ret = m_clientSocket.SendTo(outPacket.GetBytes(), outPacket.GetLength(), SocketFlags.None, endpoint);
	        return ret != 0;
	    }

	    public bool Recv(VNetPacket inPacket)
	    {
			if (m_isInitialized == false)
				return false;

            System.Collections.Generic.List<Socket> ReadList = new System.Collections.Generic.List<Socket>();
            System.Collections.Generic.List<Socket> WriteList = new System.Collections.Generic.List<Socket>();
            System.Collections.Generic.List<Socket> ErrorList = new System.Collections.Generic.List<Socket>();
            ReadList.Add(m_serverSocket);
            Socket.Select(ReadList, WriteList, ErrorList, VNetCommon.CLIENT_LINK_TV_TIMEOUT);

            if (ReadList.Count == 0)
                return false;


            // Data was recieved
            EndPoint ep = new IPEndPoint(new IPAddress(0), 0);
			int ret = m_serverSocket.ReceiveFrom(inPacket.data, SocketFlags.None, ref ep);
			if (ret == -1)
			{
				Shutdown();
				return false;
			}
            inPacket.IP_Port = ep as IPEndPoint;
			inPacket.length = (short)ret;
			return true;

	    }

	    private bool InitializeSend()
	    {
			m_clientSocket = null;
			m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			return true;
	    }

	    private bool InitializeReceive()
	    {
			m_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			IPEndPoint socketAddr = new IPEndPoint(IPAddress.Any, VNetCommon.CLIENT_LINK_LOCAL_PORT);
			m_serverSocket.Bind(socketAddr);
            
	        return true;
	    }

	    // Client specific information

		Socket m_clientSocket;
		Socket m_serverSocket;
		bool m_isInitialized;

	}
}