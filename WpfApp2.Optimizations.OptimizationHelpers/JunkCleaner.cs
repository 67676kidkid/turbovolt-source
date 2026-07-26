using System;
using System.IO;
using System.Runtime.InteropServices;
using Serilog;

namespace WpfApp2.Optimizations.OptimizationHelpers;

public static class JunkCleaner
{
	public static (int files, long bytes) CleanAll()
	{
		long num = 0L;
		int num2 = 0;
		string[] array = new string[11]
		{
			Environment.ExpandEnvironmentVariables("%TEMP%"),
			Environment.ExpandEnvironmentVariables("%WINDIR%\\Temp"),
			Environment.ExpandEnvironmentVariables("%WINDIR%\\Prefetch"),
			Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Temp"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "INetCache"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "WER"),
			Environment.ExpandEnvironmentVariables("%WINDIR%\\SoftwareDistribution\\Download"),
			Environment.ExpandEnvironmentVariables("%WINDIR%\\System32\\LogFiles"),
			Environment.ExpandEnvironmentVariables("%WINDIR%\\Logs"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache")
		};
		foreach (string path in array)
		{
			if (!Directory.Exists(path))
			{
				continue;
			}
			try
			{
				string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
				foreach (string text in files)
				{
					try
					{
						FileInfo fileInfo = new FileInfo(text);
						num += fileInfo.Length;
						num2++;
						File.Delete(text);
					}
					catch
					{
					}
				}
				files = Directory.GetDirectories(path);
				foreach (string path2 in files)
				{
					try
					{
						Directory.Delete(path2, recursive: true);
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
		}
		try
		{
			SHEmptyRecycleBin(IntPtr.Zero, null, 0);
		}
		catch
		{
		}
		Log.Information("Junk cleaned: {Files} files, {Bytes} MB", num2, num / 1024 / 1024);
		return (files: num2, bytes: num);
	}

	[DllImport("shell32.dll")]
	private static extern int SHEmptyRecycleBin(nint hwnd, string? root, int flags);
}
