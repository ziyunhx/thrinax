using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Thrinax.Helper
{
    public class RSAHelper
    {
        private static Random rand = new Random();
        public const string pempubheader = "-----BEGIN PUBLIC KEY-----";
        public const string pempubfooter = "-----END PUBLIC KEY-----";

        private BigInteger Modulus = 0;
        private BigInteger Exponent = 0;
        RSACryptoServiceProvider Rsa = null;

        private BigInteger AE(string a, int b)
        {
            if (b < a.Length + 11)
                throw new Exception("Message too long for RSA");

            byte[] c = new byte[b];
            int d = a.Length - 1;
            while (d >= 0 && b > 0)
            {
                int e = (int)a[d--];
                if (e < 128)
                {
                    c[--b] = Convert.ToByte(e);
                }
                else if ((e > 127) && (e < 2048))
                {
                    c[--b] = Convert.ToByte(((e & 63) | 128));
                    c[--b] = Convert.ToByte((e >> 6) | 192);
                }
                else
                {
                    c[--b] = Convert.ToByte((e & 63) | 128);
                    c[--b] = Convert.ToByte(((e >> 6) & 63) | 128);
                    c[--b] = Convert.ToByte(((e >> 12) | 224));
                }
            }

            c[--b] = Convert.ToByte(0);
            byte[] temp = new byte[1];
            while (b > 2)
            {
                temp[0] = Convert.ToByte(0);
                while (temp[0] == 0)
                    rand.NextBytes(temp);
                c[--b] = temp[0];
            }
            c[--b] = 2;
            c[--b] = 0;
            return new BigInteger(c);
        }

        public void SetPublic(string pemstr)
        {
            StringBuilder build = new StringBuilder(pemstr);
            build.Replace(pempubheader, "");
            build.Replace(pempubfooter, "");

            pemstr = build.ToString().Trim();
            Rsa = GetPublicKey(pemstr);
        }

        private RSACryptoServiceProvider GetPublicKey(string publicKeyString)
        {
            // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
            byte[] SeqOID = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            byte[] x509key;
            byte[] seq = new byte[15];
            int x509size;

            x509key = Convert.FromBase64String(publicKeyString);
            x509size = x509key.Length;

            // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
            MemoryStream mem = new MemoryStream(x509key);
            BinaryReader binr = new BinaryReader(mem);    //wrap Memory Stream with BinaryReader for easy reading
            byte bt = 0;
            ushort twobytes = 0;

            try
            {

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)	//data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();	//advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();	//advance 2 bytes
                else
                    return null;

                seq = binr.ReadBytes(15);		//read the Sequence OID
                if (!CompareBytearrays(seq, SeqOID))	//make sure Sequence for OID is correct
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8103)	//data read as little endian order (actual data order for Bit String is 03 81)
                    binr.ReadByte();	//advance 1 byte
                else if (twobytes == 0x8203)
                    binr.ReadInt16();	//advance 2 bytes
                else
                    return null;

                bt = binr.ReadByte();
                if (bt != 0x00)		//expect null byte next
                    return null;

                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)	//data read as little endian order (actual data order for Sequence is 30 81)
                    binr.ReadByte();	//advance 1 byte
                else if (twobytes == 0x8230)
                    binr.ReadInt16();	//advance 2 bytes
                else
                    return null;

                twobytes = binr.ReadUInt16();
                byte lowbyte = 0x00;
                byte highbyte = 0x00;

                if (twobytes == 0x8102)	//data read as little endian order (actual data order for Integer is 02 81)
                    lowbyte = binr.ReadByte();	// read next bytes which is bytes in modulus
                else if (twobytes == 0x8202)
                {
                    highbyte = binr.ReadByte();	//advance 2 bytes
                    lowbyte = binr.ReadByte();
                }
                else
                    return null;
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };   //reverse byte order since asn.1 key uses big endian order
                int modsize = BitConverter.ToInt32(modint, 0);

                int firstbyte = binr.PeekChar();
                if (firstbyte == 0x00)
                {	//if first byte (highest order) of modulus is zero, don't include it
                    binr.ReadByte();	//skip this null byte
                    modsize -= 1;	//reduce modulus buffer size by 1
                }

                byte[] modulus = binr.ReadBytes(modsize);	//read the modulus bytes

                if (binr.ReadByte() != 0x02)			//expect an Integer for the exponent data
                    return null;
                int expbytes = (int)binr.ReadByte();		// should only need one byte for actual exponent data (for all useful values)
                byte[] exponent = binr.ReadBytes(expbytes);

                // ------- create RSACryptoServiceProvider instance and initialize with public key -----
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSAParameters RSAKeyInfo = new RSAParameters();
                RSAKeyInfo.Modulus = modulus;
                RSAKeyInfo.Exponent = exponent;
                RSA.ImportParameters(RSAKeyInfo);

                return RSA;
            }

            finally
            {
                binr.Close();
            }
        }

        private bool CompareBytearrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        public void SetPublic(string modulus, string exponent)
        {
            if (string.IsNullOrEmpty(modulus) || string.IsNullOrEmpty(exponent))
                throw new Exception("Invalid RSA public key");
            Modulus = new BigInteger(modulus, 16);
            Exponent = new BigInteger(exponent, 16);
        }

        private BigInteger RSADoPublic(BigInteger x)
        {
            return x.modPow(Exponent, Modulus);
        }

        /// <summary>
        /// Encrypt the content.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Encrypt(string content)
        {
            if (Rsa == null)
            {
                BigInteger tmp = AE(content, (Modulus.bitCount() + 7) >> 3);
                tmp = RSADoPublic(tmp);
                string result = tmp.ToHexString();
                return (result.Length & 1) == 0 ? result : "0" + result;
            }
            else
                return Convert.ToBase64String(Rsa.Encrypt(Encoding.UTF8.GetBytes(content), false));
        }
    }
}
