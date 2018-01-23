using System;
using System.IO;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	[Serializable]
	public class Model
	{
		private Parameter _parameter;

		private int _numberOfClasses;

		private int _supportVectorCount;

		private Node[][] _supportVectors;

		private double[][] _supportVectorCoefficients;

		private double[] _rho;

		private double[] _pairwiseProbabilityA;

		private double[] _pairwiseProbabilityB;

		private int[] _classLabels;

		private int[] _numberOfSVPerClass;

		public Parameter Parameter
		{
			get
			{
				return this._parameter;
			}
			set
			{
				this._parameter = value;
			}
		}

		public int NumberOfClasses
		{
			get
			{
				return this._numberOfClasses;
			}
			set
			{
				this._numberOfClasses = value;
			}
		}

		public int SupportVectorCount
		{
			get
			{
				return this._supportVectorCount;
			}
			set
			{
				this._supportVectorCount = value;
			}
		}

		public Node[][] SupportVectors
		{
			get
			{
				return this._supportVectors;
			}
			set
			{
				this._supportVectors = value;
			}
		}

		public double[][] SupportVectorCoefficients
		{
			get
			{
				return this._supportVectorCoefficients;
			}
			set
			{
				this._supportVectorCoefficients = value;
			}
		}

		public double[] Rho
		{
			get
			{
				return this._rho;
			}
			set
			{
				this._rho = value;
			}
		}

		public double[] PairwiseProbabilityA
		{
			get
			{
				return this._pairwiseProbabilityA;
			}
			set
			{
				this._pairwiseProbabilityA = value;
			}
		}

		public double[] PairwiseProbabilityB
		{
			get
			{
				return this._pairwiseProbabilityB;
			}
			set
			{
				this._pairwiseProbabilityB = value;
			}
		}

		public int[] ClassLabels
		{
			get
			{
				return this._classLabels;
			}
			set
			{
				this._classLabels = value;
			}
		}

		public int[] NumberOfSVPerClass
		{
			get
			{
				return this._numberOfSVPerClass;
			}
			set
			{
				this._numberOfSVPerClass = value;
			}
		}

		internal Model()
		{
		}

		public static Model Read(string filename)
		{
			FileStream fileStream = File.OpenRead(filename);
			try
			{
				return Model.Read(fileStream);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static Model Read(Stream stream)
		{
			TemporaryCulture.Start();
			StreamReader streamReader = new StreamReader(stream);
			Model model = new Model();
			Parameter parameter2 = model.Parameter = new Parameter();
			model.Rho = null;
			model.PairwiseProbabilityA = null;
			model.PairwiseProbabilityB = null;
			model.ClassLabels = null;
			model.NumberOfSVPerClass = null;
			bool flag = false;
			while (!flag)
			{
				string text = streamReader.ReadLine();
				int num = text.IndexOf(' ');
				string text2;
				string text3;
				if (num >= 0)
				{
					text2 = text.Substring(0, num);
					text3 = text.Substring(num + 1);
				}
				else
				{
					text2 = text;
					text3 = "";
				}
				text3 = text3.ToLower();
				switch (text2)
				{
				case "svm_type":
					parameter2.SvmType = (SvmType)Enum.Parse(typeof(SvmType), text3.ToUpper());
					break;
				case "kernel_type":
					parameter2.KernelType = (KernelType)Enum.Parse(typeof(KernelType), text3.ToUpper());
					break;
				case "degree":
					parameter2.Degree = int.Parse(text3);
					break;
				case "gamma":
					parameter2.Gamma = double.Parse(text3);
					break;
				case "coef0":
					parameter2.Coefficient0 = double.Parse(text3);
					break;
				case "nr_class":
					model.NumberOfClasses = int.Parse(text3);
					break;
				case "total_sv":
					model.SupportVectorCount = int.Parse(text3);
					break;
				case "rho":
				{
					int numberOfClasses = model.NumberOfClasses * (model.NumberOfClasses - 1) / 2;
					model.Rho = new double[numberOfClasses];
					string[] array5 = text3.Split();
					for (int i = 0; i < numberOfClasses; i++)
					{
						model.Rho[i] = double.Parse(array5[i]);
					}
					break;
				}
				case "label":
				{
					int numberOfClasses = model.NumberOfClasses;
					model.ClassLabels = new int[numberOfClasses];
					string[] array4 = text3.Split();
					for (int i = 0; i < numberOfClasses; i++)
					{
						model.ClassLabels[i] = int.Parse(array4[i]);
					}
					break;
				}
				case "probA":
				{
					int numberOfClasses = model.NumberOfClasses * (model.NumberOfClasses - 1) / 2;
					model.PairwiseProbabilityA = new double[numberOfClasses];
					string[] array3 = text3.Split();
					for (int i = 0; i < numberOfClasses; i++)
					{
						model.PairwiseProbabilityA[i] = double.Parse(array3[i]);
					}
					break;
				}
				case "probB":
				{
					int numberOfClasses = model.NumberOfClasses * (model.NumberOfClasses - 1) / 2;
					model.PairwiseProbabilityB = new double[numberOfClasses];
					string[] array2 = text3.Split();
					for (int i = 0; i < numberOfClasses; i++)
					{
						model.PairwiseProbabilityB[i] = double.Parse(array2[i]);
					}
					break;
				}
				case "nr_sv":
				{
					int numberOfClasses = model.NumberOfClasses;
					model.NumberOfSVPerClass = new int[numberOfClasses];
					string[] array = text3.Split();
					for (int i = 0; i < numberOfClasses; i++)
					{
						model.NumberOfSVPerClass[i] = int.Parse(array[i]);
					}
					break;
				}
				case "SV":
					flag = true;
					break;
				default:
					throw new Exception("Unknown text in model file");
				}
			}
			int num2 = model.NumberOfClasses - 1;
			int supportVectorCount = model.SupportVectorCount;
			model.SupportVectorCoefficients = new double[num2][];
			for (int j = 0; j < num2; j++)
			{
				model.SupportVectorCoefficients[j] = new double[supportVectorCount];
			}
			model.SupportVectors = new Node[supportVectorCount][];
			for (int k = 0; k < supportVectorCount; k++)
			{
				string[] array6 = streamReader.ReadLine().Trim().Split();
				for (int l = 0; l < num2; l++)
				{
					model.SupportVectorCoefficients[l][k] = double.Parse(array6[l]);
				}
				int num3 = array6.Length - num2;
				model.SupportVectors[k] = new Node[num3];
				for (int m = 0; m < num3; m++)
				{
					string[] array7 = array6[num2 + m].Split(':');
					model.SupportVectors[k][m] = new Node();
					model.SupportVectors[k][m].Index = int.Parse(array7[0]);
					model.SupportVectors[k][m].Value = double.Parse(array7[1]);
				}
			}
			TemporaryCulture.Stop();
			return model;
		}

		public static void Write(string filename, Model model)
		{
			FileStream fileStream = File.Open(filename, FileMode.Create);
			try
			{
				Model.Write(fileStream, model);
			}
			finally
			{
				fileStream.Close();
			}
		}

		public static void Write(Stream stream, Model model)
		{
			TemporaryCulture.Start();
			StreamWriter streamWriter = new StreamWriter(stream);
			Parameter parameter = model.Parameter;
			streamWriter.Write("svm_type " + parameter.SvmType + "\n");
			streamWriter.Write("kernel_type " + parameter.KernelType + "\n");
			if (parameter.KernelType == KernelType.POLY)
			{
				streamWriter.Write("degree " + parameter.Degree + "\n");
			}
			if (parameter.KernelType == KernelType.POLY || parameter.KernelType == KernelType.RBF || parameter.KernelType == KernelType.SIGMOID)
			{
				streamWriter.Write("gamma " + parameter.Gamma + "\n");
			}
			if (parameter.KernelType == KernelType.POLY || parameter.KernelType == KernelType.SIGMOID)
			{
				streamWriter.Write("coef0 " + parameter.Coefficient0 + "\n");
			}
			int numberOfClasses = model.NumberOfClasses;
			int supportVectorCount = model.SupportVectorCount;
			streamWriter.Write("nr_class " + numberOfClasses + "\n");
			streamWriter.Write("total_sv " + supportVectorCount + "\n");
			streamWriter.Write("rho");
			for (int i = 0; i < numberOfClasses * (numberOfClasses - 1) / 2; i++)
			{
				streamWriter.Write(" " + model.Rho[i]);
			}
			streamWriter.Write("\n");
			if (model.ClassLabels != null)
			{
				streamWriter.Write("label");
				for (int j = 0; j < numberOfClasses; j++)
				{
					streamWriter.Write(" " + model.ClassLabels[j]);
				}
				streamWriter.Write("\n");
			}
			if (model.PairwiseProbabilityA != null)
			{
				streamWriter.Write("probA");
				for (int k = 0; k < numberOfClasses * (numberOfClasses - 1) / 2; k++)
				{
					streamWriter.Write(" " + model.PairwiseProbabilityA[k]);
				}
				streamWriter.Write("\n");
			}
			if (model.PairwiseProbabilityB != null)
			{
				streamWriter.Write("probB");
				for (int l = 0; l < numberOfClasses * (numberOfClasses - 1) / 2; l++)
				{
					streamWriter.Write(" " + model.PairwiseProbabilityB[l]);
				}
				streamWriter.Write("\n");
			}
			if (model.NumberOfSVPerClass != null)
			{
				streamWriter.Write("nr_sv");
				for (int m = 0; m < numberOfClasses; m++)
				{
					streamWriter.Write(" " + model.NumberOfSVPerClass[m]);
				}
				streamWriter.Write("\n");
			}
			streamWriter.Write("SV\n");
			double[][] supportVectorCoefficients = model.SupportVectorCoefficients;
			Node[][] supportVectors = model.SupportVectors;
			for (int n = 0; n < supportVectorCount; n++)
			{
				for (int num = 0; num < numberOfClasses - 1; num++)
				{
					streamWriter.Write(supportVectorCoefficients[num][n] + " ");
				}
				Node[] array = supportVectors[n];
				if (array.Length == 0)
				{
					streamWriter.WriteLine();
				}
				else
				{
					if (parameter.KernelType == KernelType.PRECOMPUTED)
					{
						streamWriter.Write("0:{0}", (int)array[0].Value);
					}
					else
					{
						streamWriter.Write("{0}:{1}", array[0].Index, array[0].Value);
						for (int num2 = 1; num2 < array.Length; num2++)
						{
							streamWriter.Write(" {0}:{1}", array[num2].Index, array[num2].Value);
						}
					}
					streamWriter.WriteLine();
				}
			}
			streamWriter.Flush();
			TemporaryCulture.Stop();
		}
	}
}
