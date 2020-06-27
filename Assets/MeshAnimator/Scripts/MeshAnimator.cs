//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------

#if !UNITY_WEBGL
#define THREADS_ENABLED
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace FSG.MeshAnimator
{
	[AddComponentMenu("Miscellaneous/Mesh Animator")]
	[RequireComponent(typeof(MeshFilter))]
	public class MeshAnimator : MonoBehaviour
	{
		[System.Serializable]
		public class MeshAnimatorLODLevel
		{
			public int fps;
			public float distance;
		}
		private struct CurrentCrossFade
		{
			public MeshFrameData fromFrame;
			public MeshFrameData toFrame;
			public int framesNeeded;
			public int currentFrame;
			public int generatedFrame;
			public bool isFading;
			public float endTime;
			public CrossFadeFrameData frame;

			public void Reset()
			{
				fromFrame = null;
				toFrame = null;
				isFading = false;
				endTime = 0;
				currentFrame = 0;
				generatedFrame = -1;
				framesNeeded = 0;
				ReturnFrame();
			}
			public void PopulateFrame(int length)
			{
				if (frame == null)
				{
					frame = new CrossFadeFrameData();
				}
				if (frame.positions == null)
				{
					frame.positions = AllocatedArray<Vector3>.Get(length);
				}
				if (frame.normals == null)
				{
					frame.normals = AllocatedArray<Vector3>.Get(length);
				}
			}
			public void ReturnFrame()
			{
				if (frame != null)
				{
					if (frame.positions != null)
						AllocatedArray<Vector3>.Return(frame.positions, false);
					if (frame.normals != null)
						AllocatedArray<Vector3>.Return(frame.normals, false);
					frame.positions = null;
					frame.normals = null;
				}
			}
		}
		private class CrossFadeFrameData
		{
			public Vector3[] positions;
			public Vector3[] normals;
			public Bounds bounds;
		}
		public Mesh baseMesh;
		public MeshAnimation defaultAnimation;
		public MeshAnimation[] animations;
		public float speed = 1;
		public bool updateWhenOffscreen = false;
		public bool playAutomatically = true;
		public bool resetOnEnable = true;
		public GameObject eventReciever;
		public MeshAnimation currentAnimation
		{
			get
			{
				if (currentAnimIndex >= animations.Length)
					currentAnimIndex = animations.Length - 1;
				else if (currentAnimIndex < 0)
					currentAnimIndex = 0;
				return animations[currentAnimIndex];
			}
		}
		public int FPS = 30;
		public bool skipLastLoopFrame = false;
		public bool recalculateCrossfadeNormals = false;
		[HideInInspector]
		public MeshFilter meshFilter;
		public Action<string> OnAnimationFinished;
		public Action OnFrameUpdated;
		public Action<bool> OnVisibilityChanged;
		public int currentFrame;
		public Transform LODCamera;
		public MeshAnimatorLODLevel[] LODLevels = new MeshAnimatorLODLevel[0];
		[HideInInspector]
		public float nextTick = 0;

		private Dictionary<string, int> animIndexes = new Dictionary<string, int>();
		private int currentAnimIndex;
		private bool isVisible = true;
		private float lastFrameTime;
		private bool pingPong = false;
		private bool isPaused = false;
		private float currentAnimTime;
		private Mesh crossFadeMesh;
		private Queue<string> queuedAnims;
		private CurrentCrossFade currentCrossFade;
		private int currentLodLevel = 0;
		private Transform mTransform;
		private Dictionary<string, Transform> childMap;
		private bool initialized = false;
		private int previousEventFrame = -1;

#if THREADS_ENABLED
		// static crossfade threading
		private static List<System.Threading.Thread> cfThreads = new List<System.Threading.Thread>();
		private static bool shutDownThreads = false;
		private static Queue<MeshAnimator> crossfadeAnimators = new Queue<MeshAnimator>();
		private static System.Threading.AutoResetEvent crossfadeWaitHandle = new System.Threading.AutoResetEvent(false);
#endif
		// static crossfade pooling
		private static Dictionary<Mesh, int> mMeshCount = new Dictionary<Mesh, int>();
		private static Dictionary<Mesh, Stack<Mesh>> crossFadePool = new Dictionary<Mesh, Stack<Mesh>>();

		private void Start()
		{
			if (animations.Length == 0)
			{
				Debug.LogWarning("No animations for MeshAnimator on object: " + name + ". Disabling.", this);
				this.enabled = false;
				return;
			}

			for (int i = 0; i < animations.Length; i++)
			{
				if (animations[i] == null)
					continue;
				if (animIndexes.ContainsKey(animations[i].name) == false)
					animIndexes.Add(animations[i].name, i);
				animations[i].GenerateFrames(baseMesh);
				if (animations[i].exposedTransforms != null)
				{
					for (int j = 0; j < animations[i].exposedTransforms.Length; j++)
					{
						string childName = animations[i].exposedTransforms[j];
						Transform t = transform.Find(childName);
						if (t)
						{
							if (childMap == null)
								childMap = new Dictionary<string, Transform>();
							if (childMap.ContainsKey(childName) == false)
							{
								childMap.Add(childName, t);
							}
						}
					}
				}
			}

			if (!meshFilter)
				meshFilter = GetComponent<MeshFilter>();

			if (!mMeshCount.ContainsKey(baseMesh))
				mMeshCount.Add(baseMesh, 1);
			else
				mMeshCount[baseMesh]++;

			if (playAutomatically) Play(defaultAnimation.name);
			else isPaused = true;

#if THREADS_ENABLED
			if (cfThreads.Count < MeshAnimatorManager.AnimatorCount / 15f && cfThreads.Count < 20)
			{
				shutDownThreads = false;
				var t = new System.Threading.Thread(new System.Threading.ThreadStart(GenerateThreadedCrossfade));
				t.Start();
				cfThreads.Add(t);
			}
#endif
			initialized = true;
		}
		private void OnBecameVisible()
		{
			isVisible = true;
			if (OnVisibilityChanged != null) OnVisibilityChanged(isVisible);
		}
		private void OnBecameInvisible()
		{
			isVisible = false;
			if (OnVisibilityChanged != null) OnVisibilityChanged(isVisible);
		}
		private void OnEnable()
		{
			mTransform = transform;
			if (resetOnEnable && meshFilter)
			{
				if (playAutomatically) Play(defaultAnimation.name);
				else isPaused = true;
				if (currentAnimation != null)
				{
					currentAnimation.GenerateFrameIfNeeded(baseMesh, currentFrame);
					currentAnimation.DisplayFrame(meshFilter, currentFrame, -1);
				}
			}
			MeshAnimatorManager.AddAnimator(this);
			lastFrameTime = Time.time;
		}
		private void OnDisable()
		{
			MeshAnimatorManager.RemoveAnimator(this);
			currentCrossFade.Reset();
			currentAnimIndex = -1;
			pingPong = false;
			if (queuedAnims != null)
				queuedAnims.Clear();
		}
		private void OnDestroy()
		{
			if (mMeshCount.ContainsKey(baseMesh) == false)
				return;
			mMeshCount[baseMesh]--;
			ReturnCrossfadeToPool();
			if (mMeshCount[baseMesh] <= 0)
			{
				mMeshCount.Remove(baseMesh);
				foreach (var v in MeshAnimation.generatedFrames[baseMesh])
					for (int i = 0; i < v.Value.Length; i++)
						DestroyImmediate(v.Value[i]);
				MeshAnimation.generatedFrames.Remove(baseMesh);
				for (int i = 0; i < animations.Length; i++)
					animations[i].Reset();
				if (crossFadePool.ContainsKey(baseMesh))
				{
					while (crossFadePool[baseMesh].Count > 0)
						GameObject.Destroy(crossFadePool[baseMesh].Pop());
					crossFadePool.Remove(baseMesh);
				}
			}
#if THREADS_ENABLED
			if (mMeshCount.Count == 0)
			{
				cfThreads.Clear();
				shutDownThreads = true;
				crossfadeAnimators.Clear();
			}
#endif
		}
		private void FireAnimationEvents(MeshAnimation cAnim, float totalSpeed, bool finished)
		{
			if (cAnim.events.Length > 0 && eventReciever != null && previousEventFrame != currentFrame)
			{
				if (finished)
				{
					if (totalSpeed < 0)
					{
						// fire off animation events, including skipped frames
						for (int i = previousEventFrame; i >= 0; i++)
							cAnim.FireEvents(eventReciever, i);
						previousEventFrame = 0;
					}
					else
					{
						// fire off animation events, including skipped frames
						for (int i = previousEventFrame; i <= cAnim.totalFrames; i++)
							cAnim.FireEvents(eventReciever, i);
						previousEventFrame = -1;
					}
					return;
				}
				else
				{
					if (totalSpeed < 0)
					{
						// fire off animation events, including skipped frames
						for (int i = currentFrame; i > previousEventFrame; i--)
							cAnim.FireEvents(eventReciever, i);
					}
					else
					{
						// fire off animation events, including skipped frames
						for (int i = previousEventFrame + 1; i <= currentFrame; i++)
							cAnim.FireEvents(eventReciever, i);
					}
					previousEventFrame = currentFrame;
				}
			}
		}

		private Mesh GetCrossfadeFromPool()
		{
			if (crossFadePool.ContainsKey(baseMesh) && crossFadePool[baseMesh].Count > 0)
				return crossFadePool[baseMesh].Pop();
			else
				return (Mesh)Instantiate(baseMesh);
		}
		private void ReturnCrossfadeToPool()
		{
			if (crossFadeMesh)
			{
				if (crossFadePool.ContainsKey(baseMesh) == false)
					crossFadePool.Add(baseMesh, new Stack<Mesh>());
				crossFadePool[baseMesh].Push(crossFadeMesh);
				crossFadeMesh = null;
			}
			currentCrossFade.Reset();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private void GenerateCrossfadeFrame()
		{
			if (currentCrossFade.generatedFrame == currentCrossFade.currentFrame)
			{
				return;
			}
			int vertexCount = currentCrossFade.toFrame.verts.Length;
			currentCrossFade.PopulateFrame(vertexCount);
			Vector3[] from = currentCrossFade.fromFrame.verts;
			Vector3[] to = currentCrossFade.toFrame.verts;
			// generate the frames for the crossfade
			CrossFadeFrameData frame = currentCrossFade.frame;
			Vector3 center = Vector3.zero;
			Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			float delta = currentCrossFade.currentFrame / (float)currentCrossFade.framesNeeded;
			for (int j = 0; j < frame.positions.Length; j++)
			{
				Vector3 pos = Vector3.Lerp(from[j], to[j], delta);
				if (pos.x < min.x) min.x = pos.x;
				if (pos.y < min.y) min.y = pos.y;
				if (pos.z < min.z) min.z = pos.z;
				if (pos.x > max.x) max.x = pos.x;
				if (pos.y > max.y) max.y = pos.y;
				if (pos.z > max.z) max.z = pos.z;
				center += pos;
				frame.positions[j] = pos;
			}
			center /= frame.positions.Length;
			currentCrossFade.frame = frame;
			currentCrossFade.frame.bounds = new Bounds(center, max - min);
			currentCrossFade.generatedFrame = currentCrossFade.currentFrame;
		}
		private static void EnqueueAnimatorForCrossfade(MeshAnimator animator)
		{
#if THREADS_ENABLED
			lock (crossfadeAnimators)
				crossfadeAnimators.Enqueue(animator);
			crossfadeWaitHandle.Set();
#endif
		}
		private static MeshAnimator DequeueAnimatorForCrossfade()
		{
#if THREADS_ENABLED
			lock (crossfadeAnimators)
			{
				if (crossfadeAnimators.Count == 0)
					return null;
				return crossfadeAnimators.Dequeue();
			}
#else
			return null;
#endif
		}
		private static Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float time)
		{
			Matrix4x4 ret = new Matrix4x4();
			for (int i = 0; i < 16; i++)
				ret[i] = Mathf.Lerp(from[i], to[i], time);
			return ret;
		}
#if THREADS_ENABLED
		private static void GenerateThreadedCrossfade()
		{
			// generates crossfade frames in queued multi-threaded order
			// lightens the load on the update function of the animator
			while (!shutDownThreads)
			{
				try
				{
					MeshAnimator ma = null;
					while ((ma = DequeueAnimatorForCrossfade()) != null)
					{
						if (ma.currentCrossFade.isFading)
						{
							ma.GenerateCrossfadeFrame();
						}
					}
				}
				catch { }
				crossfadeWaitHandle.WaitOne();
			}
		}
#endif
		public void UpdateTick(float time)
		{
			if (initialized == false)
				return;
			MeshAnimation cAnim = currentAnimation;
			if ((isVisible == false && updateWhenOffscreen == false) || isPaused || speed == 0 || cAnim.playbackSpeed == 0) // return if offscreen or crossfading
			{
				return;
			}
			// if the speed is below the normal playback speed, wait until the next frame can display
			float lodFPS = LODLevels.Length > currentLodLevel ? LODLevels[currentLodLevel].fps : FPS;
			float totalSpeed = Mathf.Abs(cAnim.playbackSpeed * speed);
			float tickRate = Mathf.Max(0.0001f, 1f / lodFPS / totalSpeed);
			float actualDelta = time - lastFrameTime;
			bool finished = false;

			float pingPongMult = pingPong ? -1 : 1;
			if (speed * cAnim.playbackSpeed < 0)
				currentAnimTime -= actualDelta * pingPongMult * totalSpeed;
			else
				currentAnimTime += actualDelta * pingPongMult * totalSpeed;

			if (currentAnimTime < 0)
			{
				currentAnimTime = cAnim.length;
				finished = true;
			}
			else if (currentAnimTime > cAnim.length)
			{
				if (cAnim.wrapMode == WrapMode.Loop)
					currentAnimTime = 0;
				finished = true;
			}

			nextTick = time + tickRate;
			lastFrameTime = time;

			float normalizedTime = currentAnimTime / cAnim.length;
			int previousFrame = currentFrame;
			currentFrame = Mathf.Min(Mathf.RoundToInt(normalizedTime * cAnim.totalFrames), cAnim.totalFrames - 1);

			// do WrapMode.PingPong
			if (cAnim.wrapMode == WrapMode.PingPong)
			{
				if (finished)
				{
					pingPong = !pingPong;
				}
			}

			if (finished)
			{
				bool stopUpdate = false;
				if (queuedAnims != null && queuedAnims.Count > 0)
				{
					Play(queuedAnims.Dequeue());
					stopUpdate = true;
				}
				else if (cAnim.wrapMode != WrapMode.Loop && cAnim.wrapMode != WrapMode.PingPong)
				{
					nextTick = float.MaxValue;
					stopUpdate = true;
				}
				if (OnAnimationFinished != null)
					OnAnimationFinished(cAnim.animationName);
				if (stopUpdate)
				{
					FireAnimationEvents(cAnim, totalSpeed, finished);
					return;
				}
			}

			// generate frames if needed and show the current animation frame
			cAnim.GenerateFrameIfNeeded(baseMesh, currentFrame);

			// if crossfading, lerp the vertices to the next frame
			if (currentCrossFade.isFading)
			{
				if (currentCrossFade.currentFrame >= currentCrossFade.framesNeeded)
				{
					currentFrame = 0;
					previousFrame = -1;
					currentAnimTime = 0;
					ReturnCrossfadeToPool();
				}
				else
				{
#if !THREADS_ENABLED
					GenerateCrossfadeFrame();
#endif
					if (currentCrossFade.generatedFrame >= currentCrossFade.currentFrame)
					{
						if (crossFadeMesh == null)
							crossFadeMesh = GetCrossfadeFromPool();
						crossFadeMesh.vertices = currentCrossFade.frame.positions;
						crossFadeMesh.bounds = currentCrossFade.frame.bounds;
						if (recalculateCrossfadeNormals)
							crossFadeMesh.RecalculateNormals();
						meshFilter.sharedMesh = crossFadeMesh;
						currentCrossFade.ReturnFrame();
						currentCrossFade.currentFrame++;
						if (currentCrossFade.currentFrame < currentCrossFade.framesNeeded)
						{
							EnqueueAnimatorForCrossfade(this);
						}
						// move exposed transforms
						bool exposedTransforms = childMap != null;
						bool applyRootMotion = cAnim.rootMotionMode == MeshAnimation.RootMotionMode.AppliedToTransform;
						if (exposedTransforms || applyRootMotion)
						{
							float delta = currentCrossFade.currentFrame / (float)currentCrossFade.framesNeeded;
							MeshFrameData fromFrame = currentCrossFade.fromFrame;
							MeshFrameData toFrame = currentCrossFade.toFrame;
							// move exposed transforms
							if (exposedTransforms)
							{
								for (int i = 0; i < cAnim.exposedTransforms.Length; i++)
								{
									Transform child = null;
									if (fromFrame.exposedTransforms.Length <= i || toFrame.exposedTransforms.Length <= i)
										continue;
									if (childMap != null && childMap.TryGetValue(cAnim.exposedTransforms[i], out child))
									{
										Matrix4x4 f = fromFrame.exposedTransforms[i];
										Matrix4x4 t = toFrame.exposedTransforms[i];
										Matrix4x4 c = MatrixLerp(f, t, delta);
										MatrixUtils.FromMatrix4x4(child, c);
									}
								}
							}
							// apply root motion
							if (applyRootMotion)
							{
								Vector3 pos = Vector3.Lerp(fromFrame.rootMotionPosition, toFrame.rootMotionPosition, delta);
								Quaternion rot = Quaternion.Lerp(fromFrame.rootMotionRotation, toFrame.rootMotionRotation, delta);
								transform.Translate(pos, Space.Self);
								transform.Rotate(rot.eulerAngles * Time.deltaTime, Space.Self);
							}
						}
					}
				}
			}
			if (currentCrossFade.isFading == false)
			{
				cAnim.DisplayFrame(meshFilter, currentFrame, previousFrame);
				if (currentFrame != previousFrame)
				{
					bool exposedTransforms = childMap != null;
					bool applyRootMotion = cAnim.rootMotionMode == MeshAnimation.RootMotionMode.AppliedToTransform;
					if (exposedTransforms || applyRootMotion)
					{
						MeshFrameData frame = cAnim.GetNearestFrame(currentFrame);
						// move exposed transforms
						if (exposedTransforms)
						{
							for (int i = 0; i < cAnim.exposedTransforms.Length; i++)
							{
								Transform child = null;
								if (frame.exposedTransforms.Length > i && childMap != null && childMap.TryGetValue(cAnim.exposedTransforms[i], out child))
								{
									MatrixUtils.FromMatrix4x4(child, frame.exposedTransforms[i]);
								}
							}
						}
						// apply root motion
						if (applyRootMotion)
						{
							if (previousFrame > currentFrame)
							{
								// animation looped around, apply motion for skipped frames at the end of the animation
								for (int i = previousFrame + 1; i < cAnim.frames.Length; i++)
								{
									MeshFrameData rootFrame = cAnim.GetNearestFrame(i);
									transform.Translate(rootFrame.rootMotionPosition, Space.Self);
									transform.Rotate(rootFrame.rootMotionRotation.eulerAngles * Time.deltaTime, Space.Self);
								}
								// now apply motion from first frame to current frame
								for (int i = 0; i <= currentFrame; i++)
								{
									MeshFrameData rootFrame = cAnim.GetNearestFrame(i);
									transform.Translate(rootFrame.rootMotionPosition, Space.Self);
									transform.Rotate(rootFrame.rootMotionRotation.eulerAngles * Time.deltaTime, Space.Self);
								}
							}
							else
							{
								for (int i = previousFrame + 1; i <= currentFrame; i++)
								{
									MeshFrameData rootFrame = cAnim.GetNearestFrame(i);
									transform.Translate(rootFrame.rootMotionPosition, Space.Self);
									transform.Rotate(rootFrame.rootMotionRotation.eulerAngles * Time.deltaTime, Space.Self);
								}
							}

						}
					}
				}
			}
			if (OnFrameUpdated != null)
				OnFrameUpdated();

			FireAnimationEvents(cAnim, totalSpeed, finished);

			// update the lod level if needed
			if (LODLevels.Length > 0 && (LODCamera != null || Camera.main))
			{
				if (LODCamera == null)
					LODCamera = Camera.main.transform;
				float dis = (LODCamera.position - mTransform.position).sqrMagnitude;
				int lodLevel = 0;
				for (int i = 0; i < LODLevels.Length; i++)
				{
					if (dis > LODLevels[i].distance * LODLevels[i].distance)
						lodLevel = i;
				}
				if (currentLodLevel != lodLevel)
				{
					currentLodLevel = lodLevel;
				}
			}
		}
		public void Crossfade(int index)
		{
			Crossfade(index, 0.1f);
		}
		public void Crossfade(string anim)
		{
			Crossfade(anim, 0.1f);
		}
		/// <summary>
		/// Crossfade an animation by index
		/// </summary>
		/// <param name="index">Index of the animation</param>
		/// <param name="speed">Duration the crossfade will take</param>
		public void Crossfade(int index, float speed)
		{
			currentCrossFade.Reset();
			currentCrossFade.framesNeeded = (int)(speed * FPS);
			currentCrossFade.isFading = true;
			currentCrossFade.endTime = Time.time + speed;
			if (currentAnimation == null)
			{
				currentCrossFade.fromFrame = defaultAnimation.GetNearestFrame(0);
			}
			else
			{
				currentCrossFade.fromFrame = currentAnimation.GetNearestFrame(currentFrame);
			}
			Play(index);
			currentCrossFade.toFrame = currentAnimation.GetNearestFrame(0);
			EnqueueAnimatorForCrossfade(this);
		}
		/// <summary>
		/// Crossfade an animation by name
		/// </summary>
		/// <param name="anim">Name of the animation</param>
		/// <param name="speed">Duration the crossfade will take</param>
		public void Crossfade(string anim, float speed)
		{
			int animIndex = -1;
			if (animIndexes.TryGetValue(anim, out animIndex))
				Crossfade(animIndex, speed);
		}
		/// <summary>
		/// Play the default animation, or resume playing a paused animator
		/// </summary>
		public void Play()
		{
			isPaused = false;
		}
		/// <summary>
		/// Play an animation by name
		/// </summary>
		/// <param name="anim">Name of the animation</param>
		public void Play(string anim)
		{
			int animIndex = -1;
			if (animIndexes.TryGetValue(anim, out animIndex))
				Play(animIndex);
		}
		/// <summary>
		/// Play an animation by index
		/// </summary>
		/// <param name="index">Index of the animation</param>
		public void Play(int index)
		{
			if (animations.Length <= index || currentAnimIndex == index)
				return;
			if (queuedAnims != null)
				queuedAnims.Clear();
			currentAnimIndex = index;
			currentFrame = 0;
			currentAnimTime = 0;
			previousEventFrame = -1;
			pingPong = false;
			isPaused = false;
			nextTick = Time.time;
		}
		/// <summary>
		/// Play a random animation
		/// </summary>
		/// <param name="anim">Animation names</param>
		public void PlayRandom(params string[] anim)
		{
			int rand = UnityEngine.Random.Range(0, anim.Length);
			string randomAnim = anim[rand];
			if (animIndexes.ContainsKey(randomAnim) == false)
				return;
			Play(animIndexes[randomAnim]);
		}
		/// <summary>
		/// Play an animation after the previous one has finished
		/// </summary>
		/// <param name="anim">Animation name</param>
		public void PlayQueued(string anim)
		{
			if (queuedAnims == null)
				queuedAnims = new Queue<string>();
			queuedAnims.Enqueue(anim);
		}
		/// <summary>
		/// Pause an animator, disabling the component also has the same effect
		/// </summary>
		public void Pause()
		{
			isPaused = true;
		}
		/// <summary>
		/// Restart the current animation from the beginning
		/// </summary>
		public void RestartAnim()
		{
			currentFrame = 0;
		}
		/// <summary>
		/// Sets the current time of the playing animation
		/// </summary>
		/// <param name="time">Time of the animation to play. Min: 0, Max: Length of animation</param>
		public void SetTime(float time, bool instantUpdate = false)
		{
			var cAnim = currentAnimation;
			if (cAnim == null)
				return;
			time = Mathf.Clamp(time, 0, cAnim.length);
			currentAnimTime = time;
			if (instantUpdate)
				UpdateTick(Time.time);
		}
		/// <summary>
		/// Set the current time of the animation, normalized
		/// </summary>
		/// <param name="time">Time of the animation to start playback (0-1)</param>
		public void SetTimeNormalized(float time, bool instantUpdate = false)
		{
			var cAnim = currentAnimation;
			if (cAnim == null)
				return;
			time = Mathf.Clamp01(time);
			currentAnimTime = time * cAnim.length;
			if (instantUpdate)
				UpdateTick(Time.time);
		}
		/// <summary>
		/// Get the MeshAnimation by name
		/// </summary>
		/// <param name="clipname">Name of the animation</param>
		/// <returns>MeshAnimation class</returns>
		public MeshAnimation GetClip(string clipname)
		{
			if (animIndexes.ContainsKey(clipname))
				return animations[animIndexes[clipname]];
			return null;
		}
	}
}