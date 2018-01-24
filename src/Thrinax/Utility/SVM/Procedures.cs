using System;
using System.IO;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	internal class Procedures
	{
		internal class decision_function
		{
			public double[] alpha;

			public double rho;
		}

		public const int LIBSVM_VERSION = 289;

		private static bool _verbose;

		public static TextWriter svm_print_string = Console.Out;

		public static bool IsVerbose
		{
			get
			{
				return Procedures._verbose;
			}
			set
			{
				Procedures._verbose = value;
			}
		}

		public static void info(string s)
		{
			if (Procedures._verbose)
			{
				Procedures.svm_print_string.Write(s);
			}
		}

		private static void solve_c_svc(Problem prob, Parameter param, double[] alpha, Solver.SolutionInfo si, double Cp, double Cn)
		{
			int count = prob.Count;
			double[] array = new double[count];
			sbyte[] array2 = new sbyte[count];
			for (int i = 0; i < count; i++)
			{
				alpha[i] = 0.0;
				array[i] = -1.0;
				if (prob.Y[i] > 0.0)
				{
					array2[i] = 1;
				}
				else
				{
					array2[i] = -1;
				}
			}
			Solver solver = new Solver();
			solver.Solve(count, new SVC_Q(prob, param, array2), array, array2, alpha, Cp, Cn, param.EPS, si, param.Shrinking);
			double num = 0.0;
			for (int i = 0; i < count; i++)
			{
				num += alpha[i];
			}
			if (Cp == Cn)
			{
				Procedures.info("nu = " + num / (Cp * (double)prob.Count) + "\n");
			}
			for (int i = 0; i < count; i++)
			{
				alpha[i] *= (double)array2[i];
			}
		}

		private static void solve_nu_svc(Problem prob, Parameter param, double[] alpha, Solver.SolutionInfo si)
		{
			int count = prob.Count;
			double nu = param.Nu;
			sbyte[] array = new sbyte[count];
			for (int i = 0; i < count; i++)
			{
				if (prob.Y[i] > 0.0)
				{
					array[i] = 1;
				}
				else
				{
					array[i] = -1;
				}
			}
			double num = nu * (double)count / 2.0;
			double num2 = nu * (double)count / 2.0;
			for (int i = 0; i < count; i++)
			{
				if (array[i] == 1)
				{
					alpha[i] = Math.Min(1.0, num);
					num -= alpha[i];
				}
				else
				{
					alpha[i] = Math.Min(1.0, num2);
					num2 -= alpha[i];
				}
			}
			double[] array2 = new double[count];
			for (int i = 0; i < count; i++)
			{
				array2[i] = 0.0;
			}
			Solver_NU solver_NU = new Solver_NU();
			solver_NU.Solve(count, new SVC_Q(prob, param, array), array2, array, alpha, 1.0, 1.0, param.EPS, si, param.Shrinking);
			double r = si.r;
			Procedures.info("C = " + 1.0 / r + "\n");
			for (int i = 0; i < count; i++)
			{
				alpha[i] *= (double)array[i] / r;
			}
			si.rho /= r;
			si.obj /= r * r;
			si.upper_bound_p = 1.0 / r;
			si.upper_bound_n = 1.0 / r;
		}

		private static void solve_one_class(Problem prob, Parameter param, double[] alpha, Solver.SolutionInfo si)
		{
			int count = prob.Count;
			double[] array = new double[count];
			sbyte[] array2 = new sbyte[count];
			int num = (int)(param.Nu * (double)prob.Count);
			for (int i = 0; i < num; i++)
			{
				alpha[i] = 1.0;
			}
			if (num < prob.Count)
			{
				alpha[num] = param.Nu * (double)prob.Count - (double)num;
			}
			for (int i = num + 1; i < count; i++)
			{
				alpha[i] = 0.0;
			}
			for (int i = 0; i < count; i++)
			{
				array[i] = 0.0;
				array2[i] = 1;
			}
			Solver solver = new Solver();
			solver.Solve(count, new ONE_CLASS_Q(prob, param), array, array2, alpha, 1.0, 1.0, param.EPS, si, param.Shrinking);
		}

		private static void solve_epsilon_svr(Problem prob, Parameter param, double[] alpha, Solver.SolutionInfo si)
		{
			int count = prob.Count;
			double[] array = new double[2 * count];
			double[] array2 = new double[2 * count];
			sbyte[] array3 = new sbyte[2 * count];
			for (int i = 0; i < count; i++)
			{
				array[i] = 0.0;
				array2[i] = param.P - prob.Y[i];
				array3[i] = 1;
				array[i + count] = 0.0;
				array2[i + count] = param.P + prob.Y[i];
				array3[i + count] = -1;
			}
			Solver solver = new Solver();
			solver.Solve(2 * count, new SVR_Q(prob, param), array2, array3, array, param.C, param.C, param.EPS, si, param.Shrinking);
			double num = 0.0;
			for (int i = 0; i < count; i++)
			{
				alpha[i] = array[i] - array[i + count];
				num += Math.Abs(alpha[i]);
			}
			Procedures.info("nu = " + num / (param.C * (double)count) + "\n");
		}

		private static void solve_nu_svr(Problem prob, Parameter param, double[] alpha, Solver.SolutionInfo si)
		{
			int count = prob.Count;
			double c = param.C;
			double[] array = new double[2 * count];
			double[] array2 = new double[2 * count];
			sbyte[] array3 = new sbyte[2 * count];
			double num = c * param.Nu * (double)count / 2.0;
			for (int i = 0; i < count; i++)
			{
				array[i] = (array[i + count] = Math.Min(num, c));
				num -= array[i];
				array2[i] = 0.0 - prob.Y[i];
				array3[i] = 1;
				array2[i + count] = prob.Y[i];
				array3[i + count] = -1;
			}
			Solver_NU solver_NU = new Solver_NU();
			solver_NU.Solve(2 * count, new SVR_Q(prob, param), array2, array3, array, c, c, param.EPS, si, param.Shrinking);
			Procedures.info("epsilon = " + (0.0 - si.r) + "\n");
			for (int i = 0; i < count; i++)
			{
				alpha[i] = array[i] - array[i + count];
			}
		}

		private static decision_function svm_train_one(Problem prob, Parameter param, double Cp, double Cn)
		{
			double[] array = new double[prob.Count];
			Solver.SolutionInfo solutionInfo = new Solver.SolutionInfo();
			switch (param.SvmType)
			{
			case SvmType.C_SVC:
				Procedures.solve_c_svc(prob, param, array, solutionInfo, Cp, Cn);
				break;
			case SvmType.NU_SVC:
				Procedures.solve_nu_svc(prob, param, array, solutionInfo);
				break;
			case SvmType.ONE_CLASS:
				Procedures.solve_one_class(prob, param, array, solutionInfo);
				break;
			case SvmType.EPSILON_SVR:
				Procedures.solve_epsilon_svr(prob, param, array, solutionInfo);
				break;
			case SvmType.NU_SVR:
				Procedures.solve_nu_svr(prob, param, array, solutionInfo);
				break;
			}
			Procedures.info("obj = " + solutionInfo.obj + ", rho = " + solutionInfo.rho + "\n");
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < prob.Count; i++)
			{
				if (Math.Abs(array[i]) > 0.0)
				{
					num++;
					if (prob.Y[i] > 0.0)
					{
						if (Math.Abs(array[i]) >= solutionInfo.upper_bound_p)
						{
							num2++;
						}
					}
					else if (Math.Abs(array[i]) >= solutionInfo.upper_bound_n)
					{
						num2++;
					}
				}
			}
			Procedures.info("nSV = " + num + ", nBSV = " + num2 + "\n");
			decision_function decision_function = new decision_function();
			decision_function.alpha = array;
			decision_function.rho = solutionInfo.rho;
			return decision_function;
		}

		private static void sigmoid_train(int l, double[] dec_values, double[] labels, double[] probAB)
		{
			double num = 0.0;
			double num2 = 0.0;
			for (int i = 0; i < l; i++)
			{
				if (labels[i] > 0.0)
				{
					num += 1.0;
				}
				else
				{
					num2 += 1.0;
				}
			}
			int num3 = 100;
			double num4 = 1E-10;
			double num5 = 1E-12;
			double num6 = 1E-05;
			double num7 = (num + 1.0) / (num + 2.0);
			double num8 = 1.0 / (num2 + 2.0);
			double[] array = new double[l];
			double num9 = 0.0;
			double num10 = Math.Log((num2 + 1.0) / (num + 1.0));
			double num11 = 0.0;
			for (int i = 0; i < l; i++)
			{
				if (labels[i] > 0.0)
				{
					array[i] = num7;
				}
				else
				{
					array[i] = num8;
				}
				double num12 = dec_values[i] * num9 + num10;
				num11 = ((!(num12 >= 0.0)) ? (num11 + ((array[i] - 1.0) * num12 + Math.Log(1.0 + Math.Exp(num12)))) : (num11 + (array[i] * num12 + Math.Log(1.0 + Math.Exp(0.0 - num12)))));
			}
			int j;
			for (j = 0; j < num3; j++)
			{
				double num13 = num5;
				double num14 = num5;
				double num15 = 0.0;
				double num16 = 0.0;
				double num17 = 0.0;
				for (int i = 0; i < l; i++)
				{
					double num12 = dec_values[i] * num9 + num10;
					double num18;
					double num19;
					if (num12 >= 0.0)
					{
						num18 = Math.Exp(0.0 - num12) / (1.0 + Math.Exp(0.0 - num12));
						num19 = 1.0 / (1.0 + Math.Exp(0.0 - num12));
					}
					else
					{
						num18 = 1.0 / (1.0 + Math.Exp(num12));
						num19 = Math.Exp(num12) / (1.0 + Math.Exp(num12));
					}
					double num20 = num18 * num19;
					num13 += dec_values[i] * dec_values[i] * num20;
					num14 += num20;
					num15 += dec_values[i] * num20;
					double num21 = array[i] - num18;
					num16 += dec_values[i] * num21;
					num17 += num21;
				}
				if (Math.Abs(num16) < num6 && Math.Abs(num17) < num6)
				{
					break;
				}
				double num22 = num13 * num14 - num15 * num15;
				double num23 = (0.0 - (num14 * num16 - num15 * num17)) / num22;
				double num24 = (0.0 - ((0.0 - num15) * num16 + num13 * num17)) / num22;
				double num25 = num16 * num23 + num17 * num24;
				double num26;
				for (num26 = 1.0; num26 >= num4; num26 /= 2.0)
				{
					double num27 = num9 + num26 * num23;
					double num28 = num10 + num26 * num24;
					double num29 = 0.0;
					for (int i = 0; i < l; i++)
					{
						double num12 = dec_values[i] * num27 + num28;
						num29 = ((!(num12 >= 0.0)) ? (num29 + ((array[i] - 1.0) * num12 + Math.Log(1.0 + Math.Exp(num12)))) : (num29 + (array[i] * num12 + Math.Log(1.0 + Math.Exp(0.0 - num12)))));
					}
					if (num29 < num11 + 0.0001 * num26 * num25)
					{
						num9 = num27;
						num10 = num28;
						num11 = num29;
						break;
					}
				}
				if (num26 < num4)
				{
					Procedures.info("Line search fails in two-class probability estimates\n");
					break;
				}
			}
			if (j >= num3)
			{
				Procedures.info("Reaching Maximal iterations in two-class probability estimates\n");
			}
			probAB[0] = num9;
			probAB[1] = num10;
		}

		private static double sigmoid_predict(double decision_value, double A, double B)
		{
			double num = decision_value * A + B;
			if (num >= 0.0)
			{
				return Math.Exp(0.0 - num) / (1.0 + Math.Exp(0.0 - num));
			}
			return 1.0 / (1.0 + Math.Exp(num));
		}

		private static void multiclass_probability(int k, double[,] r, double[] p)
		{
			int num = 0;
			int num2 = Math.Max(100, k);
			double[,] array = new double[k, k];
			double[] array2 = new double[k];
			double num3 = 0.005 / (double)k;
			for (int i = 0; i < k; i++)
			{
				p[i] = 1.0 / (double)k;
				array[i, i] = 0.0;
				for (int j = 0; j < i; j++)
				{
					array[i, i] += r[j, i] * r[j, i];
					double[,] array3 = array;
					int num4 = i;
					int num5 = j;
					double num6 = array[j, i];
					array3[num4, num5] = num6;
				}
				for (int j = i + 1; j < k; j++)
				{
					array[i, i] += r[j, i] * r[j, i];
					double[,] array4 = array;
					int num7 = i;
					int num8 = j;
					double num9 = (0.0 - r[j, i]) * r[i, j];
					array4[num7, num8] = num9;
				}
			}
			for (num = 0; num < num2; num++)
			{
				double num10 = 0.0;
				for (int i = 0; i < k; i++)
				{
					array2[i] = 0.0;
					for (int j = 0; j < k; j++)
					{
						array2[i] += array[i, j] * p[j];
					}
					num10 += p[i] * array2[i];
				}
				double num11 = 0.0;
				for (int i = 0; i < k; i++)
				{
					double num12 = Math.Abs(array2[i] - num10);
					if (num12 > num11)
					{
						num11 = num12;
					}
				}
				if (num11 < num3)
				{
					break;
				}
				for (int i = 0; i < k; i++)
				{
					double num13 = (0.0 - array2[i] + num10) / array[i, i];
					p[i] += num13;
					num10 = (num10 + num13 * (num13 * array[i, i] + 2.0 * array2[i])) / (1.0 + num13) / (1.0 + num13);
					for (int j = 0; j < k; j++)
					{
						array2[j] = (array2[j] + num13 * array[i, j]) / (1.0 + num13);
						p[j] /= 1.0 + num13;
					}
				}
			}
			if (num >= num2)
			{
				Procedures.info("Exceeds Max_iter in multiclass_prob\n");
			}
		}

		private static void svm_binary_svc_probability(Problem prob, Parameter param, double Cp, double Cn, double[] probAB)
		{
			int num = 5;
			int[] array = new int[prob.Count];
			double[] array2 = new double[prob.Count];
			Random random = new Random();
			for (int i = 0; i < prob.Count; i++)
			{
				array[i] = i;
			}
			for (int i = 0; i < prob.Count; i++)
			{
				int num2 = i + (int)(random.NextDouble() * (double)(prob.Count - i));
				int num3 = array[i];
				array[i] = array[num2];
				array[num2] = num3;
			}
			for (int i = 0; i < num; i++)
			{
				int num4 = i * prob.Count / num;
				int num5 = (i + 1) * prob.Count / num;
				Problem problem = new Problem();
				problem.Count = prob.Count - (num5 - num4);
				problem.X = new Node[problem.Count][];
				problem.Y = new double[problem.Count];
				int num6 = 0;
				for (int j = 0; j < num4; j++)
				{
					problem.X[num6] = prob.X[array[j]];
					problem.Y[num6] = prob.Y[array[j]];
					num6++;
				}
				for (int j = num5; j < prob.Count; j++)
				{
					problem.X[num6] = prob.X[array[j]];
					problem.Y[num6] = prob.Y[array[j]];
					num6++;
				}
				int num7 = 0;
				int num8 = 0;
				for (int j = 0; j < num6; j++)
				{
					if (problem.Y[j] > 0.0)
					{
						num7++;
					}
					else
					{
						num8++;
					}
				}
				if (num7 == 0 && num8 == 0)
				{
					for (int j = num4; j < num5; j++)
					{
						array2[array[j]] = 0.0;
					}
				}
				else if (num7 > 0 && num8 == 0)
				{
					for (int j = num4; j < num5; j++)
					{
						array2[array[j]] = 1.0;
					}
				}
				else if (num7 == 0 && num8 > 0)
				{
					for (int j = num4; j < num5; j++)
					{
						array2[array[j]] = -1.0;
					}
				}
				else
				{
					Parameter parameter = (Parameter)param.Clone();
					parameter.Probability = false;
					parameter.C = 1.0;
					parameter.Weights[1] = Cp;
					parameter.Weights[-1] = Cn;
					Model model = Procedures.svm_train(problem, parameter);
					for (int j = num4; j < num5; j++)
					{
						double[] array3 = new double[1];
						Procedures.svm_predict_values(model, prob.X[array[j]], array3);
						array2[array[j]] = array3[0];
						array2[array[j]] *= (double)model.ClassLabels[0];
					}
				}
			}
			Procedures.sigmoid_train(prob.Count, array2, prob.Y, probAB);
		}

		private static double svm_svr_probability(Problem prob, Parameter param)
		{
			int nr_fold = 5;
			double[] array = new double[prob.Count];
			double num = 0.0;
			Parameter parameter = (Parameter)param.Clone();
			parameter.Probability = false;
			Procedures.svm_cross_validation(prob, parameter, nr_fold, array);
			for (int i = 0; i < prob.Count; i++)
			{
				array[i] = prob.Y[i] - array[i];
				num += Math.Abs(array[i]);
			}
			num /= (double)prob.Count;
			double num2 = Math.Sqrt(2.0 * num * num);
			int num3 = 0;
			num = 0.0;
			for (int i = 0; i < prob.Count; i++)
			{
				if (Math.Abs(array[i]) > 5.0 * num2)
				{
					num3++;
				}
				else
				{
					num += Math.Abs(array[i]);
				}
			}
			num /= (double)(prob.Count - num3);
			Procedures.info("Prob. model for test data: target value = predicted value + z,\nz: Laplace distribution e^(-|z|/sigma)/(2sigma),sigma=" + num + "\n");
			return num;
		}

		private static void svm_group_classes(Problem prob, int[] nr_class_ret, int[][] label_ret, int[][] start_ret, int[][] count_ret, int[] perm)
		{
			int count = prob.Count;
			int num = 16;
			int num2 = 0;
			int[] array = new int[num];
			int[] array2 = new int[num];
			int[] array3 = new int[count];
			for (int i = 0; i < count; i++)
			{
				int num3 = (int)prob.Y[i];
				int num4 = 0;
				while (num4 < num2)
				{
					if (num3 != array[num4])
					{
						num4++;
						continue;
					}
					array2[num4]++;
					break;
				}
				array3[i] = num4;
				if (num4 == num2)
				{
					if (num2 == num)
					{
						num *= 2;
						int[] array4 = new int[num];
						Array.Copy(array, 0, array4, 0, array.Length);
						array = array4;
						array4 = new int[num];
						Array.Copy(array2, 0, array4, 0, array2.Length);
						array2 = array4;
					}
					array[num2] = num3;
					array2[num2] = 1;
					num2++;
				}
			}
			int[] array5 = new int[num2];
			array5[0] = 0;
			for (int i = 1; i < num2; i++)
			{
				array5[i] = array5[i - 1] + array2[i - 1];
			}
			for (int i = 0; i < count; i++)
			{
				perm[array5[array3[i]]] = i;
				array5[array3[i]]++;
			}
			array5[0] = 0;
			for (int i = 1; i < num2; i++)
			{
				array5[i] = array5[i - 1] + array2[i - 1];
			}
			nr_class_ret[0] = num2;
			label_ret[0] = array;
			start_ret[0] = array5;
			count_ret[0] = array2;
		}

		public static Model svm_train(Problem prob, Parameter param)
		{
			Model model = new Model();
			model.Parameter = param;
			if (param.SvmType == SvmType.ONE_CLASS || param.SvmType == SvmType.EPSILON_SVR || param.SvmType == SvmType.NU_SVR)
			{
				model.NumberOfClasses = 2;
				model.ClassLabels = null;
				model.NumberOfSVPerClass = null;
				model.PairwiseProbabilityA = null;
				model.PairwiseProbabilityB = null;
				model.SupportVectorCoefficients = new double[1][];
				if (param.Probability && (param.SvmType == SvmType.EPSILON_SVR || param.SvmType == SvmType.NU_SVR))
				{
					model.PairwiseProbabilityA = new double[1];
					model.PairwiseProbabilityA[0] = Procedures.svm_svr_probability(prob, param);
				}
				decision_function decision_function = Procedures.svm_train_one(prob, param, 0.0, 0.0);
				model.Rho = new double[1];
				model.Rho[0] = decision_function.rho;
				int num = 0;
				for (int i = 0; i < prob.Count; i++)
				{
					if (Math.Abs(decision_function.alpha[i]) > 0.0)
					{
						num++;
					}
				}
				model.SupportVectorCount = num;
				model.SupportVectors = new Node[num][];
				model.SupportVectorCoefficients[0] = new double[num];
				int num2 = 0;
				for (int i = 0; i < prob.Count; i++)
				{
					if (Math.Abs(decision_function.alpha[i]) > 0.0)
					{
						model.SupportVectors[num2] = prob.X[i];
						model.SupportVectorCoefficients[0][num2] = decision_function.alpha[i];
						num2++;
					}
				}
			}
			else
			{
				int count = prob.Count;
				int[] array = new int[1];
				int[][] array2 = new int[1][];
				int[][] array3 = new int[1][];
				int[][] array4 = new int[1][];
				int[] array5 = new int[count];
				Procedures.svm_group_classes(prob, array, array2, array3, array4, array5);
				int num3 = array[0];
				int[] array6 = array2[0];
				int[] array7 = array3[0];
				int[] array8 = array4[0];
				Node[][] array9 = new Node[count][];
				for (int j = 0; j < count; j++)
				{
					array9[j] = prob.X[array5[j]];
				}
				double[] array10 = new double[num3];
				for (int j = 0; j < num3; j++)
				{
					array10[j] = param.C;
				}
				foreach (int key in param.Weights.Keys)
				{
					int num4 = Array.IndexOf(array6, key);
					if (num4 < 0)
					{
						Console.Error.WriteLine("warning: class label " + key + " specified in weight is not found");
					}
					else
					{
						array10[num4] *= param.Weights[key];
					}
				}
				bool[] array11 = new bool[count];
				for (int j = 0; j < count; j++)
				{
					array11[j] = false;
				}
				decision_function[] array12 = new decision_function[num3 * (num3 - 1) / 2];
				double[] array13 = null;
				double[] array14 = null;
				if (param.Probability)
				{
					array13 = new double[num3 * (num3 - 1) / 2];
					array14 = new double[num3 * (num3 - 1) / 2];
				}
				int num5 = 0;
				for (int j = 0; j < num3; j++)
				{
					for (int k = j + 1; k < num3; k++)
					{
						Problem problem = new Problem();
						int num6 = array7[j];
						int num7 = array7[k];
						int num8 = array8[j];
						int num9 = array8[k];
						problem.Count = num8 + num9;
						problem.X = new Node[problem.Count][];
						problem.Y = new double[problem.Count];
						for (int l = 0; l < num8; l++)
						{
							problem.X[l] = array9[num6 + l];
							problem.Y[l] = 1.0;
						}
						for (int l = 0; l < num9; l++)
						{
							problem.X[num8 + l] = array9[num7 + l];
							problem.Y[num8 + l] = -1.0;
						}
						if (param.Probability)
						{
							double[] array15 = new double[2];
							Procedures.svm_binary_svc_probability(problem, param, array10[j], array10[k], array15);
							array13[num5] = array15[0];
							array14[num5] = array15[1];
						}
						array12[num5] = Procedures.svm_train_one(problem, param, array10[j], array10[k]);
						for (int l = 0; l < num8; l++)
						{
							if (!array11[num6 + l] && Math.Abs(array12[num5].alpha[l]) > 0.0)
							{
								array11[num6 + l] = true;
							}
						}
						for (int l = 0; l < num9; l++)
						{
							if (!array11[num7 + l] && Math.Abs(array12[num5].alpha[num8 + l]) > 0.0)
							{
								array11[num7 + l] = true;
							}
						}
						num5++;
					}
				}
				model.NumberOfClasses = num3;
				model.ClassLabels = new int[num3];
				for (int j = 0; j < num3; j++)
				{
					model.ClassLabels[j] = array6[j];
				}
				model.Rho = new double[num3 * (num3 - 1) / 2];
				for (int j = 0; j < num3 * (num3 - 1) / 2; j++)
				{
					model.Rho[j] = array12[j].rho;
				}
				if (param.Probability)
				{
					model.PairwiseProbabilityA = new double[num3 * (num3 - 1) / 2];
					model.PairwiseProbabilityB = new double[num3 * (num3 - 1) / 2];
					for (int j = 0; j < num3 * (num3 - 1) / 2; j++)
					{
						model.PairwiseProbabilityA[j] = array13[j];
						model.PairwiseProbabilityB[j] = array14[j];
					}
				}
				else
				{
					model.PairwiseProbabilityA = null;
					model.PairwiseProbabilityB = null;
				}
				int num10 = 0;
				int[] array16 = new int[num3];
				model.NumberOfSVPerClass = new int[num3];
				for (int j = 0; j < num3; j++)
				{
					int num11 = 0;
					for (int m = 0; m < array8[j]; m++)
					{
						if (array11[array7[j] + m])
						{
							num11++;
							num10++;
						}
					}
					model.NumberOfSVPerClass[j] = num11;
					array16[j] = num11;
				}
				Procedures.info("Total nSV = " + num10 + "\n");
				model.SupportVectorCount = num10;
				model.SupportVectors = new Node[num10][];
				num5 = 0;
				for (int j = 0; j < count; j++)
				{
					if (array11[j])
					{
						model.SupportVectors[num5++] = array9[j];
					}
				}
				int[] array17 = new int[num3];
				array17[0] = 0;
				for (int j = 1; j < num3; j++)
				{
					array17[j] = array17[j - 1] + array16[j - 1];
				}
				model.SupportVectorCoefficients = new double[num3 - 1][];
				for (int j = 0; j < num3 - 1; j++)
				{
					model.SupportVectorCoefficients[j] = new double[num10];
				}
				num5 = 0;
				for (int j = 0; j < num3; j++)
				{
					for (int n = j + 1; n < num3; n++)
					{
						int num13 = array7[j];
						int num14 = array7[n];
						int num15 = array8[j];
						int num16 = array8[n];
						int num17 = array17[j];
						for (int num18 = 0; num18 < num15; num18++)
						{
							if (array11[num13 + num18])
							{
								model.SupportVectorCoefficients[n - 1][num17++] = array12[num5].alpha[num18];
							}
						}
						num17 = array17[n];
						for (int num18 = 0; num18 < num16; num18++)
						{
							if (array11[num14 + num18])
							{
								model.SupportVectorCoefficients[j][num17++] = array12[num5].alpha[num15 + num18];
							}
						}
						num5++;
					}
				}
			}
			return model;
		}

		public static void svm_cross_validation(Problem prob, Parameter param, int nr_fold, double[] target)
		{
			Random random = new Random();
			int[] array = new int[nr_fold + 1];
			int count = prob.Count;
			int[] array2 = new int[count];
			if ((param.SvmType == SvmType.C_SVC || param.SvmType == SvmType.NU_SVC) && nr_fold < count)
			{
				int[] array3 = new int[1];
				int[][] array4 = new int[1][];
				int[][] array5 = new int[1][];
				int[][] array6 = new int[1][];
				Procedures.svm_group_classes(prob, array3, array4, array5, array6, array2);
				int num = array3[0];
				int[] array11 = array4[0];
				int[] array7 = array5[0];
				int[] array8 = array6[0];
				int[] array9 = new int[nr_fold];
				int[] array10 = new int[count];
				for (int i = 0; i < count; i++)
				{
					array10[i] = array2[i];
				}
				for (int j = 0; j < num; j++)
				{
					for (int i = 0; i < array8[j]; i++)
					{
						int num2 = i + (int)(random.NextDouble() * (double)(array8[j] - i));
						int num3 = array10[array7[j] + num2];
						array10[array7[j] + num2] = array10[array7[j] + i];
						array10[array7[j] + i] = num3;
					}
				}
				for (int i = 0; i < nr_fold; i++)
				{
					array9[i] = 0;
					for (int j = 0; j < num; j++)
					{
						array9[i] += (i + 1) * array8[j] / nr_fold - i * array8[j] / nr_fold;
					}
				}
				array[0] = 0;
				for (int i = 1; i <= nr_fold; i++)
				{
					array[i] = array[i - 1] + array9[i - 1];
				}
				for (int j = 0; j < num; j++)
				{
					for (int i = 0; i < nr_fold; i++)
					{
						int num4 = array7[j] + i * array8[j] / nr_fold;
						int num5 = array7[j] + (i + 1) * array8[j] / nr_fold;
						for (int k = num4; k < num5; k++)
						{
							array2[array[i]] = array10[k];
							array[i]++;
						}
					}
				}
				array[0] = 0;
				for (int i = 1; i <= nr_fold; i++)
				{
					array[i] = array[i - 1] + array9[i - 1];
				}
				goto IL_025b;
			}
			for (int i = 0; i < count; i++)
			{
				array2[i] = i;
			}
			for (int i = 0; i < count; i++)
			{
				int num6 = i + (int)(random.NextDouble() * (double)(count - i));
				int num7 = array2[i];
				array2[i] = array2[num6];
				array2[num6] = num7;
			}
			for (int i = 0; i <= nr_fold; i++)
			{
				array[i] = i * count / nr_fold;
			}
			goto IL_025b;
			IL_025b:
			for (int i = 0; i < nr_fold; i++)
			{
				int num8 = array[i];
				int num9 = array[i + 1];
				Problem problem = new Problem();
				problem.Count = count - (num9 - num8);
				problem.X = new Node[problem.Count][];
				problem.Y = new double[problem.Count];
				int num10 = 0;
				for (int l = 0; l < num8; l++)
				{
					problem.X[num10] = prob.X[array2[l]];
					problem.Y[num10] = prob.Y[array2[l]];
					num10++;
				}
				for (int l = num9; l < count; l++)
				{
					problem.X[num10] = prob.X[array2[l]];
					problem.Y[num10] = prob.Y[array2[l]];
					num10++;
				}
				Model model = Procedures.svm_train(problem, param);
				if (param.Probability && (param.SvmType == SvmType.C_SVC || param.SvmType == SvmType.NU_SVC))
				{
					double[] prob_estimates = new double[Procedures.svm_get_nr_class(model)];
					for (int l = num8; l < num9; l++)
					{
						target[array2[l]] = Procedures.svm_predict_probability(model, prob.X[array2[l]], prob_estimates);
					}
				}
				else
				{
					for (int l = num8; l < num9; l++)
					{
						target[array2[l]] = Procedures.svm_predict(model, prob.X[array2[l]]);
					}
				}
			}
		}

		public static SvmType svm_get_svm_type(Model model)
		{
			return model.Parameter.SvmType;
		}

		public static int svm_get_nr_class(Model model)
		{
			return model.NumberOfClasses;
		}

		public static void svm_get_labels(Model model, int[] label)
		{
			if (model.ClassLabels != null)
			{
				for (int i = 0; i < model.NumberOfClasses; i++)
				{
					label[i] = model.ClassLabels[i];
				}
			}
		}

		public static double svm_get_svr_probability(Model model)
		{
			if ((model.Parameter.SvmType == SvmType.EPSILON_SVR || model.Parameter.SvmType == SvmType.NU_SVR) && model.PairwiseProbabilityA != null)
			{
				return model.PairwiseProbabilityA[0];
			}
			Console.Error.WriteLine("Model doesn't contain information for SVR probability inference");
			return 0.0;
		}

		public static void svm_predict_values(Model model, Node[] x, double[] dec_values)
		{
			if (model.Parameter.SvmType == SvmType.ONE_CLASS || model.Parameter.SvmType == SvmType.EPSILON_SVR || model.Parameter.SvmType == SvmType.NU_SVR)
			{
				double[] array = model.SupportVectorCoefficients[0];
				double num = 0.0;
				for (int i = 0; i < model.SupportVectorCount; i++)
				{
					num += array[i] * Kernel.KernelFunction(x, model.SupportVectors[i], model.Parameter);
				}
				num = (dec_values[0] = num - model.Rho[0]);
			}
			else
			{
				int numberOfClasses = model.NumberOfClasses;
				int supportVectorCount = model.SupportVectorCount;
				double[] array2 = new double[supportVectorCount];
				for (int j = 0; j < supportVectorCount; j++)
				{
					array2[j] = Kernel.KernelFunction(x, model.SupportVectors[j], model.Parameter);
				}
				int[] array3 = new int[numberOfClasses];
				array3[0] = 0;
				for (int j = 1; j < numberOfClasses; j++)
				{
					array3[j] = array3[j - 1] + model.NumberOfSVPerClass[j - 1];
				}
				int num2 = 0;
				for (int j = 0; j < numberOfClasses; j++)
				{
					for (int k = j + 1; k < numberOfClasses; k++)
					{
						double num3 = 0.0;
						int num4 = array3[j];
						int num5 = array3[k];
						int num6 = model.NumberOfSVPerClass[j];
						int num7 = model.NumberOfSVPerClass[k];
						double[] array4 = model.SupportVectorCoefficients[k - 1];
						double[] array5 = model.SupportVectorCoefficients[j];
						for (int l = 0; l < num6; l++)
						{
							num3 += array4[num4 + l] * array2[num4 + l];
						}
						for (int l = 0; l < num7; l++)
						{
							num3 += array5[num5 + l] * array2[num5 + l];
						}
						num3 = (dec_values[num2] = num3 - model.Rho[num2]);
						num2++;
					}
				}
			}
		}

		public static double svm_predict(Model model, Node[] x)
		{
			if (model.Parameter.SvmType != SvmType.ONE_CLASS && model.Parameter.SvmType != SvmType.EPSILON_SVR && model.Parameter.SvmType != SvmType.NU_SVR)
			{
				int numberOfClasses = model.NumberOfClasses;
				double[] array = new double[numberOfClasses * (numberOfClasses - 1) / 2];
				Procedures.svm_predict_values(model, x, array);
				int[] array2 = new int[numberOfClasses];
				for (int i = 0; i < numberOfClasses; i++)
				{
					array2[i] = 0;
				}
				int num = 0;
				for (int i = 0; i < numberOfClasses; i++)
				{
					for (int j = i + 1; j < numberOfClasses; j++)
					{
						if (array[num++] > 0.0)
						{
							array2[i]++;
						}
						else
						{
							array2[j]++;
						}
					}
				}
				int num3 = 0;
				for (int i = 1; i < numberOfClasses; i++)
				{
					if (array2[i] > array2[num3])
					{
						num3 = i;
					}
				}
				return (double)model.ClassLabels[num3];
			}
			double[] array3 = new double[1];
			Procedures.svm_predict_values(model, x, array3);
			if (model.Parameter.SvmType == SvmType.ONE_CLASS)
			{
				return (double)((array3[0] > 0.0) ? 1 : (-1));
			}
			return array3[0];
		}

		public static void svm_predict_multi(Model model, Node[] x, out int[] Score)
		{
			if (model.Parameter.SvmType != 0)
			{
				throw new Exception("Model type " + model.Parameter.SvmType + " unable to predict multi classes.");
			}
			int numberOfClasses = model.NumberOfClasses;
			double[] array = new double[numberOfClasses * (numberOfClasses - 1) / 2];
			Procedures.svm_predict_values(model, x, array);
			int[] array2 = new int[numberOfClasses];
			for (int i = 0; i < numberOfClasses; i++)
			{
				array2[i] = 0;
			}
			int num = 0;
			for (int i = 0; i < numberOfClasses; i++)
			{
				for (int j = i + 1; j < numberOfClasses; j++)
				{
					if (array[num++] > 0.0)
					{
						array2[i]++;
					}
					else
					{
						array2[j]++;
					}
				}
			}
			Score = array2;
		}

		public static double svm_predict_probability(Model model, Node[] x, double[] prob_estimates)
		{
			if ((model.Parameter.SvmType == SvmType.C_SVC || model.Parameter.SvmType == SvmType.NU_SVC) && model.PairwiseProbabilityA != null && model.PairwiseProbabilityB != null)
			{
				int numberOfClasses = model.NumberOfClasses;
				double[] array = new double[numberOfClasses * (numberOfClasses - 1) / 2];
				Procedures.svm_predict_values(model, x, array);
				double num = 1E-07;
				double[,] array2 = new double[numberOfClasses, numberOfClasses];
				int num2 = 0;
				for (int i = 0; i < numberOfClasses; i++)
				{
					for (int j = i + 1; j < numberOfClasses; j++)
					{
						double[,] array3 = array2;
						int num3 = i;
						int num4 = j;
						double num5 = Math.Min(Math.Max(Procedures.sigmoid_predict(array[num2], model.PairwiseProbabilityA[num2], model.PairwiseProbabilityB[num2]), num), 1.0 - num);
						array3[num3, num4] = num5;
						double[,] array4 = array2;
						int num6 = j;
						int num7 = i;
						double num8 = 1.0 - array2[i, j];
						array4[num6, num7] = num8;
						num2++;
					}
				}
				Procedures.multiclass_probability(numberOfClasses, array2, prob_estimates);
				int num9 = 0;
				for (int i = 1; i < numberOfClasses; i++)
				{
					if (prob_estimates[i] > prob_estimates[num9])
					{
						num9 = i;
					}
				}
				return (double)model.ClassLabels[num9];
			}
			return Procedures.svm_predict(model, x);
		}

		public static string svm_check_parameter(Problem prob, Parameter param)
		{
			SvmType svmType = param.SvmType;
			KernelType kernelType = param.KernelType;
			if (param.Degree < 0)
			{
				return "degree of polynomial kernel < 0";
			}
			if (param.CacheSize <= 0.0)
			{
				return "cache_size <= 0";
			}
			if (param.EPS <= 0.0)
			{
				return "eps <= 0";
			}
			if (param.Gamma == 0.0)
			{
				param.Gamma = 1.0 / (double)prob.MaxIndex;
			}
			if ((svmType == SvmType.C_SVC || svmType == SvmType.EPSILON_SVR || svmType == SvmType.NU_SVR) && param.C <= 0.0)
			{
				return "C <= 0";
			}
			if (svmType != SvmType.NU_SVC && svmType != SvmType.ONE_CLASS && svmType != SvmType.NU_SVR)
			{
				goto IL_00c9;
			}
			if (!(param.Nu <= 0.0) && !(param.Nu > 1.0))
			{
				goto IL_00c9;
			}
			return "nu <= 0 or nu > 1";
			IL_00c9:
			if (svmType == SvmType.EPSILON_SVR && param.P < 0.0)
			{
				return "p < 0";
			}
			if (param.Probability && svmType == SvmType.ONE_CLASS)
			{
				return "one-class SVM probability output not supported yet";
			}
			if (svmType == SvmType.NU_SVC)
			{
				int count = prob.Count;
				int num = 16;
				int num2 = 0;
				int[] array = new int[num];
				int[] array2 = new int[num];
				for (int i = 0; i < count; i++)
				{
					int num3 = (int)prob.Y[i];
					int num4 = 0;
					while (num4 < num2)
					{
						if (num3 != array[num4])
						{
							num4++;
							continue;
						}
						array2[num4]++;
						break;
					}
					if (num4 == num2)
					{
						if (num2 == num)
						{
							num *= 2;
							int[] array3 = new int[num];
							Array.Copy(array, 0, array3, 0, array.Length);
							array = array3;
							array3 = new int[num];
							Array.Copy(array2, 0, array3, 0, array2.Length);
							array2 = array3;
						}
						array[num2] = num3;
						array2[num2] = 1;
						num2++;
					}
				}
				for (int i = 0; i < num2; i++)
				{
					int num5 = array2[i];
					for (int j = i + 1; j < num2; j++)
					{
						int num6 = array2[j];
						if (param.Nu * (double)(num5 + num6) / 2.0 > (double)Math.Min(num5, num6))
						{
							return "specified nu is infeasible";
						}
					}
				}
			}
			return null;
		}

		public static int svm_check_probability_model(Model model)
		{
			if ((model.Parameter.SvmType == SvmType.C_SVC || model.Parameter.SvmType == SvmType.NU_SVC) && model.PairwiseProbabilityA != null && model.PairwiseProbabilityB != null)
			{
				goto IL_004f;
			}
			if ((model.Parameter.SvmType == SvmType.EPSILON_SVR || model.Parameter.SvmType == SvmType.NU_SVR) && model.PairwiseProbabilityA != null)
			{
				goto IL_004f;
			}
			return 0;
			IL_004f:
			return 1;
		}
	}
}
