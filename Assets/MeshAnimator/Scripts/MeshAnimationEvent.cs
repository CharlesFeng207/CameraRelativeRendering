//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------

using UnityEngine;

namespace FSG.MeshAnimator
{
	[System.Serializable]
	public class MeshAnimationEvent
	{
		public enum Mode : byte { Data, String, Float };
		public string methodName;
		public int frame;
		public Mode eventType;
		public string stringValue;
		public float floatValue;
		public Object data;

		public void FireEvent(GameObject eventReciever)
		{
			if (eventReciever)
			{
				if (eventType == Mode.Data)
					eventReciever.SendMessage(methodName, data, SendMessageOptions.DontRequireReceiver);
				else if (eventType == Mode.Float)
					eventReciever.SendMessage(methodName, floatValue, SendMessageOptions.DontRequireReceiver);
				else if (eventType == Mode.String)
					eventReciever.SendMessage(methodName, stringValue, SendMessageOptions.DontRequireReceiver);
			}
		}
	}
}