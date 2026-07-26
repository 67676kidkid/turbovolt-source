using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TurboVolt;

public static class Updater
{
	private class UpdateInfo
	{
		public string latest { get; set; } = "";


		public string url { get; set; } = "";

	}

	public static readonly string AppVersion = "2.0.0";

	private const string VersionUrl = "https://yourdomain.com/TurboVolt/version.json";

	private const string DownloadBase = "https://github.com/yourusername/turbovolt/releases/latest/download";

	public static void CheckForUpdate(bool silentIfCurrent = true)
	{
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				using HttpClient http = new HttpClient
				{
					Timeout = TimeSpan.FromSeconds(5L)
				};
				UpdateInfo data = JsonSerializer.Deserialize<UpdateInfo>(await http.GetStringAsync("https://yourdomain.com/TurboVolt/version.json"));
				if (data == null || string.IsNullOrEmpty(data.latest))
				{
					return;
				}
				await ((DispatcherObject)Application.Current).Dispatcher.InvokeAsync((Action)delegate
				{
					if (data.latest == AppVersion)
					{
						if (!silentIfCurrent)
						{
							MessageBox.Show("You're up to date (v" + AppVersion + ").", "TurboVolt", MessageBoxButton.OK, MessageBoxImage.Asterisk);
						}
					}
					else if (MessageBox.Show($"Version {data.latest} is available (you have v{AppVersion}).\n\nDownload and install now?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						DownloadAndUpdate(data.latest);
					}
				});
			}
			catch
			{
			}
		});
	}

	private static async Task DownloadAndUpdate(string newVersion)
	{
		_ = 1;
		try
		{
			string location = Assembly.GetExecutingAssembly().Location;
			string dir = Path.GetDirectoryName(location) ?? AppDomain.CurrentDomain.BaseDirectory;
			string exeName = Path.GetFileName(location);
			string updateFile = Path.Combine(Path.GetTempPath(), "TurboVolt_" + newVersion + ".update");
			using HttpClient http = new HttpClient
			{
				Timeout = TimeSpan.FromMinutes(3L)
			};
			string requestUri = "https://github.com/yourusername/turbovolt/releases/latest/download/" + exeName;
			HttpResponseMessage httpResponseMessage = await http.GetAsync(requestUri);
			httpResponseMessage.EnsureSuccessStatusCode();
			using (FileStream fs = new FileStream(updateFile, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				await httpResponseMessage.Content.CopyToAsync(fs);
			}
			string text = Path.Combine(Path.GetTempPath(), "cbx_update.ps1");
			File.WriteAllText(text, $"\nStart-Sleep -Seconds 1\n$retry = 0\nwhile ($retry -lt 30) {{\n    try {{\n        Move-Item -LiteralPath '{updateFile}' -Destination '{dir}\\{exeName}' -Force\n        Start-Process '{dir}\\{exeName}'\n        break\n    }} catch {{\n        Start-Sleep -Milliseconds 500\n        $retry++\n    }}\n}}\nRemove-Item -LiteralPath '{text}' -Force\n");
			Process.Start(new ProcessStartInfo
			{
				FileName = "powershell",
				Arguments = "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File \"" + text + "\"",
				UseShellExecute = false
			});
			Application.Current.Shutdown();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Update failed: " + ex.Message, "Update Error", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}
}
