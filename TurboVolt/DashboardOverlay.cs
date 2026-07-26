using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Threading;

namespace TurboVolt;

public class DashboardOverlay : Window, IComponentConnector
{
	private readonly SystemMonitor _monitor;

	private readonly DispatcherTimer _timer;

	internal ProgressBar OvCpuBar;

	internal TextBlock OvCpuText;

	internal ProgressBar OvGpuBar;

	internal TextBlock OvGpuText;

	internal ProgressBar OvRamBar;

	internal TextBlock OvRamText;

	internal ProgressBar OvDiskBar;

	internal TextBlock OvDiskText;

	internal TextBlock OvFpsText;

	internal TextBlock OvNetText;

	internal TextBlock OvFanText;

	internal TextBlock OvSysInfo;

	private bool _contentLoaded;

	public DashboardOverlay(SystemMonitor monitor)
	{
		InitializeComponent();
		_monitor = monitor;
		Rect workArea = SystemParameters.WorkArea;
		double width = workArea.Width;
		base.Left = width - 260.0;
		base.Top = 60.0;
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(1L)
		};
		_timer.Tick += delegate
		{
			UpdateData();
		};
		LoadStatic();
		_timer.Start();
	}

	private void LoadStatic()
	{
		SystemStats staticInfo = _monitor.StaticInfo;
		OvSysInfo.Text = staticInfo.CpuName + "  |  " + staticInfo.GpuName;
	}

	private void UpdateData()
	{
		try
		{
			SystemStats systemStats = _monitor.Poll();
			OvCpuText.Text = $"{systemStats.CpuUsage:F0}%";
			OvCpuBar.Value = systemStats.CpuUsage;
			if (systemStats.GpuUsage >= 0f)
			{
				OvGpuText.Text = $"{systemStats.GpuUsage:F0}%";
				OvGpuBar.Value = systemStats.GpuUsage;
			}
			else
			{
				OvGpuText.Text = "N/A";
				OvGpuBar.Value = 0.0;
			}
			OvRamText.Text = $"{systemStats.RamUsage:F0}%";
			OvRamBar.Value = systemStats.RamUsage;
			try
			{
				DriveInfo driveInfo = new DriveInfo("C");
				double value = (double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / (double)driveInfo.TotalSize * 100.0;
				OvDiskText.Text = $"{value:F0}%";
				OvDiskBar.Value = value;
			}
			catch
			{
				OvDiskText.Text = $"{systemStats.DiskUsage:F0}%";
				OvDiskBar.Value = systemStats.DiskUsage;
			}
			float networkSpeed = systemStats.NetworkSpeed;
			if (networkSpeed >= 1048576f)
			{
				OvNetText.Text = $"{networkSpeed / 1048576f:F1} MB/s";
			}
			else if (networkSpeed >= 1024f)
			{
				OvNetText.Text = $"{networkSpeed / 1024f:F0} KB/s";
			}
			else
			{
				OvNetText.Text = $"{networkSpeed:F0} B/s";
			}
			OvFpsText.Text = ((systemStats.Fps > 0f) ? $"{systemStats.Fps:F0} FPS" : "--");
			OvFanText.Text = ((systemStats.HasFanData && systemStats.FanSpeed > 0f) ? $"{systemStats.FanSpeed:F0} RPM" : "N/A");
		}
		catch
		{
		}
	}

	private void Window_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			DragMove();
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		_timer.Stop();
		Close();
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.17.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TurboVolt;V15.0.0.0;component/dashboardoverlay.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.17.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((DashboardOverlay)target).MouseLeftButtonDown += Window_MouseDown;
			break;
		case 2:
			((Button)target).Click += Close_Click;
			break;
		case 3:
			OvCpuBar = (ProgressBar)target;
			break;
		case 4:
			OvCpuText = (TextBlock)target;
			break;
		case 5:
			OvGpuBar = (ProgressBar)target;
			break;
		case 6:
			OvGpuText = (TextBlock)target;
			break;
		case 7:
			OvRamBar = (ProgressBar)target;
			break;
		case 8:
			OvRamText = (TextBlock)target;
			break;
		case 9:
			OvDiskBar = (ProgressBar)target;
			break;
		case 10:
			OvDiskText = (TextBlock)target;
			break;
		case 11:
			OvFpsText = (TextBlock)target;
			break;
		case 12:
			OvNetText = (TextBlock)target;
			break;
		case 13:
			OvFanText = (TextBlock)target;
			break;
		case 14:
			OvSysInfo = (TextBlock)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}
}
