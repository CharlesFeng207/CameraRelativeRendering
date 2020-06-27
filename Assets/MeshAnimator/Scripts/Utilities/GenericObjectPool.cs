//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------
using System.Collections.Generic;

namespace FSG.MeshAnimator
{
	public static class GenericObjectPool<T>
	{
		private static Stack<T> pool = new Stack<T>();
		public static int Count
		{
			get
			{
				lock (pool)
				{
					return pool.Count;
				}
			}
		}
		public static void InitPool(int count)
		{
			for (int i = 0; i < count; i++)
			{
				T t = Get();
				Return(t);
			}
		}
		public static T Get()
		{
			if (Count > 0)
			{
				T obj;
				lock (pool)
				{
					obj = pool.Pop();
				}
				return obj;
			}
			else
			{
				return System.Activator.CreateInstance<T>();
			}

		}
		public static void Return(T obj)
		{
			lock (pool)
			{
				pool.Push(obj);
			}
		}
	}
}