using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	[Serializable]
	public class Problem
	{
		private int _count;

		private double[] _Y;

		private Node[][] _X;

		private int _maxIndex;

		public int Count
		{
			get
			{
				return this._count;
			}
			set
			{
				this._count = value;
			}
		}

		public double[] Y
		{
			get
			{
				return this._Y;
			}
			set
			{
				this._Y = value;
			}
		}

		public Node[][] X
		{
			get
			{
				return this._X;
			}
			set
			{
				this._X = value;
			}
		}

		public int MaxIndex
		{
			get
			{
				return this._maxIndex;
			}
			set
			{
				this._maxIndex = value;
			}
		}

		public Problem(int count, double[] y, Node[][] x, int maxIndex)
		{
			this._count = count;
			this._Y = y;
			this._X = x;
			this._maxIndex = maxIndex;
		}

		public Problem()
		{
		}

		public static Problem Read(Stream stream)
		{
			TemporaryCulture.Start();
			StreamReader streamReader = new StreamReader(stream);
			List<double> list = new List<double>();
			List<Node[]> list2 = new List<Node[]>();
			int num = 0;
			while (streamReader.Peek() > -1)
			{
				string[] array = streamReader.ReadLine().Trim().Split();
				list.Add(double.Parse(array[0]));
				int num2 = array.Length - 1;
				Node[] array2 = new Node[num2];
				for (int i = 0; i < num2; i++)
				{
					array2[i] = new Node();
					string[] array3 = array[i + 1].Split(':');
					array2[i].Index = int.Parse(array3[0]);
					array2[i].Value = double.Parse(array3[1]);
				}
				if (num2 > 0)
				{
					num = Math.Max(num, array2[num2 - 1].Index);
				}
				list2.Add(array2);
			}
			TemporaryCulture.Stop();
			return new Problem(list.Count, list.ToArray(), list2.ToArray(), num);
		}

		public static void Write(Stream stream, Problem problem)
		{
			TemporaryCulture.Start();
			StreamWriter streamWriter = new StreamWriter(stream);
			for (int i = 0; i < problem.Count; i++)
			{
				streamWriter.Write(problem.Y[i]);
				for (int j = 0; j < problem.X[i].Length; j++)
				{
					streamWriter.Write(" {0}:{1}", problem.X[i][j].Index, problem.X[i][j].Value);
				}
				streamWriter.WriteLine();
			}
			streamWriter.Flush();
			TemporaryCulture.Stop();
		}

		public static Problem Read(string filename)
		{
			FileStream fileStream = File.OpenRead(filename);
			try
			{
				return Problem.Read(fileStream);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static void Write(string filename, Problem problem)
		{
			FileStream fileStream = File.Open(filename, FileMode.Create);
			try
			{
				Problem.Write(fileStream, problem);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static HashSet<int> GenerateTest(Problem Original, double Percent, bool IgnoreZeroValue = false)
		{
			return Problem.GenerateTest(Original, (int)((double)Original.Count * (Percent / 100.0)), IgnoreZeroValue);
		}

		public static HashSet<int> GenerateTest(Problem Original, int TestCount, bool IgnoreZeroValue = false)
		{
			Random random = new Random();
			HashSet<int> hashSet = new HashSet<int>();
			while (hashSet.Count < TestCount && hashSet.Count < Original.Count)
			{
				int num = random.Next(Original.Count);
				if (!IgnoreZeroValue || Original.Y[num] != 0.0)
				{
					hashSet.Add(num);
				}
			}
			return hashSet;
		}

		public static Problem ExtractTestProblem(ref Problem Original, double Percent, bool IgnoreZeroValue = false)
		{
			HashSet<int> testIndexes = Problem.GenerateTest(Original, Percent, IgnoreZeroValue);
			return Problem.ExtractTestProblem(ref Original, testIndexes);
		}

		public static Problem ExtractTestProblem(ref Problem Original, int TestCount, bool IgnoreZeroValue = false)
		{
			HashSet<int> testIndexes = Problem.GenerateTest(Original, TestCount, IgnoreZeroValue);
			return Problem.ExtractTestProblem(ref Original, testIndexes);
		}

		public static Problem ExtractTestProblem(ref Problem Original, HashSet<int> TestIndexes)
		{
			double[] array = new double[TestIndexes.Count];
			Node[][] array2 = new Node[TestIndexes.Count][];
			double[] array3 = new double[Original.Count - TestIndexes.Count];
			Node[][] array4 = new Node[Original.Count - TestIndexes.Count][];
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < Original.Count; i++)
			{
				if (TestIndexes.Contains(i))
				{
					array[num] = Original.Y[i];
					array2[num] = Original.X[i];
					num++;
				}
				else
				{
					array4[num2] = Original.X[i];
					array3[num2] = Original.Y[i];
					num2++;
				}
			}
			Original.Count -= TestIndexes.Count;
			Original.X = array4;
			Original.Y = array3;
			return new Problem(TestIndexes.Count, array, array2, Original.MaxIndex);
		}
	}
}
