using System;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace TurboVolt;

public class SystemMonitor : IDisposable
{
	private PerformanceCounter? _cpu;

	private PerformanceCounter? _ram;

	private PerformanceCounter? _disk;

	private PerformanceCounter? _net;

	private bool _disposed;

	public SystemStats StaticInfo { get; } = new SystemStats();


	public SystemMonitor()
	{
		InitCounters();
		LoadWmiStaticInfo();
	}

	private void InitCounters()
	{
		try
		{
			_cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			_cpu.NextValue();
			Thread.Sleep(50);
			_cpu.NextValue();
		}
		catch
		{
			_cpu = null;
		}
		try
		{
			_ram = new PerformanceCounter("Memory", "% Committed Bytes In Use");
			_ram?.NextValue();
		}
		catch
		{
			_ram = null;
		}
		try
		{
			_disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
			_disk?.NextValue();
		}
		catch
		{
			_disk = null;
		}
		try
		{
			string[] instanceNames = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
			if (instanceNames.Length != 0)
			{
				_net = new PerformanceCounter("Network Interface", "Bytes Total/sec", instanceNames[0]);
				_net.NextValue();
			}
		}
		catch
		{
			_net = null;
		}
	}

	private void LoadWmiStaticInfo()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
			using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectSearcher.Get().GetEnumerator();
			if (managementObjectEnumerator.MoveNext())
			{
				ManagementObject managementObject = (ManagementObject)managementObjectEnumerator.Current;
				StaticInfo.CpuName = (managementObject["Name"]?.ToString() ?? "").Trim();
				StaticInfo.CpuCores = Convert.ToInt32(managementObject["NumberOfCores"]);
				StaticInfo.CpuLogical = Convert.ToInt32(managementObject["NumberOfLogicalProcessors"]);
			}
		}
		catch
		{
		}
		try
		{
			using ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
			using ManagementObjectCollection.ManagementObjectEnumerator managementObjectEnumerator = managementObjectSearcher2.Get().GetEnumerator();
			if (managementObjectEnumerator.MoveNext())
			{
				ManagementObject managementObject2 = (ManagementObject)managementObjectEnumerator.Current;
				StaticInfo.OsCaption = (managementObject2["Caption"]?.ToString() ?? "").Trim();
				StaticInfo.OsArch = managementObject2["OSArchitecture"]?.ToString() ?? "";
				StaticInfo.TotalRam = Convert.ToUInt64(managementObject2["TotalVisibleMemorySize"]) * 1024;
			}
		}
		catch
		{
		}
		try
		{
			using ManagementObjectSearcher managementObjectSearcher3 = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
			foreach (ManagementObject item in managementObjectSearcher3.Get())
			{
				string text = item["Name"]?.ToString() ?? "";
				if (!string.IsNullOrEmpty(text))
				{
					StaticInfo.GpuName = text;
					break;
				}
			}
		}
		catch
		{
		}
	}

	public SystemStats Poll()
	{
		SystemStats systemStats = new SystemStats
		{
			CpuName = StaticInfo.CpuName,
			GpuName = StaticInfo.GpuName,
			OsCaption = StaticInfo.OsCaption,
			OsArch = StaticInfo.OsArch,
			CpuCores = StaticInfo.CpuCores,
			CpuLogical = StaticInfo.CpuLogical,
			TotalRam = StaticInfo.TotalRam
		};
		try
		{
			if (_cpu != null)
			{
				systemStats.CpuUsage = Math.Max(0f, Math.Min(100f, _cpu.NextValue()));
			}
		}
		catch
		{
		}
		try
		{
			if (_ram != null)
			{
				systemStats.RamUsage = Math.Max(0f, Math.Min(100f, _ram.NextValue()));
			}
		}
		catch
		{
		}
		try
		{
			if (_disk != null)
			{
				systemStats.DiskUsage = Math.Max(0f, Math.Min(100f, _disk.NextValue()));
			}
		}
		catch
		{
		}
		try
		{
			if (_net != null)
			{
				systemStats.NetworkSpeed = _net.NextValue();
			}
		}
		catch
		{
		}
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
			float num = 0f;
			foreach (ManagementObject item in managementObjectSearcher.Get())
			{
				string text = item["Name"]?.ToString() ?? "";
				if (text.Contains("3D") || text.Contains("Compute") || text.Contains("Cuda"))
				{
					float num2 = Convert.ToSingle(item["PercentTime"]);
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
			systemStats.GpuUsage = Math.Max(0f, Math.Min(100f, num));
		}
		catch
		{
			systemStats.GpuUsage = -1f;
		}
		try
		{
			using ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT * FROM Win32_Fan");
			foreach (ManagementObject item2 in managementObjectSearcher2.Get())
			{
				object obj6 = item2["DesiredSpeed"];
				if (obj6 != null)
				{
					systemStats.FanSpeed = Convert.ToSingle(obj6);
					systemStats.HasFanData = true;
					break;
				}
			}
		}
		catch
		{
		}
		try
		{
			using ManagementObjectSearcher managementObjectSearcher3 = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_Dwmapi_Dwm");
			foreach (ManagementObject item3 in managementObjectSearcher3.Get())
			{
				object obj8 = item3["FramesPerSecond"];
				if (obj8 != null)
				{
					systemStats.Fps = Convert.ToSingle(obj8);
					break;
				}
			}
		}
		catch
		{
		}
		return systemStats;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_cpu?.Dispose();
			_ram?.Dispose();
			_disk?.Dispose();
			_net?.Dispose();
			_disposed = true;
		}
	}
}
