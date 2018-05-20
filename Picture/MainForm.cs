using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace Picture
{
	public partial class MainForm : Form
	{
		private string fileName = "";
		private int mx = -1, my = -1;
		private DateTime time;
		private Bitmap picture = null;

		public MainForm()
		{
			InitializeComponent();
		}

		private void TsmiOpen_Click(object sender, EventArgs e)
		{
			progress.Value = 0;
			labelStatus.Text = "等待选择文件...";
			if(ofd.ShowDialog() == DialogResult.OK && File.Exists(fileName = ofd.FileName))
			{
				LoadByFileName();
			}
			else
			{
				labelStatus.Text = "打开文件操作被用户取消。";
			}
		}

		private void LoadByFileName()
		{
			toolTip.Active = false;
			time = DateTime.Now;
			progress.Value = 0;
			labelStatus.Text = "正在准备解析文件...";
			long size = new FileInfo(fileName).Length;
			string ext = fileName.Substring(fileName.LastIndexOf(".") + 1).ToLower();
			if(ext == "bmp" || ext =="dib")
			{
				new Thread(new ThreadStart(LoadBmp)).Start();
			}
			else if(ext == "jpeg" || ext == "jpg")
			{
				new Thread(new ThreadStart(LoadJpeg)).Start();
			}
			else
			{
				string errorMessage = "不支持此格式！";
				MessageBox.Show(errorMessage, "Picture - 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				labelStatus.Text = errorMessage;
			}
		}

		private void LoadBmp()
		{
			picture = new Bmp(this, fileName).Bitmap;
			PaintPic();
		}

		private void LoadJpeg()
		{
			picture = new Jpeg(this, fileName).Bitmap;
			PaintPic();
		}

		private void ShowPixelDetail(object sender, MouseEventArgs e)
		{
			if(toolTip.Active && (mx!=e.X || my != e.Y))
			{
				mx = e.X;
				my = e.Y;
				Color c = picture.GetPixel(mx, my);
				toolTip.SetToolTip(pictureBox, $"x,y: ({mx},{my})\nRGB: ({c.R},{c.G},{c.B})\nHTML: (#{c.R.ToString("X2")}{c.G.ToString("X2")}{c.B.ToString("X2")})");
			}
		}

		public void PaintPic()
		{
			if(pictureBox.Image != null)
			{
				pictureBox.Image.Dispose();
			}
			pictureBox.Image = picture;
			pictureBox.Height = picture.Height;
			pictureBox.Width = picture.Width;
			labelStatus.Text = $"图片读取成功！耗时：{(int)((DateTime.Now - time).TotalMilliseconds)}ms";
			progress.Value = 100;
			toolTip.Active = true;
			GC.Collect();
		}
	}
}