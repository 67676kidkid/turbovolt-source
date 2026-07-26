using System.Collections.Generic;
using System.Linq;
using Serilog;
using WpfApp2.Optimizations.OptimizationHelpers;

namespace WpfApp2.Optimizations.Customization.CustomizationSettings;

public class OptimizationManager
{
	private readonly ToggleMap _toggleMap = new ToggleMap();

	private readonly List<OptimizationHelper> _optimizations = new List<OptimizationHelper>();

	public ToggleMap ToggleMap => _toggleMap;

	public IReadOnlyList<OptimizationHelper> Optimizations => _optimizations;

	public void Register(OptimizationHelper opt, int sortOrder = 0)
	{
		_optimizations.Add(opt);
		_toggleMap.Register(opt.Name, defaultState: false, sortOrder);
	}

	public void RegisterRange<T>(IEnumerable<(T opt, int order)> items) where T : OptimizationHelper
	{
		foreach (var (opt, sortOrder) in items)
		{
			Register(opt, sortOrder);
		}
	}

	public int ApplyEnabled()
	{
		int num = 0;
		List<string> enabledKeys = _toggleMap.GetEnabledKeys();
		Dictionary<string, OptimizationHelper> dictionary = _optimizations.ToDictionary((OptimizationHelper o) => o.Name);
		foreach (string item in enabledKeys)
		{
			if (dictionary.TryGetValue(item, out var value))
			{
				bool flag = value.Apply();
				if (flag)
				{
					num++;
				}
				Log.Information("Optimization {Name}: {Result}", item, flag ? "Applied" : "Failed");
			}
		}
		return num;
	}

	public int RestoreEnabled()
	{
		int num = 0;
		List<string> enabledKeys = _toggleMap.GetEnabledKeys();
		Dictionary<string, OptimizationHelper> dictionary = _optimizations.ToDictionary((OptimizationHelper o) => o.Name);
		foreach (string item in enabledKeys)
		{
			if (dictionary.TryGetValue(item, out var value))
			{
				bool flag = value.Restore();
				if (flag)
				{
					num++;
				}
				Log.Information("Optimization {Name}: {Result}", item, flag ? "Restored" : "Failed");
			}
		}
		return num;
	}

	public int ApplyAll()
	{
		_toggleMap.SetAll(enabled: true);
		return ApplyEnabled();
	}

	public int RestoreAll()
	{
		_toggleMap.SetAll(enabled: true);
		return RestoreEnabled();
	}

	public void SetToggle(string name, bool enabled)
	{
		_toggleMap.Set(name, enabled);
	}

	public bool GetToggle(string name)
	{
		return _toggleMap.Get(name);
	}

	public Dictionary<string, bool> GetToggleStates()
	{
		return _toggleMap.Toggles.ToDictionary<KeyValuePair<string, bool>, string, bool>((KeyValuePair<string, bool> kv) => kv.Key, (KeyValuePair<string, bool> kv) => kv.Value);
	}

	public void LoadState(Dictionary<string, bool> states)
	{
		foreach (KeyValuePair<string, bool> state in states)
		{
			_toggleMap.Set(state.Key, state.Value);
		}
	}
}
