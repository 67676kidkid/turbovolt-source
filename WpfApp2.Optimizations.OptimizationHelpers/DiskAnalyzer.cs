using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WpfApp2.Optimizations.OptimizationHelpers;

public static class DiskAnalyzer
{
	public static List<DriveInfo> GetDrives()
	{
		return (from d in DriveInfo.GetDrives()
			where d.IsReady && d.DriveType == DriveType.Fixed
			select d).ToList();
	}

	public static (long total, long free, int usedPercent) GetDriveInfo(string drive)
	{
		try
		{
			DriveInfo driveInfo = new DriveInfo(drive);
			if (!driveInfo.IsReady)
			{
				return (total: 0L, free: 0L, usedPercent: 0);
			}
			long totalSize = driveInfo.TotalSize;
			long availableFreeSpace = driveInfo.AvailableFreeSpace;
			int item = (int)((totalSize > 0) ? ((totalSize - availableFreeSpace) * 100 / totalSize) : 0);
			return (total: totalSize, free: availableFreeSpace, usedPercent: item);
		}
		catch
		{
			return (total: 0L, free: 0L, usedPercent: 0);
		}
	}
}
