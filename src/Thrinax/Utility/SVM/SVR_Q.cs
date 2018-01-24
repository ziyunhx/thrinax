/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	internal class SVR_Q : Kernel
	{
		private int l;

		private Cache cache;

		private sbyte[] sign;

		private int[] index;

		private int next_buffer;

		private float[][] buffer;

		private float[] QD;

		public SVR_Q(Problem prob, Parameter param)
			: base(prob.Count, prob.X, param)
		{
			this.l = prob.Count;
			this.cache = new Cache(this.l, (long)(param.CacheSize * 1048576.0));
			this.QD = new float[2 * this.l];
			this.sign = new sbyte[2 * this.l];
			this.index = new int[2 * this.l];
			for (int i = 0; i < this.l; i++)
			{
				this.sign[i] = 1;
				this.sign[i + this.l] = -1;
				this.index[i] = i;
				this.index[i + this.l] = i;
				this.QD[i] = (float)base.KernelFunction(i, i);
				this.QD[i + this.l] = this.QD[i];
			}
			this.buffer = new float[2][];
			this.buffer[0] = new float[2 * this.l];
			this.buffer[1] = new float[2 * this.l];
			this.next_buffer = 0;
		}

		public sealed override void SwapIndex(int i, int j)
		{
			this.sign.SwapIndex(i, j);
			this.index.SwapIndex(i, j);
			this.QD.SwapIndex(i, j);
		}

		public sealed override float[] GetQ(int i, int len)
		{
			float[] array = null;
			int i2 = this.index[i];
			if (this.cache.GetData(i2, ref array, this.l) < this.l)
			{
				for (int j = 0; j < this.l; j++)
				{
					array[j] = (float)base.KernelFunction(i2, j);
				}
			}
			float[] array2 = this.buffer[this.next_buffer];
			this.next_buffer = 1 - this.next_buffer;
			sbyte b = this.sign[i];
			for (int j = 0; j < len; j++)
			{
				array2[j] = (float)b * (float)this.sign[j] * array[this.index[j]];
			}
			return array2;
		}

		public sealed override float[] GetQD()
		{
			return this.QD;
		}
	}
}
