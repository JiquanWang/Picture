using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Picture
{
	sealed class Bmp : SupportedImage
	{
		private FileStream fs;
		private BinaryReader br;
		private int w = 0, h = 0;
		private Color[] palette;
		private Bitmap picture;

		public Bitmap Bitmap => picture;

		public Bmp(MainForm mf, string filename)
		{
			this.mf = mf;

			Load(filename);
		}

		private void Load(string filename)
		{
			bool fsok = false, brok = false;
			try
			{
				long size = new FileInfo(filename).Length;
				if(size < 26)
				{
					//
				}

				UpdateInfo("正在获取文件流...");
				fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
				fsok = true;
				br = new BinaryReader(fs);
				brok = true;

				//分析文件头
				UpdateInfo("正在解析位图文件头...");
				switch(new string(br.ReadChars(2)))
				{
					case "BM": // Windows 3.1x, 95, NT, etc.
						break;
					case "BA": // OS/2 struct Bitmap
					case "CI": // OS/2 struct Color Icon
					case "CP": // OS/2 const Color Pointer
					case "IC": // OS/2 struct Icon
					case "PT": // OS/2 Pointer
						//
					default: // Not supported file format.
						break;//
				}

				int completeSize = br.ReadInt32();
				if(size != completeSize)
				{
					//
				}

				br.ReadInt32();

				int offset = br.ReadInt32();

				//分析文件信息头
				UpdateInfo("正在解析DIB头...");
				uint dibSize = br.ReadUInt32();
				ushort depth = 0;
				uint zip = 0;
				int hrr = 0;
				int vrr = 0;
				uint colors = 0;
				long important = 0;

				switch (dibSize)
				{
					case 40:
						w = br.ReadInt32();
						h = br.ReadInt32();

						if (br.ReadInt16() != 1)
						{
							//
						}

						depth = br.ReadUInt16();
						switch (depth)
						{
							case 1:
							case 4:
							case 8:
							case 24:
							case 32:
								break;
							default:
								break;
						}

						zip = br.ReadUInt32();
						if (zip > 6)
						{
							//
						}
						if (zip != 0)
						{
							//
						}

						uint dataSize = br.ReadUInt32();

						hrr = br.ReadInt32();
						vrr = br.ReadInt32();

						colors = br.ReadUInt32();
						if (colors == 0)
						{
							colors = 1U << depth;
						}
						if (depth >= 24)
						{
							colors = 0;
						}
						if (14 + dibSize + (colors << 2) != offset)
						{
							//
						}
						important = br.ReadUInt32();
						if (important == 0)
						{
							important = colors;
						}
						if (important > colors)
						{
							//
						}
						break;
					case 12: // BITMAPCOREHEADER | OS21XBITMAPHEADER
					case 64: // BITMAPCOREHEADER2 | OS22XBITMAPHEADER
					case 52: // BITMAPV2INFOHEADER
					case 56: // BITMAPV3INFOHEADER
					case 108: // BITMAPV4HEADER
					case 124: // BITMAPV5HEADER
							  //
					default: // Not implemented DIB header.
						break;//
				}

				//读取颜色表
				if (colors > 0)
				{
					UpdateInfo("正在获取调色板信息...");
					palette = new Color[colors];
					int r, g, b;
					for(int i = 0; i < colors; i++)
					{
						b = br.ReadByte();
						g = br.ReadByte();
						r = br.ReadByte();
						br.ReadByte();
						palette[i] = Color.FromArgb(r, g, b);
					}
				}

				//绘图
				UpdateInfo("正在绘图...");

				switch(depth)
				{
					case 1:
						Depth1();
						break;
					case 4:
						Depth4();
						break;
					case 8:
						Depth8();
						break;
					case 24:
						Depth24();
						break;
					case 32:
						Depth32();
						break;
					default:
						break;
				}
				//完成
				br.Close();
				fs.Close();
			}
			catch(Exception ex)
			{
				if (brok)
				{
					br.Close();
				}
				if (fsok)
				{
					fs.Close();
				}
				throw ex;
			}
		}

		private void Depth1()
		{
			byte datum;
			int px, py = h - 1, pz, rw, all;
			picture = new Bitmap(w, h);

			px = w % 32;
			rw = px == 0 ? w : w - px + 32;
			all = rw * h;
			for (; py >= 0; --py)
			{
				for (px = 0; px < rw; px += 8)
				{
					datum = br.ReadByte();
					if (px < w)
					{
						for (pz = 0; pz < 8; ++pz)
						{
							if (px + pz < w)
							{
								picture.SetPixel(px + pz, py, palette[datum >> (7 - pz) & 1]);
								UpdateProg(100 * ((h - 1 - py) * rw + px + pz) / all);
							}
						}
					}
				}
			}
		}

		private void Depth4()
		{
			byte datum;
			int px, py = h - 1, rw, all;
			picture = new Bitmap(w, h);

			px = w % 8;
			rw = px == 0 ? w : w - px + 8;
			all = rw * h;
			for (; py >= 0; --py)
			{
				for (px = 0; px < rw; ++px)
				{
					datum = br.ReadByte();
					if (px < w)
					{
						picture.SetPixel(px, py, palette[datum >> 4]);
						if (++px < w)
						{
							picture.SetPixel(px, py, palette[datum & 0b00001111]);
						}
					}
					else
					{
						++px;
					}
					UpdateProg(100 * ((h - 1 - py) * rw + px) / all);
				}
			}
		}

		private void Depth8()
		{
			int px, py = h - 1, rw, all;
			picture = new Bitmap(w, h);

			px = w % 4;
			rw = px == 0 ? w : w - px + 4;
			all = rw * h;
			for (; py >= 0; --py)
			{
				for (px = 0; px < rw; ++px)
				{
					if (px < w)
					{
						picture.SetPixel(px, py, palette[br.ReadByte()]);
					}
					else
					{
						br.ReadByte();
					}
					UpdateProg(100 * ((h - 1 - py) * rw + px) / all);
				}
			}
		}

		private void Depth24()
		{
			int px, py = h - 1, all;
			picture = new Bitmap(w, h);

			//并未用到调色板
			all = w * h;
			int r, g, b;
			int blank = w % 4;
			for (; py >= 0; --py)
			{
				for (px = 0; px < w; ++px)
				{
					b = br.ReadByte();
					g = br.ReadByte();
					r = br.ReadByte();
					picture.SetPixel(px, py, Color.FromArgb(r, g, b));
					UpdateProg(100 * ((h - 1 - py) * w + px) / all);
				}
				if (blank > 0)
				{
					br.ReadBytes(blank);
				}
			}
		}

		private void Depth32()
		{
			//
		}

	}
}
