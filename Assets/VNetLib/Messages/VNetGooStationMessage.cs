using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;

public class VNetGooStationMessage : VNetMessage {

    public VNetGooStationMessage() : base("VNetGooStationMessage") { }
    public ulong networkID;
    public bool AllUsedUp;

    public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(networkID);
		writer.Write(AllUsedUp);
    }

    public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
        networkID = reader.ReadUInt64();
        AllUsedUp = reader.ReadBoolean();
    }

	public override VNetMessage Clone()
	{
        VNetGooStationMessage clone = (VNetGooStationMessage)base.Clone();
		clone.networkID = networkID;
		clone.AllUsedUp = AllUsedUp;
        return clone;
	}

	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetGooStationMessage();
	}
}

