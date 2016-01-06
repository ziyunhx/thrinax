using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace Thrinax.Helper
{
	/// <summary>
	/// Image helper.
	/// </summary>
	public class ImageHelper
	{
		/// <summary>
		/// Gets the bytes by image.
		/// </summary>
		/// <returns>The bytes by image.</returns>
		/// <param name="image">Image.</param>
		public static byte[] GetBytesByImage(Image image)
		{
			if(image != null)
			{
				using (MemoryStream ms = new MemoryStream())
				{
					image.Save(ms, ImageFormat.Jpeg);
					ms.Position = 0;
					byte[] imageBytes = new byte[ms.Length];
					ms.Read(imageBytes, 0, imageBytes.Length);
					return imageBytes;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the bytes by image path.
		/// </summary>
		/// <returns>The bytes by image path.</returns>
		/// <param name="strFile">String file.</param>
		public static byte[] GetBytesByImagePath(string strFile)
		{
			Image image=Image.FromFile(strFile);

			return GetBytesByImage(image);
		}

		/// <summary>
		/// Gets the image by bytes.
		/// </summary>
		/// <returns>The image by bytes.</returns>
		/// <param name="bytes">Bytes.</param>
		public static Image GetImageByBytes(byte[] bytes)
		{
			Image photo = null;
			using (MemoryStream ms = new MemoryStream(bytes))
			{
				ms.Write(bytes, 0, bytes.Length);
				photo = Image.FromStream(ms, true);
			}

			return photo;
		}
	}
}

