using UnityEngine;
using System.Collections.Generic;

namespace FSG.MeshAnimator
{
	public class MeshAnimatorManager : MonoBehaviour
	{
		public static int AnimatorCount { get { if (Instance) return mAnimators.Count; return 0; } }
		public static MeshAnimatorManager Instance
		{
			get
			{
				if (mInstance == null)
				{
					mInstance = FindObjectOfType<MeshAnimatorManager>();
					if (mInstance == null)
					{
						mInstance = new GameObject("MeshAnimatorManager").AddComponent<MeshAnimatorManager>();
						//mInstance.gameObject.hideFlags = HideFlags.HideAndDontSave;
					}
				}
				return mInstance;
			}
		}

		private static MeshAnimatorManager mInstance = null;
		private static List<MeshAnimator> mAnimators = new List<MeshAnimator>(100);

		public static void AddAnimator(MeshAnimator animator)
		{
			if (Instance)
				mAnimators.Add(animator);
		}
		public static void RemoveAnimator(MeshAnimator animator)
		{
			mAnimators.Remove(animator);
		}
		private void Update()
		{
			float t = Time.time;
			int c = mAnimators.Count;
			for (int i = 0; i < c; i++)
			{
				MeshAnimator animator = mAnimators[i];
				if (t >= animator.nextTick)
				{
					animator.UpdateTick(t);
				}
			}
		}
	}
}