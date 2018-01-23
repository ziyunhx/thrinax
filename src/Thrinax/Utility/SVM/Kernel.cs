using System;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	internal abstract class Kernel : IQMatrix
	{
		private Node[][] _x;

		private double[] _xSquare;

		private KernelType _kernelType;

		private int _degree;

		private double _gamma;

		private double _coef0;

		public abstract float[] GetQ(int column, int len);

		public abstract float[] GetQD();

		public virtual void SwapIndex(int i, int j)
		{
			this._x.SwapIndex(i, j);
			if (this._xSquare != null)
			{
				this._xSquare.SwapIndex(i, j);
			}
		}

		private static double powi(double value, int times)
		{
			double num = value;
			double num2 = 1.0;
			for (int num3 = times; num3 > 0; num3 /= 2)
			{
				if (num3 % 2 == 1)
				{
					num2 *= num;
				}
				num *= num;
			}
			return num2;
		}

		public double KernelFunction(int i, int j)
		{
			switch (this._kernelType)
			{
			case KernelType.LINEAR:
				return Kernel.dot(this._x[i], this._x[j]);
			case KernelType.POLY:
				return Kernel.powi(this._gamma * Kernel.dot(this._x[i], this._x[j]) + this._coef0, this._degree);
			case KernelType.RBF:
				return Math.Exp((0.0 - this._gamma) * (this._xSquare[i] + this._xSquare[j] - 2.0 * Kernel.dot(this._x[i], this._x[j])));
			case KernelType.SIGMOID:
				return Math.Tanh(this._gamma * Kernel.dot(this._x[i], this._x[j]) + this._coef0);
			case KernelType.PRECOMPUTED:
				return this._x[i][(int)this._x[j][0].Value].Value;
			default:
				return 0.0;
			}
		}

		public Kernel(int l, Node[][] x_, Parameter param)
		{
			this._kernelType = param.KernelType;
			this._degree = param.Degree;
			this._gamma = param.Gamma;
			this._coef0 = param.Coefficient0;
			this._x = (Node[][])x_.Clone();
			if (this._kernelType == KernelType.RBF)
			{
				this._xSquare = new double[l];
				for (int i = 0; i < l; i++)
				{
					this._xSquare[i] = Kernel.dot(this._x[i], this._x[i]);
				}
			}
			else
			{
				this._xSquare = null;
			}
		}

		private static double dot(Node[] xNodes, Node[] yNodes)
		{
			double num = 0.0;
			int num2 = xNodes.Length;
			int num3 = yNodes.Length;
			int num4 = 0;
			int num5 = 0;
			Node node = xNodes[0];
			Node node2 = yNodes[0];
			while (true)
			{
				if (node._index == node2._index)
				{
					num += node._value * node2._value;
					num4++;
					num5++;
					if (num4 < num2 && num5 < num3)
					{
						node = xNodes[num4];
						node2 = yNodes[num5];
						continue;
					}
					if (num4 < num2)
					{
						node = xNodes[num4];
					}
					else if (num5 < num3)
					{
						node2 = yNodes[num5];
					}
					break;
				}
				if (node._index > node2._index)
				{
					num5++;
					if (num5 >= num3)
					{
						break;
					}
					node2 = yNodes[num5];
				}
				else
				{
					num4++;
					if (num4 >= num2)
					{
						break;
					}
					node = xNodes[num4];
				}
			}
			return num;
		}

		private static double computeSquaredDistance(Node[] xNodes, Node[] yNodes)
		{
			Node node = xNodes[0];
			Node node2 = yNodes[0];
			int num = xNodes.Length;
			int num2 = yNodes.Length;
			int i = 0;
			int j = 0;
			double num3 = 0.0;
			while (true)
			{
				if (node._index == node2._index)
				{
					double num4 = node._value - node2._value;
					num3 += num4 * num4;
					i++;
					j++;
					if (i < num && j < num2)
					{
						node = xNodes[i];
						node2 = yNodes[j];
						continue;
					}
					if (i < num)
					{
						node = xNodes[i];
					}
					else if (j < num2)
					{
						node2 = yNodes[j];
					}
					break;
				}
				if (node._index > node2._index)
				{
					num3 += node2._value * node2._value;
					if (++j >= num2)
					{
						break;
					}
					node2 = yNodes[j];
				}
				else
				{
					num3 += node._value * node._value;
					if (++i >= num)
					{
						break;
					}
					node = xNodes[i];
				}
			}
			for (; i < num; i++)
			{
				double value = xNodes[i]._value;
				num3 += value * value;
			}
			for (; j < num2; j++)
			{
				double value2 = yNodes[j]._value;
				num3 += value2 * value2;
			}
			return num3;
		}

		public static double KernelFunction(Node[] x, Node[] y, Parameter param)
		{
			switch (param.KernelType)
			{
			case KernelType.LINEAR:
				return Kernel.dot(x, y);
			case KernelType.POLY:
				return Kernel.powi((double)param.Degree * Kernel.dot(x, y) + param.Coefficient0, param.Degree);
			case KernelType.RBF:
			{
				double num = Kernel.computeSquaredDistance(x, y);
				return Math.Exp((0.0 - param.Gamma) * num);
			}
			case KernelType.SIGMOID:
				return Math.Tanh(param.Gamma * Kernel.dot(x, y) + param.Coefficient0);
			case KernelType.PRECOMPUTED:
				return x[(int)y[0].Value].Value;
			default:
				return 0.0;
			}
		}
	}
}
