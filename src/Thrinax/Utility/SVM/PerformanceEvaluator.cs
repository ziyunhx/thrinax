using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	public class PerformanceEvaluator
	{
		private class ChangePoint
		{
			public int TP;

			public int FP;

			public int TN;

			public int FN;

			public ChangePoint(int tp, int fp, int tn, int fn)
			{
				this.TP = tp;
				this.FP = fp;
				this.TN = tn;
				this.FN = fn;
			}

			public override string ToString()
			{
				return string.Format("{0}:{1}:{2}:{3}", this.TP, this.FP, this.TN, this.FN);
			}
		}

		private List<CurvePoint> _prCurve;

		private double _ap;

		private List<CurvePoint> _rocCurve;

		private double _auc;

		private List<RankPair> _data;

		private List<ChangePoint> _changes;

		public List<CurvePoint> ROCCurve
		{
			get
			{
				return this._rocCurve;
			}
		}

		public double AuC
		{
			get
			{
				return this._auc;
			}
		}

		public List<CurvePoint> PRCurve
		{
			get
			{
				return this._prCurve;
			}
		}

		public double AP
		{
			get
			{
				return this._ap;
			}
		}

		public PerformanceEvaluator(List<RankPair> set)
		{
			this._data = set;
			this.computeStatistics();
		}

		public PerformanceEvaluator(Model model, Problem problem, double category)
			: this(model, problem, category, "tmp.results")
		{
		}

		public PerformanceEvaluator(Model model, Problem problem, double category, string resultsFile)
		{
			Prediction.Predict(problem, resultsFile, model, true, 1);
			this.parseResultsFile(resultsFile, problem.Y, category);
			this.computeStatistics();
		}

		public PerformanceEvaluator(string resultsFile, double[] correctLabels, double category)
		{
			this.parseResultsFile(resultsFile, correctLabels, category);
			this.computeStatistics();
		}

		private void parseResultsFile(string resultsFile, double[] labels, double category)
		{
			StreamReader streamReader = new StreamReader(resultsFile);
			string[] array = streamReader.ReadLine().Split(new char[1]
			{
				' '
			}, StringSplitOptions.RemoveEmptyEntries);
			int num = -1;
			int num2 = 1;
			while (num2 < array.Length)
			{
				if (double.Parse(array[num2], CultureInfo.InvariantCulture) != category)
				{
					num2++;
					continue;
				}
				num = num2;
				break;
			}
			this._data = new List<RankPair>();
			for (int i = 0; i < labels.Length; i++)
			{
				array = streamReader.ReadLine().Split(new char[1]
				{
					' '
				}, StringSplitOptions.RemoveEmptyEntries);
				double score = double.Parse(array[num], CultureInfo.InvariantCulture);
				this._data.Add(new RankPair(score, (double)((labels[i] == category) ? 1 : 0)));
			}
			streamReader.Close();
		}

		private void computeStatistics()
		{
			this._data.Sort();
			this.findChanges();
			this.computePR();
			this.computeRoC();
		}

		private void findChanges()
		{
			int num3;
			int num2;
			int num;
			int num4 = num3 = (num2 = (num = 0));
			for (int i = 0; i < this._data.Count; i++)
			{
				if (this._data[i].Label == 1.0)
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
			this._changes = new List<ChangePoint>();
			for (int j = 0; j < this._data.Count; j++)
			{
				if (this._data[j].Label == 1.0)
				{
					num4++;
					num--;
				}
				else
				{
					num3++;
					num2--;
				}
				this._changes.Add(new ChangePoint(num4, num3, num2, num));
			}
		}

		private float computePrecision(ChangePoint p)
		{
			return (float)p.TP / (float)(p.TP + p.FP);
		}

		private float computeRecall(ChangePoint p)
		{
			return (float)p.TP / (float)(p.TP + p.FN);
		}

		private void computePR()
		{
			this._prCurve = new List<CurvePoint>();
			this._prCurve.Add(new CurvePoint(0f, 1f));
			float num = this.computePrecision(this._changes[0]);
			float x = this.computeRecall(this._changes[0]);
			float num2 = 0f;
			if (this._changes[0].TP > 0)
			{
				num2 += num;
				this._prCurve.Add(new CurvePoint(x, num));
			}
			for (int i = 1; i < this._changes.Count; i++)
			{
				num = this.computePrecision(this._changes[i]);
				x = this.computeRecall(this._changes[i]);
				if (this._changes[i].TP > this._changes[i - 1].TP)
				{
					num2 += num;
					this._prCurve.Add(new CurvePoint(x, num));
				}
			}
			this._prCurve.Add(new CurvePoint(1f, (float)(this._changes[0].TP + this._changes[0].FN) / (float)(this._changes[0].FP + this._changes[0].TN)));
			this._ap = (double)(num2 / (float)(this._changes[0].FN + this._changes[0].TP));
		}

		public void WritePRCurve(string filename)
		{
			StreamWriter streamWriter = new StreamWriter(filename);
			streamWriter.WriteLine(this._ap);
			for (int i = 0; i < this._prCurve.Count; i++)
			{
				streamWriter.WriteLine("{0}\t{1}", this._prCurve[i].X, this._prCurve[i].Y);
			}
			streamWriter.Close();
		}

		public void WriteROCCurve(string filename)
		{
			StreamWriter streamWriter = new StreamWriter(filename);
			streamWriter.WriteLine(this._auc);
			for (int i = 0; i < this._rocCurve.Count; i++)
			{
				streamWriter.WriteLine("{0}\t{1}", this._rocCurve[i].X, this._rocCurve[i].Y);
			}
			streamWriter.Close();
		}

		private float computeTPR(ChangePoint cp)
		{
			return this.computeRecall(cp);
		}

		private float computeFPR(ChangePoint cp)
		{
			return (float)cp.FP / (float)(cp.FP + cp.TN);
		}

		private void computeRoC()
		{
			this._rocCurve = new List<CurvePoint>();
			this._rocCurve.Add(new CurvePoint(0f, 0f));
			float num = this.computeTPR(this._changes[0]);
			float num2 = this.computeFPR(this._changes[0]);
			this._rocCurve.Add(new CurvePoint(num2, num));
			this._auc = 0.0;
			for (int i = 1; i < this._changes.Count; i++)
			{
				float num3 = this.computeTPR(this._changes[i]);
				float num4 = this.computeFPR(this._changes[i]);
				if (this._changes[i].TP > this._changes[i - 1].TP)
				{
					this._auc += (double)(num * (num4 - num2)) + 0.5 * (double)(num3 - num) * (double)(num4 - num2);
					num = num3;
					num2 = num4;
					this._rocCurve.Add(new CurvePoint(num2, num));
				}
			}
			this._rocCurve.Add(new CurvePoint(1f, 1f));
			this._auc += (double)(num * (1f - num2)) + 0.5 * (double)(1f - num) * (double)(1f - num2);
		}
	}
}
