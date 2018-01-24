using System;
using System.Collections.Generic;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	[Serializable]
	public class Parameter : ICloneable
	{
		private SvmType _svmType;

		private KernelType _kernelType;

		private int _degree;

		private double _gamma;

		private double _coef0;

		private double _cacheSize;

		private double _C;

		private double _eps;

		private Dictionary<int, double> _weights;

		private double _nu;

		private double _p;

		private bool _shrinking;

		private bool _probability;

		public SvmType SvmType
		{
			get
			{
				return this._svmType;
			}
			set
			{
				this._svmType = value;
			}
		}

		public KernelType KernelType
		{
			get
			{
				return this._kernelType;
			}
			set
			{
				this._kernelType = value;
			}
		}

		public int Degree
		{
			get
			{
				return this._degree;
			}
			set
			{
				this._degree = value;
			}
		}

		public double Gamma
		{
			get
			{
				return this._gamma;
			}
			set
			{
				this._gamma = value;
			}
		}

		public double Coefficient0
		{
			get
			{
				return this._coef0;
			}
			set
			{
				this._coef0 = value;
			}
		}

		public double CacheSize
		{
			get
			{
				return this._cacheSize;
			}
			set
			{
				this._cacheSize = value;
			}
		}

		public double EPS
		{
			get
			{
				return this._eps;
			}
			set
			{
				this._eps = value;
			}
		}

		public double C
		{
			get
			{
				return this._C;
			}
			set
			{
				this._C = value;
			}
		}

		public Dictionary<int, double> Weights
		{
			get
			{
				return this._weights;
			}
		}

		public double Nu
		{
			get
			{
				return this._nu;
			}
			set
			{
				this._nu = value;
			}
		}

		public double P
		{
			get
			{
				return this._p;
			}
			set
			{
				this._p = value;
			}
		}

		public bool Shrinking
		{
			get
			{
				return this._shrinking;
			}
			set
			{
				this._shrinking = value;
			}
		}

		public bool Probability
		{
			get
			{
				return this._probability;
			}
			set
			{
				this._probability = value;
			}
		}

		public Parameter()
		{
			this._svmType = SvmType.C_SVC;
			this._kernelType = KernelType.RBF;
			this._degree = 3;
			this._gamma = 0.0;
			this._coef0 = 0.0;
			this._nu = 0.5;
			this._cacheSize = 40.0;
			this._C = 1.0;
			this._eps = 0.001;
			this._p = 0.1;
			this._shrinking = true;
			this._probability = false;
			this._weights = new Dictionary<int, double>();
		}

		public object Clone()
		{
			return base.MemberwiseClone();
		}
	}
}
