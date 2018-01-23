using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	public static class Prediction
	{
		public static double Predict(Problem problem, string outputFile, Model model, bool predict_probability, int MaxClassCount = 1)
		{
			int num = 0;
			int num2 = 0;
			double num3 = 0.0;
			double num4 = 0.0;
			double num5 = 0.0;
			double num6 = 0.0;
			double num7 = 0.0;
			double num8 = 0.0;
			StreamWriter streamWriter = (outputFile != null) ? new StreamWriter(outputFile) : null;
			SvmType svmType = Procedures.svm_get_svm_type(model);
			int num9 = Procedures.svm_get_nr_class(model);
			int[] array = new int[num9];
			double[] array2 = null;
			if (predict_probability)
			{
				if (svmType == SvmType.EPSILON_SVR || svmType == SvmType.NU_SVR)
				{
					Console.WriteLine("Prob. model for test data: target value = predicted value + z,\nz: Laplace distribution e^(-|z|/sigma)/(2sigma),sigma=" + Procedures.svm_get_svr_probability(model));
				}
				else
				{
					Procedures.svm_get_labels(model, array);
					array2 = new double[num9];
					if (streamWriter != null)
					{
						streamWriter.Write("labels");
						for (int i = 0; i < num9; i++)
						{
							streamWriter.Write(" " + array[i]);
						}
						streamWriter.Write("\n");
					}
				}
			}
			for (int j = 0; j < problem.Count; j++)
			{
				double num10 = problem.Y[j];
				Node[] x = problem.X[j];
				double num11;
				if (predict_probability && (svmType == SvmType.C_SVC || svmType == SvmType.NU_SVC))
				{
					num11 = Procedures.svm_predict_probability(model, x, array2);
					if (streamWriter != null)
					{
						streamWriter.Write(num11 + " ");
						for (int k = 0; k < num9; k++)
						{
							streamWriter.Write(array2[k] + " ");
						}
						streamWriter.Write("\n");
					}
				}
				else
				{
					num11 = Procedures.svm_predict(model, x);
					if (MaxClassCount == 1)
					{
						if (streamWriter != null)
						{
							streamWriter.Write(num11 + "\n");
						}
					}
					else
					{
						int[] array3 = default(int[]);
						Procedures.svm_predict_multi(model, x, out array3);
						List<KeyValuePair<int, int>> list = new List<KeyValuePair<int, int>>(array3.Length);
						for (int l = 0; l < array3.Length; l++)
						{
							list.Add(new KeyValuePair<int, int>(l, array3[l]));
						}
						list.Sort((KeyValuePair<int, int> first, KeyValuePair<int, int> second) => -first.Value.CompareTo(second.Value));
						for (int m = 0; m < Math.Min(MaxClassCount, list.Count); m++)
						{
							if (m > 0)
							{
								streamWriter.Write('\t');
							}
							streamWriter.Write(list[m].Key);
						}
						streamWriter.Write("\n");
					}
				}
				if (num11 == num10)
				{
					num++;
				}
				num3 += (num11 - num10) * (num11 - num10);
				num4 += num11;
				num5 += num10;
				num6 += num11 * num11;
				num7 += num10 * num10;
				num8 += num11 * num10;
				num2++;
			}
			if (streamWriter != null)
			{
				streamWriter.Close();
			}
			if (svmType != SvmType.EPSILON_SVR && svmType != SvmType.NU_SVR)
			{
				return (double)num / (double)num2;
			}
			return ((double)problem.Count * num8 - num4 * num5) / (Math.Sqrt((double)problem.Count * num6 - num4 * num4) * Math.Sqrt((double)problem.Count * num7 - num5 * num5));
		}

		public static double Predict(Model model, Node[] x)
		{
			return Procedures.svm_predict(model, x);
		}

		public static double[] Predict(Problem problem, Model model)
		{
			double[] array = new double[problem.Count];
			for (int i = 0; i < problem.Count; i++)
			{
				array[i] = Prediction.Predict(model, problem.X[i]);
			}
			return array;
		}

		public static double ComputeMSE(Problem problem, double[] Predictions, int StartIndex = 0)
		{
			double num = 0.0;
			for (int i = StartIndex; i < problem.Count; i++)
			{
				num += Math.Pow(problem.Y[i] - Predictions[i], 2.0);
			}
			return num / (double)(problem.Count - StartIndex);
		}

		public static double ComputePrecision(Problem problem, double[] Predictions, double ErrorsExcepted, int StartIndex = 0)
		{
			int num = 0;
			for (int i = StartIndex; i < problem.Count; i++)
			{
				if (Math.Abs(Predictions[i] - problem.Y[i]) < ErrorsExcepted)
				{
					num++;
				}
			}
			return (double)num / (double)(problem.Count - StartIndex);
		}

		public static int[] Predict_multi(Model model, Node[] x)
		{
			int[] result = default(int[]);
			Procedures.svm_predict_multi(model, x, out result);
			return result;
		}

		public static double[] PredictProbability(Model model, Node[] x)
		{
			SvmType svmType = Procedures.svm_get_svm_type(model);
			if (svmType != 0 && svmType != SvmType.NU_SVC)
			{
				throw new Exception("Model type " + svmType + " unable to predict probabilities.");
			}
			int num = Procedures.svm_get_nr_class(model);
			double[] array = new double[num];
			Procedures.svm_predict_probability(model, x, array);
			return array;
		}
	}
}
