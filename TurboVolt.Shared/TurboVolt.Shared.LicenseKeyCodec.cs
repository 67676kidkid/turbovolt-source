using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TurboVolt.Shared;

public static class LicenseKeyCodec
{
	private const string Secret = "TurboVolt|v2|offline-hmac|2026|gold-elite";

	private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private const string Prefix = "CBX";

	public static (string name, LicenseTier tier, decimal price, string[] features)[] TierPricing => new(string, LicenseTier, decimal, string[])[3]
	{
		("Basic", LicenseTier.Basic, 2.99m, new string[3] { "20 essential privacy tweaks", "Gaming Mode", "Email support" }),
		("Professional", LicenseTier.Pro, 4.99m, new string[4] { "Everything in Basic", "Network & GPU tuning", "Memory & Scheduler Pro", "Priority support" }),
		("Elite", LicenseTier.Elite, 7.99m, new string[6] { "Everything in Pro", "AI Advisor", "Unlimited profiles", "Hardware-locked keys", "24/7 priority support", "Lifetime validity" })
	};

	public static string GenerateKey(int days, LicenseTier tier, bool lockToMachine = false, string? machineId = null)
	{
		if (days < 1)
		{
			days = 30;
		}
		string value = TierToCode(tier);
		string value2 = ToBase36(DateTime.UtcNow.AddDays(days).Ticks).PadLeft(8, '0');
		string value3 = Guid.NewGuid().ToString("N").Substring(0, 12)
			.ToUpperInvariant();
		string value4 = Signature($"{value}|{value2}|{value3}");
		return $"{"CBX"}-{value}-{value2}-{value3}-{value4}";
	}

	public static List<string> GenerateBulk(int count, int days, LicenseTier tier)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < count; i++)
		{
			list.Add(GenerateKey(days, tier));
		}
		return list;
	}

	public static bool TryValidate(string key, out LicenseInfo? info, out string message)
	{
		info = null;
		message = "Invalid license key.";
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		string text = key.Trim().ToUpperInvariant();
		string[] array = text.Split('-');
		if (array.Length != 5 || array[0] != "CBX")
		{
			message = "Key must start with CBX and have 5 parts.";
			return false;
		}
		LicenseTier tier = CodeToTier(array[1]);
		string text2 = array[2];
		string text3 = array[3];
		string a = array[4];
		string b = Signature($"{array[1]}|{text2}|{text3}");
		if (!ConstantEquals(a, b))
		{
			message = "Key signature verification failed.";
			return false;
		}
		if (!TryFromBase36(text2, out var value))
		{
			message = "Could not read key expiry.";
			return false;
		}
		DateTime dateTime = new DateTime(value, DateTimeKind.Utc);
		if (dateTime <= DateTime.Now.ToUniversalTime())
		{
			message = "This license key has expired.";
			return false;
		}
		info = new LicenseInfo
		{
			Key = text,
			Tier = tier,
			ExpiresUtc = dateTime,
			KeyId = text3
		};
		message = "Pending activation";
		return true;
	}

	public static (bool ok, string msg) ActivateKey(string key, LicenseTier tier, DateTime expiresUtc, string keyId)
	{
		string id = MachineIdentity.GetId();
		if (ActivationStore.IsActivated(keyId))
		{
			if (ActivationStore.GetActivatedMachine(keyId) != id)
			{
				return (ok: false, msg: "This key was already activated on a different PC.");
			}
			return (ok: true, msg: "License already active on this machine.");
		}
		if (!ActivationStore.TryActivate(keyId, id))
		{
			return (ok: false, msg: "Activation failed. Try again.");
		}
		return (ok: true, msg: "Activated for " + id.Substring(0, 12) + "...");
	}

	public static string TierToCode(LicenseTier tier)
	{
		return tier switch
		{
			LicenseTier.Basic => "B", 
			LicenseTier.Pro => "P", 
			LicenseTier.Elite => "E", 
			_ => "B", 
		};
	}

	public static LicenseTier CodeToTier(string code)
	{
		return code switch
		{
			"B" => LicenseTier.Basic, 
			"P" => LicenseTier.Pro, 
			"E" => LicenseTier.Elite, 
			_ => LicenseTier.Basic, 
		};
	}

	private static string Signature(string payload)
	{
		using HMACSHA256 hMACSHA = new HMACSHA256(Encoding.UTF8.GetBytes("TurboVolt|v2|offline-hmac|2026|gold-elite"));
		return ToBase36((long)(BitConverter.ToUInt64(hMACSHA.ComputeHash(Encoding.UTF8.GetBytes(payload)), 0) & 0x7FFFFFFFFFFFFFFFL)).PadLeft(10, '0').Substring(0, 10);
	}

	private static bool ConstantEquals(string a, string b)
	{
		if (a == null || b == null || a.Length != b.Length)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < a.Length; i++)
		{
			num |= a[i] ^ b[i];
		}
		return num == 0;
	}

	private static string ToBase36(long value)
	{
		if (value == 0L)
		{
			return "0";
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (long num = value; num > 0; num /= 36)
		{
			stringBuilder.Insert(0, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(int)(num % 36)]);
		}
		return stringBuilder.ToString();
	}

	private static bool TryFromBase36(string text, out long value)
	{
		value = 0L;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		try
		{
			string text2 = text.ToUpperInvariant();
			foreach (char value2 in text2)
			{
				int num = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(value2);
				if (num < 0 || num > 35)
				{
					return false;
				}
				value = value * 36 + num;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
}
