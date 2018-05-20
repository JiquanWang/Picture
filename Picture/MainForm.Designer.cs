namespace Picture
{
	partial class MainForm
	{
		/// <summary>
		/// 必需的设计器变量。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 清理所有正在使用的资源。
		/// </summary>
		/// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows 窗体设计器生成的代码

		/// <summary>
		/// 设计器支持所需的方法 - 不要修改
		/// 使用代码编辑器修改此方法的内容。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.TsmiFile = new System.Windows.Forms.ToolStripMenuItem();
			this.TsmiOpen = new System.Windows.Forms.ToolStripMenuItem();
			this.TsmiSave = new System.Windows.Forms.ToolStripMenuItem();
			this.ofd = new System.Windows.Forms.OpenFileDialog();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.progress = new System.Windows.Forms.ToolStripProgressBar();
			this.labelStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.panel = new System.Windows.Forms.Panel();
			this.pictureBox = new System.Windows.Forms.PictureBox();
			this.menuStrip.SuspendLayout();
			this.statusStrip.SuspendLayout();
			this.panel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// menuStrip
			// 
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TsmiFile});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(643, 25);
			this.menuStrip.TabIndex = 0;
			this.menuStrip.Text = "menuStrip";
			// 
			// TsmiFile
			// 
			this.TsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.TsmiOpen,
            this.TsmiSave});
			this.TsmiFile.Name = "TsmiFile";
			this.TsmiFile.Size = new System.Drawing.Size(44, 21);
			this.TsmiFile.Text = "文件";
			// 
			// TsmiOpen
			// 
			this.TsmiOpen.Name = "TsmiOpen";
			this.TsmiOpen.Size = new System.Drawing.Size(100, 22);
			this.TsmiOpen.Text = "打开";
			this.TsmiOpen.Click += new System.EventHandler(this.TsmiOpen_Click);
			// 
			// TsmiSave
			// 
			this.TsmiSave.Name = "TsmiSave";
			this.TsmiSave.Size = new System.Drawing.Size(100, 22);
			this.TsmiSave.Text = "保存";
			// 
			// ofd
			// 
			this.ofd.Filter = "所有支持的图片格式|*.bmp;*.dib;*.jpg;*.jpeg;*.jpe;*.jfif|位图文件|*.bmp;*.dib|JPEG图片|*.jpg;*.j" +
    "peg;*.jpe;*.jfif";
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.progress,
            this.labelStatus});
			this.statusStrip.Location = new System.Drawing.Point(0, 416);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(643, 22);
			this.statusStrip.TabIndex = 10;
			this.statusStrip.Text = "statusStrip1";
			// 
			// progress
			// 
			this.progress.Name = "progress";
			this.progress.Size = new System.Drawing.Size(100, 16);
			// 
			// labelStatus
			// 
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(104, 17);
			this.labelStatus.Text = "程序初始化完毕。";
			// 
			// panel
			// 
			this.panel.AllowDrop = true;
			this.panel.AutoScroll = true;
			this.panel.BackColor = System.Drawing.SystemColors.Control;
			this.panel.Controls.Add(this.pictureBox);
			this.panel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel.Location = new System.Drawing.Point(0, 25);
			this.panel.Name = "panel";
			this.panel.Size = new System.Drawing.Size(643, 391);
			this.panel.TabIndex = 11;
			// 
			// pictureBox
			// 
			this.pictureBox.Cursor = System.Windows.Forms.Cursors.Cross;
			this.pictureBox.Location = new System.Drawing.Point(0, 0);
			this.pictureBox.Margin = new System.Windows.Forms.Padding(0);
			this.pictureBox.Name = "pictureBox";
			this.pictureBox.Size = new System.Drawing.Size(0, 0);
			this.pictureBox.TabIndex = 0;
			this.pictureBox.TabStop = false;
			this.pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ShowPixelDetail);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(643, 438);
			this.Controls.Add(this.panel);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.menuStrip);
			this.MainMenuStrip = this.menuStrip;
			this.Name = "MainForm";
			this.Text = "Picture";
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			this.panel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem TsmiFile;
		private System.Windows.Forms.ToolStripMenuItem TsmiOpen;
		private System.Windows.Forms.ToolStripMenuItem TsmiSave;
		private System.Windows.Forms.OpenFileDialog ofd;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.StatusStrip statusStrip;
		public System.Windows.Forms.ToolStripProgressBar progress;
		public System.Windows.Forms.ToolStripStatusLabel labelStatus;
		private System.Windows.Forms.Panel panel;
		private System.Windows.Forms.PictureBox pictureBox;
	}
}

