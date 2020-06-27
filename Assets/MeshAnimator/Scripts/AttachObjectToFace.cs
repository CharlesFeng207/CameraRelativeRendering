//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------

using UnityEngine;

namespace FSG.MeshAnimator
{
	[ExecuteInEditMode]
	public class AttachObjectToFace : MonoBehaviour
	{
		public MeshAnimator meshAnimator;
		public int faceIndex;
		public Vector3 offset;
		public Vector3 rotationOffset;

		public bool drawFaceDebugInfo = false;
		public Color debugColor = Color.black;

		private Transform mTransform;
		private Transform mMeshAnimatorTransform;
		private int[] triangles;
		private Vector3[] vertices;

		void Awake()
		{
			mTransform = transform;
			if (meshAnimator)
			{
				mMeshAnimatorTransform = meshAnimator.transform;
				triangles = meshAnimator.GetComponent<MeshFilter>().sharedMesh.triangles;
				vertices = meshAnimator.GetComponent<MeshFilter>().sharedMesh.vertices;
			}
		}
		void LateUpdate()
		{
			if (meshAnimator)
			{
				if (!mMeshAnimatorTransform)
					mMeshAnimatorTransform = meshAnimator.transform;
				if (triangles.Length == 0)
				{
					triangles = meshAnimator.GetComponent<MeshFilter>().sharedMesh.triangles;
					vertices = meshAnimator.GetComponent<MeshFilter>().sharedMesh.vertices;
				}

				faceIndex = Mathf.Clamp(faceIndex, 0, triangles.Length);
				Vector3 v1, v2, v3;
				if (Application.isPlaying == false || !meshAnimator || !meshAnimator.currentAnimation)
				{
					v1 = vertices[triangles[faceIndex + 0]];
					v2 = vertices[triangles[faceIndex + 1]];
					v3 = vertices[triangles[faceIndex + 2]];
				}
				else
				{
					v1 = meshAnimator.currentAnimation.GetFrame(meshAnimator.currentFrame)[triangles[faceIndex + 0]];
					v2 = meshAnimator.currentAnimation.GetFrame(meshAnimator.currentFrame)[triangles[faceIndex + 1]];
					v3 = meshAnimator.currentAnimation.GetFrame(meshAnimator.currentFrame)[triangles[faceIndex + 2]];
				}
				Vector3 faceCenter = Vector3.zero;

				faceCenter += v1;
				faceCenter += v2;
				faceCenter += v3;
				faceCenter /= 3;

				Vector3 vec1 = v1 - v2;
				Vector3 vec2 = v1 - v3;
				Vector3 n = Vector3.Cross(vec1, vec2);
				n.Normalize();
				Quaternion rot = Quaternion.identity;
				if (n != Vector3.zero)
					rot = Quaternion.LookRotation(n);
				mTransform.position = mMeshAnimatorTransform.TransformPoint(faceCenter) + (rot * offset);
				mTransform.rotation = rot;
				mTransform.Rotate(rotationOffset);
			}
		}
#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			if (drawFaceDebugInfo && meshAnimator)
			{
				GUIStyle style = new GUIStyle(GUI.skin.label);
				style.fontStyle = FontStyle.Bold;
				style.normal.textColor = debugColor;
				int[] tris = meshAnimator.GetComponent<MeshFilter>().sharedMesh.triangles;
				Vector3[] verts = meshAnimator.GetComponent<MeshFilter>().sharedMesh.vertices;
				for (int i = 0; i < tris.Length; i += 3)
				{
					Vector3 v1 = meshAnimator.transform.TransformPoint(verts[tris[i + 0]]);
					Vector3 v2 = meshAnimator.transform.TransformPoint(verts[tris[i + 1]]);
					Vector3 v3 = meshAnimator.transform.TransformPoint(verts[tris[i + 2]]);

					Vector3 faceCenter = Vector3.zero;

					faceCenter += v1;
					faceCenter += v2;
					faceCenter += v3;
					faceCenter /= 3;

					//UnityEditor.Handles.Label(faceCenter, i.ToString(), style);
					if (i == faceIndex)
					{
						Gizmos.color = debugColor;
						Gizmos.DrawLine(v1, v2);
						Gizmos.DrawLine(v2, v3);
						Gizmos.DrawLine(v1, v3);
					}
				}
			}
		}
#endif
	}
}