using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;

public class VNetMessageRunCoroutine : VNetMessage
{
	public VNetMessageRunCoroutine() : base("VNetMessageRunCoroutine") { }
	public ulong networkID;
	public string componentType;
	public string coroutineName;
	public double netTimeStart;


	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(networkID);
		writer.Write(componentType);
		writer.Write(coroutineName);
		writer.Write(netTimeStart);
	}

	public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
		networkID = reader.ReadUInt64();
		componentType = reader.ReadString();
		coroutineName = reader.ReadString();
		netTimeStart = reader.ReadDouble();
	}
	public override VNetMessage Clone()
	{
		VNetMessageRunCoroutine clone = (VNetMessageRunCoroutine)base.Clone();
		clone.networkID = networkID;
		clone.componentType = componentType;
		clone.coroutineName = coroutineName;
		clone.netTimeStart = netTimeStart;
		return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetMessageRunCoroutine();
	}
}

