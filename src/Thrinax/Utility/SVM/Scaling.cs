/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	public static class Scaling
	{
		public static Problem Scale(this IRangeTransform range, Problem prob)
		{
			Problem problem = new Problem(prob.Count, new double[prob.Count], new Node[prob.Count][], prob.MaxIndex);
			for (int i = 0; i < problem.Count; i++)
			{
				problem.X[i] = new Node[prob.X[i].Length];
				for (int j = 0; j < problem.X[i].Length; j++)
				{
					problem.X[i][j] = new Node(prob.X[i][j].Index, range.Transform(prob.X[i][j].Value, prob.X[i][j].Index));
				}
				problem.Y[i] = prob.Y[i];
			}
			return problem;
		}
	}
}
