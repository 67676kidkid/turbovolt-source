using System;

namespace TurboVolt;

public class SystemStats
{
	public float CpuUsage { get; set; }

	public float GpuUsage { get; set; }

	public float RamUsage { get; set; }

	public float DiskUsage { get; set; }

	public float NetworkSpeed { get; set; }

	public float FanSpeed { get; set; }

	public bool HasFanData { get; set; }

	public float Fps { get; set; }

	public string CpuName { get; set; } = "Unknown";


	public string GpuName { get; set; } = "Unknown";


	public string OsCaption { get; set; } = "Unknown";


	public string OsArch { get; set; } = "";


	public int CpuCores { get; set; }

	public int CpuLogical { get; set; }

	public ulong TotalRam { get; set; }

	public double TotalRamGB => Math.Round((double)TotalRam / 1073741824.0, 1);

	public double UsedRamGB => Math.Round(TotalRamGB * (double)RamUsage / 100.0, 1);
}
