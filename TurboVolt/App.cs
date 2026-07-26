using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using Serilog.Events;

namespace TurboVolt;

public class App : Application
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static DispatcherUnhandledExceptionEventHandler _003C_003E9__0_0;

		internal void _003COnStartup_003Eb__0_0(object _, DispatcherUnhandledExceptionEventArgs args)
		{
			Log.Fatal(args.Exception, "Unhandled exception: {Msg}", args.Exception.Message);
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			args.Handled = true;
		}
	}

	private bool _contentLoaded;

	protected override void OnStartup(StartupEventArgs e)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		object obj = _003C_003Ec._003C_003E9__0_0;
		if (obj == null)
		{
			DispatcherUnhandledExceptionEventHandler val = delegate(object _, DispatcherUnhandledExceptionEventArgs args)
			{
				Log.Fatal(args.Exception, "Unhandled exception: {Msg}", args.Exception.Message);
				if (Debugger.IsAttached)
				{
					Debugger.Break();
				}
				args.Handled = true;
			};
			_003C_003Ec._003C_003E9__0_0 = val;
			obj = (object)val;
		}
		base.DispatcherUnhandledException += (DispatcherUnhandledExceptionEventHandler)obj;
		try
		{
			string text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
			Directory.CreateDirectory(text);
			Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(Path.Combine(text, "optimizations-.log"), LogEventLevel.Verbose, "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null, 1073741824L, null, buffered: false, shared: false, null, RollingInterval.Day, rollOnFileSizeLimit: false, 31).WriteTo.Console().CreateLogger();
		}
		catch
		{
		}
		Log.Information("App starting v{Ver} (Admin: {Admin})", Updater.AppVersion, IsAdministrator());
		base.OnStartup(e);
		Updater.CheckForUpdate();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		Log.CloseAndFlush();
		base.OnExit(e);
	}

	public static bool IsAdministrator()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.17.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
			Uri resourceLocator = new Uri("/TurboVolt;V15.0.0.0;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.17.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
