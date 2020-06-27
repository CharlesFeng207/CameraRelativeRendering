//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------
using System.Collections.Generic;

namespace FSG.MeshAnimator
{
	public static class AllocatedArray<T>
	{
		private static T defaultValue = default(T);
		private static Dictionary<int, Stack<T[]>> allocatedArrays = new Dictionary<int, Stack<T[]>>(Comparers.Int);
		private static T[] AllocateArray(int size)
		{
			return new T[size];
		}
		public static T[] Get(int size)
		{
			if (allocatedArrays.ContainsKey(size))
			{
				if (allocatedArrays[size].Count == 0)
					return AllocateArray(size);
				else
					return allocatedArrays[size].Pop();
			}
			else
			{
				return AllocateArray(size);
			}
		}
		public static void Return(T[] array, bool resetValues = true)
		{
			if (allocatedArrays.ContainsKey(array.Length) == false)
				allocatedArrays.Add(array.Length, new Stack<T[]>());
			if (resetValues)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = defaultValue;
				}
			}
			allocatedArrays[array.Length].Push(array);
		}
	}
}