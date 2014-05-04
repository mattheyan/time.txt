using System;
using System.Collections.Generic;
using System.IO;
using TimeTxt.Core;

namespace TimeTxt.Exe
{
	internal static class FileSearchers
	{
		private static IEnumerable<IFileSearcher> GetAllSearchers()
		{
			yield return new DropboxFolderSearcher();
			yield return new HomeFolderSearcher();
			yield return new AppDataFolderSearcher();
			yield return new DesktopSearcher();
		}

		internal static void RegisterAll()
		{
			foreach (var searcher in GetAllSearchers())
				Services.Locator.UseService(searcher, searcher.LogicalName);
		}

		abstract class BaseUserFileSearcher : IFileSearcher
		{
			private readonly string logicalName;
			private readonly string friendlyName;
			private readonly string friendlyLocationDescription;
			private readonly bool canCreateFolder;

			protected BaseUserFileSearcher(string logicalName, string friendlyName, string friendlyLocationDescription, bool canCreateFolder)
			{
				this.logicalName = logicalName;
				this.friendlyName = friendlyName;
				this.friendlyLocationDescription = friendlyLocationDescription;
				this.canCreateFolder = canCreateFolder;
			}

			string IFileSearcher.LogicalName
			{
				get { return logicalName; }
			}

			string IFileSearcher.FriendlyName
			{
				get { return friendlyName; }
			}

			string IFileSearcher.FriendlyLocationDescription
			{
				get { return friendlyLocationDescription; }
			}

			bool IFileSearcher.IsAvailable
			{
				get
				{
					if (canCreateFolder)
					{
						string baseFolderPath;
						return TryGetBaseFolder(out baseFolderPath);
					}

					string folderPath;
					return TryGetFolder(out folderPath);
				}
			}

			protected static bool TryGetBaseFolder(out string folderPath)
			{
				Services.DefaultLogger.WriteLine("Searching for user folder...");

				var userDir = Path.Combine(@"C:\Users", Environment.UserName);
				if (!Directory.Exists(userDir))
				{
					folderPath = null;
					Services.DefaultLogger.WriteLine("WARNING: User folder '{0}' not found!", userDir);
					return false;
				}

				Services.DefaultLogger.WriteLine("Found user folder '{0}'.", userDir);
				folderPath = userDir;
				return true;
			}

			protected static bool TryGetSubFolder(string path, out string folderPath)
			{
				string homeDir;
				if (!TryGetBaseFolder(out homeDir))
				{
					folderPath = null;
					return false;
				}

				Services.DefaultLogger.WriteLine("Searching for sub-folder '{0}'...", path);

				var subFolderPath = Path.Combine(homeDir, path);
				if (!Directory.Exists(subFolderPath))
				{
					folderPath = null;
					return false;
				}

				Services.DefaultLogger.WriteLine("Found '{0}'.", subFolderPath);
				folderPath = subFolderPath;
				return true;
			}

			protected abstract bool TryGetFolder(out string folderPath);

			protected virtual bool TryGetFile(out string filePath)
			{
				string folderPath;
				if (!TryGetFolder(out folderPath))
				{
					filePath = null;
					return false;
				}

				var txtFilePath = Path.Combine(folderPath, "time.txt");
				if (!File.Exists(txtFilePath))
				{
					filePath = null;
					return false;
				}

				filePath = txtFilePath;
				return true;
			}

			bool IFileSearcher.TryGetFolder(out string folderPath)
			{
				Services.DefaultLogger.WriteLine("Searching for {0}...", friendlyName);
				if (!TryGetFolder(out folderPath))
				{
					Services.DefaultLogger.WriteLine("Folder not found.");
					return false;
				}

				Services.DefaultLogger.WriteLine("Folder found at '{0}'.", folderPath);
				return true;
			}

			bool IFileSearcher.TryGetFile(out string filePath)
			{
				Services.DefaultLogger.WriteLine("Searching for time.txt {0}...", friendlyLocationDescription);
				if (!TryGetFile(out filePath))
				{
					Services.DefaultLogger.WriteLine("File not found.");
					return false;
				}

				Services.DefaultLogger.WriteLine("File found at '{0}'.", filePath);
				return true;
			}

			protected virtual string CreateFolder()
			{
				throw new NotImplementedException(string.Format("Unable to create {0}!", friendlyName));
			}

			string IFileSearcher.CreateFile()
			{
				string filePath;
				if (TryGetFile(out filePath))
					throw new Exception(string.Format("File '{0}' already exists!", filePath));

				string folderPath;
				if (!TryGetFolder(out folderPath))
				{
					if (!canCreateFolder)
						throw new Exception(string.Format("Cannot create {0}!", friendlyName));

					string baseFolderPath;
					if (!TryGetBaseFolder(out baseFolderPath))
						throw new Exception("User folder could not be determined!");

					folderPath = CreateFolder();
				}

				filePath = Path.Combine(folderPath, "time.txt");
				using (var stream = File.CreateText(filePath))
				{
					stream.Flush();
				}
				return filePath;
			}
		}

		class DropboxFolderSearcher : BaseUserFileSearcher
		{
			public DropboxFolderSearcher()
				: base("dropbox", "Dropbox folder", "in you Dropbox folder", false)
			{
			}

			protected override bool TryGetFolder(out string folderPath)
			{
				return TryGetSubFolder(@"Dropbox\Apps\time.txt", out folderPath);
			}
		}

		class HomeFolderSearcher : BaseUserFileSearcher
		{
			public HomeFolderSearcher()
				: base("home", "Home folder", "in you Home folder", false)
			{
			}

			protected override bool TryGetFolder(out string folderPath)
			{
				return TryGetBaseFolder(out folderPath);
			}
		}

		class AppDataFolderSearcher : BaseUserFileSearcher
		{
			public AppDataFolderSearcher()
				: base("appdata", "AppData folder", "in your application data folder", true)
			{
			}

			protected override bool TryGetFolder(out string folderPath)
			{
				return TryGetSubFolder(@"AppData\Local\time.txt", out folderPath);
			}
		}

		class DesktopSearcher : BaseUserFileSearcher
		{
			public DesktopSearcher()
				: base("desktop", "Desktop", "on your Desktop", false)
			{
			}

			protected override bool TryGetFolder(out string folderPath)
			{
				return TryGetBaseFolder(out folderPath);
			}
		}
	}

	internal interface IFileSearcher
	{
		string LogicalName { get; }
		string FriendlyName { get; }
		string FriendlyLocationDescription { get; }
		bool IsAvailable { get; }
		bool TryGetFolder(out string folderPath);
		bool TryGetFile(out string filePath);
		string CreateFile();
	}
}
