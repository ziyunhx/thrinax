using System;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	internal class Solver_NU : Solver
	{
		private SolutionInfo si;

		public sealed override void Solve(int l, IQMatrix Q, double[] p, sbyte[] y, double[] alpha, double Cp, double Cn, double eps, SolutionInfo si, bool shrinking)
		{
			this.si = si;
			base.Solve(l, Q, p, y, alpha, Cp, Cn, eps, si, shrinking);
		}

		private int select_working_set(int[] working_set)
		{
			double num = double.NegativeInfinity;
			double num2 = double.NegativeInfinity;
			int num3 = -1;
			double num4 = double.NegativeInfinity;
			double num5 = double.NegativeInfinity;
			int num6 = -1;
			int num7 = -1;
			double num8 = double.PositiveInfinity;
			for (int i = 0; i < base.active_size; i++)
			{
				if (base.y[i] == 1)
				{
					if (!base.is_upper_bound(i) && 0.0 - base.G[i] >= num)
					{
						num = 0.0 - base.G[i];
						num3 = i;
					}
				}
				else if (!base.is_lower_bound(i) && base.G[i] >= num4)
				{
					num4 = base.G[i];
					num6 = i;
				}
			}
			int num9 = num3;
			int num10 = num6;
			float[] array = null;
			float[] array2 = null;
			if (num9 != -1)
			{
				array = base.Q.GetQ(num9, base.active_size);
			}
			if (num10 != -1)
			{
				array2 = base.Q.GetQ(num10, base.active_size);
			}
			for (int j = 0; j < base.active_size; j++)
			{
				if (base.y[j] == 1)
				{
					if (!base.is_lower_bound(j))
					{
						double num11 = num + base.G[j];
						if (base.G[j] >= num2)
						{
							num2 = base.G[j];
						}
						if (num11 > 0.0)
						{
							double num12 = (double)(array[num9] + base.QD[j] - 2f * array[j]);
							double num13 = (!(num12 > 0.0)) ? ((0.0 - num11 * num11) / 1E-12) : ((0.0 - num11 * num11) / num12);
							if (num13 <= num8)
							{
								num7 = j;
								num8 = num13;
							}
						}
					}
				}
				else if (!base.is_upper_bound(j))
				{
					double num14 = num4 - base.G[j];
					if (0.0 - base.G[j] >= num5)
					{
						num5 = 0.0 - base.G[j];
					}
					if (num14 > 0.0)
					{
						double num15 = (double)(array2[num10] + base.QD[j] - 2f * array2[j]);
						double num16 = (!(num15 > 0.0)) ? ((0.0 - num14 * num14) / 1E-12) : ((0.0 - num14 * num14) / num15);
						if (num16 <= num8)
						{
							num7 = j;
							num8 = num16;
						}
					}
				}
			}
			if (Math.Max(num + num2, num4 + num5) < base.EPS)
			{
				return 1;
			}
			if (base.y[num7] == 1)
			{
				working_set[0] = num3;
			}
			else
			{
				working_set[0] = num6;
			}
			working_set[1] = num7;
			return 0;
		}

		private bool be_shrunk(int i, double GMax1, double GMax2, double GMax3, double GMax4)
		{
			if (base.is_upper_bound(i))
			{
				if (base.y[i] == 1)
				{
					return 0.0 - base.G[i] > GMax1;
				}
				return 0.0 - base.G[i] > GMax4;
			}
			if (base.is_lower_bound(i))
			{
				if (base.y[i] == 1)
				{
					return base.G[i] > GMax2;
				}
				return base.G[i] > GMax3;
			}
			return false;
		}

		private void do_shrinking()
		{
			double num = double.NegativeInfinity;
			double num2 = double.NegativeInfinity;
			double num3 = double.NegativeInfinity;
			double num4 = double.NegativeInfinity;
			for (int i = 0; i < base.active_size; i++)
			{
				if (!base.is_upper_bound(i))
				{
					if (base.y[i] == 1)
					{
						if (0.0 - base.G[i] > num)
						{
							num = 0.0 - base.G[i];
						}
					}
					else if (0.0 - base.G[i] > num4)
					{
						num4 = 0.0 - base.G[i];
					}
				}
				if (!base.is_lower_bound(i))
				{
					if (base.y[i] == 1)
					{
						if (base.G[i] > num2)
						{
							num2 = base.G[i];
						}
					}
					else if (base.G[i] > num3)
					{
						num3 = base.G[i];
					}
				}
			}
			if (!base.unshrink && Math.Max(num + num2, num3 + num4) <= base.EPS * 10.0)
			{
				base.unshrink = true;
				base.reconstruct_gradient();
				base.active_size = base.l;
			}
			for (int i = 0; i < base.active_size; i++)
			{
				if (this.be_shrunk(i, num, num2, num3, num4))
				{
					base.active_size--;
					while (base.active_size > i)
					{
						if (this.be_shrunk(base.active_size, num, num2, num3, num4))
						{
							base.active_size--;
							continue;
						}
						base.swap_index(i, base.active_size);
						break;
					}
				}
			}
		}

		private double calculate_rho()
		{
			int num = 0;
			int num2 = 0;
			double num3 = double.PositiveInfinity;
			double num4 = double.PositiveInfinity;
			double num5 = double.NegativeInfinity;
			double num6 = double.NegativeInfinity;
			double num7 = 0.0;
			double num8 = 0.0;
			for (int i = 0; i < base.active_size; i++)
			{
				if (base.y[i] == 1)
				{
					if (base.is_lower_bound(i))
					{
						num3 = Math.Min(num3, base.G[i]);
					}
					else if (base.is_upper_bound(i))
					{
						num5 = Math.Max(num5, base.G[i]);
					}
					else
					{
						num++;
						num7 += base.G[i];
					}
				}
				else if (base.is_lower_bound(i))
				{
					num4 = Math.Min(num4, base.G[i]);
				}
				else if (base.is_upper_bound(i))
				{
					num6 = Math.Max(num6, base.G[i]);
				}
				else
				{
					num2++;
					num8 += base.G[i];
				}
			}
			double num9 = (num <= 0) ? ((num3 + num5) / 2.0) : (num7 / (double)num);
			double num10 = (num2 <= 0) ? ((num4 + num6) / 2.0) : (num8 / (double)num2);
			this.si.r = (num9 + num10) / 2.0;
			return (num9 - num10) / 2.0;
		}
	}
}
