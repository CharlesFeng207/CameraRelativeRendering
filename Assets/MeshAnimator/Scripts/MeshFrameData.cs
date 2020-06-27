//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

namespace FSG.MeshAnimator
{
	[System.Serializable]
	public class MeshFrameData
	{
		public Vector3[] verts { get { return decompressed; } }
		[System.NonSerialized]
		private Vector3[] decompressed = null;
		[System.NonSerialized]
		public Matrix4x4[] exposedTransforms;
		[System.NonSerialized]
		public Vector3 rootMotionPosition;
		[System.NonSerialized]
		public Quaternion rootMotionRotation;
		public void SetVerts(Vector3[] v)
		{
			decompressed = v;
		}
		public override bool Equals(object obj)
		{
			if (obj is MeshFrameData)
			{
				MeshFrameData other = (MeshFrameData)obj;
				if (other.verts.Length != verts.Length)
					return false;
				for (int i = 0; i < other.verts.Length; i++)
				{
					if (verts[i] != other.verts[i])
						return false;
				}
				return true;
			}
			return base.Equals(obj);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
	[System.Serializable]
	public class DeltaCompressedFrameData
	{
		public static float compressionAccuracy = 1000;

		public float accuracy = 1000;
		public int sizeOffset = 1;
		public int vertLength;
		public int exposedLength;

		[HideInInspector]
		public short[] positionsUShort;
		[HideInInspector]
		public int[] positions;
		[HideInInspector]
		public int[] frameIndexes;
		[HideInInspector]
		public Matrix4x4[] exposedTransforms;
		[HideInInspector]
		public Vector3[] rootMotionPositions;
		[HideInInspector]
		public Quaternion[] rootMotionRotations;

		public static implicit operator MeshFrameData[] (DeltaCompressedFrameData s)
		{
			MeshFrameData[] frames = new MeshFrameData[s.frameIndexes.Length / s.vertLength];
			bool usingShortCompression = s.positionsUShort != null && s.positionsUShort.Length > 0;
			for (int i = 0; i < frames.Length; i++)
			{
				Vector3[] verts = new Vector3[s.vertLength];
				for (int j = 0; j < verts.Length; j++)
				{
					Vector3 v = Vector3.zero;
					int index = s.frameIndexes[i * s.vertLength + j] * 3;
					int index0 = index + 0;
					int index1 = index + 1;
					int index2 = index + 2;

					if (usingShortCompression)
					{
						v.x = s.positionsUShort[index0] / s.accuracy;
						v.y = s.positionsUShort[index1] / s.accuracy;
						v.z = s.positionsUShort[index2] / s.accuracy;
					}
					else
					{
						v.x = s.positions[index0] / s.accuracy;
						v.y = s.positions[index1] / s.accuracy;
						v.z = s.positions[index2] / s.accuracy;
					}
					v.x -= s.sizeOffset;
					v.y -= s.sizeOffset;
					v.z -= s.sizeOffset;
					verts[j] = v;
				}
				frames[i] = new MeshFrameData();
				frames[i].exposedTransforms = new Matrix4x4[s.exposedLength];
				for (int j = 0; j < s.exposedLength; j++)
				{
					frames[i].exposedTransforms[j] = s.exposedTransforms[j * frames.Length + i];
				}
				frames[i].SetVerts(verts);
				if (s.rootMotionPositions != null && s.rootMotionPositions.Length > i)
					frames[i].rootMotionPosition = s.rootMotionPositions[i];
				if (s.rootMotionRotations != null && s.rootMotionRotations.Length > i)
					frames[i].rootMotionRotation = s.rootMotionRotations[i];
			}
			return frames;
		}
		public static implicit operator DeltaCompressedFrameData(MeshFrameData[] frames)
		{
			if (frames.Length == 0)
				return new DeltaCompressedFrameData();
			bool hasRootMotion = false;
			for (int i = 0; i < frames.Length; i++)
			{
				if (frames[i].rootMotionPosition.x != 0 ||
					frames[i].rootMotionPosition.y != 0 ||
					frames[i].rootMotionPosition.z != 0 ||
					frames[i].rootMotionRotation.x != 0 ||
					frames[i].rootMotionRotation.y != 0 ||
					frames[i].rootMotionRotation.z != 0 ||
					frames[i].rootMotionRotation.w != 0)
					hasRootMotion = true;
			}
			DeltaCompressedFrameData output = new DeltaCompressedFrameData()
			{
				vertLength = frames[0].verts.Length,
				frameIndexes = new int[frames[0].verts.Length * frames.Length],
				accuracy = compressionAccuracy,
				exposedLength = frames[0].exposedTransforms.Length,
				exposedTransforms = new Matrix4x4[frames.Length * frames[0].exposedTransforms.Length],
				rootMotionPositions = hasRootMotion ? new Vector3[frames.Length] : null,
				rootMotionRotations = hasRootMotion ? new Quaternion[frames.Length] : null
			};
			List<Vector3> allPositions = new List<Vector3>();
			Dictionary<Vector3, int> indexRemaps = new Dictionary<Vector3, int>();
			int sizeOffset = 1;
			for (int i = 0; i < frames.Length; i++)
			{
				for (int j = 0; j < frames[i].verts.Length; j++)
				{
					if (indexRemaps.ContainsKey(frames[i].verts[j]) == false)
					{
						indexRemaps.Add(frames[i].verts[j], allPositions.Count);
						allPositions.Add(frames[i].verts[j]);
						// credit to user jbooth for finding this bug!
						while (Mathf.Abs(frames[i].verts[j].x) > sizeOffset)
							sizeOffset *= 10;
						while (Mathf.Abs(frames[i].verts[j].y) > sizeOffset)
							sizeOffset *= 10;
						while (Mathf.Abs(frames[i].verts[j].z) > sizeOffset)
							sizeOffset *= 10;
					}
					output.frameIndexes[i * output.vertLength + j] = indexRemaps[frames[i].verts[j]];
				}
				for (int j = 0; j < frames[i].exposedTransforms.Length; j++)
				{
					output.exposedTransforms[frames.Length * j + i] = frames[i].exposedTransforms[j];
				}
				if (output.rootMotionPositions != null)
					output.rootMotionPositions[i] = frames[i].rootMotionPosition;
				if (output.rootMotionRotations != null)
					output.rootMotionRotations[i] = frames[i].rootMotionRotation;
			}
			output.sizeOffset = sizeOffset;
			bool canUseShort = true;
			output.positions = new int[allPositions.Count * 3];
			for (int i = 0; i < allPositions.Count; i++)
			{
				output.positions[i * 3 + 0] = (int)((allPositions[i].x + output.sizeOffset) * output.accuracy);
				output.positions[i * 3 + 1] = (int)((allPositions[i].y + output.sizeOffset) * output.accuracy);
				output.positions[i * 3 + 2] = (int)((allPositions[i].z + output.sizeOffset) * output.accuracy);
				if (canUseShort)
				{
					if (output.positions[i * 3 + 0] > ushort.MaxValue)
						canUseShort = false;
					else if (output.positions[i * 3 + 1] > ushort.MaxValue)
						canUseShort = false;
					else if (output.positions[i * 3 + 2] > ushort.MaxValue)
						canUseShort = false;
				}
			}
			if (canUseShort)
			{
				output.positionsUShort = new short[output.positions.Length];
				for (int i = 0; i < output.positions.Length; i++)
					output.positionsUShort[i] = (short)output.positions[i];
				output.positions = null;
			}
			return output;
		}
	}
}