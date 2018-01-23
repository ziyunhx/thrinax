/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	public class CurvePoint
	{
		private float _x;

		private float _y;

		public float X
		{
			get
			{
				return this._x;
			}
		}

		public float Y
		{
			get
			{
				return this._y;
			}
		}

		public CurvePoint(float x, float y)
		{
			this._x = x;
			this._y = y;
		}

		public override string ToString()
		{
			return string.Format("({0}, {1})", this._x, this._y);
		}
	}
}
