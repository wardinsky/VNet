using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;

public class VNetMessageBlacklightPower : VNetMessage
{
	public VNetMessageBlacklightPower() : base("VNetMessageBlacklightPower") { }
	public ulong networkID;
	public float power;
    public bool pulse;

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(networkID);
		writer.Write(power);
		writer.Write(pulse);
    }

	public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
		networkID = reader.ReadUInt64();
		power = reader.ReadSingle();
		pulse = reader.ReadBoolean();
    }
	public override VNetMessage Clone()
	{
		VNetMessageBlacklightPower clone = (VNetMessageBlacklightPower)base.Clone();
		clone.power = power;
		clone.networkID = networkID;
		clone.pulse = pulse;
        return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetMessageBlacklightPower();
	}
}

