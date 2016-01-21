using System;
using System.Linq;
using System.Text;
using Thrinax.Data;

namespace Thrinax.Helper
{
    public class NChardetHelper
    {
        /// <summary>
        /// Recog the Encoding from byte array.
        /// </summary>
        /// <param name="bytes">the byte array.</param>
        /// <param name="language">the language.</param>
        /// <returns>charset string, will be empty when can't recog.</returns>
        public static Encoding RecogEncoding(byte[] bytes, NChardetLanguage language = NChardetLanguage.ALL)
        {
            string charset = RecogCharset(bytes, language);
            if (!string.IsNullOrEmpty(charset))
                return Encoding.GetEncoding(charset); 

            return Encoding.Default;
        }

        /// <summary>
        /// Recog the charset from byte array.
        /// </summary>
        /// <param name="bytes">the byte array.</param>
        /// <param name="language">the language.</param>
        /// <returns>charset string, will be empty when can't recog.</returns>
        public static string RecogCharset(byte[] bytes, NChardetLanguage language = NChardetLanguage.ALL)
        {
            PSMDetector detector = new PSMDetector(language);

            int time = 0;
            int maxLength = 1024;
            string charset = String.Empty;

            do
            {
                var tempBytes = bytes.Skip(maxLength * time);
                if (tempBytes == null || tempBytes.Count() <= 0 || detector.HandleData(tempBytes.ToArray(), tempBytes.Count(), ref charset))                
                    break;
            }
            while (true);

            if(string.IsNullOrEmpty(charset))
                detector.DataEnd(ref charset);

            return charset;
        }
    }
}
