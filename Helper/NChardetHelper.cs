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
        /// <param name="maxLength">max length per time.</param>
        /// <returns>charset string, will be empty when can't recog.</returns>
        public static string RecogCharset(byte[] bytes, NChardetLanguage language = NChardetLanguage.ALL, int maxLength = -1)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            PSMDetector detector = new PSMDetector(language);
            string charset = String.Empty;

            if (maxLength > 0)
            {
                int count = 0;

                do
                {
                    var tempBytes = bytes.Skip(maxLength * count).Take(maxLength);
                    if (tempBytes == null || tempBytes.Count() == 0)
                        break;

                    detector.HandleData(tempBytes.ToArray(), tempBytes.Count(), ref charset);
                    if (!string.IsNullOrEmpty(charset))
                        break;

                    count++;
                }
                while (true);
            }
            else
            {
                detector.HandleData(bytes, bytes.Length, ref charset);
            }

            if (string.IsNullOrEmpty(charset))
                detector.DataEnd(ref charset);

            return charset;
        }
    }
}
