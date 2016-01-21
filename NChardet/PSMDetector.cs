using System;
using Thrinax.Data;

namespace Thrinax
{
    /// <summary>
    /// Description of PSMDetector.
    /// </summary>
    public class PSMDetector
    {
        public static readonly int MAX_VERIFIERS = 16;

        private Verifier[] mVerifier;
        private EUCStatistics[] mStatisticsData;
        private EUCSampler mSampler = new EUCSampler();
        private byte[] mState = new byte[MAX_VERIFIERS];
        private int[] mItemIdx = new int[MAX_VERIFIERS];
        private int mItems;
        private int mClassItems;
        public bool mDone;
        public bool mRunSampler;
        public bool mClassRunSampler;

        public PSMDetector()
        {
            initVerifiers(NChardetLanguage.ALL);
            Reset();
        }

        public PSMDetector(NChardetLanguage langFlag)
        {
            initVerifiers(langFlag);
            Reset();
        }

        public PSMDetector(int aItems, Verifier[] aVerifierSet, EUCStatistics[] aStatisticsSet)
        {
            mClassRunSampler = (aStatisticsSet != null);
            mStatisticsData = aStatisticsSet;
            mVerifier = aVerifierSet;
            mClassItems = aItems;
            Reset();
        }

        public void Reset()
        {
            mRunSampler = mClassRunSampler;
            mDone = false;
            mItems = mClassItems;
            for (int i = 0; i < mItems; i++)
            {
                mState[i] = 0;
                mItemIdx[i] = i;
            }
            mSampler.Reset();
        }

        protected void initVerifiers(NChardetLanguage currVerSet)
        {
            //int idx = 0 ;
            NChardetLanguage currVerifierSet;

            if (currVerSet >= 0 && currVerSet < NChardetLanguage.NO_OF_LANGUAGES)
            {
                currVerifierSet = currVerSet;
            }
            else {
                currVerifierSet = NChardetLanguage.ALL;
            }

            mVerifier = null;
            mStatisticsData = null;

            if (currVerifierSet == NChardetLanguage.TRADITIONAL_CHINESE)
            {
                mVerifier = new Verifier[] {
                      new UTF8Verifier(),
                      new BIG5Verifier(),
                      new ISO2022CNVerifier(),
                      new EUCTWVerifier(),
                      new CP1252Verifier(),
                      new UCS2BEVerifier(),
                      new UCS2LEVerifier()
               };

                mStatisticsData = new EUCStatistics[] {
                      null,
                      new Big5Statistics(),
                      null,
                      new EUCTWStatistics(),
                      null,
                      null,
                      null
               };
            }

            //==========================================================
            else if (currVerifierSet == NChardetLanguage.KOREAN)
            {
                mVerifier = new Verifier[] {
                      new UTF8Verifier(),
                      new EUCKRVerifier(),
                      new ISO2022KRVerifier(),
                      new CP1252Verifier(),
                      new UCS2BEVerifier(),
                      new UCS2LEVerifier()
               };
            }

            //==========================================================
            else if (currVerifierSet == NChardetLanguage.SIMPLIFIED_CHINESE)
            {
                mVerifier = new Verifier[] {
                      new UTF8Verifier(),
                      new GB2312Verifier(),
                      new GB18030Verifier(),
                      new ISO2022CNVerifier(),
                      new HZVerifier(),
                      new CP1252Verifier(),
                      new UCS2BEVerifier(),
                      new UCS2LEVerifier()
               };
            }

            //==========================================================
            else if (currVerifierSet == NChardetLanguage.JAPANESE)
            {
                mVerifier = new Verifier[] {
                      new UTF8Verifier(),
                      new SJISVerifier(),
                      new EUCJPVerifier(),
                      new ISO2022JPVerifier(),
                      new CP1252Verifier(),
                      new UCS2BEVerifier(),
                      new UCS2LEVerifier()
               };
            }
            //==========================================================
            else if (currVerifierSet == NChardetLanguage.CHINESE)
            {
                mVerifier = new Verifier[] {
                      new UTF8Verifier(),
                      new GB2312Verifier(),
                      new GB18030Verifier(),
                      new BIG5Verifier(),
                      new ISO2022CNVerifier(),
                      new HZVerifier(),
                      new EUCTWVerifier(),
                      new CP1252Verifier(),
                      new UCS2BEVerifier(),
                      new UCS2LEVerifier()
               };
                mStatisticsData = new EUCStatistics[] {
                      null,
                      new GB2312Statistics(),
                null,
                      new Big5Statistics(),
                      null,
                      null,
                      new EUCTWStatistics(),
                      null,
                      null,
                      null
               };
            }

            //==========================================================
            else if (currVerifierSet == NChardetLanguage.ALL)
            {
                mVerifier = new Verifier[] {
                      new UTF8Verifier(),
                      new SJISVerifier(),
                      new EUCJPVerifier(),
                      new ISO2022JPVerifier(),
                      new EUCKRVerifier(),
                      new ISO2022KRVerifier(),
                      new BIG5Verifier(),
                      new EUCTWVerifier(),
                      new GB2312Verifier(),
                      new GB18030Verifier(),
                      new ISO2022CNVerifier(),
                      new HZVerifier(),
                      new CP1252Verifier(),
                      new UCS2BEVerifier(),
                      new UCS2LEVerifier()
               };
                mStatisticsData = new EUCStatistics[] {
                      null,
                      null,
                      new EUCJPStatistics(),
                      null,
                      new EUCKRStatistics(),
                      null,
                      new Big5Statistics(),
                      new EUCTWStatistics(),
                      new GB2312Statistics(),
                      null,
                      null,
                      null,
                      null,
                      null,
                      null
               };
            }
            mClassRunSampler = (mStatisticsData != null);
            mClassItems = mVerifier.Length;
        }

