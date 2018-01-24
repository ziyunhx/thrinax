/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	internal static class SVMExtensions
	{
		public static void SwapIndex<T>(this T[] list, int i, int j)
		{
			T val = list[i];
			list[i] = list[j];
			list[j] = val;
		}
	}
}
