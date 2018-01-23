namespace Thrinax.Chardet.NChardet
{
	/// <summary>
	/// Description of EUCStatistics.
	/// </summary>
	public abstract class EUCStatistics
	{
		public abstract float[] mFirstByteFreq() ;
     	public abstract float   mFirstByteStdDev();
     	public abstract float   mFirstByteMean();
     	public abstract float   mFirstByteWeight();
     	public abstract float[] mSecondByteFreq();
     	public abstract float   mSecondByteStdDev();
     	public abstract float   mSecondByteMean();
     	public abstract float   mSecondByteWeight();
     	
		public EUCStatistics()
		{
		}
	}
}
