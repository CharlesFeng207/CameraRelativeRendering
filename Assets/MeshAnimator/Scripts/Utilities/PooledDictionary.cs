//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------

using System.Collections.Generic;

namespace FSG.MeshAnimator
{
	public class PooledDictionary<T, T2> : Dictionary<T, T2>
	{
		private static Stack<PooledDictionary<T, T2>> stack = new Stack<PooledDictionary<T, T2>>();
		private static uint checkedOut = 0;
		private static uint returned = 0;

		public bool recycleable { get; set; }

		public static void Init(int initialPoolSize = 10)
		{
			for (int i = 0; i < initialPoolSize; i++)
				stack.Push(new PooledDictionary<T, T2>());
		}
		public static PooledDictionary<T, T2> Get(int capacity, IEqualityComparer<T> comparer = null)
		{
			lock (stack)
			{
				if (stack.Count > 0)
				{
					checkedOut++;
					var list = stack.Pop();
					list.recycleable = true;
					return list;
				}
			}
			checkedOut++;
			if (comparer != null)
				return new PooledDictionary<T, T2>(capacity, comparer) { recycleable = true };
			else
				return new PooledDictionary<T, T2>(capacity) { recycleable = true };
		}
		public static PooledDictionary<T, T2> Get()
		{
			return Get(0, null);
		}
		public static PooledDictionary<T, T2> Get(IEqualityComparer<T> comparer)
		{
			return Get(0, comparer);
		}

		public PooledDictionary() : base() { }
		public PooledDictionary(int capacity) : base(capacity) { }
		public PooledDictionary(int capacity, IEqualityComparer<T> comparer) : base(capacity, comparer) { }
		public void ReturnToPool(bool force = false)
		{
			if (recycleable == false && !force)
				return;
			recycleable = true;
			returned++;
			Clear();
			lock (stack)
			{
				stack.Push(this);
			}
		}
	}
}