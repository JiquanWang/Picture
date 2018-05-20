using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picture
{
	class SupportedImage
	{
		protected MainForm mf;
		private int lastValue = 0;

		protected void UpdateInfo(string msg)
		{
			mf.labelStatus.Text = msg;
		}

		protected void UpdateProg(int value)
		{
			if(value != lastValue)
			{
				lastValue = value;
				mf.progress.Value = value;
			}
		}
	}
}
