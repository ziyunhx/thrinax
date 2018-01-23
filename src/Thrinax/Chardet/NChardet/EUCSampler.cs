namespace Thrinax.Chardet.NChardet
{
	/// <summary>
	/// Description of EUCSampler.
	/// </summary>
	public class EUCSampler
	{
		private int mTotal = 0;
		private int mThreshold = 200 ;
		private int mState = 0;
		public int[] mFirstByteCnt = new int[94] ;
		public int[] mSecondByteCnt = new int[94] ;
		public float[] mFirstByteFreq = new float[94] ;
		public float[] mSecondByteFreq = new float[94];
	
		public EUCSampler()
		{
			Reset() ;
		}
		
		public void Reset() {
			mTotal = 0 ;
			mState = 0 ;
			for(int i=0; i<94; i++)
				mFirstByteCnt[i] = mSecondByteCnt[i] = 0 ;
		}
		
		public bool EnoughData() { return mTotal > mThreshold; }
		public bool GetSomeData() { return mTotal > 1; }
		
		public bool Sample(byte[] aIn, int aLen) {

           if(mState == 1)
               return false;

           int p = 0;

           // if(aLen + mTotal > 0x80000000) 
           //    aLen = 0x80000000 - mTotal;

           int i;
           for(i=0; (i<aLen) && (1 != mState) ;i++,p++)
           {
               switch(mState) {
                  case 0:
                    if( ( aIn[p] & 0x0080 ) != 0 )  
                    {
                       if((0xff==(0xff&aIn[p])) || ( 0xa1>(0xff&aIn[p]))) {
                          mState = 1;
                       } else {
                          mTotal++;
                          mFirstByteCnt[(0xff&aIn[p]) - 0xa1]++;
                          mState = 2;
                       }
                    }
                    break;
                  case 1:
                    break;
                  case 2:
                    if( (aIn[p] & 0x0080)  != 0 )
                    {
                       if((0xff == (0xff&aIn[p])) 
				|| ( 0xa1 > (0xff&aIn[p]))) {
                          mState = 1;
                       } else {
                          mTotal++;
                          mSecondByteCnt[(0xff&aIn[p]) - 0xa1]++;
                          mState = 0;
                       }
                    } else {
                       mState = 1;
                    }
                    break;
                  default:
                    mState = 1;
                    break;
               }
            }
            return (1 != mState  );
        }
		
		public void CalFreq() {
		   	for(int i = 0 ; i < 94; i++) {
		      mFirstByteFreq[i] = (float)mFirstByteCnt[i] / (float)mTotal;
		      mSecondByteFreq[i] = (float)mSecondByteCnt[i] / (float)mTotal;
		   	}
		}
		
		public float GetScore(float[] aFirstByteFreq, float aFirstByteWeight,float[] aSecondByteFreq, float aSecondByteWeight)
		{
       	   return aFirstByteWeight * GetScore(aFirstByteFreq, mFirstByteFreq) +
              aSecondByteWeight * GetScore(aSecondByteFreq, mSecondByteFreq);
		}	
		
		public float GetScore(float[] array1, float[] array2) {
           float s;
           float sum=0.0f;
       	   for(int i=0;i<94;i++) {
              s = array1[i] - array2[i];
              sum += s * s;
           }
           return (float)System.Math.Sqrt((double)sum) / 94.0f;
		}
	}
}
