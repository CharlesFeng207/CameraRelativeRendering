using UnityEngine;
using System.Collections;

namespace FSG.MeshAnimator
{
	public class AnimatorStateMachine : MonoBehaviour
	{
		public MeshAnimator meshAnimator;
		public bool crossFade = false;
		void Start()
		{
			meshAnimator.Play();
			meshAnimator.OnAnimationFinished += OnAnimationFinished;
		}
		void OnAnimationFinished(string anim)
		{
			int newAnim = 0;
			switch (anim)
			{
				case "idle":
					newAnim = 2;
					break;
				case "run_forward":
					newAnim = 1;
					break;
				case "run_backward":
					newAnim = 3;
					break;
				case "run_left":
					newAnim = 4;
					break;
				case "run_right":
					newAnim = 0;
					break;
			}
			if (crossFade)
				meshAnimator.Crossfade(newAnim);
			else
				meshAnimator.Play(newAnim);
		}
	}
}