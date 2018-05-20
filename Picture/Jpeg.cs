using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Picture
{
	unsafe sealed class Jpeg : SupportedImage
	{
		/*
        Define ZigZag[] first and then generate AntiZigZag[].

        static readonly byte[] ZigZag =
        {
             0,   1,   5,   6,  14,  15,  27,  28,
             2,   4,   7,  13,  16,  26,  29,  42,
             3,   8,  12,  17,  25,  30,  41,  43,
             9,  11,  18,  24,  31,  40,  44,  53,
            10,  19,  23,  32,  39,  45,  52,  54,
            20,  22,  33,  38,  46,  51,  55,  60,
            21,  34,  37,  47,  50,  56,  59,  61,
            35,  36,  48,  49,  57,  58,  62,  63
        }
        for (int i = 0; i < 64; ++i)
        {
            AntiZigZag[ZigZag[i]] = i;
        }

        Here's the result we need:
        */
		private static readonly byte[] AntiZigZag =
		{
			 0,   1,   8,  16,   9,   2,   3,  10,
			17,  24,  32,  25,  18,  11,   4,   5,
			12,  19,  26,  33,  40,  48,  41,  34,
			27,  20,  13,   6,   7,  14,  21,  28,
			35,  42,  49,  56,  57,  50,  43,  36,
			29,  22,  15,  23,  30,  37,  44,  51,
			58,  59,  52,  45,  38,  31,  39,  46,
			53,  60,  61,  54,  47,  55,  62,  63
		};

		private const int W1 = 2841;
		private const int W2 = 2676;
		private const int W3 = 2408;
		private const int W5 = 1609;
		private const int W6 = 1108;
		private const int W7 = 565;

		private const int CF4A = -9;
		private const int CF4B = 111;
		private const int CF4C = 29;
		private const int CF4D = -3;
		private const int CF3A = 28;
		private const int CF3B = 109;
		private const int CF3C = -9;
		private const int CF3X = 104;
		private const int CF3Y = 27;
		private const int CF3Z = -3;
		private const int CF2A = 139;
		private const int CF2B = -11;

		private bool IsDisposed;
		private bool IsDone;

		private byte* pos;
		private int size;
		private int length;
		private int width, height;
		private int mbwidth, mbheight;
		private int mbsizex, mbsizey;
		private int ncomp;
		private Component* comp;
		private int qtused, qtavail;
		private byte** qtab;
		private VLCCode** vlctab;
		private int buf, bufbits;
		private int* block;
		private int rstinterval;
		private byte* rgb;

		public Bitmap Bitmap
		{
			get
			{
				UpdateInfo("正在绘制图像…");
				UpdateProg(3);

				long all = (long)width * height * ncomp;
				Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
				LockBitmap lb = new LockBitmap(bmp);
				lb.LockBits();
				if (ncomp == 1)
				{
					byte* start = comp[0].pixels;
					int count = 0;
					for (int y = 0; y < height; ++y)
					{
						for (int x = 0; x < width; ++x)
						{
							lb.SetPixel(x, y, Color.FromArgb(start[count], start[count], start[count]));
							++count;
							UpdateProg((int)(3 + 97L * count / all));
						}
					}
				}
				else
				{
					byte* start = rgb;
					int count = 0;
					for (int y = 0; y < height; ++y)
					{
						for (int x = 0; x < width; ++x)
						{
							lb.SetPixel(x, y, Color.FromArgb(start[count], start[count + 1], start[count + 2]));
							count += 3;
							UpdateProg((int)(3 + 97L * count / all));
						}
					}
				}
				lb.UnlockBits();
				return bmp;
			}
		}

		public Jpeg(MainForm fm, string filename)
		{
			this.mf = fm;

			comp = (Component*)Marshal.AllocHGlobal(3 * Marshal.SizeOf(typeof(Component)));
			block = (int*)Marshal.AllocHGlobal(64 * Marshal.SizeOf(typeof(int)));

			FillMem(comp, new Component(), 3);

			qtab = (byte**)Marshal.AllocHGlobal(4 * IntPtr.Size);
			vlctab = (VLCCode**)Marshal.AllocHGlobal(4 * IntPtr.Size);
			for (int i = 0; i < 4; i++)
			{
				qtab[i] = (byte*)Marshal.AllocHGlobal(64 * Marshal.SizeOf(typeof(byte)));
				vlctab[i] = (VLCCode*)Marshal.AllocHGlobal(65536 * Marshal.SizeOf(typeof(VLCCode)));

				FillMem((long*)qtab[i], 0, 64 / 8); // use long instead of byte
				FillMem((long*)vlctab[i], 0, 65536 / 4); // use long instead of VLCCode (length=2)
			}

			Decode(filename);
		}

		~Jpeg()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool managed)
		{
			if (!IsDisposed)
			{
				if (rgb != null)
				{
					Marshal.FreeHGlobal((IntPtr)rgb);
					rgb = null;
				}

				for (int i = 0; i < 4; i++)
				{
					if (qtab[i] != null)
					{
						Marshal.FreeHGlobal((IntPtr)qtab[i]);
					}
					if (vlctab[i] != null)
					{
						Marshal.FreeHGlobal((IntPtr)vlctab[i]);
					}
				}

				if (qtab != null)
				{
					Marshal.FreeHGlobal((IntPtr)qtab);
				}
				if (vlctab != null)
				{
					Marshal.FreeHGlobal((IntPtr)vlctab);
				}
				if (comp != null)
				{
					Marshal.FreeHGlobal((IntPtr)comp);
				}
				if (block != null)
				{
					Marshal.FreeHGlobal((IntPtr)block);
				}
				IsDisposed = true;
			}
		}

		private void Decode(string jpeg)
		{
			byte[] data = File.ReadAllBytes(jpeg);
			fixed (byte* ptr = data)
			{
				Decode(ptr, data.Length, false);
			}
		}

		private void Decode(byte* jpeg, int size, bool flip)
		{
			Init();

			UpdateInfo("JPEG解码器初始化完成，正在分析文件头…");
			UpdateProg(1);

			pos = jpeg;
			this.size = size & 0x7FFFFFFF;
			if (this.size < 2)
			{
				//
			}
			if (((pos[0] ^ 0xFF) | (pos[1] ^ 0xD8)) != 0)
			{
				//
			}
			Skip(2);
			while (!IsDone)
			{
				if ((this.size < 2) || (pos[0] != 0xFF))
				{
					//
				}
				Skip(2);
				switch (pos[-1])
				{
					case 0xC0: DecodeSOF(); break;
					case 0xC4: DecodeDHT(); break;
					case 0xDB: DecodeDQT(); break;
					case 0xDD: DecodeDRI(); break;
					case 0xDA: DecodeScan(); break;
					case 0xFE: SkipMarker(); break;
					default:
						if ((pos[-1] & 0xF0) >= 0xC0)
						{
							SkipMarker();
						}
						else
						{
							//
						}
						break;
				}
			}

			UpdateInfo("正在解码…");
			UpdateProg(2);

			ConvertYCC(flip);
		}

		private void Init()
		{
			if (rgb != null)
			{
				Marshal.FreeHGlobal((IntPtr)rgb);
				rgb = null;
			}

			FillMem(comp, new Component(), 3);
			IsDone = false;
			bufbits = 0;
		}

		private void RowIDCT(int* blk)
		{
			int x0, x1, x2, x3, x4, x5, x6, x7, x8;
			if (((x1 = blk[4] << 11)
				| (x2 = blk[6])
				| (x3 = blk[2])
				| (x4 = blk[1])
				| (x5 = blk[7])
				| (x6 = blk[5])
				| (x7 = blk[3])) == 0)
			{
				blk[0] = blk[1] = blk[2] = blk[3] = blk[4] = blk[5] = blk[6] = blk[7] = blk[0] << 3;
				return;
			}
			x0 = (blk[0] << 11) + 128;
			x8 = W7 * (x4 + x5);
			x4 = x8 + (W1 - W7) * x4;
			x5 = x8 - (W1 + W7) * x5;
			x8 = W3 * (x6 + x7);
			x6 = x8 - (W3 - W5) * x6;
			x7 = x8 - (W3 + W5) * x7;
			x8 = x0 + x1;
			x0 -= x1;
			x1 = W6 * (x3 + x2);
			x2 = x1 - (W2 + W6) * x2;
			x3 = x1 + (W2 - W6) * x3;
			x1 = x4 + x6;
			x4 -= x6;
			x6 = x5 + x7;
			x5 -= x7;
			x7 = x8 + x3;
			x8 -= x3;
			x3 = x0 + x2;
			x0 -= x2;
			x2 = (181 * (x4 + x5) + 128) >> 8;
			x4 = (181 * (x4 - x5) + 128) >> 8;
			blk[0] = (x7 + x1) >> 8;
			blk[1] = (x3 + x2) >> 8;
			blk[2] = (x0 + x4) >> 8;
			blk[3] = (x8 + x6) >> 8;
			blk[4] = (x8 - x6) >> 8;
			blk[5] = (x0 - x4) >> 8;
			blk[6] = (x3 - x2) >> 8;
			blk[7] = (x7 - x1) >> 8;
		}

		private void ColIDCT(int* blk, byte* outv, int stride)
		{
			int x0, x1, x2, x3, x4, x5, x6, x7, x8;
			if (((x1 = blk[8 * 4] << 8)
				| (x2 = blk[8 * 6])
				| (x3 = blk[8 * 2])
				| (x4 = blk[8 * 1])
				| (x5 = blk[8 * 7])
				| (x6 = blk[8 * 5])
				| (x7 = blk[8 * 3])) == 0)
			{
				x1 = Clip(((blk[0] + 32) >> 6) + 128);
				for (x0 = 8; x0 != 0; --x0)
				{
					*outv = (byte)x1;
					outv += stride;
				}
				return;
			}
			x0 = (blk[0] << 8) + 8192;
			x8 = W7 * (x4 + x5) + 4;
			x4 = (x8 + (W1 - W7) * x4) >> 3;
			x5 = (x8 - (W1 + W7) * x5) >> 3;
			x8 = W3 * (x6 + x7) + 4;
			x6 = (x8 - (W3 - W5) * x6) >> 3;
			x7 = (x8 - (W3 + W5) * x7) >> 3;
			x8 = x0 + x1;
			x0 -= x1;
			x1 = W6 * (x3 + x2) + 4;
			x2 = (x1 - (W2 + W6) * x2) >> 3;
			x3 = (x1 + (W2 - W6) * x3) >> 3;
			x1 = x4 + x6;
			x4 -= x6;
			x6 = x5 + x7;
			x5 -= x7;
			x7 = x8 + x3;
			x8 -= x3;
			x3 = x0 + x2;
			x0 -= x2;
			x2 = (181 * (x4 + x5) + 128) >> 8;
			x4 = (181 * (x4 - x5) + 128) >> 8;
			*outv = Clip(((x7 + x1) >> 14) + 128); outv += stride;
			*outv = Clip(((x3 + x2) >> 14) + 128); outv += stride;
			*outv = Clip(((x0 + x4) >> 14) + 128); outv += stride;
			*outv = Clip(((x8 + x6) >> 14) + 128); outv += stride;
			*outv = Clip(((x8 - x6) >> 14) + 128); outv += stride;
			*outv = Clip(((x0 - x4) >> 14) + 128); outv += stride;
			*outv = Clip(((x3 - x2) >> 14) + 128); outv += stride;
			*outv = Clip(((x7 - x1) >> 14) + 128);
		}

		private int ShowBits(int bits)
		{
			byte newbyte;
			if (bits == 0)
			{
				return 0;
			}
			while (bufbits < bits)
			{
				if (size <= 0)
				{
					buf = (buf << 8) | 0xFF;
					bufbits += 8;
					continue;
				}
				newbyte = *pos++;
				size--;
				bufbits += 8;
				buf = (buf << 8) | newbyte;
				if (newbyte == 0xFF)
				{
					if (size != 0)
					{
						byte marker = *pos++;
						size--;
						switch (marker)
						{
							case 0x00:
							case 0xFF:
								break;
							case 0xD9: size = 0; break;
							default:
								if ((marker & 0xF8) != 0xD0)
								{
									//
								}
								else
								{
									buf = (buf << 8) | marker;
									bufbits += 8;
								}
								break;
						}
					}
					else
					{
						//
					}
				}
			}
			return (buf >> (bufbits - bits)) & ((1 << bits) - 1);
		}

		private void DecodeSOF()
		{
			int i, ssxmax = 0, ssymax = 0;
			Component* c;
			DecodeLength();
			if (length < 9)
			{
				//
			}
			if (pos[0] != 8)
			{
				//
			}
			height = Decode16(pos + 1);
			width = Decode16(pos + 3);
			ncomp = pos[5];
			Skip(6);
			switch (ncomp)
			{
				case 1:
				case 3:
					break;
				default:
					break;//
			}
			if (length < (ncomp * 3))
			{
				//
			}
			for (i = 0, c = comp; i < ncomp; ++i, ++c)
			{
				c->cid = pos[0];
				if ((c->ssx = pos[1] >> 4) == 0)
				{
					//
				}
				if ((c->ssx & (c->ssx - 1)) != 0)
				{
					// non-power of two
				}
				if ((c->ssy = pos[1] & 15) == 0)
				{
					//
				}
				if ((c->ssy & (c->ssy - 1)) != 0)
				{
					// non-power of two
				}
				if (((c->qtsel = pos[2]) & 0xFC) != 0)
				{
					//
				}
				Skip(3);
				qtused |= 1 << c->qtsel;
				if (c->ssx > ssxmax)
				{
					ssxmax = c->ssx;
				}
				if (c->ssy > ssymax)
				{
					ssymax = c->ssy;
				}
			}
			if (ncomp == 1)
			{
				c = comp;
				c->ssx = c->ssy = ssxmax = ssymax = 1;
			}
			mbsizex = ssxmax << 3;
			mbsizey = ssymax << 3;
			mbwidth = (width + mbsizex - 1) / mbsizex;
			mbheight = (height + mbsizey - 1) / mbsizey;
			for (i = 0, c = comp; i < ncomp; ++i, ++c)
			{
				c->width = (width * c->ssx + ssxmax - 1) / ssxmax;
				c->height = (height * c->ssy + ssymax - 1) / ssymax;
				c->stride = mbwidth * c->ssx << 3;
				if (((c->width < 3) && (c->ssx != ssxmax)) || ((c->height < 3) && (c->ssy != ssymax)))
				{
					//
				}
				c->pixels = (byte*)Marshal.AllocHGlobal(c->stride * mbheight * c->ssy << 3);
			}
			if (ncomp == 3)
			{
				rgb = (byte*)Marshal.AllocHGlobal(width * height * ncomp);
			}
			Skip(length);
		}

		private void DecodeDHT()
		{
			int codelen, currcnt, remain, spread, i, j;
			VLCCode* vlc;
			byte* counts = stackalloc byte[16];
			DecodeLength();
			while (length >= 17)
			{
				i = pos[0];
				if ((i & 0xEC) != 0)
				{
					//
				}
				if ((i & 0x02) != 0)
				{
					//
				}
				i = (i | (i >> 3)) & 3;  // combined DC/AC + tableid value
				for (codelen = 1; codelen <= 16; ++codelen)
				{
					counts[codelen - 1] = pos[codelen];
				}
				Skip(17);
				vlc = &vlctab[i][0];
				remain = spread = 65536;
				for (codelen = 1; codelen <= 16; ++codelen)
				{
					spread >>= 1;
					currcnt = counts[codelen - 1];
					if (currcnt == 0)
					{
						continue;
					}
					if (length < currcnt)
					{
						//
					}
					remain -= currcnt << (16 - codelen);
					if (remain < 0)
					{
						//
					}
					for (i = 0; i < currcnt; ++i)
					{
						byte code = pos[i];
						for (j = spread; j != 0; --j)
						{
							vlc->bits = (byte)codelen;
							vlc->code = code;
							++vlc;
						}
					}
					Skip(currcnt);
				}
				while (remain-- != 0)
				{
					vlc->bits = 0;
					++vlc;
				}
			}
			if (length != 0)
			{
				//
			}
		}

		private void DecodeDQT()
		{
			int i;
			byte* t;
			DecodeLength();
			while (length >= 65)
			{
				i = pos[0];
				if ((i & 0xFC) != 0)
				{
					//
				}
				qtavail |= 1 << i;
				t = &qtab[i][0];
				for (i = 0; i < 64; ++i)
				{
					t[i] = pos[i + 1];
				}
				Skip(65);
			}
			if (length != 0)
			{
				//
			}
		}

		private int GetVLC(VLCCode* vlc, byte* code)
		{
			int value = ShowBits(16);
			int bits = vlc[value].bits;
			if (bits == 0)
			{
				//
			}
			SkipBits(bits);
			value = vlc[value].code;
			if (code != null)
			{
				*code = (byte)value;
			}
			bits = value & 15;
			if (bits == 0)
			{
				return 0;
			}
			value = GetBits(bits);
			if (value < (1 << (bits - 1)))
			{
				value += ((-1) << bits) + 1;
			}
			return value;
		}

		private void DecodeBlock(Component* c, byte* outv)
		{
			byte code = 0;
			int value, coef = 0;
			FillMem((long*)block, 0, 64 / 2); // use long instead of int
			c->dcpred += GetVLC(&vlctab[c->dctabsel][0], null);
			block[0] = (c->dcpred) * qtab[c->qtsel][0];
			do
			{
				value = GetVLC(&vlctab[c->actabsel][0], &code);
				if (code == 0)
				{
					break;  // EOB
				}
				if ((code & 0x0F) == 0 && code != 0xF0)
				{
					//
				}
				coef += (code >> 4) + 1;
				if (coef > 63)
				{
					//
				}
				block[AntiZigZag[coef]] = value * qtab[c->qtsel][coef];
			} while (coef < 63);
			for (coef = 0; coef < 64; coef += 8) { RowIDCT(&block[coef]); }
			for (coef = 0; coef < 8; ++coef) { ColIDCT(&block[coef], &outv[coef], c->stride); }
		}

		private void DecodeScan()
		{
			int i, mbx, mby, sbx, sby;
			int rstcount = rstinterval, nextrst = 0;
			Component* c;
			DecodeLength();
			if (length < (4 + 2 * ncomp))
			{
				//
			}
			if (pos[0] != ncomp)
			{
				//
			}
			Skip(1);
			for (i = 0, c = comp; i < ncomp; ++i, ++c)
			{
				if (pos[0] != c->cid)
				{
					//
				}
				if ((pos[1] & 0xEE) != 0)
				{
					//
				}
				c->dctabsel = pos[1] >> 4;
				c->actabsel = (pos[1] & 1) | 2;
				Skip(2);
			}
			if (pos[0] != 0 || pos[1] != 63 || pos[2] != 0)
			{
				//
			}
			Skip(length);
			mbx = mby = 0;
			while (true)
			{
				for (i = 0, c = comp; i < ncomp; ++i, ++c)
				{
					for (sby = 0; sby < c->ssy; ++sby)
					{
						for (sbx = 0; sbx < c->ssx; ++sbx)
						{
							DecodeBlock(c, &c->pixels[((mby * c->ssy + sby) * c->stride + mbx * c->ssx + sbx) << 3]);
						}
					}
				}
				if (++mbx >= mbwidth)
				{
					mbx = 0;
					if (++mby >= mbheight)
					{
						break;
					}
				}
				if (rstinterval != 0 && --rstcount == 0)
				{
					bufbits &= 0xF8;
					i = GetBits(16);
					if ((i & 0xFFF8) != 0xFFD0 || (i & 7) != nextrst)
					{
						//
					}
					nextrst = (nextrst + 1) & 7;
					rstcount = rstinterval;
					for (i = 0; i < 3; ++i) { comp[i].dcpred = 0; }
				}
			}
			IsDone = true;
		}

		private void UpsampleH(Component* c)
		{
			int xmax = c->width - 3;
			byte* outv, lin, lout;
			int x, y;
			try
			{
				outv = (byte*)Marshal.AllocHGlobal((c->width * c->height) << 1);
				lin = c->pixels;
				lout = outv;
				for (y = c->height; y != 0; --y)
				{
					lout[0] = CF(CF2A * lin[0] + CF2B * lin[1]);
					lout[1] = CF(CF3X * lin[0] + CF3Y * lin[1] + CF3Z * lin[2]);
					lout[2] = CF(CF3A * lin[0] + CF3B * lin[1] + CF3C * lin[2]);

					for (x = 0; x < xmax; ++x)
					{
						lout[(x << 1) + 3] = CF(CF4A * lin[x] + CF4B * lin[x + 1] + CF4C * lin[x + 2] + CF4D * lin[x + 3]);
						lout[(x << 1) + 4] = CF(CF4D * lin[x] + CF4C * lin[x + 1] + CF4B * lin[x + 2] + CF4A * lin[x + 3]);
					}

					lin += c->stride;
					lout += c->width << 1;

					lout[-3] = CF(CF3A * lin[-1] + CF3B * lin[-2] + CF3C * lin[-3]);
					lout[-2] = CF(CF3X * lin[-1] + CF3Y * lin[-2] + CF3Z * lin[-3]);
					lout[-1] = CF(CF2A * lin[-1] + CF2B * lin[-2]);
				}
				c->width <<= 1;
				c->stride = c->width;
			}
			finally
			{
				if (c->pixels != null)
				{
					Marshal.FreeHGlobal((IntPtr)c->pixels);
				}
			}
			c->pixels = outv;
		}

		private void UpsampleV(Component* c)
		{
			int w = c->width, s1 = c->stride, s2 = s1 + s1;
			byte* outv, cin, cout;
			int x, y;
			try
			{
				outv = (byte*)Marshal.AllocHGlobal((c->width * c->height) << 1);
				for (x = 0; x < w; ++x)
				{
					cin = &c->pixels[x];
					cout = &outv[x];
					*cout = CF(CF2A * cin[0] + CF2B * cin[s1]); cout += w;
					*cout = CF(CF3X * cin[0] + CF3Y * cin[s1] + CF3Z * cin[s2]); cout += w;
					*cout = CF(CF3A * cin[0] + CF3B * cin[s1] + CF3C * cin[s2]); cout += w;
					cin += s1;
					for (y = c->height - 3; y != 0; --y)
					{
						*cout = CF(CF4A * cin[-s1] + CF4B * cin[0] + CF4C * cin[s1] + CF4D * cin[s2]); cout += w;
						*cout = CF(CF4D * cin[-s1] + CF4C * cin[0] + CF4B * cin[s1] + CF4A * cin[s2]); cout += w;
						cin += s1;
					}
					cin += s1;
					*cout = CF(CF3A * cin[0] + CF3B * cin[-s1] + CF3C * cin[-s2]); cout += w;
					*cout = CF(CF3X * cin[0] + CF3Y * cin[-s1] + CF3Z * cin[-s2]); cout += w;
					*cout = CF(CF2A * cin[0] + CF2B * cin[-s1]);
				}
				c->height <<= 1;
				c->stride = c->width;
			}
			finally
			{
				if (c->pixels != null)
				{
					Marshal.FreeHGlobal((IntPtr)c->pixels);
				}
			}
			c->pixels = outv;
		}

		private void ConvertYCC(bool flip)
		{
			int i;
			int w = width;
			int h = height;
			Component* c;

			for (i = 0, c = comp; i < ncomp; ++i, ++c)
			{
				while ((c->width < w) || (c->height < h))
				{
					if (c->width < w)
					{
						UpsampleH(c);
					}
					if (c->height < h)
					{
						UpsampleV(c);
					}
				}
				if ((c->width < w) || (c->height < h))
				{
					//
				}
			}

			if (ncomp == 3)
			{
				// convert to RGB
				int x, yy, y, cb, cr, r, g, b;
				byte* prgb = rgb;
				byte* py = comp[0].pixels;
				byte* pcb = comp[1].pixels;
				byte* pcr = comp[2].pixels;
				int rs = comp[0].stride - w;
				int gs = comp[1].stride - w;
				int bs = comp[2].stride - w;

				for (yy = height; yy != 0; --yy)
				{
					for (x = 0; x < w; ++x)
					{
						y = *py++ << 8;
						cb = *pcb++ - 128;
						cr = *pcr++ - 128;

						g = (y - 88 * cb - 183 * cr + 128) >> 8;

						if (flip)
						{
							b = (y + 359 * cr + 128) >> 8;
							r = (y + 454 * cb + 128) >> 8;
						}
						else
						{
							r = (y + 359 * cr + 128) >> 8;
							b = (y + 454 * cb + 128) >> 8;
						}

						if (r < 0)
						{
							*prgb++ = 0;
						}
						else if (r > 0xFF)
						{
							*prgb++ = 0xFF;
						}
						else
						{
							*prgb++ = (byte)r;
						}

						if (g < 0)
						{
							*prgb++ = 0;
						}
						else if (g > 0xFF)
						{
							*prgb++ = 0xFF;
						}
						else
						{
							*prgb++ = (byte)g;
						}

						if (b < 0)
						{
							*prgb++ = 0;
						}
						else if (b > 0xFF)
						{
							*prgb++ = 0xFF;
						}
						else
						{
							*prgb++ = (byte)b;
						}
					}
					py += rs;
					pcb += gs;
					pcr += bs;
				}

				Marshal.FreeHGlobal((IntPtr)comp[0].pixels);
				Marshal.FreeHGlobal((IntPtr)comp[1].pixels);
				Marshal.FreeHGlobal((IntPtr)comp[2].pixels);
				comp[0].pixels = comp[1].pixels = comp[2].pixels = null;
			}
			else if (comp[0].width != comp[0].stride)
			{
				// grayscale -> only remove stride
				int y, x;
				int cw = comp[0].width;
				int cs = comp[0].stride;
				int d = cs - cw;
				byte* pin = &comp[0].pixels[cs];
				byte* pout = &comp[0].pixels[cw];

				for (y = comp[0].height - 1; y != 0; --y)
				{
					for (x = 0; x < cw; x++)
					{
						*pout++ = *pin++;
					}
					pin += d;
				}
				comp[0].stride = cw;

				Marshal.FreeHGlobal((IntPtr)comp[0].pixels);
				comp[0].pixels = null;
			}
		}

		private static byte Clip(int x)
		{
			if (x < 0)
			{
				return 0;
			}
			else if (x > 0xFF)
			{
				return 0xFF;
			}
			else
			{
				return (byte)x;
			}
		}

		private static byte CF(int x)
		{
			x = (x + 64) >> 7;
			if (x < 0)
			{
				return 0;
			}
			else if (x > 0xFF)
			{
				return 0xFF;
			}
			else
			{
				return (byte)x;
			}
		}

		private void SkipBits(int bits)
		{
			if (bufbits < bits)
			{
				ShowBits(bits);
			}
			bufbits -= bits;
		}

		private int GetBits(int bits)
		{
			int res = ShowBits(bits);
			SkipBits(bits);
			return res;
		}

		private void Skip(int count)
		{
			pos += count;
			size -= count;
			length -= count;
			if (size < 0)
			{
				//
			}
		}

		private void DecodeLength()
		{
			if (size < 2)
			{
				//
			}
			length = Decode16(pos);
			if (length > size)
			{
				//
			}
			Skip(2);
		}

		private void SkipMarker()
		{
			DecodeLength();
			Skip(length);
		}

		private void DecodeDRI()
		{
			DecodeLength();
			if (length < 2)
			{
				//
			}
			rstinterval = Decode16(pos);
			Skip(length);
		}

		private static ushort Decode16(byte* pos)
		{
			return (ushort)((pos[0] << 8) | pos[1]);
		}

		private static void FillMem(byte* block, byte value, int count)
		{
			for (int i = 0; i < count; i++)
			{
				block[i] = value;
			}
		}

		private static void FillMem(int* block, int value, int count)
		{
			for (int i = 0; i < count; i++)
			{
				block[i] = value;
			}
		}

		private static void FillMem(long* block, int value, int count)
		{
			for (int i = 0; i < count; i++)
			{
				block[i] = value;
			}
		}

		private static void FillMem(Component* block, Component value, int count)
		{
			for (int i = 0; i < count; i++)
			{
				block[i] = value;
			}
		}

		private static void FillMem(VLCCode* block, VLCCode value, int count)
		{
			for (int i = 0; i < count; i++)
			{
				block[i] = value;
			}
		}
	}
}
