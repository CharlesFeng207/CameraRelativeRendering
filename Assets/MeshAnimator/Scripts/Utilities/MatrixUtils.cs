using UnityEngine;

namespace FSG.MeshAnimator
{
	public static class MatrixUtils
	{
		public static void FromMatrix4x4(Transform transform, Matrix4x4 matrix)
		{
			transform.localPosition = GetPosition(matrix);
			transform.localRotation = GetRotation(matrix);
			transform.localScale = GetScale(matrix);
		}
		public static Quaternion GetRotation(Matrix4x4 matrix)
		{
			var f = matrix.GetColumn(2);
			if (f == Vector4.zero)
				return Quaternion.identity;
			return Quaternion.LookRotation(f, matrix.GetColumn(1));
		}
		public static Vector3 GetPosition(Matrix4x4 matrix)
		{
			return matrix.GetColumn(3);
		}
		public static Vector3 GetScale(Matrix4x4 m)
		{
			return new Vector3(m.GetColumn(0).magnitude, m.GetColumn(1).magnitude, m.GetColumn(2).magnitude);
		}
	}
}