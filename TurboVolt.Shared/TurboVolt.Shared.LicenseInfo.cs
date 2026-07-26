using System;

namespace TurboVolt.Shared;

public class LicenseInfo
{
	public string Key { get; set; } = "";


	public LicenseTier Tier { get; set; }

	public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;


	public DateTime ExpiresUtc { get; set; } = DateTime.MinValue;


	public string KeyId { get; set; } = "";


	public bool LockedToMachine { get; set; }

	public bool IsValid => ExpiresUtc > DateTime.UtcNow;

	public int DaysLeft => Math.Max(0, (int)Math.Ceiling((ExpiresUtc - DateTime.UtcNow).TotalDays));

	public string DisplayTier => Tier switch
	{
		LicenseTier.Basic => "Basic", 
		LicenseTier.Pro => "Professional", 
		LicenseTier.Elite => "Elite", 
		_ => "Basic", 
	};
}
