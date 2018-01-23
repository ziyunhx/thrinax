using System;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	internal class Solver
	{
		public class SolutionInfo
		{
			public double obj;

			public double rho;

			public double upper_bound_p;

			public double upper_bound_n;

			public double r;
		}

		private const byte LOWER_BOUND = 0;

		private const byte UPPER_BOUND = 1;

		private const byte FREE = 2;

		protected const double INF = double.PositiveInfinity;

		protected int active_size;

		protected sbyte[] y;

		protected double[] G;

		private byte[] alpha_status;

		private double[] alpha;

		protected IQMatrix Q;

		protected float[] QD;

		protected double EPS;

		private double Cp;

		private double Cn;

		private double[] p;

		private int[] active_set;

		private double[] G_bar;

		protected int l;

		protected bool unshrink;

		private double get_C(int i)
		{
			if (this.y[i] <= 0)
			{
				return this.Cn;
			}
			return this.Cp;
		}

		private void update_alpha_status(int i)
		{
			if (this.alpha[i] >= this.get_C(i))
			{
				this.alpha_status[i] = 1;
			}
			else if (this.alpha[i] <= 0.0)
			{
				this.alpha_status[i] = 0;
			}
			else
			{
				this.alpha_status[i] = 2;
			}
		}

		protected bool is_upper_bound(int i)
		{
			return this.alpha_status[i] == 1;
		}

		protected bool is_lower_bound(int i)
		{
			return this.alpha_status[i] == 0;
		}

		private bool is_free(int i)
		{
			return this.alpha_status[i] == 2;
		}

		protected void swap_index(int i, int j)
		{
			this.Q.SwapIndex(i, j);
			this.y.SwapIndex(i, j);
			this.G.SwapIndex(i, j);
			this.alpha_status.SwapIndex(i, j);
			this.alpha.SwapIndex(i, j);
			this.p.SwapIndex(i, j);
			this.active_set.SwapIndex(i, j);
			this.G_bar.SwapIndex(i, j);
		}

		protected void reconstruct_gradient()
		{
			if (this.active_size != this.l)
			{
				int num = 0;
				for (int i = this.active_size; i < this.l; i++)
				{
					this.G[i] = this.G_bar[i] + this.p[i];
				}
				for (int i = 0; i < this.active_size; i++)
				{
					if (this.is_free(i))
					{
						num++;
					}
				}
				if (2 * num < this.active_size)
				{
					Procedures.info("\nWarning: using -h 0 may be faster\n");
				}
				if (num * this.l > 2 * this.active_size * (this.l - this.active_size))
				{
					for (int j = this.active_size; j < this.l; j++)
					{
						float[] q = this.Q.GetQ(j, this.active_size);
						for (int i = 0; i < this.active_size; i++)
						{
							if (this.is_free(i))
							{
								this.G[j] += this.alpha[i] * (double)q[i];
							}
						}
					}
				}
				else
				{
					for (int j = 0; j < this.active_size; j++)
					{
						if (this.is_free(j))
						{
							float[] q2 = this.Q.GetQ(j, this.l);
							double num2 = this.alpha[j];
							for (int i = this.active_size; i < this.l; i++)
							{
								this.G[i] += num2 * (double)q2[i];
							}
						}
					}
				}
			}
		}

		public virtual void Solve(int l, IQMatrix Q, double[] p_, sbyte[] y_, double[] alpha_, double Cp, double Cn, double eps, SolutionInfo si, bool shrinking)
		{
			this.l = l;
			this.Q = Q;
			this.QD = Q.GetQD();
			this.p = (double[])p_.Clone();
			this.y = (sbyte[])y_.Clone();
			this.alpha = (double[])alpha_.Clone();
			this.Cp = Cp;
			this.Cn = Cn;
			this.EPS = eps;
			this.unshrink = false;
			this.alpha_status = new byte[l];
			for (int i = 0; i < l; i++)
			{
				this.update_alpha_status(i);
			}
			this.active_set = new int[l];
			for (int j = 0; j < l; j++)
			{
				this.active_set[j] = j;
			}
			this.active_size = l;
			this.G = new double[l];
			this.G_bar = new double[l];
			for (int k = 0; k < l; k++)
			{
				this.G[k] = this.p[k];
				this.G_bar[k] = 0.0;
			}
			for (int k = 0; k < l; k++)
			{
				if (!this.is_lower_bound(k))
				{
					float[] q = Q.GetQ(k, l);
					double num = this.alpha[k];
					for (int m = 0; m < l; m++)
					{
						this.G[m] += num * (double)q[m];
					}
					if (this.is_upper_bound(k))
					{
						for (int m = 0; m < l; m++)
						{
							this.G_bar[m] += this.get_C(k) * (double)q[m];
						}
					}
				}
			}
			int num2 = 0;
			int num3 = Math.Min(l, 1000) + 1;
			int[] array = new int[2];
			while (true)
			{
				if (--num3 == 0)
				{
					num3 = Math.Min(l, 1000);
					if (shrinking)
					{
						this.do_shrinking();
					}
					Procedures.info(".");
				}
				if (this.select_working_set(array) != 0)
				{
					this.reconstruct_gradient();
					this.active_size = l;
					Procedures.info("*");
					if (this.select_working_set(array) != 0)
					{
						break;
					}
					num3 = 1;
				}
				int num4 = array[0];
				int num5 = array[1];
				num2++;
				float[] q2 = Q.GetQ(num4, this.active_size);
				float[] q3 = Q.GetQ(num5, this.active_size);
				double num6 = this.get_C(num4);
				double num7 = this.get_C(num5);
				double num8 = this.alpha[num4];
				double num9 = this.alpha[num5];
				if (this.y[num4] != this.y[num5])
				{
					double num10 = (double)(q2[num4] + q3[num5] + 2f * q2[num5]);
					if (num10 <= 0.0)
					{
						num10 = 1E-12;
					}
					double num11 = (0.0 - this.G[num4] - this.G[num5]) / num10;
					double num12 = this.alpha[num4] - this.alpha[num5];
					this.alpha[num4] += num11;
					this.alpha[num5] += num11;
					if (num12 > 0.0)
					{
						if (this.alpha[num5] < 0.0)
						{
							this.alpha[num5] = 0.0;
							this.alpha[num4] = num12;
						}
					}
					else if (this.alpha[num4] < 0.0)
					{
						this.alpha[num4] = 0.0;
						this.alpha[num5] = 0.0 - num12;
					}
					if (num12 > num6 - num7)
					{
						if (this.alpha[num4] > num6)
						{
							this.alpha[num4] = num6;
							this.alpha[num5] = num6 - num12;
						}
					}
					else if (this.alpha[num5] > num7)
					{
						this.alpha[num5] = num7;
						this.alpha[num4] = num7 + num12;
					}
				}
				else
				{
					double num13 = (double)(q2[num4] + q3[num5] - 2f * q2[num5]);
					if (num13 <= 0.0)
					{
						num13 = 1E-12;
					}
					double num14 = (this.G[num4] - this.G[num5]) / num13;
					double num15 = this.alpha[num4] + this.alpha[num5];
					this.alpha[num4] -= num14;
					this.alpha[num5] += num14;
					if (num15 > num6)
					{
						if (this.alpha[num4] > num6)
						{
							this.alpha[num4] = num6;
							this.alpha[num5] = num15 - num6;
						}
					}
					else if (this.alpha[num5] < 0.0)
					{
						this.alpha[num5] = 0.0;
						this.alpha[num4] = num15;
					}
					if (num15 > num7)
					{
						if (this.alpha[num5] > num7)
						{
							this.alpha[num5] = num7;
							this.alpha[num4] = num15 - num7;
						}
					}
					else if (this.alpha[num4] < 0.0)
					{
						this.alpha[num4] = 0.0;
						this.alpha[num5] = num15;
					}
				}
				double num16 = this.alpha[num4] - num8;
				double num17 = this.alpha[num5] - num9;
				for (int n = 0; n < this.active_size; n++)
				{
					this.G[n] += (double)q2[n] * num16 + (double)q3[n] * num17;
				}
				bool flag = this.is_upper_bound(num4);
				bool flag2 = this.is_upper_bound(num5);
				this.update_alpha_status(num4);
				this.update_alpha_status(num5);
				if (flag != this.is_upper_bound(num4))
				{
					q2 = Q.GetQ(num4, l);
					if (flag)
					{
						for (int num18 = 0; num18 < l; num18++)
						{
							this.G_bar[num18] -= num6 * (double)q2[num18];
						}
					}
					else
					{
						for (int num18 = 0; num18 < l; num18++)
						{
							this.G_bar[num18] += num6 * (double)q2[num18];
						}
					}
				}
				if (flag2 != this.is_upper_bound(num5))
				{
					q3 = Q.GetQ(num5, l);
					if (flag2)
					{
						for (int num18 = 0; num18 < l; num18++)
						{
							this.G_bar[num18] -= num7 * (double)q3[num18];
						}
					}
					else
					{
						for (int num18 = 0; num18 < l; num18++)
						{
							this.G_bar[num18] += num7 * (double)q3[num18];
						}
					}
				}
			}
			si.rho = this.calculate_rho();
			double num19 = 0.0;
			for (int num20 = 0; num20 < l; num20++)
			{
				num19 += this.alpha[num20] * (this.G[num20] + this.p[num20]);
			}
			si.obj = num19 / 2.0;
			for (int num21 = 0; num21 < l; num21++)
			{
				alpha_[this.active_set[num21]] = this.alpha[num21];
			}
			si.upper_bound_p = Cp;
			si.upper_bound_n = Cn;
			Procedures.info("\noptimization finished, #iter = " + num2 + "\n");
		}

		private int select_working_set(int[] working_set)
		{
			double num = double.NegativeInfinity;
			double num2 = double.NegativeInfinity;
			int num3 = -1;
			int num4 = -1;
			double num5 = double.PositiveInfinity;
			for (int i = 0; i < this.active_size; i++)
			{
				if (this.y[i] == 1)
				{
					if (!this.is_upper_bound(i) && 0.0 - this.G[i] >= num)
					{
						num = 0.0 - this.G[i];
						num3 = i;
					}
				}
				else if (!this.is_lower_bound(i) && this.G[i] >= num)
				{
					num = this.G[i];
					num3 = i;
				}
			}
			int num6 = num3;
			float[] array = null;
			if (num6 != -1)
			{
				array = this.Q.GetQ(num6, this.active_size);
			}
			for (int j = 0; j < this.active_size; j++)
			{
				if (this.y[j] == 1)
				{
					if (!this.is_lower_bound(j))
					{
						double num7 = num + this.G[j];
						if (this.G[j] >= num2)
						{
							num2 = this.G[j];
						}
						if (num7 > 0.0)
						{
							double num8 = (double)(array[num6] + this.QD[j]) - 2.0 * (double)this.y[num6] * (double)array[j];
							double num9 = (!(num8 > 0.0)) ? ((0.0 - num7 * num7) / 1E-12) : ((0.0 - num7 * num7) / num8);
							if (num9 <= num5)
							{
								num4 = j;
								num5 = num9;
							}
						}
					}
				}
				else if (!this.is_upper_bound(j))
				{
					double num10 = num - this.G[j];
					if (0.0 - this.G[j] >= num2)
					{
						num2 = 0.0 - this.G[j];
					}
					if (num10 > 0.0)
					{
						double num11 = (double)(array[num6] + this.QD[j]) + 2.0 * (double)this.y[num6] * (double)array[j];
						double num12 = (!(num11 > 0.0)) ? ((0.0 - num10 * num10) / 1E-12) : ((0.0 - num10 * num10) / num11);
						if (num12 <= num5)
						{
							num4 = j;
							num5 = num12;
						}
					}
				}
			}
			if (num + num2 < this.EPS)
			{
				return 1;
			}
			working_set[0] = num3;
			working_set[1] = num4;
			return 0;
		}

		private bool be_shrunk(int i, double GMax1, double GMax2)
		{
			if (this.is_upper_bound(i))
			{
				if (this.y[i] == 1)
				{
					return 0.0 - this.G[i] > GMax1;
				}
				return 0.0 - this.G[i] > GMax2;
			}
			if (this.is_lower_bound(i))
			{
				if (this.y[i] == 1)
				{
					return this.G[i] > GMax2;
				}
				return this.G[i] > GMax1;
			}
			return false;
		}

		private void do_shrinking()
		{
			double num = double.NegativeInfinity;
			double num2 = double.NegativeInfinity;
			for (int i = 0; i < this.active_size; i++)
			{
				if (this.y[i] == 1)
				{
					if (!this.is_upper_bound(i) && 0.0 - this.G[i] >= num)
					{
						num = 0.0 - this.G[i];
					}
					if (!this.is_lower_bound(i) && this.G[i] >= num2)
					{
						num2 = this.G[i];
					}
				}
				else
				{
					if (!this.is_upper_bound(i) && 0.0 - this.G[i] >= num2)
					{
						num2 = 0.0 - this.G[i];
					}
					if (!this.is_lower_bound(i) && this.G[i] >= num)
					{
						num = this.G[i];
					}
				}
			}
			if (!this.unshrink && num + num2 <= this.EPS * 10.0)
			{
				this.unshrink = true;
				this.reconstruct_gradient();
				this.active_size = this.l;
			}
			for (int i = 0; i < this.active_size; i++)
			{
				if (this.be_shrunk(i, num, num2))
				{
					this.active_size--;
					while (this.active_size > i)
					{
						if (this.be_shrunk(this.active_size, num, num2))
						{
							this.active_size--;
							continue;
						}
						this.swap_index(i, this.active_size);
						break;
					}
				}
			}
		}

		private double calculate_rho()
		{
			int num = 0;
			double num2 = double.PositiveInfinity;
			double num3 = double.NegativeInfinity;
			double num4 = 0.0;
			for (int i = 0; i < this.active_size; i++)
			{
				double num5 = (double)this.y[i] * this.G[i];
				if (this.is_lower_bound(i))
				{
					if (this.y[i] > 0)
					{
						num2 = Math.Min(num2, num5);
					}
					else
					{
						num3 = Math.Max(num3, num5);
					}
				}
				else if (this.is_upper_bound(i))
				{
					if (this.y[i] < 0)
					{
						num2 = Math.Min(num2, num5);
					}
					else
					{
						num3 = Math.Max(num3, num5);
					}
				}
				else
				{
					num++;
					num4 += num5;
				}
			}
			if (num > 0)
			{
				return num4 / (double)num;
			}
			return (num2 + num3) / 2.0;
		}
	}
}
