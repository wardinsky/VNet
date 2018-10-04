using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;

public class VNetGameStatusMessage : VNetMessage {

    public enum RestartType {    
        ToTitle,
        SameConnectionType,
        AsHost,
        AsClient,
        DefaultConnection,
        QuitToDesktop
    }

    public VNetGameStatusMessage() : base("VNetGameStatusMessage") { }
	public bool TimeUp;
	public bool Player2ReadyToBegin;
    public float BeginGameAtTime;
    public int Restart;
    public int RemoteRestartConfirmed;

    public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(TimeUp);
		writer.Write(Player2ReadyToBegin);
		writer.Write(BeginGameAtTime);
		writer.Write(Restart);
		writer.Write(RemoteRestartConfirmed);
    }

    public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
        TimeUp = reader.ReadBoolean();
        Player2ReadyToBegin = reader.ReadBoolean();
        BeginGameAtTime = reader.ReadSingle();
        Restart = reader.ReadInt32();
        RemoteRestartConfirmed = reader.ReadInt32();
    }

	public override VNetMessage Clone()
	{
        VNetGameStatusMessage clone = (VNetGameStatusMessage)base.Clone();
		clone.TimeUp = TimeUp;
		clone.Player2ReadyToBegin = Player2ReadyToBegin;
		clone.BeginGameAtTime = BeginGameAtTime;        
		clone.Restart = Restart;
		clone.RemoteRestartConfirmed = RemoteRestartConfirmed;
        return clone;
	}

	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetGameStatusMessage();
	}
}

