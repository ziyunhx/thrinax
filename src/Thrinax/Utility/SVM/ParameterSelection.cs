using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	public static class ParameterSelection
	{
		private class Point3D
		{
			public int C;

			public int G;

			public int P;

			public Point3D(int X, int Y, int Z)
			{
				this.C = X;
				this.G = Y;
				this.P = Z;
			}
		}

		public class RegressionParameter : IComparable<RegressionParameter>
		{
			public double C;

			public double G;

			public double P;

			public double SortValue;

			public double[] ValidatePredictions;

			public RegressionParameter(double C, double G, double P, double SortValue)
			{
				this.C = C;
				this.G = G;
				this.P = P;
				this.SortValue = SortValue;
			}

			public int CompareTo(RegressionParameter other)
			{
				return this.SortValue.CompareTo(other.SortValue);
			}
		}

		private enum TrendLineDirection
		{
			LeftBelow_to_RightHigh,
			AllNeighbors
		}

		private class GridComputingParameter
		{
			public Problem problem;

			public Parameter parameter;

			public int nrfold;

			public GridComputingParameter(Problem problem, Parameter parameter, int nrfold)
			{
				this.problem = problem;
				this.parameter = parameter;
				this.nrfold = nrfold;
			}
		}

		public const int NFOLD = 5;

		public const int MIN_C = -5;

		public const int MAX_C = 15;

		public const int C_STEP = 2;

		public const int MIN_G = -15;

		public const int MAX_G = 3;

		public const int G_STEP = 2;

		public static List<double> GetList(double minPower, double maxPower, double iteration)
		{
			List<double> list = new List<double>();
			for (double num = minPower; num <= maxPower; num += iteration)
			{
				list.Add(Math.Pow(2.0, num));
			}
			return list;
		}

		public static void Grid(Problem problem, Parameter parameters, string outputFile, out double C, out double Gamma)
		{
			ParameterSelection.Grid(problem, parameters, ParameterSelection.GetList(-5.0, 15.0, 2.0), ParameterSelection.GetList(-15.0, 3.0, 2.0), outputFile, 5, out C, out Gamma);
		}

		public static void Grid(Problem problem, Parameter parameters, List<double> CValues, List<double> GammaValues, string outputFile, out double C, out double Gamma)
		{
			ParameterSelection.Grid(problem, parameters, CValues, GammaValues, outputFile, 5, out C, out Gamma);
		}

		public static void Grid(Problem problem, Parameter parameters, List<double> CValues, List<double> GammaValues, string outputFile, int nrfold, out double C, out double Gamma)
		{
			C = 0.0;
			Gamma = 0.0;
			double num = -1.7976931348623157E+308;
			if (!string.IsNullOrEmpty(outputFile))
			{
				File.WriteAllText(outputFile, string.Empty);
			}
			for (int i = 0; i < CValues.Count; i++)
			{
				for (int j = 0; j < GammaValues.Count; j++)
				{
					parameters.C = CValues[i];
					parameters.Gamma = GammaValues[j];
					double num2 = Training.PerformCrossValidation(problem, parameters, nrfold);
					Console.Write("{0} {1} {2}", parameters.C, parameters.Gamma, num2);
					if (!string.IsNullOrEmpty(outputFile))
					{
						File.AppendAllText(outputFile, string.Format("{0} {1} {2}\n", parameters.C, parameters.Gamma, num2));
					}
					if (num2 > num)
					{
						C = parameters.C;
						Gamma = parameters.Gamma;
						num = num2;
						Console.WriteLine(" New Maximum!");
					}
					else
					{
						Console.WriteLine();
					}
				}
			}
		}

		public static void GridRegression(Problem problem, Parameter parameters, List<double> CValues, List<double> GammaValues, List<double> PValues, string outputFile, int nrfold, out double C, out double Gamma, out double P)
		{
			C = 0.0;
			Gamma = 0.0;
			P = 0.0;
			double num = 1.7976931348623157E+308;
			if (!string.IsNullOrEmpty(outputFile))
			{
				File.WriteAllText(outputFile, string.Empty);
			}
			for (int i = 0; i < CValues.Count; i++)
			{
				for (int j = 0; j < GammaValues.Count; j++)
				{
					for (int k = 0; k < PValues.Count; k++)
					{
						parameters.C = CValues[i];
						parameters.Gamma = GammaValues[j];
						parameters.P = PValues[k];
						double num2 = Math.Abs(Training.PerformCrossValidation(problem, parameters, nrfold));
						Console.Write("{0} {1} {2} {3}", parameters.C, parameters.Gamma, parameters.P, num2);
						if (!string.IsNullOrEmpty(outputFile))
						{
							File.AppendAllText(outputFile, string.Format("{0} {1} {2} {3}\n", parameters.C, parameters.Gamma, parameters.P, num2));
						}
						if (num2 < num)
						{
							C = parameters.C;
							Gamma = parameters.Gamma;
							P = parameters.P;
							num = num2;
							Console.WriteLine(" New Min!");
						}
						else
						{
							Console.WriteLine();
						}
					}
				}
			}
		}

		public static RegressionParameter[] GridRegression(Problem problem, Parameter parameters, List<double> CValues, List<double> GammaValues, List<double> PValues, string outputFile, int nrfold, int MaxBestCount)
		{
			if (!string.IsNullOrEmpty(outputFile))
			{
				File.WriteAllText(outputFile, string.Empty);
			}
			List<RegressionParameter> list = new List<RegressionParameter>(CValues.Count * GammaValues.Count * PValues.Count);
			for (int i = 0; i < CValues.Count; i++)
			{
				for (int j = 0; j < GammaValues.Count; j++)
				{
					for (int k = 0; k < PValues.Count; k++)
					{
						parameters.C = CValues[i];
						parameters.Gamma = GammaValues[j];
						parameters.P = PValues[k];
						double num = Math.Abs(Training.PerformCrossValidation(problem, parameters, nrfold));
						Console.Write("{0} {1} {2} {3}", parameters.C, parameters.Gamma, parameters.P, num);
						if (!string.IsNullOrEmpty(outputFile))
						{
							File.AppendAllText(outputFile, string.Format("{0} {1} {2} {3}\n", parameters.C, parameters.Gamma, parameters.P, num));
						}
						list.Add(new RegressionParameter(parameters.C, parameters.Gamma, parameters.P, num));
					}
				}
			}
			list.Sort();
			if (MaxBestCount > 0)
			{
				int l;
				for (l = Math.Min(MaxBestCount, list.Count); l < list.Count && l < MaxBestCount * 3 && list[l - 1].SortValue == list[l].SortValue; l++)
				{
				}
				if (list.Count > l)
				{
					list.RemoveRange(l, list.Count - l);
				}
				list.TrimExcess();
			}
			return list.ToArray();
		}

		public static int SmartGrid(Problem problem, Parameter parameters, double C_Start, double C_Multi, double Gamma_Start, double Gamma_Multi, string outputFile, int nrfold, out double C, out double Gamma, bool Parallel = true)
		{
			C = C_Start;
			Gamma = Gamma_Start;
			double num = -1.7976931348623157E+308;
			if (!string.IsNullOrEmpty(outputFile))
			{
				File.WriteAllText(outputFile, string.Empty);
			}
			int num2 = 0;
			double[,] array = new double[21, 21];
			double[] array2 = new double[21]
			{
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				C_Start,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0
			};
			double[] array3 = new double[21]
			{
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				Gamma_Start,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0
			};
			for (int i = 0; i < 10; i++)
			{
				array2[9 - i] = array2[10 - i] / C_Multi;
				array2[11 + i] = array2[10 + i] * C_Multi;
				array3[9 - i] = array3[10 - i] / Gamma_Multi;
				array3[11 + i] = array3[10 + i] * Gamma_Multi;
			}
			Point p = new Point(10, 10);
			Point[] array4 = ParameterSelection.FindNeedCompute(array, p, TrendLineDirection.LeftBelow_to_RightHigh);
			while (array4.Length > 0)
			{
				Task<double>[] array5 = new Task<double>[array4.Length];
				if (Parallel)
				{
					for (int j = 0; j < array4.Length; j++)
					{
						Parameter parameter = (Parameter)parameters.Clone();
						parameter.C = array2[array4[j].X];
						parameter.Gamma = array3[array4[j].Y];
						GridComputingParameter state = new GridComputingParameter(problem, parameter, nrfold);
						array5[j] = new Task<double>((object obj) => ParameterSelection.GridCompute((GridComputingParameter)obj), state);
						array5[j].Start();
					}
				}
				for (int k = 0; k < array4.Length; k++)
				{
					double num3;
					if (Parallel)
					{
						num3 = array5[k].Result;
					}
					else
					{
						parameters.C = array2[array4[k].X];
						parameters.Gamma = array3[array4[k].Y];
						num3 = Training.PerformCrossValidation(problem, parameters, nrfold);
					}
					array[array4[k].X, array4[k].Y] = num3;
					Console.Write(string.Format("{0} {1} {2}", array2[array4[k].X], array3[array4[k].Y], num3));
					if (!string.IsNullOrEmpty(outputFile))
					{
						File.AppendAllText(outputFile, string.Format("{0} {1} {2}\n", array2[array4[k].X], array3[array4[k].Y], num3));
					}
					if (num3 > num)
					{
						num = num3;
						p.X = array4[k].X;
						p.Y = array4[k].Y;
						C = array2[array4[k].X];
						Gamma = array3[array4[k].Y];
						Console.WriteLine(" New Maximum!");
					}
					else
					{
						Console.WriteLine();
					}
					num2++;
				}
				array4 = ParameterSelection.FindNeedCompute(array, p, TrendLineDirection.LeftBelow_to_RightHigh);
			}
			return num2;
		}

		public static int SmartGridRegression(Problem problem, Parameter parameter, double C_Start, double C_Multi, double Gamma_Start, double Gamma_Multi, double P_Start, double P_Multi, string outputFile, int nrfold, out double C, out double Gamma, out double P)
		{
			C = C_Start;
			Gamma = Gamma_Start;
			P = P_Start;
			double num = -1.7976931348623157E+308;
			if (!string.IsNullOrEmpty(outputFile))
			{
				File.WriteAllText(outputFile, string.Empty);
			}
			int num2 = 0;
			double[] array = new double[21]
			{
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				C_Start,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0
			};
			double[] array2 = new double[21]
			{
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				Gamma_Start,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0
			};
			double[] array3 = new double[21]
			{
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				P_Start,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0,
				0.0
			};
			for (int i = 0; i < 10; i++)
			{
				array[9 - i] = array[10 - i] / C_Multi;
				array[11 + i] = array[10 + i] * C_Multi;
				array2[9 - i] = array2[10 - i] / Gamma_Multi;
				array2[11 + i] = array2[10 + i] * Gamma_Multi;
				array3[9 - i] = array3[10 - i] / P_Multi;
				array3[11 + i] = array3[10 + i] / P_Multi;
			}
			double[,,] array4 = new double[21, 21, 21];
			Point3D point3D = new Point3D(10, 10, 10);
			Point3D[] array5 = ParameterSelection.FindNeedCompute(array4, point3D);
			while (array5.Length > 0)
			{
				for (int j = 0; j < array5.Length; j++)
				{
					parameter.C = array[array5[j].C];
					parameter.Gamma = array2[array5[j].G];
					parameter.P = array3[array5[j].P];
					double num3 = Training.PerformCrossValidation(problem, parameter, nrfold);
					array4[array5[j].C, array5[j].G, array5[j].P] = num3;
					Console.Write(string.Format("{0} {1} {2} {3}", array[array5[j].C], array2[array5[j].G], array3[array5[j].P], num3));
					if (!string.IsNullOrEmpty(outputFile))
					{
						File.AppendAllText(outputFile, string.Format("{0} {1} {2} {3}\n", array[array5[j].C], array2[array5[j].G], array3[array5[j].P], num3));
					}
					if (num3 < num)
					{
						num = num3;
						point3D.C = array5[j].C;
						point3D.G = array5[j].G;
						point3D.P = array5[j].P;
						C = array[array5[j].C];
						Gamma = array2[array5[j].G];
						P = array3[array5[j].P];
						Console.WriteLine(" New Min!");
					}
					else
					{
						Console.WriteLine();
					}
					num2++;
				}
				array5 = ParameterSelection.FindNeedCompute(array4, point3D);
			}
			return num2;
		}

		private static Point[] FindNeedCompute(double[,] matrix, Point p, TrendLineDirection dirc)
		{
			List<Point> list = new List<Point>();
			if (matrix[p.X, p.Y] <= 0.0)
			{
				list.Add(p);
			}
			if (matrix[p.X - 1, p.Y] <= 0.0)
			{
				list.Add(new Point(p.X - 1, p.Y));
			}
			if (matrix[p.X - 1, p.Y + 1] <= 0.0)
			{
				list.Add(new Point(p.X - 1, p.Y + 1));
			}
			if (matrix[p.X, p.Y + 1] <= 0.0)
			{
				list.Add(new Point(p.X, p.Y + 1));
			}
			if (matrix[p.X + 1, p.Y] <= 0.0)
			{
				list.Add(new Point(p.X + 1, p.Y));
			}
			if (matrix[p.X + 1, p.Y - 1] <= 0.0)
			{
				list.Add(new Point(p.X + 1, p.Y - 1));
			}
			if (matrix[p.X, p.Y - 1] <= 0.0)
			{
				list.Add(new Point(p.X, p.Y - 1));
			}
			if (dirc == TrendLineDirection.AllNeighbors)
			{
				if (matrix[p.X - 1, p.Y - 1] <= 0.0)
				{
					list.Add(new Point(p.X - 1, p.Y - 1));
				}
				if (matrix[p.X + 1, p.Y + 1] <= 0.0)
				{
					list.Add(new Point(p.X + 1, p.Y + 1));
				}
			}
			return list.ToArray();
		}

		private static Point3D[] FindNeedCompute(double[,,] matrix, Point3D p)
		{
			List<Point3D> list = new List<Point3D>();
			if (matrix[p.C - 1, p.G - 1, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G - 1, p.P - 1));
			}
			if (matrix[p.C - 1, p.G, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G, p.P - 1));
			}
			if (matrix[p.C - 1, p.G + 1, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G + 1, p.P - 1));
			}
			if (matrix[p.C, p.G - 1, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G - 1, p.P - 1));
			}
			if (matrix[p.C, p.G, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G, p.P - 1));
			}
			if (matrix[p.C, p.G + 1, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G + 1, p.P - 1));
			}
			if (matrix[p.C + 1, p.G - 1, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G - 1, p.P - 1));
			}
			if (matrix[p.C + 1, p.G, p.P - 1] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G, p.P - 1));
			}
			if (matrix[p.C + 1, p.G + 1, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G + 1, p.P - 1));
			}
			if (matrix[p.C - 1, p.G - 1, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G - 1, p.P));
			}
			if (matrix[p.C - 1, p.G, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G, p.P));
			}
			if (matrix[p.C - 1, p.G + 1, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G + 1, p.P));
			}
			if (matrix[p.C, p.G - 1, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G - 1, p.P));
			}
			if (matrix[p.C, p.G + 1, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G + 1, p.P));
			}
			if (matrix[p.C + 1, p.G - 1, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G - 1, p.P));
			}
			if (matrix[p.C + 1, p.G, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G, p.P));
			}
			if (matrix[p.C + 1, p.G + 1, p.P] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G + 1, p.P));
			}
			if (matrix[p.C - 1, p.G - 1, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G - 1, p.P + 1));
			}
			if (matrix[p.C - 1, p.G, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G, p.P + 1));
			}
			if (matrix[p.C - 1, p.G + 1, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C - 1, p.G + 1, p.P + 1));
			}
			if (matrix[p.C, p.G - 1, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G - 1, p.P + 1));
			}
			if (matrix[p.C, p.G, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G, p.P + 1));
			}
			if (matrix[p.C, p.G + 1, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C, p.G + 1, p.P + 1));
			}
			if (matrix[p.C + 1, p.G - 1, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G - 1, p.P + 1));
			}
			if (matrix[p.C + 1, p.G, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G, p.P + 1));
			}
			if (matrix[p.C + 1, p.G + 1, p.P + 1] <= 0.0)
			{
				list.Add(new Point3D(p.C + 1, p.G + 1, p.P + 1));
			}
			return list.ToArray();
		}

		private static double GridCompute(GridComputingParameter Para)
		{
			return Training.PerformCrossValidation(Para.problem, Para.parameter, Para.nrfold);
		}

		public static void Grid(Problem problem, Problem validation, Parameter parameters, string outputFile, out double C, out double Gamma)
		{
			ParameterSelection.Grid(problem, validation, parameters, ParameterSelection.GetList(-5.0, 15.0, 2.0), ParameterSelection.GetList(-15.0, 3.0, 2.0), outputFile, out C, out Gamma);
		}

		public static void Grid(Problem problem, Problem validation, Parameter parameters, List<double> CValues, List<double> GammaValues, string outputFile, out double C, out double Gamma)
		{
			C = 0.0;
			Gamma = 0.0;
			double num = -1.7976931348623157E+308;
			if (!string.IsNullOrEmpty(outputFile))
			{
				File.WriteAllText(outputFile, string.Empty);
			}
			for (int i = 0; i < CValues.Count; i++)
			{
				for (int j = 0; j < GammaValues.Count; j++)
				{
					parameters.C = CValues[i];
					parameters.Gamma = GammaValues[j];
					Model model = Training.Train(problem, parameters);
					double num2 = Prediction.Predict(validation, "tmp.txt", model, false, 1);
					Console.Write("{0} {1} {2}", parameters.C, parameters.Gamma, num2);
					if (!string.IsNullOrEmpty(outputFile))
					{
						File.AppendAllText(outputFile, string.Format("{0} {1} {2}\n", parameters.C, parameters.Gamma, num2));
					}
					if (num2 > num)
					{
						C = parameters.C;
						Gamma = parameters.Gamma;
						num = num2;
						Console.WriteLine(" New Maximum!");
					}
					else
					{
						Console.WriteLine();
					}
				}
			}
		}

		public static RegressionParameter[] GridRegression(Problem problem, Problem validation, Parameter parameter, List<double> CValues, List<double> GammaValues, List<double> PValues, string outputFile, int MaxBestCount, double IgnoreVolatility)
		{
			if (!string.IsNullOrEmpty(outputFile))
			{
				File.WriteAllText(outputFile, string.Empty);
			}
			List<RegressionParameter> list = new List<RegressionParameter>(CValues.Count * GammaValues.Count * PValues.Count);
			for (int i = 0; i < CValues.Count; i++)
			{
				for (int j = 0; j < GammaValues.Count; j++)
				{
					for (int k = 0; k < PValues.Count; k++)
					{
						parameter.C = CValues[i];
						parameter.Gamma = GammaValues[j];
						parameter.P = PValues[k];
						Model model = Training.Train(problem, parameter);
						double[] array = Prediction.Predict(validation, model);
						double num = (IgnoreVolatility > 0.0 && (Math.Abs(array[0] - array[1]) <= IgnoreVolatility || Math.Abs(array[1] - array[2]) <= IgnoreVolatility)) ? 1.7976931348623157E+308 : Prediction.ComputeMSE(validation, array, 0);
						Console.Write("{0} {1} {2} {3}", parameter.C, parameter.Gamma, parameter.P, num);
						if (!string.IsNullOrEmpty(outputFile))
						{
							File.AppendAllText(outputFile, string.Format("{0} {1} {2} {3}\n", parameter.C, parameter.Gamma, parameter.P, num));
						}
						RegressionParameter regressionParameter = new RegressionParameter(parameter.C, parameter.Gamma, parameter.P, num);
						regressionParameter.ValidatePredictions = array;
						list.Add(regressionParameter);
					}
				}
			}
			list.Sort();
			if (MaxBestCount > 0)
			{
				int l;
				for (l = Math.Min(MaxBestCount, list.Count); l < list.Count && l < MaxBestCount * 3 && list[l - 1].SortValue == list[l].SortValue; l++)
				{
				}
				if (list.Count > l)
				{
					list.RemoveRange(l, list.Count - l);
				}
				list.TrimExcess();
			}
			return list.ToArray();
		}
	}
}
