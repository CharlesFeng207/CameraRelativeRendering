//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace FSG.MeshAnimator
{
	public class MecanimMeshAnimatorController : MonoBehaviour
	{
		public Animator animator;
		public MeshAnimator meshAnimator;
		public bool crossFade = false;

		private Dictionary<int, string> animHashes = new Dictionary<int, string>();
		private string cAnim = string.Empty;

		protected virtual void Awake()
		{
			if (!meshAnimator)
			{
				Debug.LogError("MecanimMeshAnimatorController.meshAnimator is null", this);
				return;
			}
			for (int i = 0; i < meshAnimator.animations.Length; i++)
			{
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6
				animHashes.Add(Animator.StringToHash(meshAnimator.animations[i].name), meshAnimator.animations[i].name);
#else
				animHashes.Add(Animator.StringToHash("Base Layer." + meshAnimator.animations[i].name),
													meshAnimator.animations[i].name);
#endif
			}
		}
		protected virtual void LateUpdate()
		{
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6
			int id = stateInfo.nameHash;
#else
			int id = stateInfo.fullPathHash;
#endif
			if (animHashes.ContainsKey(id))
			{
				if (cAnim != animHashes[id])
				{
					cAnim = animHashes[id];
					if (crossFade)
						meshAnimator.Crossfade(animHashes[id]);
					else
						meshAnimator.Play(animHashes[id]);
				}
			}
		}
	}
}