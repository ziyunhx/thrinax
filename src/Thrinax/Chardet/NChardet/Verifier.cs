namespace Thrinax
{
	/// <summary>
	/// Description of Verifier.
	/// </summary>
	public abstract class Verifier
	{
		internal static readonly byte eStart = (byte)0;
		internal static readonly byte eError = (byte)1;
		internal static readonly byte eItsMe = (byte)2;
		internal static readonly int eidxSft4bits = 3;
	    internal static readonly int eSftMsk4bits = 7;
     	internal static readonly int eBitSft4bits = 2;
     	internal static readonly int eUnitMsk4bits = 0x0000000F;
     	
		public Verifier()
		{
		}
		public abstract string charset();
    	public abstract int stFactor();
     	public abstract int[] cclass();
     	public abstract int[] states();

     	public abstract bool isUCS2();
     
     	public static byte getNextState(Verifier v, byte b, byte s) {

         return (byte) ( 0xFF & 
	     (((v.states()[((
		   (s*v.stFactor()+(((v.cclass()[((b&0xFF)>>eidxSft4bits)]) 
		   >> ((b & eSftMsk4bits) << eBitSft4bits)) 
		   & eUnitMsk4bits ))&0xFF)
		>> eidxSft4bits) ]) >> (((
		   (s*v.stFactor()+(((v.cclass()[((b&0xFF)>>eidxSft4bits)]) 
		   >> ((b & eSftMsk4bits) << eBitSft4bits)) 
		   & eUnitMsk4bits ))&0xFF) 
		& eSftMsk4bits) << eBitSft4bits)) & eUnitMsk4bits )
	 	) ;

     	}
	}
}
