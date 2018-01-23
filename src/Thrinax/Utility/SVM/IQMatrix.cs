/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace SVM
{
	internal interface IQMatrix
	{
		float[] GetQ(int column, int len);

		float[] GetQD();

		void SwapIndex(int i, int j);
	}
}
