using Microsoft.Win32;
using Serilog;

namespace WpfApp2.Optimizations.OptimizationHelpers;

public class PrivacyOptimization : OptimizationHelper
{
	private readonly string _displayName;

	private readonly string _detail;

	private readonly string _group;

	private readonly RegistryHive _hive;

	private readonly string _regPath;

	private readonly string _regName;

	private readonly object _desiredValue;

	private readonly object _restoreValue;

	private readonly bool _needsAdmin;

	private readonly RegistryValueKind _valueKind;

	public override string Name => _displayName;

	public override string Description => _detail;

	public override string Category => _group;

	public override bool RequiresAdmin => _needsAdmin;

	public PrivacyOptimization(string name, string description, string category, RegistryHive hive, string regPath, string regName, object desiredValue, object restoreValue, bool requiresAdmin = false, RegistryValueKind valueKind = RegistryValueKind.DWord)
	{
		_displayName = name;
		_detail = description;
		_group = category;
		_hive = hive;
		_regPath = regPath;
		_regName = regName;
		_desiredValue = desiredValue;
		_restoreValue = restoreValue;
		_needsAdmin = requiresAdmin;
		_valueKind = valueKind;
	}

	public override bool Apply()
	{
		Log.Information("Applying: {Name}", _displayName);
		return SetRegistryValue(_hive, _regPath, _regName, _desiredValue, _valueKind);
	}

	public override bool Restore()
	{
		Log.Information("Restoring: {Name}", _displayName);
		return SetRegistryValue(_hive, _regPath, _regName, _restoreValue, _valueKind);
	}

	public override bool IsApplied()
	{
		object registryValue = GetRegistryValue(_hive, _regPath, _regName);
		if (registryValue != null)
		{
			return registryValue.ToString() == _desiredValue.ToString();
		}
		return false;
	}
}
