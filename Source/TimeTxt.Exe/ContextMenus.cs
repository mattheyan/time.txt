using System;
using System.Reflection;
using System.Windows.Forms;
using TimeTxt.Exe.Properties;

namespace TimeTxt.Exe
{
	internal class ContextMenus
	{
		private ITextLogger logger;

		public ContextMenuStrip Create()
		{
			var menu = new ContextMenuStrip();

			var exitItem = new ToolStripMenuItem();
			exitItem.Text = Resources.ContextMenuExit;
			exitItem.Click += exit_Click;
			exitItem.Image = Resources.ExitIcon;
			menu.Items.Add(exitItem);

			return menu;
		}

		private void exit_Click(object sender, EventArgs e)
		{
			logger.WriteLine("User exited application.");
			Application.Exit();
		}

		public void UseLogger(ITextLogger logger)
		{
			this.logger = logger;
		}
	}
}