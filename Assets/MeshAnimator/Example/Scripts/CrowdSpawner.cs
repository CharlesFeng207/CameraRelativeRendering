using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FSG.MeshAnimator
{
	public class CrowdSpawner : MonoBehaviour
	{
		public GameObject[] options;
		public string[] optionsDesc;
		public int sizeOfCrowd = 1000;
		public int selectedOption = 0;
		public int maxSize = 5000;

		private string fps;
		private int previousFrame = 0;
		private List<GameObject> spawnedObjects = new List<GameObject>();

		void Start()
		{
			SpawnCrowd();
			InvokeRepeating("UpdateFPS", 0.0001f, 1f);
		}
		void UpdateFPS()
		{
			fps = ((Time.frameCount - previousFrame) / 1f).ToString("00.00");
			previousFrame = Time.frameCount;
		}
		void SpawnCrowd()
		{
			foreach (var obj in spawnedObjects)
				if (obj) Destroy(obj);

			spawnedObjects.Clear();
			List<int> occupiedSpaces = new List<int>();
			for (int i = 0; i < sizeOfCrowd; i++)
			{
				if (i > transform.childCount)
					occupiedSpaces.Clear();
				int space = Random.Range(0, transform.childCount);
				while (occupiedSpaces.Contains(space))
					space = Random.Range(0, transform.childCount);
				var g = (GameObject)GameObject.Instantiate(options[selectedOption]);
				g.transform.position = transform.GetChild(space).position + new Vector3(Random.value, 0, Random.value) * 2;
				g.transform.LookAt(transform, Vector3.up);
				if (g.GetComponent<Animator>())
				{
					g.GetComponent<Animator>().speed = Random.Range(0.9f, 1.1f);
					g.GetComponent<Animator>().SetInteger("Anim", Random.Range(0, 6));
				}
				else if (g.GetComponent<MeshAnimator>())
				{
					MeshAnimator ma = g.GetComponent<MeshAnimator>();
					ma.defaultAnimation = ma.animations[Random.Range(0, ma.animations.Length)];
					ma.speed = Random.Range(0.9f, 1.1f);
				}
				spawnedObjects.Add(g);
			}
		}
		void OnGUI()
		{
			GUI.skin.label.richText = true;
			GUILayout.BeginArea(new Rect(Screen.height * 0.1f, Screen.width * 0.1f, Screen.width * 0.3f, Screen.height));
			{
				if (Application.loadedLevelName != "PromoScene")
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
					GUI.color = selectedOption == 0 ? Color.green : Color.white;
					if (GUILayout.Button("Skinned Mesh Crowd"))
					{
						selectedOption = 0;
						SpawnCrowd();
					}
					GUI.color = selectedOption == 1 ? Color.green : Color.white;
					if (GUILayout.Button("Mesh Animator Crowd"))
					{
						selectedOption = 1;
						SpawnCrowd();
					}
					GUI.color = Color.white;
					int size = sizeOfCrowd;
					GUILayout.Label("Crowd Size: " + sizeOfCrowd);
					sizeOfCrowd = (int)GUILayout.HorizontalSlider(sizeOfCrowd, 1, maxSize);
					if (size != sizeOfCrowd)
					{
						CancelInvoke("SpawnCrowd");
						Invoke("SpawnCrowd", 1);
					}
				}
				else
				{
					GUILayout.Label("<color=black><size=19><b>FPS: " + fps + "</b></size></color>");
				}
			}
			GUILayout.EndArea();
		}
	}
}