        public bool HandleData(byte[] aBuf, int len, ref string charset)
        {
            int i, j;
            byte b, st;
            for (i = 0; i < len; i++)
            {
                b = aBuf[i];
                for (j = 0; j < mItems;)
                {
                    st = Verifier.getNextState(mVerifier[mItemIdx[j]],
                                    b, mState[j]);
                    if (st == Verifier.eItsMe)
                    {
                        charset = mVerifier[mItemIdx[j]].charset();
                        mDone = true;
                        return mDone;
                    }
                    else if (st == Verifier.eError)
                    {
                        mItems--;
                        if (j < mItems)
                        {
                            mItemIdx[j] = mItemIdx[mItems];
                            mState[j] = mState[mItems];
                        }
                    }
                    else {
                        mState[j++] = st;
                    }
                }


                if (mItems <= 1)
                {
                    if (1 == mItems)
                    {
                        charset = mVerifier[mItemIdx[0]].charset();
                    }
                    mDone = true;
                    return mDone;
                }
                else {

                    int nonUCS2Num = 0;
                    int nonUCS2Idx = 0;

                    for (j = 0; j < mItems; j++)
                    {
                        if ((!(mVerifier[mItemIdx[j]].isUCS2())) &&
                         (!(mVerifier[mItemIdx[j]].isUCS2())))
                        {
                            nonUCS2Num++;
                            nonUCS2Idx = j;
                        }
                    }

                    if (1 == nonUCS2Num)
                    {
                        charset = mVerifier[mItemIdx[nonUCS2Idx]].charset();
                        mDone = true;
                        return mDone;
                    }
                }

            } // End of for( i=0; i < len ...

            if (mRunSampler)
                Sample(aBuf, len, ref charset);

            return mDone;
        }

        public void DataEnd(ref string charset)
        {
            if (mDone == true)
                return;
            if (mItems == 2)
            {
                if ((mVerifier[mItemIdx[0]].charset()).Equals("GB18030"))
                {
                    charset = mVerifier[mItemIdx[1]].charset();
                    mDone = true;
                }
                else if ((mVerifier[mItemIdx[1]].charset()).Equals("GB18030"))
                {
                    charset = mVerifier[mItemIdx[0]].charset();
                    mDone = true;
                }
            }
            if (mRunSampler)
                Sample(null, 0, true, ref charset);
        }

        public void Sample(byte[] aBuf, int aLen, ref string charset)
        {
            Sample(aBuf, aLen, false, ref charset);
        }

        public void Sample(byte[] aBuf, int aLen, bool aLastChance, ref string charset)
        {
            int possibleCandidateNum = 0;
            int j;
            int eucNum = 0;

            for (j = 0; j < mItems; j++)
            {
                if (null != mStatisticsData[mItemIdx[j]])
                    eucNum++;
                if ((!mVerifier[mItemIdx[j]].isUCS2()) &&
                     (!(mVerifier[mItemIdx[j]].charset()).Equals("GB18030")))
                    possibleCandidateNum++;
            }

            mRunSampler = (eucNum > 1);
            if (mRunSampler)
            {
                mRunSampler = mSampler.Sample(aBuf, aLen);
                if (((aLastChance && mSampler.GetSomeData()) ||
                    mSampler.EnoughData())
                   && (eucNum == possibleCandidateNum))
                {
                    mSampler.CalFreq();

                    int bestIdx = -1;
                    int eucCnt = 0;
                    float bestScore = 0.0f;
                    for (j = 0; j < mItems; j++)
                    {
                        if ((null != mStatisticsData[mItemIdx[j]]) &&
                          (!(mVerifier[mItemIdx[j]].charset()).Equals("Big5")))
                        {
                            float score = mSampler.GetScore(
                               mStatisticsData[mItemIdx[j]].mFirstByteFreq(),
                               mStatisticsData[mItemIdx[j]].mFirstByteWeight(),
                               mStatisticsData[mItemIdx[j]].mSecondByteFreq(),
                               mStatisticsData[mItemIdx[j]].mSecondByteWeight());

                            if ((0 == eucCnt++) || (bestScore > score))
                            {
                                bestScore = score;
                                bestIdx = j;
                            } // if(( 0 == eucCnt++) || (bestScore > score )) 
                        } // if(null != ...)
                    } // for
                    if (bestIdx >= 0)
                    {
                        charset = mVerifier[mItemIdx[bestIdx]].charset();
                        mDone = true;
                    }
                } // if (eucNum == possibleCandidateNum)
            } // if(mRunSampler)
        }

        public String[] getProbableCharsets()
        {

            if (mItems <= 0)
            {
                String[] nomatch = new String[1];
                nomatch[0] = "nomatch";
                return nomatch;
            }

            string[] ret = new String[mItems];
            for (int i = 0; i < mItems; i++)
                ret[i] = mVerifier[mItemIdx[i]].charset();
            return ret;
        }
    }
}
