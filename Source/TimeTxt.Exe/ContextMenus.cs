using System;
using System.IO;
using System.Windows.Forms;
using TimeTxt.Core;
using TimeTxt.Exe.Properties;

namespace TimeTxt.Exe
{
	internal class ContextMenus
	{
		public event FileChangedEvent FileChanged;

		public ContextMenuStrip Create()
		{
			var menu = new ContextMenuStrip();

			var chooseFileItem = new ToolStripMenuItem();
			chooseFileItem.Text = Resources.ContextMenuChooseFile;
			chooseFileItem.Click += ChooseFile_Click;
			chooseFileItem.Image = Resources.TxtFileImage;
			menu.Items.Add(chooseFileItem);

			menu.Items.Add(new ToolStripSeparator());

			foreach (var searcher in Services.Locator.GetAllInstances<IFileSearcher>())
			{
				string filePath;
				if (searcher.TryGetFile(out filePath))
				{
					var chooseDiscoveredFileItem = new ToolStripMenuItem();
					chooseDiscoveredFileItem.Text = @"Use file " + searcher.FriendlyLocationDescription;
					chooseDiscoveredFileItem.Click += (sender, args) => FileChanged(menu, new FileChangedEventArgs(filePath));
					chooseDiscoveredFileItem.Image = Resources.TxtFileImage;
					menu.Items.Add(chooseDiscoveredFileItem);
				}
				else if (searcher.IsAvailable)
				{
					var createDiscoveredFileItem = new ToolStripMenuItem();
					createDiscoveredFileItem.Text = @"Create file " + searcher.FriendlyLocationDescription;

					var currentSearcher = searcher;
					createDiscoveredFileItem.Click += (sender, args) =>
					{
						var newFilePath = currentSearcher.CreateFile();
						FileChanged(menu, new FileChangedEventArgs(newFilePath));

						var idx = menu.Items.IndexOf(createDiscoveredFileItem);
						menu.Items.Remove(createDiscoveredFileItem);

						var chooseDiscoveredFileItem = new ToolStripMenuItem();
						chooseDiscoveredFileItem.Text = @"Use file " + currentSearcher.FriendlyLocationDescription;
						chooseDiscoveredFileItem.Click += (s, a) => FileChanged(menu, new FileChangedEventArgs(newFilePath));
						chooseDiscoveredFileItem.Image = Resources.TxtFileImage;
						menu.Items.Insert(idx, chooseDiscoveredFileItem);
					};
					createDiscoveredFileItem.Image = Resources.TxtFileImage;
					menu.Items.Add(createDiscoveredFileItem);
				}
			}

			menu.Items.Add(new ToolStripSeparator());

			var exitItem = new ToolStripMenuItem();
			exitItem.Text = Resources.ContextMenuExit;
			exitItem.Click += Exit_Click;
			exitItem.Image = Resources.ExitImage;
			menu.Items.Add(exitItem);

			return menu;
		}

		private void ChooseFile_Click(object sender, EventArgs e)
		{
			var openFileDialog = new OpenFileDialog();

			openFileDialog.Title = Resources.OpenFileDialogTitle;

			if (!string.IsNullOrEmpty(Settings.Default.TimeTxtFile))
				openFileDialog.InitialDirectory = Path.GetDirectoryName(Settings.Default.TimeTxtFile);
			else
			{
				foreach (var searcher in Services.Locator.GetAllInstances<IFileSearcher>())
				{
					string filePath;
					if (searcher.TryGetFile(out filePath))
					{
						openFileDialog.InitialDirectory = Path.GetDirectoryName(filePath);
						break;
					}
				}
			}

			openFileDialog.Filter = Resources.OpenFileDialogFilter + @"|time.txt";
			openFileDialog.Multiselect = false;

			if (openFileDialog.ShowDialog() == DialogResult.OK)
				FileChanged(this, new FileChangedEventArgs(openFileDialog.FileName));
		}

		private void Exit_Click(object sender, EventArgs e)
		{
			Services.DefaultLogger.WriteLine("User exited application.");
			Application.Exit();
		}
	}

	public delegate void FileChangedEvent(object sender, FileChangedEventArgs args);

	public class FileChangedEventArgs : EventArgs
	{
		public string FilePath { get; private set; }

		public FileChangedEventArgs(string filePath)
		{
			FilePath = filePath;
		}
	}

}