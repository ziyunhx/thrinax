using System;
using System.IO;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	public class RangeTransform : IRangeTransform
	{
		public const int DEFAULT_LOWER_BOUND = -1;

		public const int DEFAULT_UPPER_BOUND = 1;

		private double[] _inputStart;

		private double[] _inputScale;

		private double _outputStart;

		private double _outputScale;

		private int _length;

		public static RangeTransform Compute(Problem prob)
		{
			return RangeTransform.Compute(prob, -1.0, 1.0);
		}

		public static RangeTransform Compute(Problem prob, double lowerBound, double upperBound)
		{
			double[] array = new double[prob.MaxIndex];
			double[] array2 = new double[prob.MaxIndex];
			for (int i = 0; i < prob.MaxIndex; i++)
			{
				array[i] = 1.7976931348623157E+308;
				array2[i] = -1.7976931348623157E+308;
			}
			for (int j = 0; j < prob.Count; j++)
			{
				for (int k = 0; k < prob.X[j].Length; k++)
				{
					int num = prob.X[j][k].Index - 1;
					double value = prob.X[j][k].Value;
					array[num] = Math.Min(array[num], value);
					array2[num] = Math.Max(array2[num], value);
				}
			}
			for (int l = 0; l < prob.MaxIndex; l++)
			{
				if (array[l] == 1.7976931348623157E+308 || array2[l] == -1.7976931348623157E+308)
				{
					array[l] = 0.0;
					array2[l] = 0.0;
				}
			}
			return new RangeTransform(array, array2, lowerBound, upperBound);
		}

		public RangeTransform(double[] minValues, double[] maxValues, double lowerBound, double upperBound)
		{
			this._length = minValues.Length;
			if (maxValues.Length != this._length)
			{
				throw new Exception("Number of max and min values must be equal.");
			}
			this._inputStart = new double[this._length];
			this._inputScale = new double[this._length];
			for (int i = 0; i < this._length; i++)
			{
				this._inputStart[i] = minValues[i];
				this._inputScale[i] = maxValues[i] - minValues[i];
			}
			this._outputStart = lowerBound;
			this._outputScale = upperBound - lowerBound;
		}

		private RangeTransform(double[] inputStart, double[] inputScale, double outputStart, double outputScale, int length)
		{
			this._inputStart = inputStart;
			this._inputScale = inputScale;
			this._outputStart = outputStart;
			this._outputScale = outputScale;
			this._length = length;
		}

		public Node[] Transform(Node[] input)
		{
			Node[] array = new Node[input.Length];
			for (int i = 0; i < array.Length; i++)
			{
				int index = input[i].Index;
				double value = input[i].Value;
				array[i] = new Node(index, this.Transform(value, index));
			}
			return array;
		}

		public double Transform(double input, int index)
		{
			index--;
			double num = input - this._inputStart[index];
			if (this._inputScale[index] == 0.0)
			{
				return 0.0;
			}
			num /= this._inputScale[index];
			num *= this._outputScale;
			return num + this._outputStart;
		}

		public static void Write(Stream stream, RangeTransform r)
		{
			TemporaryCulture.Start();
			StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.WriteLine(r._length);
			streamWriter.Write(r._inputStart[0]);
			for (int i = 1; i < r._inputStart.Length; i++)
			{
				streamWriter.Write(" " + r._inputStart[i]);
			}
			streamWriter.WriteLine();
			streamWriter.Write(r._inputScale[0]);
			for (int j = 1; j < r._inputScale.Length; j++)
			{
				streamWriter.Write(" " + r._inputScale[j]);
			}
			streamWriter.WriteLine();
			streamWriter.WriteLine("{0} {1}", r._outputStart, r._outputScale);
			streamWriter.Flush();
			TemporaryCulture.Stop();
		}

		public static void Write(string outputFile, RangeTransform r)
		{
			FileStream fileStream = File.Open(outputFile, FileMode.Create);
			try
			{
				RangeTransform.Write(fileStream, r);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static RangeTransform Read(string inputFile)
		{
			FileStream fileStream = File.OpenRead(inputFile);
			try
			{
				return RangeTransform.Read(fileStream);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static RangeTransform Read(Stream stream)
		{
			TemporaryCulture.Start();
			StreamReader streamReader = new StreamReader(stream);
			int num = int.Parse(streamReader.ReadLine());
			double[] array = new double[num];
			double[] array2 = new double[num];
			string[] array3 = streamReader.ReadLine().Split();
			for (int i = 0; i < num; i++)
			{
				array[i] = double.Parse(array3[i]);
			}
			array3 = streamReader.ReadLine().Split();
			for (int j = 0; j < num; j++)
			{
				array2[j] = double.Parse(array3[j]);
			}
			array3 = streamReader.ReadLine().Split();
			double outputStart = double.Parse(array3[0]);
			double outputScale = double.Parse(array3[1]);
			TemporaryCulture.Stop();
			return new RangeTransform(array, array2, outputStart, outputScale, num);
		}
	}
}
