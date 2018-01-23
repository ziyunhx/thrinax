using System;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	[Serializable]
	public class Node : IComparable<Node>
	{
		internal int _index;

		internal double _value;

		public int Index
		{
			get
			{
				return this._index;
			}
			set
			{
				this._index = value;
			}
		}

		public double Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this._value = value;
			}
		}

		public Node()
		{
		}

		public Node(int index, double value)
		{
			this._index = index;
			this._value = value;
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}", this._index, this._value);
		}

		public int CompareTo(Node other)
		{
			return this._index.CompareTo(other._index);
		}
	}
}
