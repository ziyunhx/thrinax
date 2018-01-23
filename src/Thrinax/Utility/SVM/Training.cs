using System;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	public static class Training
	{
		public static bool IsVerbose
		{
			get
			{
				return Procedures.IsVerbose;
			}
			set
			{
				Procedures.IsVerbose = value;
			}
		}

		private static double doCrossValidation(Problem problem, Parameter parameters, int nr_fold)
		{
			double[] array = new double[problem.Count];
			Procedures.svm_cross_validation(problem, parameters, nr_fold, array);
			int num = 0;
			double num2 = 0.0;
			double num3 = 0.0;
			double num4 = 0.0;
			double num5 = 0.0;
			double num6 = 0.0;
			double num7 = 0.0;
			if (parameters.SvmType != SvmType.EPSILON_SVR && parameters.SvmType != SvmType.NU_SVR)
			{
				for (int i = 0; i < problem.Count; i++)
				{
					if (array[i] == problem.Y[i])
					{
						num++;
					}
				}
				return (double)num / (double)problem.Count;
			}
			for (int i = 0; i < problem.Count; i++)
			{
				double num8 = problem.Y[i];
				double num9 = array[i];
				num2 += (num9 - num8) * (num9 - num8);
				num3 += num9;
				num4 += num8;
				num5 += num9 * num9;
				num6 += num8 * num8;
				num7 += num9 * num8;
			}
			return ((double)problem.Count * num7 - num3 * num4) / (Math.Sqrt((double)problem.Count * num5 - num3 * num3) * Math.Sqrt((double)problem.Count * num6 - num4 * num4));
		}

		[Obsolete("Provided only for legacy compatibility, use the other Train() methods")]
		public static void Train(params string[] args)
		{
			Parameter parameters = default(Parameter);
			Problem problem = default(Problem);
			bool flag = default(bool);
			int nrfold = default(int);
			string filename = default(string);
			Training.parseCommandLine(args, out parameters, out problem, out flag, out nrfold, out filename);
			if (flag)
			{
				Training.PerformCrossValidation(problem, parameters, nrfold);
			}
			else
			{
				Model.Write(filename, Training.Train(problem, parameters));
			}
		}

		public static double PerformCrossValidation(Problem problem, Parameter parameters, int nrfold)
		{
			string text = Procedures.svm_check_parameter(problem, parameters);
			if (text == null)
			{
				return Training.doCrossValidation(problem, parameters, nrfold);
			}
			throw new Exception(text);
		}

		public static Model Train(Problem problem, Parameter parameters)
		{
			string text = Procedures.svm_check_parameter(problem, parameters);
			if (text == null)
			{
				return Procedures.svm_train(problem, parameters);
			}
			throw new Exception(text);
		}

		private static void parseCommandLine(string[] args, out Parameter parameters, out Problem problem, out bool crossValidation, out int nrfold, out string modelFilename)
		{
			parameters = new Parameter();
			crossValidation = false;
			nrfold = 0;
			int num = 0;
			while (num < args.Length && args[num][0] == '-')
			{
				num++;
				switch (args[num - 1][1])
				{
				case 's':
					parameters.SvmType = (SvmType)int.Parse(args[num]);
					break;
				case 't':
					parameters.KernelType = (KernelType)int.Parse(args[num]);
					break;
				case 'd':
					parameters.Degree = int.Parse(args[num]);
					break;
				case 'g':
					parameters.Gamma = double.Parse(args[num]);
					break;
				case 'r':
					parameters.Coefficient0 = double.Parse(args[num]);
					break;
				case 'n':
					parameters.Nu = double.Parse(args[num]);
					break;
				case 'm':
					parameters.CacheSize = double.Parse(args[num]);
					break;
				case 'c':
					parameters.C = double.Parse(args[num]);
					break;
				case 'e':
					parameters.EPS = double.Parse(args[num]);
					break;
				case 'p':
					parameters.P = double.Parse(args[num]);
					break;
				case 'h':
					parameters.Shrinking = (int.Parse(args[num]) == 1);
					break;
				case 'b':
					parameters.Probability = (int.Parse(args[num]) == 1);
					break;
				case 'v':
					crossValidation = true;
					nrfold = int.Parse(args[num]);
					if (nrfold >= 2)
					{
						break;
					}
					throw new ArgumentException("n-fold cross validation: n must >= 2");
				case 'w':
					parameters.Weights[int.Parse(args[num - 1].Substring(2))] = double.Parse(args[1]);
					break;
				default:
					throw new ArgumentException("Unknown Parameter");
				}
				num++;
			}
			if (num >= args.Length)
			{
				throw new ArgumentException("No input file specified");
			}
			problem = Problem.Read(args[num]);
			if (parameters.Gamma == 0.0)
			{
				parameters.Gamma = 1.0 / (double)problem.MaxIndex;
			}
			if (num < args.Length - 1)
			{
				modelFilename = args[num + 1];
			}
			else
			{
				int startIndex = args[num].LastIndexOf('/') + 1;
				modelFilename = args[num].Substring(startIndex) + ".model";
			}
		}
	}
}
