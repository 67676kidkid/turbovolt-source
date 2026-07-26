using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TurboVolt.Shared;

public static class ActivationStore
{
	private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TurboVolt|activation|store|v2");

	private static readonly string StorePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activations.bin");

	private static Dictionary<string, string>? _cache;

	private static Dictionary<string, string> Load()
	{
		if (_cache != null)
		{
			return _cache;
		}
		_cache = new Dictionary<string, string>();
		try
		{
			if (File.Exists(StorePath))
			{
				byte[] bytes = ProtectedData.Unprotect(File.ReadAllBytes(StorePath), Entropy, DataProtectionScope.CurrentUser);
				_cache = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(bytes)) ?? new Dictionary<string, string>();
			}
		}
		catch
		{
			_cache = new Dictionary<string, string>();
		}
		return _cache;
	}

	private static void Save()
	{
		try
		{
			string s = JsonSerializer.Serialize(_cache ?? new Dictionary<string, string>());
			byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(s), Entropy, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(StorePath, bytes);
		}
		catch
		{
		}
	}

	public static bool IsActivated(string keyId)
	{
		return Load().ContainsKey(keyId);
	}

	public static string? GetActivatedMachine(string keyId)
	{
		if (!Load().TryGetValue(keyId, out string value))
		{
			return null;
		}
		return value;
	}

	public static bool TryActivate(string keyId, string machineId)
	{
		Dictionary<string, string> dictionary = Load();
		if (dictionary.ContainsKey(keyId))
		{
			if (dictionary[keyId] != machineId)
			{
				return false;
			}
			return true;
		}
		dictionary[keyId] = machineId;
		_cache = dictionary;
		Save();
		return true;
	}
}
