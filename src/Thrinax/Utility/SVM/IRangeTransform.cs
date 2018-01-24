/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	public interface IRangeTransform
	{
		double Transform(double input, int index);

		Node[] Transform(Node[] input);
	}
}
