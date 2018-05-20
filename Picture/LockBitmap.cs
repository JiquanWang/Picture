using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Picture
{
	public class LockBitmap
	{
		Bitmap source = null;
		IntPtr Iptr = IntPtr.Zero;
		BitmapData bitmapData = null;

		public byte[] Pixels { get; set; }
		public int Depth { get; private set; }
		public int Width { get; private set; }
		public int RawWidth { get; private set; }
		public int PixelWidth { get; private set; }
		public int Height { get; private set; }
		public bool Locked { get; private set; }

		public LockBitmap(Bitmap source)
		{
			this.source = source;
			Locked = false;
		}

		public void LockBits()
		{
			try
			{
				Width = source.Width;
				Height = source.Height;
				switch (Depth = Image.GetPixelFormatSize(source.PixelFormat)) // To make it easy, it only supports 8, 24 or 32 bit depth graphics.
				{
					//case 1:
					//    RawWidth = ((Width & 0b11111) == 0) ? Width : Width - (Width & 0b11111) + 32;
					//    PixelWidth = RawWidth >> 3;
					//    break;
					//case 4:
					//    RawWidth = ((Width & 0b111) == 0) ? Width : Width - (Width & 0b111) + 8;
					//    PixelWidth = RawWidth >> 1;
					//    break;
					case 8:
						RawWidth = ((Width & 0b11) == 0) ? Width : Width - (Width & 0b11) + 4;
						PixelWidth = RawWidth;
						break;
					//case 16:
					//    RawWidth = ((Width & 1) == 0) ? Width : Width - (Width & 1) + 2;
					//    PixelWidth = RawWidth << 1;
					//    break;
					case 24:
						RawWidth = Width;
						PixelWidth = Width * 3 + (Width & 0b11);
						break;
					case 32:
						RawWidth = Width;
						PixelWidth = Width << 2;
						break;
				}
				Rectangle rect = new Rectangle(0, 0, Width, Height);
				int PixelCount = PixelWidth * Height;

				bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite, source.PixelFormat);
				Pixels = new byte[PixelCount];
				Iptr = bitmapData.Scan0;

				// Copy data from pointer to array.
				Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);

				Locked = true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void UnlockBits()
		{
			try
			{
				// Copy data from byte array to pointer.
				Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

				// Unlock bitmap data.
				source.UnlockBits(bitmapData);

				Locked = false;
			}
			catch
			{
				//
			}
		}

		public Color GetPixel(int x, int y)
		{
			if (!Locked)
			{
				//
			}

			Color clr = Color.Empty;

			// Get start index of the specified pixel
			int i = y * PixelWidth + x * Depth / 8;

			if (i > Pixels.Length - Depth / 8)
			{
				//throw new IndexOutOfRangeException();
				//
			}

			switch (Depth)
			{
				case 32:
					clr = Color.FromArgb(Pixels[i + 3], Pixels[i + 2], Pixels[i + 1], Pixels[i]);
					break;
				case 24:
					clr = Color.FromArgb(Pixels[i + 2], Pixels[i + 1], Pixels[i]);
					break;
				case 8:
					clr = source.Palette.Entries[Pixels[i]];
					break;
				default:
					break;//
			}
			return clr;
		}

		public void SetPixel(int x, int y, Color color)
		{
			if (!Locked)
			{
				//
			}

			int index;

			switch (Depth)
			{
				case 32:
					index = y * PixelWidth + x * 4;
					Pixels[index] = color.B;
					Pixels[index + 1] = color.G;
					Pixels[index + 2] = color.R;
					Pixels[index + 3] = color.A;
					break;
				case 24:
					index = y * PixelWidth + x * 3;
					Pixels[index] = color.B;
					Pixels[index + 1] = color.G;
					Pixels[index + 2] = color.R;
					break;
				default:
					break;//
			}
		}

		public void SetPixel(int x, int y, byte index)
		{
			if (!Locked)
			{
				//
			}
			if (Depth == 8)
			{
				Pixels[y * PixelWidth + x] = index;
			}
			else
			{
				//
			}
		}
	}
}
