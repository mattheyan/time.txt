using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using TimeTxt.Core;
using TimeTxt.Exe.Properties;

namespace TimeTxt.Exe
{
	class TrayIcon : IDisposable
	{
		private readonly NotifyIcon icon;
		private readonly IList<IDisposable> dependentDisposables = new List<IDisposable>();

		public TrayIcon()
		{
			icon = new NotifyIcon();
		}

		public MouseEventHandler DoubleClick;

		public event FileChangedEvent FileChanged;

		public void Display()
		{
			var appVersion = Assembly.GetEntryAssembly().GetName().Version;

			icon.MouseDoubleClick += icon_MouseDoubleClick;

#if DEBUG
			icon.Icon = Resources.DebugAppIcon;
#else
			icon.Icon = Resources.AppIcon;
#endif

			icon.Text = string.Format("{0} v{1}", Resources.AppTitle, appVersion);
			icon.Visible = true;

			var contextMenus = new ContextMenus();

			icon.ContextMenuStrip = contextMenus.Create();

			contextMenus.FileChanged += ContextMenu_FileChanged;
		}

		private void ContextMenu_FileChanged(object sender, FileChangedEventArgs args)
		{
			FileChanged(this, args);
		}

		public void Dispose()
		{
			Services.DefaultLogger.WriteLine("Disposing of application...");

			// Attempt to dispose dependents.
			foreach (var dependent in dependentDisposables)
			{
				try
				{
					dependent.Dispose();
				}
				catch (Exception e)
				{
					Services.DefaultLogger.WriteLine("ERROR: {0}", e.Message);
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

		public bool RemoveDependent(IDisposable disposable)
		{
			return dependentDisposables.Remove(disposable);
		}

		public void ShowMessageInTray(string title, string message)
		{
			icon.BalloonTipTitle = title;
			icon.BalloonTipText = message;
			icon.ShowBalloonTip(2000);
		}

		public void ShowMessage(string title, string message)
		{
			ShowMessageInTray(title, message);
		}

		public void ShowMessage(string title, string message, object arg0)
		{
			ShowMessageInTray(title, string.Format(message, arg0));
		}

		public void ShowMessage(string title, string message, object arg0, object arg1)
		{
			ShowMessageInTray(title, string.Format(message, arg0, arg1));
		}

		public void ShowMessage(string title, string message, object arg0, object arg1, object arg2)
		{
			ShowMessageInTray(title, string.Format(message, arg0, arg1, arg2));
		}

		public void ShowMessage(string title, string message, params object[] args)
		{
			ShowMessageInTray(title, string.Format(message, args));
		}

	}
}
