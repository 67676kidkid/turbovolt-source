using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using Serilog;

namespace WpfApp2.Optimizations.OptimizationHelpers;

public class GameLauncherWatcher : IDisposable
{
	private readonly DispatcherTimer _timer = new DispatcherTimer();

	private readonly HashSet<string> _detected = new HashSet<string>();

	private readonly Dictionary<string, string> _launchers = new Dictionary<string, string>
	{
		["Steam"] = "steam",
		["Epic Games"] = "EpicGamesLauncher",
		["Battle.net"] = "Battle.net",
		["Ubisoft Connect"] = "upc",
		["EA App"] = "EADesktop",
		["GOG Galaxy"] = "GalaxyClient",
		["Xbox App"] = "GameBar",
		["Discord"] = "Discord",
		["PlayStation PC"] = "PSNow",
		["Amazon Games"] = "AmazonGames"
	};

	public IReadOnlySet<string> DetectedLaunchers => _detected;

	public bool AnyGameLauncherRunning => _detected.Count > 0;

	public event Action<HashSet<string>>? LaunchersChanged;

	public GameLauncherWatcher()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		_timer.Interval = TimeSpan.FromSeconds(5L);
		_timer.Tick += delegate
		{
			Check();
		};
	}

	public void Start()
	{
		_timer.Start();
	}

	public void Stop()
	{
		_timer.Stop();
		_detected.Clear();
	}

	private void Check()
	{
		HashSet<string> hashSet = new HashSet<string>(_detected);
		_detected.Clear();
		foreach (KeyValuePair<string, string> launcher in _launchers)
		{
			Process[] processesByName = Process.GetProcessesByName(launcher.Value);
			if (processesByName.Length != 0)
			{
				_detected.Add(launcher.Key);
			}
			Process[] array = processesByName;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose();
			}
		}
		if (!hashSet.SetEquals(_detected))
		{
			Log.Information("Game launchers changed: {Launchers}", string.Join(", ", _detected));
			this.LaunchersChanged?.Invoke(new HashSet<string>(_detected));
		}
	}

	public void Dispose()
	{
		_timer.Stop();
	}
}
