using System;
using System.Globalization;
using System.IO;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	public class GaussianTransform : IRangeTransform
	{
		private double[] _means;

		private double[] _stddevs;

		public static GaussianTransform Compute(Problem prob)
		{
			int[] array = new int[prob.MaxIndex];
			double[] array2 = new double[prob.MaxIndex];
			Node[][] x = prob.X;
			foreach (Node[] array3 in x)
			{
				for (int j = 0; j < array3.Length; j++)
				{
					array2[array3[j].Index - 1] += array3[j].Value;
					array[array3[j].Index - 1]++;
				}
			}
			for (int k = 0; k < prob.MaxIndex; k++)
			{
				if (array[k] == 0)
				{
					array[k] = 2;
				}
				array2[k] /= (double)array[k];
			}
			double[] array4 = new double[prob.MaxIndex];
			Node[][] x2 = prob.X;
			foreach (Node[] array5 in x2)
			{
				for (int m = 0; m < array5.Length; m++)
				{
					double num = array5[m].Value - array2[array5[m].Index - 1];
					array4[array5[m].Index - 1] += num * num;
				}
			}
			for (int n = 0; n < prob.MaxIndex; n++)
			{
				if (array4[n] != 0.0)
				{
					array4[n] /= (double)(array[n] - 1);
					array4[n] = Math.Sqrt(array4[n]);
				}
			}
			return new GaussianTransform(array2, array4);
		}

		public GaussianTransform(double[] means, double[] stddevs)
		{
			this._means = means;
			this._stddevs = stddevs;
		}

		public static void Write(Stream stream, GaussianTransform transform)
		{
			TemporaryCulture.Start();
			StreamWriter streamWriter = new StreamWriter(stream);
			streamWriter.WriteLine(transform._means.Length);
			for (int i = 0; i < transform._means.Length; i++)
			{
				streamWriter.WriteLine("{0} {1}", transform._means[i], transform._stddevs[i]);
			}
			streamWriter.Flush();
			TemporaryCulture.Stop();
		}

		public static GaussianTransform Read(Stream stream)
		{
			TemporaryCulture.Start();
			StreamReader streamReader = new StreamReader(stream);
			int num = int.Parse(streamReader.ReadLine(), CultureInfo.InvariantCulture);
			double[] array = new double[num];
			double[] array2 = new double[num];
			for (int i = 0; i < num; i++)
			{
				string[] array3 = streamReader.ReadLine().Split();
				array[i] = double.Parse(array3[0], CultureInfo.InvariantCulture);
				array2[i] = double.Parse(array3[1], CultureInfo.InvariantCulture);
			}
			TemporaryCulture.Stop();
			return new GaussianTransform(array, array2);
		}

		public static void Write(string filename, GaussianTransform transform)
		{
			FileStream fileStream = File.Open(filename, FileMode.Create);
			try
			{
				GaussianTransform.Write(fileStream, transform);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static GaussianTransform Read(string filename)
		{
			FileStream fileStream = File.Open(filename, FileMode.Open);
			try
			{
				return GaussianTransform.Read(fileStream);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public double Transform(double input, int index)
		{
			index--;
			if (this._stddevs[index] == 0.0)
			{
				return 0.0;
			}
			double num = input - this._means[index];
			return num / this._stddevs[index];
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
	}
}
