using System;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	public class RankPair : IComparable<RankPair>
	{
		private double _score;

		private double _label;

		public double Score
		{
			get
			{
				return this._score;
			}
		}

		public double Label
		{
			get
			{
				return this._label;
			}
		}

		public RankPair(double score, double label)
		{
			this._score = score;
			this._label = label;
		}

		public int CompareTo(RankPair other)
		{
			return other.Score.CompareTo(this.Score);
		}

		public override string ToString()
		{
			return string.Format("{0}:{1}", this.Score, this.Label);
		}
	}
}
