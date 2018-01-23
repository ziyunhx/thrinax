using System;
using System.Collections.Generic;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	[Serializable]
	public class PrecomputedKernel
	{
		private float[,] _similarities;

		private int _rows;

		private int _columns;

		public PrecomputedKernel(float[,] similarities)
		{
			this._similarities = similarities;
			this._rows = this._similarities.GetLength(0);
			this._columns = this._similarities.GetLength(1);
		}

		public PrecomputedKernel(List<Node[]> nodes, Parameter param)
		{
			this._rows = nodes.Count;
			this._columns = this._rows;
			this._similarities = new float[this._rows, this._columns];
			for (int i = 0; i < this._rows; i++)
			{
				for (int j = 0; j < i; j++)
				{
					float[,] similarities = this._similarities;
					int num = i;
					int num2 = j;
					float num3 = this._similarities[j, i];
					similarities[num, num2] = num3;
				}
				this._similarities[i, i] = 1f;
				for (int k = i + 1; k < this._columns; k++)
				{
					float[,] similarities2 = this._similarities;
					int num4 = i;
					int num5 = k;
					float num6 = (float)Kernel.KernelFunction(nodes[i], nodes[k], param);
					similarities2[num4, num5] = num6;
				}
			}
		}

		public PrecomputedKernel(List<Node[]> rows, List<Node[]> columns, Parameter param)
		{
			this._rows = rows.Count;
			this._columns = columns.Count;
			this._similarities = new float[this._rows, this._columns];
			for (int i = 0; i < this._rows; i++)
			{
				for (int j = 0; j < this._columns; j++)
				{
					float[,] similarities = this._similarities;
					int num = i;
					int num2 = j;
					float num3 = (float)Kernel.KernelFunction(rows[i], columns[j], param);
					similarities[num, num2] = num3;
				}
			}
		}

		public Problem Compute(double[] rowLabels, double[] columnLabels)
		{
			List<Node[]> list = new List<Node[]>();
			List<double> list2 = new List<double>();
			int num = 0;
			for (int i = 0; i < columnLabels.Length; i++)
			{
				if (columnLabels[i] != 0.0)
				{
					num++;
				}
			}
			num++;
			for (int j = 0; j < this._rows; j++)
			{
				if (rowLabels[j] != 0.0)
				{
					List<Node> list3 = new List<Node>();
					list3.Add(new Node(0, (double)(list.Count + 1)));
					for (int k = 0; k < this._columns; k++)
					{
						if (columnLabels[k] != 0.0)
						{
							double value = (double)this._similarities[j, k];
							list3.Add(new Node(list3.Count, value));
						}
					}
					list.Add(list3.ToArray());
					list2.Add(rowLabels[j]);
				}
			}
			return new Problem(list.Count, list2.ToArray(), list.ToArray(), num);
		}
	}
}
