using System.Globalization;
using System.Threading;

/// <summary>
/// A .NET Support Vector Machine library adapted from libsvm
/// Copyright ©  Matthew Johnson 2009
/// </summary>
namespace Thrinax.Utility.SVM
{
	internal static class TemporaryCulture
	{
		private static CultureInfo _culture;

		public static void Start()
		{
			TemporaryCulture._culture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		}

		public static void Stop()
		{
			Thread.CurrentThread.CurrentCulture = TemporaryCulture._culture;
		}
	}
}
