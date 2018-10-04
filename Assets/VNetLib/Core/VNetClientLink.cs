using System;

namespace VNetLib
{

	public abstract class VNetClientLink
	{
	    public VNetClientLink()
	    {

	    }

	    public abstract bool Initialize();
	    public abstract bool Shutdown();

	    public abstract bool Send(VNetPacket outPacket);

	    public abstract bool Recv(VNetPacket inPacket);

	    protected bool m_isInitialized;

	}
}