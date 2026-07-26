using System;
using Microsoft.Win32;
using Serilog;

namespace WpfApp2.Optimizations.OptimizationHelpers;

public abstract class OptimizationHelper
{
	public abstract string Name { get; }

	public abstract string Description { get; }

	public abstract string Category { get; }

	public abstract bool RequiresAdmin { get; }

	public abstract bool Apply();

	public abstract bool Restore();

	public abstract bool IsApplied();

	protected bool SetRegistryValue(RegistryHive hive, string path, string name, object value, RegistryValueKind kind)
	{
		try
		{
			using (RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
			{
				using RegistryKey registryKey2 = registryKey.CreateSubKey(path);
				registryKey2.SetValue(name, value, kind);
			}
			Log.Information("Registry set: {Hive}\\{Path} -> {Name} = {Value}", hive, path, name, value);
			return true;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to set registry: {Hive}\\{Path} -> {Name}", hive, path, name);
			return false;
		}
	}

	protected object? GetRegistryValue(RegistryHive hive, string path, string name)
	{
		try
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
			using RegistryKey registryKey2 = registryKey.OpenSubKey(path, writable: false);
			return registryKey2?.GetValue(name);
		}
		catch (Exception exception)
		{
			Log.Warning(exception, "Failed to read registry: {Hive}\\{Path} -> {Name}", hive, path, name);
			return null;
		}
	}
}
