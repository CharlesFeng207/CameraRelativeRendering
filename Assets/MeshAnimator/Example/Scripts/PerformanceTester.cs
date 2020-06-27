using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FSG.MeshAnimator
{
	public class PerformanceTester : MonoBehaviour
	{
		public GameObject[] animObjects;
		public string[] options;
		public float cameraSpeed = 20;
		public Vector3 spawnOffset = new Vector3(-10, 0, 5);

		private int[] spawnedMeshes = new int[3];
		private List<GameObject> meshes = new List<GameObject>();
		private string fps;
		private int previousFrame = 0;
		private Vector3 offset = new Vector3(-10, 0, 0);
		private bool crossFade = false;

		void Start()
		{
			InvokeRepeating("UpdateFPS", 0.0001f, 1f);
		}
		void UpdateFPS()
		{
			fps = ((Time.frameCount - previousFrame) / 1f).ToString("00.00");
			previousFrame = Time.frameCount;
		}
		void Update()
		{
			if (Input.GetKey(KeyCode.W))
				transform.position += Vector3.forward * Time.deltaTime * cameraSpeed;
			if (Input.GetKey(KeyCode.A))
				transform.position -= Vector3.right * Time.deltaTime * cameraSpeed;
			if (Input.GetKey(KeyCode.S))
				transform.position -= Vector3.forward * Time.deltaTime * cameraSpeed;
			if (Input.GetKey(KeyCode.D))
				transform.position += Vector3.right * Time.deltaTime * cameraSpeed;
		}
		void OnDisable()
		{
			for (int i = 0; i < options.Length; i++)
			{
				if (animObjects[i].GetComponent<MecanimMeshAnimatorController>())
					animObjects[i].GetComponent<MecanimMeshAnimatorController>().crossFade = false;
				if (animObjects[i].GetComponent<AnimatorStateMachine>())
					animObjects[i].GetComponent<AnimatorStateMachine>().crossFade = false;
			}
		}
		void OnGUI()
		{
			GUI.skin.label.richText = true;
			GUILayout.BeginArea(new Rect(Screen.height * 0.1f, Screen.width * 0.1f, Screen.width * 0.3f, Screen.height));
			{
				GUILayout.Label("<size=20><b>Test Scenes</b></size>");
				switch (Application.loadedLevel)
				{
					case 0:
						{
							GUILayout.Label("With large amount's of SkinnedMeshRenderer's in the scene. Unity's built-in performance doesn't come close to Mesh Animator's");
							break;
						}
					case 1:
						{
							GUILayout.Label("SkinnedMeshRenderer = Worst Performance");
							GUILayout.Label("Mesh Animator + Mecanim Controller = Better Performance");
							GUILayout.Label("Mesh Animator + Event Callbacks = Best Performance");
							break;
						}
					case 2:
						{
							GUILayout.Label("Animated objects can be combined into a single object and draw call, greatly improving preformance.");
							break;
						}
				}
				GUI.color = Application.loadedLevel == 0 ? Color.green : Color.white;
				if (GUILayout.Button("Stadium Test"))
					Application.LoadLevel(0);
				GUI.color = Application.loadedLevel == 1 ? Color.green : Color.white;
				if (GUILayout.Button("Skinned Mesh Test"))
					Application.LoadLevel(1);
				GUI.color = Application.loadedLevel == 2 ? Color.green : Color.white;
				if (GUILayout.Button("Mesh Filter Test"))
					Application.LoadLevel(2);
				GUI.color = Color.white;
				GUILayout.Label("<size=20><b>FPS: " + fps + "</b></size>");
				GUILayout.Label("WASD to move the camera");
				bool toggled = crossFade;
				crossFade = GUILayout.Toggle(crossFade, "Crossfade", GUILayout.Height(Screen.height * 0.05f));
				if (toggled != crossFade)
				{
					for (int i = 0; i < options.Length; i++)
					{
						if (animObjects[i].GetComponent<MecanimMeshAnimatorController>())
							animObjects[i].GetComponent<MecanimMeshAnimatorController>().crossFade = crossFade;
						if (animObjects[i].GetComponent<AnimatorStateMachine>())
							animObjects[i].GetComponent<AnimatorStateMachine>().crossFade = crossFade;
					}
					for (int i = 0; i < meshes.Count; i++)
					{
						if (meshes[i].GetComponent<MecanimMeshAnimatorController>())
							meshes[i].GetComponent<MecanimMeshAnimatorController>().crossFade = crossFade;
						if (meshes[i].GetComponent<AnimatorStateMachine>())
							meshes[i].GetComponent<AnimatorStateMachine>().crossFade = crossFade;
					}
				}
				for (int i = 0; i < options.Length; i++)
				{
					if (GUILayout.RepeatButton(options[i] + " Spawned: " + spawnedMeshes[i], GUILayout.Height(Screen.height * 0.05f)))
					{
						meshes.Add((GameObject)GameObject.Instantiate(animObjects[i], offset, Quaternion.Euler(0, 180, 0)));
						spawnedMeshes[i]++;
						offset.x += -spawnOffset.x / 10f;
						if (meshes.Count % 20 == 0)
						{
							offset.x = spawnOffset.x;
							offset.z += spawnOffset.z;
						}
					}
				}
				if (GUILayout.Button("Clear", GUILayout.Height(Screen.height * 0.05f)))
				{
					foreach (var m in meshes)
						GameObject.Destroy(m);
					meshes.Clear();
					spawnedMeshes = new int[3];
					offset = new Vector3(-10, 0, 0);
				}
			}
			GUILayout.EndArea();
		}
	}
}