using System;
using System.Net;
using System.Net.Sockets;

namespace VNetLib
{
		

	public class VNetSockMultiLink : UnityEngine.MonoBehaviour
	{
		public void OnDestroy()
		{
			Shutdown();
		}		
	    public bool Initialize()
	    {
			if (m_isInitialized)
				return true;
			
	        m_isInitialized = InitializeClient();
	        if (m_isInitialized)
	            m_isInitialized = InitializeServer();
	        return m_isInitialized;
	    }

	    public bool Shutdown()
	    {
			if (serverSocket != null)
			{
				serverSocket.Close();
			}
			if (clientSocket != null)
			{
				clientSocket.Close();
			}


			serverSocket = null;
			clientSocket = null;

	        return true;
	    }

	    public bool Send(VNetPacket outPacket)
	    {
	        if (m_isInitialized == false)
	            return false;

			int bytesSent = clientSocket.SendTo(outPacket.GetBytes(), 
				outPacket.GetLength(), 
				SocketFlags.None,
				multiEndpoint);
			return bytesSent > 0;
	    }

	    public bool Recv(VNetPacket inPacket)
	    {
			if (m_isInitialized == false)
				return false;

			System.Collections.Generic.List<Socket> socketList = new System.Collections.Generic.List<Socket>();
			socketList.Add(serverSocket);

			Socket.Select(socketList, null, null, VNetCommon.CLIENT_LINK_TV_TIMEOUT);
			if (socketList.Count == 0)
				return false;

            EndPoint ep = new IPEndPoint(new IPAddress(0), 0);
			int ret = serverSocket.ReceiveFrom(inPacket.data, ref ep);
			if (ret == -1)
			{
				Shutdown();
				return false;
			}
			if (ret == 0)
				return false;

            inPacket.IP_Port = ep as IPEndPoint;
            inPacket.length = (short)(ret);

			return true;

	    }

	    ///////////
	    bool InitializeClient()
	    {
			
			if (VNetManager.Inst.UseIPv6)
			{
				clientSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
				IPAddress ip = IPAddress.Parse(VNetManager.Inst.MulticastV6Address);
				IPEndPoint any = new IPEndPoint(IPAddress.IPv6Any, 0);
				multiEndpoint = new IPEndPoint(ip, VNetCommon.MULTI_LINK_PORT);

				clientSocket.Bind(any);
				//clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(ip, 0));
				clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, VNetCommon.MULTI_LINK_TTL);
			} 
			else
			{
				clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				IPAddress ip = IPAddress.Parse(VNetManager.Inst.MulticastAddress);
				multiEndpoint = new IPEndPoint(ip, VNetCommon.MULTI_LINK_PORT);
				IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);

				clientSocket.Bind(any);
				clientSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, 0));
				clientSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, VNetCommon.MULTI_LINK_TTL);
			}
			

			//clientSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.UseLoopback, 1);
	        return true;
	    }

	    bool InitializeServer()
	    {
			if (VNetManager.Inst.UseIPv6)
			{
				serverSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
				IPEndPoint ipep = new IPEndPoint(IPAddress.IPv6Any, VNetCommon.MULTI_LINK_PORT);
				IPAddress ip = IPAddress.Parse(VNetManager.Inst.MulticastV6Address);

				serverSocket.Bind(ipep);
				//serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(ip));
				serverSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ReuseAddress, true);
			}
			else
			{				
				serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				IPEndPoint ipep = new IPEndPoint(IPAddress.Any, VNetCommon.MULTI_LINK_PORT);
				IPAddress ip = IPAddress.Parse(VNetManager.Inst.MulticastAddress);

				serverSocket.Bind(ipep);
				serverSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Any));
				serverSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
			}

            return true;
	    }

	    bool m_isInitialized;

	    // Sockets
		IPEndPoint multiEndpoint;
	    Socket clientSocket;
	    Socket serverSocket;
	}
}
