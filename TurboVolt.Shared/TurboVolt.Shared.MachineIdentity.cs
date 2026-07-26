using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace TurboVolt.Shared;

public static class MachineIdentity
{
	private static string? _cached;

	public static string GetId()
	{
		if (_cached != null)
		{
			return _cached;
		}
		try
		{
			string text = "";
			string text2 = "";
			ManagementClass managementClass = new ManagementClass("win32_processor");
			try
			{
				using ManagementObjectCollection managementObjectCollection = managementClass.GetInstances();
				using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectCollection.GetEnumerator();
				if (managementObjectEnumerator.MoveNext())
				{
					text = (managementObjectEnumerator.Current["ProcessorId"] ?? "").ToString().Trim();
				}
			}
			finally
			{
				((IDisposable)managementClass)?.Dispose();
			}
			ManagementClass managementClass2 = new ManagementClass("win32_baseboard");
			try
			{
				using ManagementObjectCollection managementObjectCollection2 = managementClass2.GetInstances();
				using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectCollection2.GetEnumerator();
				if (managementObjectEnumerator.MoveNext())
				{
					text2 = (managementObjectEnumerator.Current["SerialNumber"] ?? "").ToString().Trim();
				}
			}
			finally
			{
				((IDisposable)managementClass2)?.Dispose();
			}
			using (SHA256 sHA = SHA256.Create())
			{
				_cached = Convert.ToHexString(sHA.ComputeHash(Encoding.UTF8.GetBytes("CBX" + text + "|" + text2))).Substring(0, 24).ToUpperInvariant();
			}
			return _cached;
		}
		catch
		{
			_cached = "UNKNOWN-" + Guid.NewGuid().ToString("N").Substring(0, 16)
				.ToUpperInvariant();
			return _cached;
		}
	}
}
