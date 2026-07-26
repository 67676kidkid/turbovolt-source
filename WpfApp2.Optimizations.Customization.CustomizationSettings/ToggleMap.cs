using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace WpfApp2.Optimizations.Customization.CustomizationSettings;

public class ToggleMap
{
	private readonly Dictionary<string, bool> _toggles = new Dictionary<string, bool>();

	private readonly Dictionary<string, int> _order = new Dictionary<string, int>();

	public IReadOnlyDictionary<string, bool> Toggles => _toggles;

	public int Count => _toggles.Count;

	public int EnabledCount => _toggles.Count<KeyValuePair<string, bool>>((KeyValuePair<string, bool> kv) => kv.Value);

	public void Register(string key, bool defaultState = false, int sortOrder = 0)
	{
		if (!_toggles.ContainsKey(key))
		{
			_toggles[key] = defaultState;
			_order[key] = sortOrder;
		}
	}

	public void Set(string key, bool enabled)
	{
		if (_toggles.ContainsKey(key))
		{
			_toggles[key] = enabled;
			Log.Debug("Toggle {Key} set to {State}", key, enabled);
		}
	}

	public bool Get(string key)
	{
		bool value;
		return _toggles.TryGetValue(key, out value) && value;
	}

	public void SetAll(bool enabled)
	{
		foreach (string item in _toggles.Keys.ToList())
		{
			_toggles[item] = enabled;
		}
	}

	public List<string> GetEnabledKeys()
	{
		return (from kv in _toggles
			where kv.Value
			orderby _order.GetValueOrDefault(kv.Key, 0)
			select kv.Key).ToList();
	}

	public List<string> GetSortedKeys()
	{
		return _toggles.Keys.OrderBy((string k) => _order.GetValueOrDefault(k, 0)).ToList();
	}
}
