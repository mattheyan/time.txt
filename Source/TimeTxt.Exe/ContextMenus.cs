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

			var appVersion = Assembly.GetEntryAssembly().GetName().Version;

			menu.Items.Add(string.Format("{0} v{1}", Resources.AppTitle, appVersion));

			var sep = new ToolStripSeparator();
			menu.Items.Add(sep);

			var item = new ToolStripMenuItem();
			item.Text = Resources.ContextMenuExit;
			item.Click += exit_Click;
			item.Image = Resources.ExitIcon;
			menu.Items.Add(item);

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