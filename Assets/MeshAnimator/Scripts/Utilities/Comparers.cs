//----------------------------------------------
// Mesh Animator
// Copyright © 2015-2017 Jacob Schieck
// http://www.jacobschieck.com
//----------------------------------------------
using System.Collections.Generic;
using System;

namespace FSG.MeshAnimator
{
	public static class Comparers
	{
		[Serializable]
		public class StringComparer : IEqualityComparer<string>
		{
			bool IEqualityComparer<string>.Equals(string x, string y) { return x == y; }
			int IEqualityComparer<string>.GetHashCode(string obj) { return obj.GetHashCode(); }
		}
		[Serializable]
		public class FloatComparer : IEqualityComparer<float>
		{
			bool IEqualityComparer<float>.Equals(float x, float y) { return x == y; }
			int IEqualityComparer<float>.GetHashCode(float obj) { return obj.GetHashCode(); }
		}
		[Serializable]
		public class IntComparer : IEqualityComparer<int>
		{
			bool IEqualityComparer<int>.Equals(int x, int y) { return x == y; }
			int IEqualityComparer<int>.GetHashCode(int obj) { return obj.GetHashCode(); }
		}
		[Serializable]
		public class ByteComparer : IEqualityComparer<byte>
		{
			bool IEqualityComparer<byte>.Equals(byte x, byte y) { return x == y; }
			int IEqualityComparer<byte>.GetHashCode(byte obj) { return obj.GetHashCode(); }
		}
		[Serializable]
		public class BoolComparer : IEqualityComparer<bool>
		{
			bool IEqualityComparer<bool>.Equals(bool x, bool y) { return x == y; }
			int IEqualityComparer<bool>.GetHashCode(bool obj) { return obj.GetHashCode(); }
		}
		public static readonly StringComparer String = new StringComparer();
		public static readonly FloatComparer Float = new FloatComparer();
		public static readonly IntComparer Int = new IntComparer();
		public static readonly ByteComparer Byte = new ByteComparer();
		public static readonly BoolComparer Bool = new BoolComparer();

		public static IEqualityComparer<T> Create<T>(Func<T, T, bool> equals, Func<T, int> hash = null)
		{
			return new FuncEqualityComparer<T>(equals, hash ?? (t => 1));
		}

		[Serializable]
		private class FuncEqualityComparer<T> : EqualityComparer<T>
		{
			private readonly Func<T, T, bool> equals;
			private readonly Func<T, int> hash;

			public FuncEqualityComparer(Func<T, T, bool> equals, Func<T, int> hash)
			{
				this.equals = equals;
				this.hash = hash;
			}
			public override bool Equals(T a, T b)
			{
				if (this.equals == null)
				{
					return a.Equals(b);
				}
				return this.equals(a, b);
				//return a == null ? b == null : b != null && this.equals(a, b);
			}
			public override int GetHashCode(T obj)
			{
				if (this.hash == null)
				{
					return obj.GetHashCode();
				}
				return this.hash(obj);
				//return obj == null ? 0 : this.hash(obj);
			}
		}
	}
}