using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;

public class VNetAlienReportMessage : VNetMessage
{
	public VNetAlienReportMessage() : base("VNetAlienReportMessage") { }
	public float AlienHealth;
	public bool AlienEscaped;
	public bool AlienDied;
	public float LatestTeleportTime;

    public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(AlienHealth);
		writer.Write(AlienEscaped);
		writer.Write(AlienDied);
		writer.Write(LatestTeleportTime);        
    }

    public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
		AlienHealth = reader.ReadSingle();
		AlienEscaped = reader.ReadBoolean();
		AlienDied = reader.ReadBoolean();
        LatestTeleportTime = reader.ReadSingle();
    }
	public override VNetMessage Clone()
	{
		VNetAlienReportMessage clone = (VNetAlienReportMessage)base.Clone();
		clone.AlienHealth = AlienHealth;
		clone.AlienDied = AlienDied;
		clone.AlienEscaped = AlienEscaped;
		clone.LatestTeleportTime = LatestTeleportTime;
        return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetAlienReportMessage();
	}
}

