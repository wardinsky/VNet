using UnityEngine;
using System.Collections;
using VNetLib;
using System.IO;

public class VNetMessageTeleportBlobMessage : VNetMessage
{
	public VNetMessageTeleportBlobMessage() : base("VNetMessageTeleportBlobMessage") { }
	public int numBlobs;

	public ulong[] blobNetIDs;
	public Vector3[] blobPositions;
	public bool[] blobsAlive;
	public float[] blobSizes;

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(numBlobs);
		for (int i = 0; i < numBlobs; i++)
		{
			writer.Write(blobNetIDs [i]);
			writer.Write(blobPositions [i].x);
			writer.Write(blobPositions [i].y);
			writer.Write(blobPositions [i].z);
			writer.Write(blobSizes [i]);
			writer.Write(blobsAlive [i]);
		}
	}

	public override void FromBytes(BinaryReader reader)
	{
		base.FromBytes(reader);
		numBlobs = reader.ReadInt32();
		blobNetIDs = new ulong[numBlobs];
		blobPositions = new Vector3[numBlobs];
		blobSizes = new float[numBlobs];
		blobsAlive = new bool[numBlobs];

		for (int i = 0; i < numBlobs; i++)
		{
			blobNetIDs [i] = reader.ReadUInt64();
			float x = reader.ReadSingle();
			float y = reader.ReadSingle();
			float z = reader.ReadSingle();
			blobPositions [i] = new Vector3(x, y, z);
			blobSizes [i] = reader.ReadSingle();
			blobsAlive [i] = reader.ReadBoolean();
		}
	}
	public override VNetMessage Clone()
	{
		VNetMessageTeleportBlobMessage clone = (VNetMessageTeleportBlobMessage)base.Clone();

		clone.numBlobs = numBlobs;
		clone.blobNetIDs = new ulong[numBlobs];
		clone.blobPositions = new Vector3[numBlobs];
		clone.blobSizes = new float[numBlobs];
		clone.blobsAlive = new bool[numBlobs];

		for (int i = 0; i < numBlobs; i++)
		{
			clone.blobNetIDs [i] = blobNetIDs [i];
			clone.blobPositions [i] = blobPositions [i];
			clone.blobSizes [i] = blobSizes [i];
			clone.blobsAlive [i] = blobsAlive [i];
		}

		return clone;
	}
	protected override VNetMessage CreateInstanceForClone()
	{
		return new VNetMessageTeleportBlobMessage();
	}
}

