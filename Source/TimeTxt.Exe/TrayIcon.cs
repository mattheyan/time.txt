using System;
using System.Collections.Generic;
using System.Reflection;
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
			var appVersion = Assembly.GetEntryAssembly().GetName().Version;

			icon.MouseDoubleClick += icon_MouseDoubleClick;
			icon.Icon = Resources.AppIcon;
			icon.Text = string.Format("{0} v{1}", Resources.AppTitle, appVersion);
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

		private void icon_MouseDoubleClick(object sender, MouseEventArgs args)
		{
			if (args.Button == MouseButtons.Left)
			{
				if (DoubleClick != null)
					DoubleClick(sender, args);
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
