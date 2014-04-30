using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TimeTxt.Exe.Properties;

namespace TimeTxt.Exe
{
	class TrayIcon : IDisposable
	{
		private readonly NotifyIcon icon;
		private readonly IList<IDisposable> dependentDisposables = new List<IDisposable>();
		private ITextLogger logger;

		public TrayIcon()
		{
			icon = new NotifyIcon();
		}

		public MouseEventHandler DoubleClick;

		public void Display()
		{
			icon.MouseClick += icon_MouseClick;
			icon.MouseDoubleClick += icon_MouseDoubleClick;
			icon.Icon = Resources.AppIcon;
			icon.Text = Resources.AppTitle;
			icon.Visible = true;

			var contextMenus = new ContextMenus();
			contextMenus.UseLogger(logger);

			icon.ContextMenuStrip = contextMenus.Create();
		}

		public void Dispose()
		{
			logger.WriteLine("Disposing of application...");

			// Attempt to dispose dependents.
			foreach (var dependent in dependentDisposables)
			{
				try
				{
					dependent.Dispose();
				}
				catch (Exception e)
				{
					logger.WriteLine("ERROR: {0}", e.Message);
				}
			}

			icon.Dispose();
		}

		private void icon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				// Also show the context menu when left-clicked.
				icon.ContextMenuStrip.Show();
		}

		private void icon_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (DoubleClick != null)
					DoubleClick(sender, e);
			}
		}

		public void AddDependent(IDisposable disposable)
		{
			dependentDisposables.Add(disposable);
		}

		public void UseLogger(ITextLogger logger)
		{
			this.logger = logger;
		}
	}
}
