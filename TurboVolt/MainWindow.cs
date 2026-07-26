using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;
using Serilog;
using TurboVolt.Shared;
using WpfApp2.Optimizations.Customization.CustomizationSettings;
using WpfApp2.Optimizations.OptimizationHelpers;

namespace TurboVolt;

public class MainWindow : Window, IComponentConnector, IStyleConnector
{
	private readonly OptimizationManager _manager = new OptimizationManager();

	private readonly ObservableCollection<OptimizationItem> _allItems = new ObservableCollection<OptimizationItem>();

	private readonly ObservableCollection<CategoryCardData> _categoryCards = new ObservableCollection<CategoryCardData>();

	private LicenseInfo? _license;

	private string? _savedKey;

	private readonly string _licensePath;

	private readonly string _trialPath;

	private readonly string _togglesPath;

	private string _currentCategory = "all";

	private string _currentSearch = "";

	private int _filterStatus;

	private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("TurboVolt|2026|secure|license");

	private List<CategoryCardData> _allCards = new List<CategoryCardData>();

	internal TextBlock LicenseBadge;

	internal RadioButton NavOptimize;

	internal RadioButton NavGaming;

	internal RadioButton NavHardware;

	internal RadioButton NavPrivacy;

	internal RadioButton NavWindows;

	internal RadioButton NavStartup;

	internal RadioButton NavProcess;

	internal RadioButton NavNetwork;

	internal RadioButton NavHosts;

	internal RadioButton NavPower;

	internal RadioButton NavContext;

	internal RadioButton NavDisk;

	internal RadioButton NavBrowser;

	internal RadioButton NavJunkCleaner;

	internal RadioButton NavActivate;

	internal RadioButton NavBoost;

	internal Grid PageContainer;

	internal Grid PageOptimizer;

	internal TextBlock OptSubtitle;

	internal RadioButton CatAll;

	internal TextBlock CatAllCount;

	internal RadioButton CatGaming;

	internal TextBlock CatGamingCount;

	internal RadioButton CatHardware;

	internal TextBlock CatHardwareCount;

	internal RadioButton CatPrivacy;

	internal TextBlock CatPrivacyCount;

	internal RadioButton CatWindows;

	internal TextBlock CatWindowsCount;

	internal ItemsControl CategoryCardsControl;

	internal Button FilterBtn;

	internal TextBlock SearchPlaceholder;

	internal TextBox SearchBox;

	internal Button SearchClearBtn;

	internal Grid PageActivate;

	internal TextBlock ActivateSubtitle;

	internal TextBox ActivateKeyBox;

	internal TextBlock ActivateStatus;

	internal Button ActivateBtn;

	internal Border TrialBanner;

	internal TextBlock TrialBannerTitle;

	internal TextBlock TrialBannerSub;

	internal TextBlock LicenseStatusValue;

	internal TextBlock LicenseTierValue;

	internal TextBlock LicenseExpiryValue;

	internal Grid PageJunkCleaner;

	internal Border JunkResultBorder;

	internal TextBlock JunkLastResult;

	internal Button JunkCleanBtn;

	internal Grid PageStartup;

	internal Button StartupRefreshBtn;

	internal ItemsControl StartupList;

	internal Grid PageProcess;

	internal Button ProcessKillBtn;

	internal Button ProcessRefreshBtn;

	internal ListBox ProcessList;

	internal Grid PageNetwork;

	internal Button NetworkFlushBtn;

	internal Button NetworkResetBtn;

	internal Button NetworkReleaseBtn;

	internal TextBlock NetworkResult;

	internal Grid PageHosts;

	internal Button HostsSaveBtn;

	internal TextBox HostsEditor;

	internal TextBlock HostsStatus;

	internal Grid PagePower;

	internal ItemsControl PowerPlanList;

	internal Button PowerHighPerfBtn;

	internal Button PowerUltimateBtn;

	internal TextBlock PowerStatus;

	internal Grid PageContext;

	internal ItemsControl ContextMenuList;

	internal Button ContextRefreshBtn;

	internal Grid PageDisk;

	internal Button DiskRefreshBtn;

	internal ItemsControl DiskDriveList;

	internal Grid PageBrowser;

	internal Button BrowserChromeBtn;

	internal Button BrowserFirefoxBtn;

	internal Button BrowserEdgeBtn;

	internal TextBlock BrowserResult;

	internal Grid PageBoost;

	internal Button BoostGamingBtn;

	internal Button BoostHardwareBtn;

	internal Button BoostPrivacyBtn;

	internal Button BoostWindowsBtn;

	internal TextBlock StatusBarText;

	internal TextBlock StatusBarVersion;

	private bool _contentLoaded;

	private bool HasPremium => _license?.IsValid ?? false;

	private bool IsTrial => _license?.KeyId == "TRIAL";

	private bool TrialExpired
	{
		get
		{
			if (File.Exists(_trialPath))
			{
				if (_license != null)
				{
					return !_license.IsValid;
				}
				return true;
			}
			return false;
		}
	}

	public MainWindow()
	{
		InitializeComponent();
		_licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.bin");
		_trialPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trial.bin");
		_togglesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "toggles.dat");
		LoadLicense();
		StartTrial();
		InitializeOptimizations();
		CategoryCardsControl.ItemsSource = _categoryCards;
		BuildCategoryCards();
		bool flag = App.IsAdministrator();
		ActStatusBar(flag ? "Running as Administrator" : "Running as user - some tweaks need admin");
		StatusBarVersion.Text = "v" + Updater.AppVersion;
		FilterByCategory("all");
		LoadToggleStates();
		RefreshAllBadges();
		ApplyLicenseState();
	}

	private void LoadLicense()
	{
		try
		{
			bool flag = false;
			if (File.Exists(_trialPath))
			{
				byte[] bytes = ProtectedData.Unprotect(File.ReadAllBytes(_trialPath), DpapiEntropy, DataProtectionScope.CurrentUser);
				TrialData trialData = JsonSerializer.Deserialize<TrialData>(Encoding.UTF8.GetString(bytes));
				if (trialData != null && trialData.Expires > DateTime.UtcNow && trialData.MachineId == MachineIdentity.GetId())
				{
					_license = new LicenseInfo
					{
						Key = "TRIAL",
						Tier = LicenseTier.Pro,
						IssuedUtc = trialData.Issued,
						ExpiresUtc = trialData.Expires,
						KeyId = "TRIAL"
					};
					flag = true;
					Log.Information("Active trial - {Days}d left", _license.DaysLeft);
				}
			}
			if (flag || !File.Exists(_licensePath))
			{
				return;
			}
			byte[] bytes2 = ProtectedData.Unprotect(File.ReadAllBytes(_licensePath), DpapiEntropy, DataProtectionScope.CurrentUser);
			string text = Encoding.UTF8.GetString(bytes2).Trim();
			if (LicenseKeyCodec.TryValidate(text, out LicenseInfo info, out string _) && info != null)
			{
				string id = MachineIdentity.GetId();
				string activatedMachine = ActivationStore.GetActivatedMachine(info.KeyId);
				if (activatedMachine == id)
				{
					_license = info;
					_savedKey = text;
					Log.Information("License activated: {Tier}, {Days}d left", info.Tier, info.DaysLeft);
				}
				else if (activatedMachine != null)
				{
					Log.Warning("License key was activated on a different machine - rejecting");
				}
				else
				{
					Log.Information("Saved key found but not yet activated on this machine");
				}
			}
		}
		catch
		{
			_license = null;
		}
	}

	private void StartTrial()
	{
		LicenseInfo license = _license;
		if ((license != null && license.IsValid) || File.Exists(_trialPath))
		{
			return;
		}
		try
		{
			TrialData trialData = new TrialData
			{
				Issued = DateTime.UtcNow,
				Expires = DateTime.UtcNow.AddDays(3.0),
				MachineId = MachineIdentity.GetId()
			};
			string s = JsonSerializer.Serialize(trialData);
			byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(s), DpapiEntropy, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(_trialPath, bytes);
			_license = new LicenseInfo
			{
				Key = "TRIAL",
				Tier = LicenseTier.Pro,
				IssuedUtc = trialData.Issued,
				ExpiresUtc = trialData.Expires,
				KeyId = "TRIAL"
			};
			Log.Information("3-day trial started for {Machine}", trialData.MachineId.Substring(0, 12));
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to start trial");
		}
	}

	private void SaveToggleStates()
	{
		try
		{
			string s = JsonSerializer.Serialize(_allItems.ToDictionary((OptimizationItem i) => i.Name, (OptimizationItem i) => i.IsEnabled));
			byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(s), DpapiEntropy, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(_togglesPath, bytes);
		}
		catch
		{
		}
	}

	private void LoadToggleStates()
	{
		try
		{
			if (!File.Exists(_togglesPath))
			{
				return;
			}
			byte[] bytes = ProtectedData.Unprotect(File.ReadAllBytes(_togglesPath), DpapiEntropy, DataProtectionScope.CurrentUser);
			Dictionary<string, bool> dictionary = JsonSerializer.Deserialize<Dictionary<string, bool>>(Encoding.UTF8.GetString(bytes));
			if (dictionary == null)
			{
				return;
			}
			foreach (OptimizationItem allItem in _allItems)
			{
				if (dictionary.TryGetValue(allItem.Name, out var value) && value)
				{
					allItem.SetEnabledDirect(value);
					_manager.SetToggle(allItem.Name, value);
				}
			}
		}
		catch
		{
		}
	}

	private void SaveLicense(string key)
	{
		try
		{
			byte[] bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), DpapiEntropy, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(_licensePath, bytes);
			if (File.Exists(_trialPath))
			{
				File.Delete(_trialPath);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Failed to save license");
		}
	}

	private void InitializeOptimizations()
	{
		Add("Disable Game DVR", "Disables background Game DVR capture.", "gaming", "Gaming", RegistryHive.CurrentUser, "System\\GameConfigStore", "GameDVR_Enabled", 0, 1, admin: false, 1);
		Add("Enable Game Mode", "Enables Windows Game Mode.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\GameBar", "AutoGameModeEnabled", 1, 0, admin: false, 2);
		Add("Disable Xbox Game Bar", "Disables the Xbox Game Bar overlay.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", 0, 1, admin: false, 3);
		Add("Game Boost Profile", "GPU scheduling + high performance mode.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 2, 1, admin: true, 4, premium: true);
		Add("GPU Priority Mode", "Sets high GPU scheduling priority for games.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "GPU Priority", 8, 0, admin: true, 5, premium: true);
		Add("Network Throttling Disable", "Disables Windows network throttling for lower ping.", "gaming", "Network", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", -1, 0, admin: true, 6, premium: true);
		Add("Disable Nagle's Algorithm", "Reduces network latency for real-time apps.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DisableTaskOffload", 1, 0, admin: true, 7, premium: true);
		Add("Disable Startup Delay", "Reduces Explorer startup delay.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, 1, admin: false, 8);
		Add("Disable Thumbnail Cache", "Stops Explorer from caching thumbnails.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCache", 1, 0, admin: false, 9);
		Add("Disable Power Throttling", "Disables power throttling for consistent CPU perf.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling", "PowerThrottlingOff", 1, 0, admin: true, 10, premium: true);
		Add("Disable Prefetch", "Disables prefetching for faster boot on SSDs.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher", 0, 3, admin: true, 11, premium: true);
		Add("Disable Fast Startup", "Disables hybrid boot for cleaner restarts.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", 0, 1, admin: true, 12, premium: true);
		Add("TCP/IP Optimization (RSS)", "Enables Receive Side Scaling for faster networking.", "hardware", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableRSS", 1, 0, admin: true, 13, premium: true);
		Add("Advanced Memory Cleanup", "Clears page file at shutdown for memory optimization.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown", 1, 0, admin: true, 14, premium: true);
		Add("Disable Advertising ID", "Stops apps from using the Windows advertising ID.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 0, 1, admin: false, 15);
		Add("Disable Telemetry", "Limits diagnostic data collection.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry", 0, 1, admin: true, 16);
		Add("Disable Activity History", "Stops Timeline uploads.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "PublishUserActivities", 0, 1, admin: true, 17);
		Add("Disable Cortana Consent", "Turns off Cortana consent.", "privacy", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "CortanaConsent", 0, 1, admin: false, 18);
		Add("Disable Web Search", "Prevents Bing in Windows Search.", "privacy", "Windows", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "DisableWebSearch", 1, 0, admin: true, 19);
		Add("Disable Silent Installs", "Prevents silent promotional app installs.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0, 1, admin: false, 20);
		Add("Disable Suggested Content", "Stops suggested app content.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0, 1, admin: false, 21);
		Add("Disable Cortana Service", "Disables Cortana entirely via policy.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "AllowCortana", 0, 1, admin: true, 22);
		Add("Disable Search Web Results", "Removes web results from Windows Search.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "ConnectedSearchUseWeb", 0, 1, admin: true, 23);
		Add("Disable News Feed", "Removes news feed from taskbar.", "privacy", "Windows", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Feeds", "EnableFeeds", 0, 1, admin: true, 24);
		Add("Show File Extensions", "Shows known file extensions in Explorer.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "HideFileExt", 0, 1, admin: false, 25);
		Add("Disable Chat Icon", "Hides the Teams chat icon from taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn", 0, 1, admin: false, 26);
		Add("Disable Widgets", "Disables the Widgets board feature.", "windows", "Windows", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Dsh", "AllowNewsAndInterests", 0, 1, admin: true, 27);
		Add("Show Search Icon", "Changes search box to icon-only on taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", 0, 1, admin: false, 28);
		Add("Disable AutoPlay", "Prevents automatic playback of media.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\AutoplayHandlers", "DisableAutoplay", 1, 0, admin: false, 29);
		Add("AutoTray Icons", "Shows all notification icons in the taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", "EnableAutoTray", 0, 1, admin: false, 30, premium: true);
		Add("Disable Mouse Acceleration", "Disables mouse acceleration for consistent aiming.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Mouse", "MouseSpeed", 0, 1, admin: false, 31);
		Add("Disable 1st Mouse Threshold", "Reduces mouse threshold for precise input.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Mouse", "MouseThreshold1", 0, 6, admin: false, 32);
		Add("Disable 2nd Mouse Threshold", "Reduces mouse threshold for precise input.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Mouse", "MouseThreshold2", 0, 10, admin: false, 33);
		Add("Disable WU Driver Updates", "Prevents Windows Update from replacing GPU/audio drivers.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1, 0, admin: true, 34);
		Add("Disable TCP Auto-Tuning", "Disables TCP auto-tuning for lower network latency.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableTCPA", 0, 1, admin: true, 35, premium: true);
		Add("Disable Large Send Offload", "Disables LSO for reduced network latency.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DisableLargeSendOffload", 1, 0, admin: true, 36, premium: true);
		Add("Disable Auto-DoH Probing", "Disables automatic DNS over HTTPS probing.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "EnableAutoDoh", 0, 2, admin: true, 37);
		Add("Disable LMHosts Lookup", "Disables NetBIOS LMHosts lookup for faster name resolution.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters", "EnableLMHOSTS", 0, 1, admin: true, 38);
		Add("Disable NTFS Last Access", "Disables NTFS last access timestamps for faster disk IO.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FileSystem", "NtfsDisableLastAccessUpdate", 1, 0, admin: true, 39);
		Add("Disable 8.3 Name Creation", "Disables 8.3 filename creation for faster file operations.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FileSystem", "NtfsDisable8dot3NameCreation", 1, 0, admin: true, 40, premium: true);
		Add("Disable Paging Executive", "Keeps kernel and drivers in RAM for faster performance.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1, 0, admin: true, 41, premium: true);
		Add("Enable Large System Cache", "Increases file system cache for faster disk IO.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache", 1, 0, admin: true, 42);
		Add("Verbose Boot Status", "Shows detailed boot messages instead of Windows logo.", "hardware", "Performance", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "VerboseStatus", 1, 0, admin: true, 43, premium: true);
		Add("Auto-End Hung Tasks", "Automatically ends hung tasks on shutdown/restart.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "AutoEndTasks", "1", "0", admin: false, 44, premium: false, RegistryValueKind.String);
		Add("Reduce HungApp Timeout", "Reduces hung app timeout to 2 seconds.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "HungAppTimeout", "2000", "5000", admin: false, 45, premium: false, RegistryValueKind.String);
		Add("Reduce WaitToKill Timeout", "Reduces shutdown wait time to 2 seconds.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "WaitToKillAppTimeout", "2000", "5000", admin: false, 46, premium: false, RegistryValueKind.String);
		Add("Reduce Menu Show Delay", "Removes menu animation delay for instant menus.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "MenuShowDelay", "0", "400", admin: false, 47, premium: false, RegistryValueKind.String);
		Add("Disable P2P Update Sharing", "Disables peer-to-peer Windows Update distribution.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization", "DODownloadMode", 0, 1, admin: true, 48);
		Add("Disable Windows Error Reporting", "Disables Windows Error Reporting (WER).", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", 1, 0, admin: true, 49);
		Add("Disable CEIP", "Disables Customer Experience Improvement Program.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows", "CEIPEnabled", 0, 1, admin: true, 50);
		Add("Disable Inventory Collector", "Disables app inventory collection.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisableInventory", 1, 0, admin: true, 51);
		Add("Disable Handwriting Data", "Stops handwriting data collection.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization", "RestrictImplicitInkCollection", 1, 0, admin: false, 52);
		Add("Disable Text Data", "Stops typing/text data collection.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection", 1, 0, admin: false, 53);
		Add("Disable Root Cert Auto-Update", "Disables automatic root certificate updates.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\SystemCertificates\\AuthRoot", "DisableRootAutoUpdate", 1, 0, admin: true, 54);
		Add("Disable Xbox Telemetry", "Disables Xbox Game DVR telemetry reporting.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0, 1, admin: true, 55, premium: true);
		Add("Disable Bing Search", "Disables Bing search in Windows Search results.", "privacy", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled", 0, 1, admin: false, 56);
		Add("Disable Cloud Search", "Disables cloud content search.", "privacy", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "AllowCloudSearch", 0, 1, admin: false, 57);
		Add("Disable OneDrive Sync", "Disables OneDrive file sync via policy.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive", "DisableFileSyncNGSC", 1, 0, admin: true, 58);
		Add("Disable Store Auto-Update", "Disables automatic Store app updates.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsStore\\WindowsUpdate", "AutoDownload", 0, 4, admin: true, 59);
		Add("Disable Consumer Features", "Prevents auto-install of recommended apps.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures", 1, 0, admin: true, 60, premium: true);
		Add("Hide Task View Button", "Hides the Task View button from the taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton", 0, 1, admin: false, 61);
		Add("Disable Snap Assist", "Disables window snap assist/arranging.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "SnapFill", 0, 1, admin: false, 62);
		Add("Disable Lock Screen", "Disables the Windows lock screen entirely.", "windows", "Windows", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen", 1, 0, admin: true, 63);
		Add("Launch to This PC", "Opens Explorer to This PC instead of Quick Access.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "LaunchTo", 1, 0, admin: false, 64);
		Add("Show All Folders", "Shows all user folders in the Explorer navigation pane.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "NavPaneShowAllFolders", 1, 0, admin: false, 65);
		Add("System Dark Theme", "Enables dark theme for system UI elements.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", 0, 1, admin: false, 66);
		Add("App Dark Theme", "Enables dark theme for apps.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 0, 1, admin: false, 67);
		Add("Hide Recent Files", "Hides recent files from Quick Access.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowRecent", 0, 1, admin: false, 68);
		Add("Hide Frequent Folders", "Hides frequent folders from Quick Access.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowFrequent", 0, 1, admin: false, 69);
		Add("Hide Cortana Button", "Hides the Cortana button from the taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowCortanaButton", 0, 1, admin: false, 70);
		Add("Disable Taskbar Animations", "Disables taskbar animation effects.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAnimations", 0, 1, admin: false, 71);
		Add("Disable Low Disk Warning", "Disables low disk space warning notifications.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLowDiskSpaceChecks", 1, 0, admin: false, 72);
		Add("Show Seconds in Clock", "Shows seconds in the taskbar system clock.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowSecondsInSystemClock", 1, 0, admin: false, 73, premium: true);
		Add("Enable NumLock at Boot", "Enables NumLock at Windows startup.", "windows", "Windows", RegistryHive.CurrentUser, "Control Panel\\Keyboard", "InitialKeyboardIndicators", 2, 0, admin: false, 74);
		Add("Taskbar Left Alignment", "Aligns taskbar icons to the left (Windows 11).", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAl", 0, 1, admin: false, 75);
		Add("Keyboard Response Delay", "Reduces keyboard input delay for faster response.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Keyboard", "KeyboardDelay", 0, 1, admin: false, 76);
		Add("Disable Background Apps", "Stops background apps from consuming resources.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1, 0, admin: false, 77, premium: true);
		Add("Disable ICMP Redirect", "Prevents ICMP redirect attacks for gaming stability.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableICMPRedirect", 0, 1, admin: true, 78, premium: true);
		Add("Disable DHCP Media Sense", "Faster network reconnection by disabling media sensing.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DisableDHCPMediaSense", 1, 0, admin: true, 79);
		Add("Disable NetBIOS over TCP", "Disables NetBIOS for reduced network overhead.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters", "NetbiosOptions", 2, 0, admin: true, 80, premium: true);
		Add("Disable Windows Key", "Disables the Windows key during fullscreen apps.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoWinKeys", 1, 0, admin: false, 81, premium: true);
		Add("Mouse Sub-Speed", "Disables mouse sub-speed for consistent aiming.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Mouse", "MouseSubSpeed", 0, 1, admin: false, 82);
		Add("Foreground Responsiveness", "Boosts foreground app responsiveness for gaming.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 10, 20, admin: true, 83, premium: true);
		Add("Foreground Lock Timeout", "Eliminates foreground lock delay for snappier inputs.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Desktop", "ForegroundLockTimeout", 0, 200000, admin: false, 84, premium: true);
		Add("Foreground Flash Count", "Reduces taskbar flash count for foreground apps.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Desktop", "ForegroundFlashCount", 0, 3, admin: false, 85);
		Add("Win32 Priority Separation", "Optimizes CPU scheduling for foreground apps.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 38, 2, admin: true, 86, premium: true);
		Add("Disable Desktop Preview", "Disables thumbnail previews on the taskbar.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisablePreviewDesktop", 1, 0, admin: false, 87);
		Add("MFT Zone Size", "Reserves more MFT space for faster NTFS performance.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FileSystem", "NtfsMftZoneReservation", 2, 1, admin: true, 88, premium: true);
		Add("Disable Sync Notifications", "Hides OneDrive sync status in File Explorer.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowSyncProviderNotifications", 0, 1, admin: false, 89);
		Add("Disable Aero Shake", "Prevents window shake minimize gesture.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisallowShaking", 1, 0, admin: false, 90, premium: true);
		Add("Hide Recently Added Apps", "Hides recently added apps from the Start menu.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "HideRecentlyAddedApps", 1, 0, admin: false, 91);
		Add("Disable Balloon Tips", "Disables system balloon notification tips.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableBalloonTips", 0, 1, admin: false, 92, premium: true);
		Add("Disable People Bar", "Hides the people contacts bar from the taskbar.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowPeopleBar", 0, 1, admin: false, 93);
		Add("Show Run in Start", "Adds the Run command to the Start menu.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_ShowRun", 1, 0, admin: false, 94);
		Add("Show Settings in Start", "Adds the Settings link to the Start menu.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_ShowSettings", 1, 0, admin: false, 95);
		Add("Show Downloads in Start", "Adds the Downloads folder to the Start menu.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_ShowDownloads", 1, 0, admin: false, 96);
		Add("Disable Toast Notifications", "Disables all toast notification popups.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "ToastEnabled", 0, 1, admin: false, 97);
		Add("Disable Lock Screen Spotlight", "Disables Windows Spotlight lock screen images.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenEnabled", 0, 1, admin: false, 98, premium: true);
		Add("Disable Location Tracking", "Disables location services system-wide.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\LocationAndSensors", "DisableLocation", 1, 0, admin: true, 99);
		Add("Disable Advertising Policy", "Blocks the advertising ID via group policy.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo", "DisabledByGroupPolicy", 1, 0, admin: true, 100);
		Add("Disable Tailored Experiences", "Stops personalized ads from diagnostic data.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableTailoredExperiencesWithDiagnosticData", 1, 0, admin: true, 101, premium: true);
		Add("Disable Soft Landing", "Prevents Windows from reinstalling bloatware.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableSoftLanding", 1, 0, admin: true, 102);
		Add("Disable Windows Hello", "Disables biometric Windows Hello sign-in.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Biometrics", "Enabled", 0, 1, admin: true, 103);
		Add("Disable Edge Telemetry", "Disables Microsoft Edge telemetry collection.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\Main", "AllowTelemetry", 0, 1, admin: true, 104, premium: true);
		Add("Disable Edge Preload", "Prevents Edge from preloading at boot.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\MicrosoftEdge\\Main", "AllowPrelaunch", 0, 1, admin: true, 105);
		Add("Disable App Suggestions", "Stops Windows from suggesting apps in Start.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-353694Enabled", 0, 1, admin: false, 106);
		Add("Disable WiFi Sense", "Disables auto-connect to suggested hotspots.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting", "value", 0, 1, admin: true, 107, premium: true);
		Add("Disable Camera Access", "Blocks camera access system-wide.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam", "Value", "Deny", "Allow", admin: true, 108, premium: true, RegistryValueKind.String);
		Add("Disable Microphone Access", "Blocks microphone access system-wide.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone", "Value", "Deny", "Allow", admin: true, 109, premium: true, RegistryValueKind.String);
		Add("Disable Notification Center", "Hides the notification center (action center).", "windows", "Windows", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableNotificationCenter", 1, 0, admin: true, 110, premium: true);
		Add("Disable Quick Settings", "Hides the quick settings panel (Windows 11).", "windows", "Windows", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableControlCenter", 1, 0, admin: true, 111, premium: true);
		Add("Show My Computer in Start", "Shows This PC in the Start menu.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_ShowMyComputer", 1, 0, admin: false, 112);
		Add("Show Control Panel in Start", "Shows Control Panel in the Start menu.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_ShowControlPanel", 1, 0, admin: false, 113);
		Add("Show Music in Start", "Shows the Music folder in the Start menu.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_ShowMyMusic", 0, 1, admin: false, 114);
		Add("Hide Network in Start", "Hides the Network flyout from the Start menu.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_ShowNetPlaces", 0, 1, admin: false, 115, premium: true);
		Add("Disable Network Thumbnails", "Disables thumbnail caching for network folders.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisableThumbnailCacheOnNetwork", 1, 0, admin: false, 116);
		Add("Disable Auto-Correction", "Disables touch keyboard auto-correction.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\TabletTip\\1.7", "EnableAutoCorrection", 0, 1, admin: false, 117);
		Add("Disable Auto-Complete", "Disables touch keyboard auto-complete.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\TabletTip\\1.7", "EnableAutocomplete", 0, 1, admin: false, 118);
		Add("Disable Spell Check", "Disables touch keyboard spell check.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\TabletTip\\1.7", "EnableSpellChecker", 0, 1, admin: false, 119, premium: true);
		Add("Disable Text Prediction", "Disables touch keyboard text prediction.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\TabletTip\\1.7", "EnableTextPrediction", 0, 1, admin: false, 120, premium: true);
		Add("Roblox High CPU Priority", "Forces Roblox to run at high CPU priority for less stutter.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\RobloxPlayerBeta.exe\\PerfOptions", "CpuPriorityClass", 3, 2, admin: true, 121, premium: true);
		Add("Roblox High GPU Priority", "Forces Roblox to use high performance GPU mode.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\DirectX\\UserGpuPreferences", "RobloxPlayerBeta.exe", "GpuPreference=1;", "", admin: false, 122, premium: true, RegistryValueKind.String);
		Add("Roblox No Fullscreen Opt", "Disables fullscreen optimizations for Roblox (reduces input lag).", "gaming", "Gaming", RegistryHive.CurrentUser, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers", "RobloxPlayerBeta.exe", "~DISABLEDXMAXIMIZEDWINDOWEDMODE", "", admin: false, 123, premium: true, RegistryValueKind.String);
		Add("Roblox Low Latency Mode", "Enables low latency network mode for Roblox servers.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", "TCPNoDelay", 1, 0, admin: true, 124, premium: true);
		Add("Roblox Mouse Smoothing Off", "Disables raw mouse smoothing for consistent aiming.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Mouse", "SmoothMouseXCurve", "", "0,0;1,1", admin: false, 125, premium: false, RegistryValueKind.String);
		Add("Roblox High IO Priority", "Gives Roblox high disk IO priority for faster asset loading.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\RobloxPlayerBeta.exe\\PerfOptions", "IoPriority", 3, 2, admin: true, 126, premium: true);
		Add("Roblox High Page Priority", "Keeps Roblox pages in RAM for less stutter.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\RobloxPlayerBeta.exe\\PerfOptions", "PagePriority", 5, 1, admin: true, 127, premium: true);
		Add("Roblox Use Large Pages", "Enables large memory pages for Roblox (reduces TLB misses).", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\RobloxPlayerBeta.exe", "UseLargePages", 1, 0, admin: true, 128, premium: true);
		Add("Roblox Disable Heap Check", "Disables heap corruption detection for less overhead.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\RobloxPlayerBeta.exe", "DisableHeapLookaside", 1, 0, admin: true, 129, premium: true);
		Add("Roblox Disable Cursor Shadow", "Removes cursor shadow for slightly less GPU load.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Cursors", "CursorShadow", 0, 1, admin: false, 130);
		Add("Roblox Disable Windows Ink", "Disables Windows Ink for Roblox (pen input overhead).", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\PenService", "PenService", 0, 1, admin: false, 131);
		Add("Roblox Disable Taskbar Blink", "Stops taskbar animations while Roblox is running.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAnimations", 0, 1, admin: false, 132);
		Add("Roblox Faster Keyboard", "Increases keyboard repeat rate for snappier Roblox controls.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Keyboard", "KeyboardSpeed", 48, 31, admin: false, 133);
		Add("Roblox Disable Alt-Tab Delay", "Removes Alt-Tab transition delay while in Roblox.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Desktop", "CoolSwitchColumns", "1", "7", admin: false, 134, premium: false, RegistryValueKind.String);
		Add("Roblox Force Single GPU", "Forces Roblox to use one GPU (no multi-GPU overhead).", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\RobloxPlayerBeta.exe", "EnableMIGPU", 0, 1, admin: true, 135, premium: true);
		Add("Disable Core Parking", "Prevents CPU cores from parking for consistent performance.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583", "Attributes", 0, 1, admin: true, 136, premium: true);
		Add("Disable C-States", "Prevents deep CPU idle states for lower latency.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\abfc05f4-45a9-4b96-a8f4-ca18c0b39e2e", "Attributes", 0, 1, admin: true, 137, premium: true);
		Add("Disable USB Suspend", "Prevents USB selective suspend for connected devices.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\2a737441-1930-4402-8d77-b2bebba308a3\\48e6b7a6-50f5-4782-a5d4-53bb8f07e226", "Attributes", 0, 1, admin: true, 138, premium: true);
		Add("Disable Link State Power", "Disables PCI Express link state power management.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\501a4d13-42af-4429-9fd1-a8218c268e20\\ee12f906-dff1-4094-b310-51664a8f7df1", "Attributes", 0, 1, admin: true, 139, premium: true);
		Add("High Performance Power Plan", "Enables the High Performance power plan.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\User\\PowerSchemes", "ActivePowerScheme", "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", "381b4222-f694-41f0-9685-ff5bb260df2e", admin: true, 140, premium: true, RegistryValueKind.String);
		Add("Disable HPET", "Disables High Precision Event Timer for gaming latency.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", 0, 1, admin: true, 141, premium: true);
		Add("Disable GPU Power Saving", "Prevents GPU from entering low power states.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\7516b95f-f776-4464-8c53-06167f40cc99\\fff9f3e7-7e3f-4f6b-9df2-293b74ecb80a", "Attributes", 0, 1, admin: true, 142, premium: true);
		Add("Disable CPU Throttling", "Disables thermal throttling for max CPU performance.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\be337238-0d82-4146-a960-4f3749d470c7", "Attributes", 0, 1, admin: true, 143, premium: true);
		Add("Optimize SSD TRIM", "Enables TRIM command for SSD drives.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FileSystem", "DisableDeleteNotification", 0, 1, admin: true, 144);
		Add("Disable Drive Indexing", "Disables Windows Search indexing on all drives.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableSuperfetch", 0, 3, admin: true, 145, premium: true);
		Add("Faster Shutdown", "Reduces service shutdown timeout to speed up shutdown.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control", "WaitToKillServiceTimeout", "2000", "5000", admin: true, 146, premium: false, RegistryValueKind.String);
		Add("Disable Speech Recognition", "Disables online speech recognition.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\InputPersonalization", "AllowInputPersonalization", 0, 1, admin: true, 147);
		Add("Disable Handwriting Collection", "Stops handwriting pattern data collection.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\TabletPC", "PreventHandwritingDataSharing", 1, 0, admin: true, 148);
		Add("Disable Lock Screen Ads", "Removes ads from the lock screen.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", 0, 1, admin: true, 149);
		Add("Disable App Telemetry", "Disables app diagnostic data collection.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsRunInBackground", 2, 0, admin: true, 150);
		Add("Disable Microsoft Account", "Signs out of Microsoft Account sign-in assistant.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "NoConnectedUser", 3, 0, admin: true, 151);
		Add("Disable Context Menu Delay", "Removes the right-click context menu delay.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "MenuShowDelay", "0", "400", admin: false, 152, premium: false, RegistryValueKind.String);
		Add("Disable Animations", "Disables minimize/maximize window animations.", "windows", "Windows", RegistryHive.CurrentUser, "Control Panel\\Desktop\\WindowMetrics", "MinAnimate", "0", "1", admin: false, 153, premium: false, RegistryValueKind.String);
		Add("Classic Context Menu", "Enables classic full context menu (Windows 11).", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableXamlMenus", 0, 1, admin: false, 154, premium: true);
		Add("Disable Snap Window Sizing", "Disables snap window drag-to-size behavior.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "SnapSizing", 0, 1, admin: false, 155);
		Add("Disable SysMain Service", "Disables the SysMain (Superfetch) service for reduced background activity.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\SysMain", "Start", 4, 3, admin: true, 156, premium: true);
		Add("Disable Windows Search", "Disables Windows Search indexing service entirely.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WSearch", "Start", 4, 3, admin: true, 157, premium: true);
		Add("Disable Windows Update P2P", "Disables P2P Windows Update downloads for bandwidth savings.", "gaming", "Network", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config", "DODownloadMode", 0, 1, admin: true, 158);
		Add("Disable Background Apps Policy", "Disables background app execution via policy.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsRunInBackground", 2, 0, admin: true, 159, premium: true);
		Add("Disable Print Spooler", "Disables the print spooler service for reduced overhead.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Spooler", "Start", 4, 3, admin: true, 160, premium: true);
		Add("Disable Sticky Keys Prompt", "Prevents the Sticky Keys popup when Shift is held.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Accessibility\\StickyKeys", "Flags", "506", "510", admin: false, 161, premium: false, RegistryValueKind.String);
		Add("Disable Filter Keys Prompt", "Prevents the Filter Keys popup when holding right Shift.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Accessibility\\Keyboard Response", "Flags", "122", "126", admin: false, 162, premium: false, RegistryValueKind.String);
		Add("Disable Toggle Keys Prompt", "Prevents the Toggle Keys popup when holding Num Lock.", "gaming", "Gaming", RegistryHive.CurrentUser, "Control Panel\\Accessibility\\ToggleKeys", "Flags", "58", "62", admin: false, 163, premium: false, RegistryValueKind.String);
		Add("Maximum GPU Performance", "Forces maximum GPU performance profile.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\\Scheduler", "EnablePreemption", 0, 1, admin: true, 164, premium: true);
		Add("Disable GameInput Service", "Disables the GameInput service for reduced latency.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\GameInput", "Start", 4, 3, admin: true, 165, premium: true);
		Add("Disable Touch Keyboard", "Disables the touch keyboard for fullscreen gaming.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\TabletTip\\1.7", "EnableDesktopModeAutoInvoke", 0, 1, admin: false, 166);
		Add("Ultimate Performance Plan", "Enables the Ultimate Performance power scheme.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\User\\PowerSchemes", "ActivePowerScheme", "e9a42b02-d5df-448d-aa00-03f14749eb61", "381b4222-f694-41f0-9685-ff5bb260df2e", admin: true, 167, premium: true, RegistryValueKind.String);
		Add("Disable Memory Compression", "Disables memory compression for less CPU overhead.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "EnableCompression", 0, 1, admin: true, 168, premium: true);
		Add("Disable Windows Tips", "Disables Windows tips and suggestions.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, 1, admin: false, 169);
		Add("Disable Xbox Networking", "Disables Xbox networking services (XblAuth, XblGameSave).", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\XblAuthManager", "Start", 4, 3, admin: true, 170, premium: true);
		Add("Disable Xbox Live Game Save", "Disables Xbox Live game save sync service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\XblGameSave", "Start", 4, 3, admin: true, 171, premium: true);
		Add("Disable Xbox Net Api", "Disables Xbox Live networking API service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\XboxNetApiSvc", "Start", 4, 3, admin: true, 172, premium: true);
		Add("Disable Windows Update AutoRestart", "Prevents automatic restarts after Windows Update.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoRebootWithLoggedOnUsers", 1, 0, admin: true, 173, premium: true);
		Add("Disable Fullscreen Opt Global", "Disables fullscreen optimizations globally for less input lag.", "gaming", "Gaming", RegistryHive.CurrentUser, "SYSTEM\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, 0, admin: false, 174, premium: true);
		Add("Disable Nagle's Algorithm Global", "Disables Nagle's algorithm for all TCP connections.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", "TCPNoDelay", 1, 0, admin: true, 175, premium: true);
		Add("Disable RDC", "Disables Remote Desktop Connection for security.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Terminal Server", "fDenyTSConnections", 1, 0, admin: true, 176);
		Add("Disable Windows Defender Realtime", "Disables real-time Windows Defender monitoring for gaming.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableRealtimeMonitoring", 1, 0, admin: true, 177, premium: true);
		Add("Disable Defender Cloud", "Disables Defender cloud-based protection.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SubmitSamplesConsent", 0, 1, admin: true, 178, premium: true);
		Add("Disable Defender MAPS", "Disables Microsoft Active Protection Service reporting.", "gaming", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SpynetReporting", 0, 2, admin: true, 179, premium: true);
		Add("Disable Edge Preload Service", "Prevents Microsoft Edge from pre-launching.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\edgeupdate", "Start", 4, 3, admin: true, 180, premium: true);
		Add("Disable Bluetooth Service", "Disables Bluetooth support service.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\BthServ", "Start", 4, 3, admin: true, 181, premium: true);
		Add("Disable Windows Error Reporting Srv", "Disables Windows Error Reporting background service.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WerSvc", "Start", 4, 3, admin: true, 182);
		Add("Disable Diagnostic Service", "Disables the Diagnostic Execution Service.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\diagsvc", "Start", 4, 3, admin: true, 183);
		Add("Disable Diagnostic Track", "Disables the Diagnostic Tracking Service.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\DiagTrack", "Start", 4, 3, admin: true, 184);
		Add("Disable WLAN AutoConfig", "Disables WLAN AutoConfig service for wired gaming rigs.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WlanSvc", "Start", 4, 3, admin: true, 185, premium: true);
		Add("Reduce Boot Menu Timeout", "Reduces boot menu timeout to 3 seconds.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager", "BootExecuteTimeout", 3, 30, admin: true, 186);
		Add("Disable Boot Logo", "Disables the Windows boot logo for slightly faster boot.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "DisableBootLogo", 1, 0, admin: false, 187, premium: true);
		Add("Enable Write Cache", "Enables write caching on fixed disks.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FileSystem", "NtfsDisableLastAccessUpdate", 1, 0, admin: true, 188);
		Add("Disable Hibernation", "Disables hibernation to free system drive space.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power", "HibernateEnabled", 0, 1, admin: true, 189, premium: true);
		Add("Disable System Restore", "Disables System Restore for performance.", "hardware", "Performance", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows NT\\SystemRestore", "DisableConfig", 1, 0, admin: true, 190, premium: true);
		Add("Disable BOOTEX", "Disables boot-time disk defragmentation.", "hardware", "Performance", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Dfrg\\BootOptimizeFunction", "Enable", "N", "Y", admin: true, 191, premium: false, RegistryValueKind.String);
		Add("Disable Prefetch on Boot", "Disables boot prefetching for SSDs.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableBootPrefetcher", 0, 3, admin: true, 192, premium: true);
		Add("Disable Application PreLaunch", "Disables application pre-launching (App PreLaunch).", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 0, 1, admin: false, 193, premium: true);
		Add("Increase IO Priority", "Raises default IO priority for foreground processes.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IoPriority", 3, 0, admin: true, 194, premium: true);
		Add("Disable Desktop Composition", "Disables DWM composition for less GPU overhead.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\DWM", "Composition", 0, 1, admin: false, 195, premium: true);
		Add("Disable Animations All", "Disables all Windows animations and transitions.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 2, 0, admin: false, 196);
		Add("Disable Thumbnail Prefetch", "Disables thumbnail prefetching for faster browsing.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ThumbnailCache", "DisableThumbnailCache", 1, 0, admin: false, 197, premium: true);
		Add("Disable Mouse Shadow", "Disables the mouse pointer shadow (slight GPU savings).", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Cursors", "CursorShadow", 0, 1, admin: false, 198);
		Add("Disable Font Smoothing", "Disables font smoothing for less GPU load.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "FontSmoothing", "0", "2", admin: false, 199, premium: false, RegistryValueKind.String);
		Add("Disable Windows Defender Service", "Disables Windows Defender Antivirus service entirely.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WinDefend", "Start", 4, 2, admin: true, 200, premium: true);
		Add("Disable Event Log", "Disables Windows Event Log service.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\EventLog", "Start", 4, 3, admin: true, 201, premium: true);
		Add("Disable Task Scheduler", "Disables Task Scheduler for less background activity.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Schedule", "Start", 4, 3, admin: true, 202, premium: true);
		Add("Disable Time Broker", "Disables Time Broker service for reduced overhead.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\TimeBrokerSvc", "Start", 4, 3, admin: true, 203, premium: true);
		Add("Disable Wallet Service", "Disables WalletService for less background activity.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WalletService", "Start", 4, 3, admin: true, 204, premium: true);
		Add("Disable All User Services", "Disables Xbox, W search and sync services in one sweep.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\MessagingService", "Start", 4, 3, admin: true, 205, premium: true);
		Add("Disable Find My Device", "Disables Find My Device location tracking.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\FindMyDevice", "AllowFindMyDevice", 0, 1, admin: true, 206);
		Add("Disable Share Across Devices", "Disables cross-device sharing.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CDP", "RomotingEnabled", 0, 1, admin: false, 207);
		Add("Disable Clipboard Sync", "Disables clipboard sync across devices.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Clipboard", "EnableClipboardHistory", 0, 1, admin: false, 208);
		Add("Disable Cloud Clipboard", "Disables clipboard sync to the cloud.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Clipboard", "CloudClipboardEnabled", 0, 1, admin: false, 209);
		Add("Disable Timeline", "Disables Windows Timeline activity history.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", "EnableActivityFeed", 0, 1, admin: false, 210);
		Add("Disable SmartScreen", "Disables SmartScreen for app and file checking.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen", 0, 1, admin: true, 211);
		Add("Disable Store Suggestions", "Disables app suggestions in Windows Store.", "privacy", "Privacy", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, 1, admin: false, 212);
		Add("Disable AutoInstall Apps", "Prevents automatic installation of Store apps.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures", 1, 0, admin: true, 213);
		Add("Disable Cloud Sync Settings", "Disables Windows settings sync across devices.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\SettingSync", "DisableSettingSync", 2, 0, admin: true, 214, premium: true);
		Add("Disable Browser Sync", "Disables browser data sync to Microsoft.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Edge\\Sync", "SyncDisabled", 1, 0, admin: true, 215, premium: true);
		Add("Disable Defender Telemetry", "Disables Windows Defender telemetry reporting.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows Defender", "DisableTelemetry", 1, 0, admin: true, 216, premium: true);
		Add("Disable .NET Telemetry", "Disables .NET framework telemetry collection.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\\\.NETFramework", "EnableTelemetry", 0, 1, admin: true, 217, premium: true);
		Add("Disable PowerShell Telemetry", "Disables PowerShell telemetry.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\PowerShell", "EnableScriptBlockLogging", 0, 1, admin: true, 218, premium: true);
		Add("Disable Language List Sync", "Disables language settings sync across devices.", "privacy", "Privacy", RegistryHive.CurrentUser, "Control Panel\\International\\User Profile", "HttpAcceptLanguageOptOut", 1, 0, admin: false, 219);
		Add("Disable WER Telemetry", "Disables Windows Error Reporting telemetry.", "privacy", "Telemetry", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\Windows Error Reporting", "DontShowUI", 1, 0, admin: false, 220);
		Add("Disable WiFi Sense Policy", "Disables automatic hotspot connection via policy.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting", "value", 0, 1, admin: true, 221);
		Add("Disable Handwriting Personaliza", "Stops handwriting recognition personalization data upload.", "privacy", "Privacy", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\Handwriting", "DisableHandwritingPersonalization", 1, 0, admin: true, 222, premium: true);
		Add("Disable Inking Telemetry", "Disables inking and typing telemetry.", "privacy", "Telemetry", RegistryHive.CurrentUser, "Software\\Microsoft\\InputPersonalization\\TrainedDataStore", "HarvestContacts", 0, 1, admin: false, 223, premium: true);
		Add("Disable Windows Defender Cloud", "Disables Defender cloud-delivered protection reporting.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "DisableBlockAtFirstSeen", 1, 0, admin: true, 224, premium: true);
		Add("Disable Activity Feed", "Disables the Activity Feed service.", "privacy", "Privacy", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WFDSConMgrSvc", "Start", 4, 3, admin: true, 225);
		Add("Disable Connected User Svc", "Disables Connected User Experiences and Telemetry service.", "privacy", "Telemetry", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\UsoSvc", "Start", 4, 3, admin: true, 226);
		Add("Disable CDP User Service", "Disables User Data Access service.", "privacy", "Privacy", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\UdkUserSvc", "Start", 4, 3, admin: true, 227, premium: true);
		Add("Disable Messaging Service", "Disables Messaging service for privacy.", "privacy", "Privacy", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\MessagingService", "Start", 4, 3, admin: true, 228);
		Add("Disable PcaSvc", "Disables Program Compatibility Assistant service.", "privacy", "Privacy", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\PcaSvc", "Start", 4, 3, admin: true, 229);
		Add("Disable WMP Network Sharing", "Disables Windows Media Player network sharing.", "privacy", "Privacy", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WMPNetworkSvc", "Start", 4, 3, admin: true, 230);
		Add("Disable Search Highlights", "Disables search highlights and trending content.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "EnableDynamicContentInWSB", 0, 1, admin: false, 231);
		Add("Disable Taskbar News", "Disables news and weather on the taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Feeds", "ShellFeedsTaskbarViewMode", 2, 0, admin: false, 232, premium: true);
		Add("Hide Search Box", "Hides the search box from the taskbar entirely.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", 0, 1, admin: false, 233);
		Add("Show This PC on Desktop", "Adds This PC icon to the desktop.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0, 1, admin: false, 234);
		Add("Show Network on Desktop", "Adds Network icon to the desktop.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", "{F02C1A0D-BE21-4350-A0B6-736A0D004FC7}", 0, 1, admin: false, 235);
		Add("Show User Folder on Desktop", "Shows the user folder icon on desktop.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", "{59031A47-3F72-44A7-89C5-5595FE6B30EE}", 0, 1, admin: false, 236);
		Add("Show Control Panel on Desktop", "Shows Control Panel icon on the desktop.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 0, 1, admin: false, 237, premium: true);
		Add("Disable Sticky Keys Shortcut", "Disables the Shift+5x sticky keys keyboard shortcut.", "windows", "Windows", RegistryHive.CurrentUser, "Control Panel\\Accessibility\\StickyKeys", "Flags", "506", "510", admin: false, 238, premium: false, RegistryValueKind.String);
		Add("Disable Filter Keys Shortcut", "Disables the right-Shift+8s filter keys shortcut.", "windows", "Windows", RegistryHive.CurrentUser, "Control Panel\\Accessibility\\Keyboard Response", "Flags", "122", "126", admin: false, 239, premium: false, RegistryValueKind.String);
		Add("Disable Toggle Keys Shortcut", "Disables the NumLock+5s toggle keys shortcut.", "windows", "Windows", RegistryHive.CurrentUser, "Control Panel\\Accessibility\\ToggleKeys", "Flags", "58", "62", admin: false, 240, premium: false, RegistryValueKind.String);
		Add("Disable Aero Peek", "Disables Aero Peek desktop preview.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\DWM", "EnableAeroPeek", 0, 1, admin: false, 241);
		Add("Disable Transparency", "Disables window transparency effects.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 0, 1, admin: false, 242);
		Add("Disable Snap Window Layout", "Disables the Windows 11 snap layout popup on maximize.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapAssistFlyout", 0, 1, admin: false, 243, premium: true);
		Add("Disable Notification Center", "Hides notification center quick settings panel.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "HideSCANotificationCenter", 1, 0, admin: false, 244, premium: true);
		Add("Disable Lock Screen Blur", "Disables lock screen background blur.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Personalization", "NoLockScreenBlur", 1, 0, admin: false, 245, premium: true);
		Add("Disable Control Center", "Disables the quick settings panel (Windows 11).", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "DisableControlCenter", 1, 0, admin: false, 246, premium: true);
		Add("Disable Notifications Quiet", "Disables quiet hours / focus assist auto-rules.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\QuietHours", "Enabled", 0, 1, admin: false, 247);
		Add("Classic Volume Control", "Enables the classic volume mixer instead of modern one.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows NT\\CurrentVersion\\MTCUVC", "EnableMtcUvc", 0, 1, admin: false, 248, premium: true);
		Add("Disable Meet Now Icon", "Hides the Meet Now icon from the taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn", 0, 1, admin: false, 249);
		Add("Disable CoPilot Button", "Hides the Copilot button from the taskbar.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowCopilotButton", 0, 1, admin: false, 250, premium: true);
		Add("Show Clock with Seconds", "Shows seconds in the taskbar clock (dual).", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowSecondsInSystemClock", 1, 0, admin: false, 251);
		Add("Disable USB Notifications", "Disables USB connection/disconnection notifications.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTPN", 0, 1, admin: false, 252);
		Add("Disable Hibernate Button", "Hides the Hibernate option from Power menu.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\Explorer", "ShowHibernateOption", 0, 1, admin: false, 253);
		Add("Disable Sleep Button", "Hides the Sleep option from Power menu.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Policies\\Microsoft\\Windows\\Explorer", "ShowSleepOption", 0, 1, admin: false, 254);
		Add("Show File Operation Details", "Shows detailed file copy/move progress.", "windows", "Windows", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowDetailedProgress", 1, 0, admin: false, 255, premium: true);
		Add("Disable Windows Defender Sandbox", "Disables Defender sandbox for less overhead.", "gaming", "Gaming", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows Defender", "DisableAntiSpyware", 1, 0, admin: true, 256, premium: true);
		Add("Disable Cloud Search Indexing", "Disables cloud-based search indexing.", "gaming", "Gaming", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "AllowCloudSearch", 0, 1, admin: false, 257);
		Add("Disable Network Discovery", "Disables network discovery for less overhead.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\f38bf404-1d43-42f2-9305-67de0b28fc23\\205cffbd-7744-4f8b-9f7a-06b8a63b0c6f", "ACSettingIndex", 0, 1, admin: true, 258, premium: true);
		Add("Disable QoS", "Disables QoS packet scheduling for network performance.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Psched", "Start", 4, 3, admin: true, 259, premium: true);
		Add("Disable Receive Segment Coalesce", "Disables RSC for lower network latency.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DisableRSC", 1, 0, admin: true, 260, premium: true);
		Add("Disable TCP Chimney", "Disables TCP Chimney Offload.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableTCPChimney", 0, 1, admin: true, 261, premium: true);
		Add("Disable TCP Checksum Offload", "Disables TCP checksum offloading for consistent networking.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", "TcpChecksumOffloadIPv4", 0, 1, admin: true, 262, premium: true);
		Add("Disable UDP Checksum Offload", "Disables UDP checksum offloading.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces", "UdpChecksumOffloadIPv4", 0, 1, admin: true, 263, premium: true);
		Add("Max TCP Window Size", "Sets TCP window size to 64KB for faster transfers.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "GlobalMaxTcpWindowSize", 65536, 8760, admin: true, 264, premium: true);
		Add("Disable ECN", "Disables Explicit Congestion Notification.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "EnableECN", 0, 2, admin: true, 265, premium: true);
		Add("Disable Non-Delivery Alerts", "Reduces network disconnection alerts for gaming.", "gaming", "Network", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Notifications\\Settings", "NNE_GamingOptIn", 0, 1, admin: false, 266);
		Add("Disable Widgets Service", "Disables the Windows Widgets service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WidgetService", "Start", 4, 3, admin: true, 267, premium: true);
		Add("Disable WebView Host", "Disables WebView host process for less overhead.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WpnService", "Start", 4, 3, admin: true, 268);
		Add("Disable Push Notifications", "Disables Windows Push Notification service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\WpnUserService", "Start", 4, 3, admin: true, 269);
		Add("Disable BITS", "Disables Background Intelligent Transfer service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\BITS", "Start", 4, 3, admin: true, 270, premium: true);
		Add("Disable Delivery Optimization", "Disables Delivery Optimization service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\DoSvc", "Start", 4, 3, admin: true, 271, premium: true);
		Add("Disable Windows Store Service", "Disables the Windows Store service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\AppXSvc", "Start", 4, 3, admin: true, 272, premium: true);
		Add("Disable License Manager", "Disables Windows License Manager service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\wlidsvc", "Start", 4, 3, admin: true, 273, premium: true);
		Add("Disable Software Protection", "Disables Software Protection service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\sppsvc", "Start", 4, 3, admin: true, 274, premium: true);
		Add("High Priority Network Gaming", "Sets network QoS to prioritize games.", "gaming", "Network", RegistryHive.LocalMachine, "SOFTWARE\\Policies\\Microsoft\\Windows\\QoS", "QoS Fast Path", 1, 0, admin: true, 275, premium: true);
		Add("Disable NDU", "Disables Network Data Usage service.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Ndu", "Start", 4, 3, admin: true, 276, premium: true);
		Add("Disable WCM", "Disables Windows Connection Manager service.", "gaming", "Network", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\Wcmsvc", "Start", 4, 3, admin: true, 277, premium: true);
		Add("Disable Tablet Input Service", "Disables Tablet Input service for non-touch devices.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\TabletInputService", "Start", 4, 3, admin: true, 278, premium: true);
		Add("Disable Sensor Service", "Disables Sensor service for less background activity.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\SensorService", "Start", 4, 3, admin: true, 279, premium: true);
		Add("Disable Sync Host", "Disables Sync Host service.", "gaming", "Gaming", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Services\\SyncHost", "Start", 4, 3, admin: true, 280, premium: true);
		Add("Disable Disable Paging Executive", "Keeps kernel in RAM for max performance.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1, 0, admin: true, 281);
		Add("Disable LargeSystemCache", "Increases system file cache for disk performance.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache", 1, 0, admin: true, 282);
		Add("Set IO Priority to High", "Sets global IO priority to high for performance.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "IoPriority", 3, 0, admin: true, 283, premium: true);
		Add("Disable Process Idle Page", "Disables process idle page trimming.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingOfHeap", 1, 0, admin: true, 284, premium: true);
		Add("Disable Swapfile", "Disables swapfile on system drive (needs 8GB+ RAM).", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "SwapfileSize", 0, 1, admin: true, 285, premium: true);
		Add("Disable Virtual Memory", "Disables paging file (advanced, needs 16GB+ RAM).", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "PagingFiles", "", "", admin: true, 286, premium: true);
		Add("Disable Boot Defrag", "Disables boot-time defragmentation.", "hardware", "Performance", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft\\Dfrg\\BootOptimizeFunction", "Enable", "N", "Y", admin: true, 287, premium: false, RegistryValueKind.String);
		Add("Disable Last Access Time", "Disables NTFS last access time updates system-wide.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\FileSystem", "NtfsDisableLastAccessUpdate", 1, 0, admin: true, 288);
		Add("Enable Large Pages", "Enables large page support for applications.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargePageMinimum", 1, 0, admin: true, 289, premium: true);
		Add("Set Memory Priority to High", "Sets default process memory priority to high.", "hardware", "Performance", RegistryHive.LocalMachine, "SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "MemoryPriority", 3, 0, admin: true, 290, premium: true);
		Add("Disable Taskbar AutoHiding", "Disables taskbar auto-hide for snappier response.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StuckRects3", "Settings", "", "", admin: false, 291);
		Add("Disable Cascading Menus", "Disables cascading Start menu animations.", "hardware", "Performance", RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "CascadeMenu", 0, 1, admin: false, 292);
		Add("Disable ComboBox Animation", "Disables combo box animation effects.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "ComboBoxAnimation", "0", "1", admin: false, 293, premium: false, RegistryValueKind.String);
		Add("Disable Cursor Blink", "Disables cursor blinking in text fields.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "CursorBlinkRate", "-1", "530", admin: false, 294, premium: false, RegistryValueKind.String);
		Add("Disable ListView Animation", "Disables list view animation effects.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "ListviewAlphaSelect", "0", "1", admin: false, 295, premium: false, RegistryValueKind.String);
		Add("Disable Window Fade", "Disables fade effects for menus and tooltips.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "UserPreferencesMask", "90,12,03,80", "9E,1E,07,80", admin: false, 296, premium: false, RegistryValueKind.String);
		Add("Disable Selection Fade", "Disables selection fade effects.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "SelectionFade", "0", "1", admin: false, 297, premium: false, RegistryValueKind.String);
		Add("Disable Tooltip Fade", "Disables tooltip animation effects.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "TooltipAnimation", "0", "1", admin: false, 298, premium: false, RegistryValueKind.String);
		Add("Disable Full Window Drag", "Disables full window dragging for less GPU load.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "DragFullWindows", "0", "1", admin: false, 299, premium: false, RegistryValueKind.String);
		Add("Disable Window Shadow", "Disables window drop shadows for performance.", "hardware", "Performance", RegistryHive.CurrentUser, "Control Panel\\Desktop", "EnableWindowShadow", 0, 1, admin: false, 300);
		Log.Information("Initialized {Count} optimizations", _allItems.Count);
		UpdateCounts();
		void Add(string name, string desc, string cat, string group, RegistryHive hive, string path, string val, object desired, object restore, bool admin, int order, bool premium = false, RegistryValueKind kind = RegistryValueKind.DWord)
		{
			PrivacyOptimization opt = new PrivacyOptimization(name, desc, group, hive, path, val, desired, restore, admin, kind);
			_manager.Register(opt, order);
			_allItems.Add(new OptimizationItem(name, desc, group, _manager, opt, premium, cat));
		}
	}

	private void UpdateCounts()
	{
		CatAllCount.Text = _allItems.Count.ToString();
		CatGamingCount.Text = _allItems.Count((OptimizationItem i) => i.TweakCategory == "gaming").ToString();
		CatHardwareCount.Text = _allItems.Count((OptimizationItem i) => i.TweakCategory == "hardware").ToString();
		CatPrivacyCount.Text = _allItems.Count((OptimizationItem i) => i.TweakCategory == "privacy").ToString();
		CatWindowsCount.Text = _allItems.Count((OptimizationItem i) => i.TweakCategory == "windows").ToString();
	}

	private void Category_Click(object sender, RoutedEventArgs e)
	{
		FilterByCategory(_currentCategory = ((RadioButton)sender).Tag?.ToString() ?? "all");
	}

	private void FilterByCategory(string category)
	{
		_currentCategory = category;
		_categoryCards.Clear();
		foreach (CategoryCardData allCard in _allCards)
		{
			if (category != "all" && allCard.CategoryName != category)
			{
				continue;
			}
			allCard.Items.Clear();
			foreach (OptimizationItem allItem in allCard.AllItems)
			{
				bool num = string.IsNullOrEmpty(_currentSearch) || allItem.Name.IndexOf(_currentSearch, StringComparison.OrdinalIgnoreCase) >= 0 || allItem.Description.IndexOf(_currentSearch, StringComparison.OrdinalIgnoreCase) >= 0;
				bool flag = _filterStatus == 0 || (_filterStatus == 1 && allItem.IsEnabled) || (_filterStatus == 2 && !allItem.IsEnabled);
				if (num && flag)
				{
					allCard.Items.Add(allItem);
				}
			}
			if (allCard.Items.Count != 0 || (string.IsNullOrEmpty(_currentSearch) && _filterStatus == 0))
			{
				_categoryCards.Add(allCard);
				RefreshCardBadge(allCard);
			}
		}
	}

	private void BuildCategoryCards()
	{
		IEnumerable<IGrouping<string, OptimizationItem>> enumerable = from i in _allItems
			group i by i.TweakCategory;
		_allCards = new List<CategoryCardData>();
		foreach (IGrouping<string, OptimizationItem> item in enumerable)
		{
			item.First();
			CategoryCardData categoryCardData = new CategoryCardData
			{
				CategoryName = item.Key,
				DisplayName = GetCategoryDisplayName(item.Key),
				Icon = GetCategoryIcon(item.Key),
				AccentBrush = GetCategoryBrush(item.Key),
				Items = new ObservableCollection<OptimizationItem>(item),
				AllItems = new ObservableCollection<OptimizationItem>(item)
			};
			RefreshCardBadge(categoryCardData);
			_allCards.Add(categoryCardData);
		}
		FilterByCategory(_currentCategory);
	}

	private void RefreshCardBadge(CategoryCardData card)
	{
		int count = card.Items.Count;
		int num = card.Items.Count((OptimizationItem i) => i.IsEnabled);
		card.BadgeText = $"{num}/{count}";
		card.ProgressWidth = ((count > 0) ? ((double)num / (double)count * 60.0) : 0.0);
	}

	private string GetCategoryDisplayName(string cat)
	{
		return cat switch
		{
			"gaming" => "Gaming", 
			"hardware" => "Hardware", 
			"privacy" => "Privacy", 
			"windows" => "Windows UI", 
			_ => cat, 
		};
	}

	private Geometry GetCategoryIcon(string cat)
	{
		return cat switch
		{
			"gaming" => (Application.Current.TryFindResource("GamepadIcon") as Geometry) ?? new EllipseGeometry(), 
			"hardware" => (Application.Current.TryFindResource("CpuIcon") as Geometry) ?? new EllipseGeometry(), 
			"privacy" => (Application.Current.TryFindResource("ShieldIcon") as Geometry) ?? new EllipseGeometry(), 
			"windows" => (Application.Current.TryFindResource("WindowIcon") as Geometry) ?? new EllipseGeometry(), 
			_ => new EllipseGeometry(), 
		};
	}

	private SolidColorBrush GetCategoryBrush(string cat)
	{
		return cat switch
		{
			"gaming" => new SolidColorBrush(Color.FromRgb(95, 159, 95)), 
			"hardware" => new SolidColorBrush(Color.FromRgb(95, 175, 207)), 
			"privacy" => new SolidColorBrush(Color.FromRgb(207, 143, 95)), 
			"windows" => new SolidColorBrush(Color.FromRgb(207, 143, 207)), 
			_ => new SolidColorBrush(Color.FromRgb(136, 136, 136)), 
		};
	}

	private void CatApply_Click(object sender, RoutedEventArgs e)
	{
		string cat = ((Button)sender).Tag?.ToString() ?? "";
		int num = 0;
		foreach (OptimizationItem item in _allItems.Where((OptimizationItem i) => i.TweakCategory == cat))
		{
			if ((!item.IsPremium || HasPremium) && item.Opt.Apply())
			{
				item.IsEnabled = true;
				item.SyncToggleToManager();
				num++;
			}
		}
		SaveToggleStates();
		RefreshAllBadges();
		ActStatusBar($"{num} {GetCategoryDisplayName(cat)} tweaks applied");
	}

	private void CatRestore_Click(object sender, RoutedEventArgs e)
	{
		string cat = ((Button)sender).Tag?.ToString() ?? "";
		int num = 0;
		foreach (OptimizationItem item in _allItems.Where((OptimizationItem i) => i.TweakCategory == cat))
		{
			if (item.Opt.Restore())
			{
				item.IsEnabled = false;
				item.SyncToggleToManager();
				num++;
			}
		}
		SaveToggleStates();
		RefreshAllBadges();
		ActStatusBar($"{num} {GetCategoryDisplayName(cat)} tweaks restored");
	}

	private void RefreshAllBadges()
	{
		foreach (CategoryCardData allCard in _allCards)
		{
			RefreshCardBadge(allCard);
		}
	}

	private void HideAllPages()
	{
		PageOptimizer.Visibility = Visibility.Collapsed;
		PageActivate.Visibility = Visibility.Collapsed;
		PageJunkCleaner.Visibility = Visibility.Collapsed;
		PageStartup.Visibility = Visibility.Collapsed;
		PageProcess.Visibility = Visibility.Collapsed;
		PageNetwork.Visibility = Visibility.Collapsed;
		PageHosts.Visibility = Visibility.Collapsed;
		PagePower.Visibility = Visibility.Collapsed;
		PageContext.Visibility = Visibility.Collapsed;
		PageDisk.Visibility = Visibility.Collapsed;
		PageBrowser.Visibility = Visibility.Collapsed;
		PageBoost.Visibility = Visibility.Collapsed;
	}

	private void Nav_Click(object sender, RoutedEventArgs e)
	{
		switch (((RadioButton)sender)?.Tag?.ToString())
		{
		case "startup":
			ShowStartupPage();
			break;
		case "process":
			ShowProcessPage();
			break;
		case "network":
			ShowNetworkPage();
			break;
		case "hosts":
			ShowHostsPage();
			break;
		case "power":
			ShowPowerPage();
			break;
		case "context":
			ShowContextPage();
			break;
		case "disk":
			ShowDiskPage();
			break;
		case "browser":
			ShowBrowserPage();
			break;
		case "junk":
			ShowJunkCleanerPage();
			break;
		case "boost":
			ShowBoostPage();
			break;
		case "activate":
			ShowActivatePage();
			break;
		default:
			ShowOptimizerPage();
			break;
		}
	}

	private void NavActivate_Click(object sender, RoutedEventArgs e)
	{
		ShowActivatePage();
	}

	private void NavBoost_Click(object sender, RoutedEventArgs e)
	{
		ShowBoostPage();
	}

	private void ActStatusBar(string msg)
	{
		StatusBarText.Text = $"[{DateTime.Now:HH:mm:ss}] {msg}";
	}

	private void ShowOptimizerPage()
	{
		HideAllPages();
		PageOptimizer.Visibility = Visibility.Visible;
	}

	private void ShowActivatePage()
	{
		HideAllPages();
		PageActivate.Visibility = Visibility.Visible;
		UpdateActivatePageDisplay();
	}

	private void ShowJunkCleanerPage()
	{
		HideAllPages();
		PageJunkCleaner.Visibility = Visibility.Visible;
	}

	private void ShowStartupPage()
	{
		HideAllPages();
		PageStartup.Visibility = Visibility.Visible;
		StartupRefresh_Click(null, null);
	}

	private void ShowProcessPage()
	{
		HideAllPages();
		PageProcess.Visibility = Visibility.Visible;
		ProcessRefresh_Click(null, null);
	}

	private void ShowNetworkPage()
	{
		HideAllPages();
		PageNetwork.Visibility = Visibility.Visible;
		NetworkResult.Text = "Ready";
	}

	private void ShowHostsPage()
	{
		HideAllPages();
		PageHosts.Visibility = Visibility.Visible;
		try
		{
			HostsEditor.Text = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers\\etc\\hosts"));
		}
		catch
		{
			HostsEditor.Text = "/* Could not read hosts file */";
		}
		HostsStatus.Text = "Loaded";
	}

	private void ShowPowerPage()
	{
		HideAllPages();
		PagePower.Visibility = Visibility.Visible;
		RefreshPowerPlans();
	}

	private void ShowContextPage()
	{
		HideAllPages();
		PageContext.Visibility = Visibility.Visible;
		ContextRefresh_Click(null, null);
	}

	private void ShowDiskPage()
	{
		HideAllPages();
		PageDisk.Visibility = Visibility.Visible;
		DiskRefresh_Click(null, null);
	}

	private void ShowBrowserPage()
	{
		HideAllPages();
		PageBrowser.Visibility = Visibility.Visible;
		BrowserResult.Text = "Select a browser to clean above";
	}

	private void ShowBoostPage()
	{
		HideAllPages();
		PageBoost.Visibility = Visibility.Visible;
	}

	private void ApplyLicenseState()
	{
		bool hasPremium = HasPremium;
		foreach (OptimizationItem allItem in _allItems)
		{
			allItem.RefreshLockState(hasPremium);
		}
		UpdateLicenseBadge();
		UpdateActivatePageDisplay();
	}

	private void UpdateLicenseBadge()
	{
		if (_license != null && _license.IsValid)
		{
			TextBlock licenseBadge = LicenseBadge;
			string text = ((!IsTrial) ? (_license.Tier switch
			{
				LicenseTier.Basic => "BASIC", 
				LicenseTier.Pro => "PRO", 
				LicenseTier.Elite => "ELITE", 
				_ => "ACTIVE", 
			}) : "TRIAL");
			licenseBadge.Text = text;
		}
		else
		{
			LicenseBadge.Text = "COMMUNITY";
		}
		TextBlock licenseBadge2 = LicenseBadge;
		LicenseInfo license = _license;
		licenseBadge2.Foreground = ((license != null && license.IsValid) ? new SolidColorBrush(Color.FromRgb(95, 159, 95)) : new SolidColorBrush(Color.FromRgb(136, 136, 136)));
	}

	private void UpdateActivatePageDisplay()
	{
		if (_license != null && _license.IsValid)
		{
			LicenseStatusValue.Text = (IsTrial ? "Trial Active" : "Active");
			LicenseStatusValue.Foreground = new SolidColorBrush(Color.FromRgb(95, 159, 95));
			LicenseTierValue.Text = _license.DisplayTier + (IsTrial ? " (Trial)" : "");
			LicenseExpiryValue.Text = _license.ExpiresUtc.ToString("yyyy-MM-dd") + $" ({_license.DaysLeft} days)";
			ActivateSubtitle.Text = (IsTrial ? $"Trial active - {_license.DaysLeft} days remaining. Enter a key to unlock permanently." : "Your license is active. Premium features unlocked.");
			ActivateKeyBox.IsEnabled = true;
			ActivateBtn.IsEnabled = ActivateKeyBox.Text.Trim().Length >= 10;
			ActivateBtn.Content = "⚡ ACTIVATE";
			TrialBanner.Visibility = ((!IsTrial) ? Visibility.Collapsed : Visibility.Visible);
			TrialBannerTitle.Text = (IsTrial ? $"3-Day Free Trial — {_license.DaysLeft} day(s) left" : "");
			TrialBannerSub.Text = (IsTrial ? "Premium features unlocked for 3 days. Enter a key to keep access." : "");
		}
		else if (TrialExpired)
		{
			LicenseStatusValue.Text = "Trial Expired";
			LicenseStatusValue.Foreground = new SolidColorBrush(Color.FromRgb(229, 85, 85));
			LicenseTierValue.Text = "-";
			LicenseExpiryValue.Text = "-";
			ActivateSubtitle.Text = "Your 3-day trial has ended. Enter a license key to unlock premium features.";
			ActivateKeyBox.IsEnabled = true;
			ActivateBtn.IsEnabled = ActivateKeyBox.Text.Trim().Length >= 10;
			ActivateBtn.Content = "⚡ ACTIVATE";
			TrialBanner.Visibility = Visibility.Collapsed;
		}
		else
		{
			LicenseStatusValue.Text = "Community Edition";
			LicenseStatusValue.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
			LicenseTierValue.Text = "-";
			LicenseExpiryValue.Text = "-";
			ActivateSubtitle.Text = "Enter your license key to unlock premium features";
			ActivateKeyBox.IsEnabled = true;
			ActivateBtn.IsEnabled = ActivateKeyBox.Text.Trim().Length >= 10;
			ActivateBtn.Content = "⚡ ACTIVATE";
			TrialBanner.Visibility = Visibility.Collapsed;
		}
	}

	private void ActivateKeyBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		ActivateBtn.IsEnabled = ActivateKeyBox.Text.Trim().Length >= 10;
		ActivateStatus.Text = "";
	}

	private void ActivateBtn_Click(object sender, RoutedEventArgs e)
	{
		string text = ActivateKeyBox.Text.Trim().ToUpperInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		ActivateStatus.Text = "Validating key...";
		ActivateStatus.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 136));
		ActivateBtn.IsEnabled = false;
		if (!LicenseKeyCodec.TryValidate(text, out LicenseInfo info, out string message))
		{
			ActivateStatus.Text = message;
			ActivateStatus.Foreground = new SolidColorBrush(Color.FromRgb(229, 85, 85));
			ActivateBtn.IsEnabled = true;
			Log.Warning("Activation failed: {Msg}", message);
			return;
		}
		var (flag, text2) = LicenseKeyCodec.ActivateKey(text, info.Tier, info.ExpiresUtc, info.KeyId);
		if (!flag)
		{
			ActivateStatus.Text = text2;
			ActivateStatus.Foreground = new SolidColorBrush(Color.FromRgb(229, 85, 85));
			ActivateBtn.IsEnabled = true;
			Log.Warning("Activation rejected: {Msg}", text2);
			return;
		}
		SaveLicense(text);
		_license = info;
		_savedKey = text;
		ActivateStatus.Text = "License activated successfully! Premium features unlocked.";
		ActivateStatus.Foreground = new SolidColorBrush(Color.FromRgb(95, 159, 95));
		ActivateBtn.Content = "ACTIVATED";
		ActivateKeyBox.IsEnabled = false;
		Log.Information("License activated: {Tier}, {Days}d remaining", info.Tier, info.DaysLeft);
		ActStatusBar($"License activated - {info.DisplayTier}, {info.DaysLeft} days remaining");
		ApplyLicenseState();
	}

	private void ApplyAll_Click(object sender, RoutedEventArgs e)
	{
		int num = 0;
		int num2 = 0;
		foreach (OptimizationItem allItem in _allItems)
		{
			if (allItem.IsPremium && !HasPremium)
			{
				num++;
				continue;
			}
			allItem.IsEnabled = true;
			allItem.SyncToggleToManager();
			if (allItem.Opt.Apply())
			{
				num2++;
			}
		}
		SaveToggleStates();
		RefreshAllBadges();
		ActStatusBar((num > 0) ? $"{num2} applied ({num} premium locked)" : $"All {num2} applied");
	}

	private void ApplySelected_Click(object sender, RoutedEventArgs e)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (OptimizationItem allItem in _allItems)
		{
			allItem.SyncToggleToManager();
			if (allItem.IsEnabled)
			{
				if (allItem.IsPremium && !HasPremium)
				{
					num3++;
				}
				else if (allItem.Opt.Apply())
				{
					num++;
				}
			}
			else if (allItem.Opt.Restore())
			{
				num2++;
			}
		}
		SaveToggleStates();
		RefreshAllBadges();
		string text = $"{num} applied";
		if (num2 > 0)
		{
			text += $", {num2} restored";
		}
		if (num3 > 0)
		{
			text += $" ({num3} premium locked)";
		}
		ActStatusBar(text);
	}

	private void RestoreAll_Click(object sender, RoutedEventArgs e)
	{
		int num = 0;
		foreach (OptimizationItem allItem in _allItems)
		{
			if (allItem.Opt.Restore())
			{
				num++;
			}
			allItem.IsEnabled = false;
			allItem.SyncToggleToManager();
		}
		SaveToggleStates();
		RefreshAllBadges();
		ActStatusBar($"All {num} tweaks restored to defaults");
	}

	private void RestoreSelected_Click(object sender, RoutedEventArgs e)
	{
		foreach (OptimizationItem allItem in _allItems)
		{
			allItem.SyncToggleToManager();
		}
		ActStatusBar($"{_manager.RestoreEnabled()} restored");
	}

	private void GamingBoost_Click(object sender, RoutedEventArgs e)
	{
		BoostCategory("gaming", "Gaming");
	}

	private void HardwareBoost_Click(object sender, RoutedEventArgs e)
	{
		BoostCategory("hardware", "Hardware");
	}

	private void PrivacyBoost_Click(object sender, RoutedEventArgs e)
	{
		BoostCategory("privacy", "Privacy");
	}

	private void WindowsBoost_Click(object sender, RoutedEventArgs e)
	{
		BoostCategory("windows", "Windows");
	}

	private void BoostCategory(string cat, string label)
	{
		string cat2 = cat;
		int num = 0;
		int num2 = 0;
		foreach (OptimizationItem item in _allItems.Where((OptimizationItem i) => i.TweakCategory == cat2))
		{
			if (item.IsPremium && !HasPremium)
			{
				num2++;
			}
			else if (item.Opt.Apply())
			{
				item.IsEnabled = true;
				item.SyncToggleToManager();
				num++;
			}
		}
		SaveToggleStates();
		RefreshAllBadges();
		ActStatusBar((num2 > 0) ? $"{label} Boost: {num} applied ({num2} premium locked)" : $"{label} Boost: {num} tweaks applied");
	}

	private async void CleanJunk_Click(object sender, RoutedEventArgs e)
	{
		Button btn = (Button)sender;
		btn.IsEnabled = false;
		ActStatusBar("Cleaning junk files...");
		(int, long) tuple = await Task.Run(() => JunkCleaner.CleanAll());
		string text = $"Cleaned {tuple.Item1} files ({tuple.Item2 / 1024 / 1024} MB)";
		ActStatusBar(text);
		JunkLastResult.Text = text;
		JunkResultBorder.Visibility = Visibility.Visible;
		btn.IsEnabled = true;
	}

	private void CheckUpdate_Click(object sender, RoutedEventArgs e)
	{
		Updater.CheckForUpdate(silentIfCurrent: false);
	}

	private void OpenDiscord_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo("https://discord.gg/d3zSkZesv4")
			{
				UseShellExecute = true
			});
		}
		catch
		{
			ActStatusBar("Could not open Discord link");
		}
	}

	private void Minimize_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = WindowState.Minimized;
	}

	private void Maximize_Click(object sender, RoutedEventArgs e)
	{
		base.WindowState = ((base.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void NavCategory_Click(object sender, RoutedEventArgs e)
	{
		string category = ((RadioButton)sender).Tag?.ToString() ?? "gaming";
		NavOptimize.IsChecked = true;
		FilterByCategory(category);
	}

	private void FilterBtn_Click(object sender, RoutedEventArgs e)
	{
		_filterStatus = (_filterStatus + 1) % 3;
		string[] array = new string[3] { "All", "Enabled", "Disabled" };
		ActStatusBar("Filter: " + array[_filterStatus]);
		FilterByCategory(_currentCategory);
		ToolTipService.SetToolTip((DependencyObject)(object)FilterBtn, "Filter: " + array[_filterStatus]);
	}

	private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		_currentSearch = SearchBox.Text;
		SearchPlaceholder.Visibility = ((!string.IsNullOrEmpty(_currentSearch)) ? Visibility.Collapsed : Visibility.Visible);
		SearchClearBtn.Visibility = (string.IsNullOrEmpty(_currentSearch) ? Visibility.Collapsed : Visibility.Visible);
		FilterByCategory(_currentCategory);
	}

	private void SearchClear_Click(object sender, RoutedEventArgs e)
	{
		SearchBox.Text = "";
		SearchClearBtn.Visibility = Visibility.Collapsed;
	}

	private void StartupRefresh_Click(object? sender, RoutedEventArgs? e)
	{
		try
		{
			List<object> list = new List<object>();
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
			if (registryKey != null)
			{
				string[] valueNames = registryKey.GetValueNames();
				foreach (string name in valueNames)
				{
					list.Add(new
					{
						Name = name,
						Value = registryKey.GetValue(name)?.ToString(),
						Scope = "Current User"
					});
				}
			}
			using RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
			if (registryKey2 != null)
			{
				string[] valueNames = registryKey2.GetValueNames();
				foreach (string name2 in valueNames)
				{
					list.Add(new
					{
						Name = name2,
						Value = registryKey2.GetValue(name2)?.ToString(),
						Scope = "All Users"
					});
				}
			}
			string text = string.Join("\n", list.Select((dynamic i) => $"• {(object?)i.Name}  ({(object?)i.Scope})  → {(object?)i.Value}"));
			StartupList.ItemsSource = null;
			TextBlock textBlock = new TextBlock
			{
				Text = (string.IsNullOrEmpty(text) ? "No startup entries found." : text),
				FontSize = 11.0,
				Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#888"),
				TextWrapping = TextWrapping.Wrap
			};
			StartupList.ItemsSource = new TextBlock[1] { textBlock };
			ActStatusBar($"Loaded {list.Count} startup entries");
		}
		catch (Exception ex)
		{
			ActStatusBar("Startup: " + ex.Message);
		}
	}

	private void ProcessRefresh_Click(object? sender, RoutedEventArgs? e)
	{
		try
		{
			var list = (from p in Process.GetProcesses()
				orderby p.ProcessName
				select new
				{
					Name = $"{p.ProcessName} (PID: {p.Id})",
					Proc = p
				}).ToList();
			ProcessList.ItemsSource = null;
			ProcessList.Items.Clear();
			foreach (var item in list)
			{
				ProcessList.Items.Add(item.Name);
			}
			ActStatusBar($"Loaded {list.Count} processes");
		}
		catch (Exception ex)
		{
			ActStatusBar("Process: " + ex.Message);
		}
	}

	private void ProcessKill_Click(object sender, RoutedEventArgs e)
	{
		if (!(ProcessList.SelectedItem is string input))
		{
			ActStatusBar("Select a process first");
			return;
		}
		try
		{
			Match match = Regex.Match(input, "\\(PID: (\\d+)\\)");
			if (match.Success)
			{
				int num = int.Parse(match.Groups[1].Value);
				Process.GetProcessById(num).Kill();
				ActStatusBar($"Killed process {num}");
				ProcessRefresh_Click(null, null);
			}
		}
		catch (Exception ex)
		{
			ActStatusBar("Could not kill: " + ex.Message);
		}
	}

	private void NetworkFlush_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			RunCmd("ipconfig /flushdns");
			NetworkResult.Text = "DNS cache flushed.";
		}
		catch (Exception ex)
		{
			NetworkResult.Text = "Error: " + ex.Message;
		}
	}

	private void NetworkReset_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			RunCmd("netsh winsock reset");
			NetworkResult.Text = "Winsock reset. Reboot may be required.";
		}
		catch (Exception ex)
		{
			NetworkResult.Text = "Error: " + ex.Message;
		}
	}

	private void NetworkRelease_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			RunCmd("ipconfig /release");
			RunCmd("ipconfig /renew");
			NetworkResult.Text = "IP released and renewed.";
		}
		catch (Exception ex)
		{
			NetworkResult.Text = "Error: " + ex.Message;
		}
	}

	private void RunCmd(string args)
	{
		using Process process = Process.Start(new ProcessStartInfo("cmd", "/c " + args)
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			Verb = "runas"
		});
		process?.WaitForExit(10000);
	}

	private void HostsSave_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers\\etc\\hosts"), HostsEditor.Text);
			HostsStatus.Text = "Saved successfully.";
			ActStatusBar("Hosts file saved");
		}
		catch (Exception ex)
		{
			HostsStatus.Text = "Error: " + ex.Message;
		}
	}

	private void RefreshPowerPlans()
	{
		try
		{
			using Process process = Process.Start(new ProcessStartInfo("powercfg", "/list")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true
			});
			string obj = process?.StandardOutput.ReadToEnd() ?? "";
			process?.WaitForExit(3000);
			List<string> list = (from l in obj.Split('\n', StringSplitOptions.RemoveEmptyEntries)
				where l.Contains("(")
				select l.Trim()).ToList();
			TextBlock textBlock = new TextBlock
			{
				Text = string.Join("\n", list) + "\n\nUse the buttons below to create additional plans.",
				FontSize = 11.0,
				Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#888"),
				TextWrapping = TextWrapping.Wrap
			};
			PowerPlanList.ItemsSource = null;
			PowerPlanList.ItemsSource = new TextBlock[1] { textBlock };
			PowerStatus.Text = $"Found {list.Count} power plan(s)";
		}
		catch (Exception ex)
		{
			PowerStatus.Text = "Error: " + ex.Message;
		}
	}

	private void PowerHighPerf_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			RunCmd("powercfg /duplicatescheme 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
			PowerStatus.Text = "High Performance plan created.";
			RefreshPowerPlans();
		}
		catch (Exception ex)
		{
			PowerStatus.Text = "Error: " + ex.Message;
		}
	}

	private void PowerUltimate_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			RunCmd("powercfg /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
			PowerStatus.Text = "Ultimate Performance plan created.";
			RefreshPowerPlans();
		}
		catch (Exception ex)
		{
			PowerStatus.Text = "Error: " + ex.Message;
		}
	}

	private void ContextRefresh_Click(object? sender, RoutedEventArgs? e)
	{
		try
		{
			List<string> list = new List<string>();
			using RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey("*\\shell");
			if (registryKey != null)
			{
				string[] subKeyNames = registryKey.GetSubKeyNames();
				foreach (string text in subKeyNames)
				{
					list.Add("• File: " + text);
				}
			}
			using RegistryKey registryKey2 = Registry.ClassesRoot.OpenSubKey("Directory\\shell");
			if (registryKey2 != null)
			{
				string[] subKeyNames = registryKey2.GetSubKeyNames();
				foreach (string text2 in subKeyNames)
				{
					list.Add("• Directory: " + text2);
				}
			}
			using RegistryKey registryKey3 = Registry.ClassesRoot.OpenSubKey("Directory\\Background\\shell");
			if (registryKey3 != null)
			{
				string[] subKeyNames = registryKey3.GetSubKeyNames();
				foreach (string text3 in subKeyNames)
				{
					list.Add("• Background: " + text3);
				}
			}
			TextBlock textBlock = new TextBlock
			{
				Text = ((list.Count > 0) ? string.Join("\n", list) : "No context menu shell extensions found."),
				FontSize = 11.0,
				Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#888"),
				TextWrapping = TextWrapping.Wrap
			};
			ContextMenuList.ItemsSource = null;
			ContextMenuList.ItemsSource = new TextBlock[1] { textBlock };
			ActStatusBar($"Found {list.Count} context menu entries");
		}
		catch (Exception ex)
		{
			ActStatusBar("Context: " + ex.Message);
		}
	}

	private void DiskRefresh_Click(object? sender, RoutedEventArgs? e)
	{
		try
		{
			List<DriveInfo> list = (from d in DriveInfo.GetDrives()
				where d.IsReady
				select d).ToList();
			List<string> values = list.Select(delegate(DriveInfo d)
			{
				long num = d.TotalSize / 1073741824;
				long num2 = d.AvailableFreeSpace / 1073741824;
				int value = (int)((num > 0) ? ((num - num2) * 100 / num) : 0);
				return $"{d.Name}  {num} GB total  {num2} GB free  ({value}% used)";
			}).ToList();
			TextBlock textBlock = new TextBlock
			{
				Text = string.Join("\n", values),
				FontSize = 11.0,
				Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#888"),
				TextWrapping = TextWrapping.Wrap
			};
			DiskDriveList.ItemsSource = null;
			DiskDriveList.ItemsSource = new TextBlock[1] { textBlock };
			ActStatusBar($"Found {list.Count} drive(s)");
		}
		catch (Exception ex)
		{
			ActStatusBar("Disk: " + ex.Message);
		}
	}

	private void BrowserChrome_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			(int, long, string) tuple = BrowserCleaner.CleanChrome();
			BrowserResult.Text = $"Chrome: cleaned {tuple.Item1} files ({tuple.Item2 / 1024} KB)";
			ActStatusBar($"Chrome cache: {tuple.Item1} files removed");
		}
		catch (Exception ex)
		{
			BrowserResult.Text = "Error: " + ex.Message;
		}
	}

	private void BrowserFirefox_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			(int, long, string) tuple = BrowserCleaner.CleanFirefox();
			BrowserResult.Text = $"Firefox: cleaned {tuple.Item1} files ({tuple.Item2 / 1024} KB)";
			ActStatusBar($"Firefox cache: {tuple.Item1} files removed");
		}
		catch (Exception ex)
		{
			BrowserResult.Text = "Error: " + ex.Message;
		}
	}

	private void BrowserEdge_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			(int, long, string) tuple = BrowserCleaner.CleanEdge();
			BrowserResult.Text = $"Edge: cleaned {tuple.Item1} files ({tuple.Item2 / 1024} KB)";
			ActStatusBar($"Edge cache: {tuple.Item1} files removed");
		}
		catch (Exception ex)
		{
			BrowserResult.Text = "Error: " + ex.Message;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.17.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/TurboVolt;V15.0.0.0;component/mainwindow.xaml", UriKind.Relative);
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
			LicenseBadge = (TextBlock)target;
			break;
		case 2:
			((Button)target).Click += Minimize_Click;
			break;
		case 3:
			((Button)target).Click += Maximize_Click;
			break;
		case 4:
			((Button)target).Click += Close_Click;
			break;
		case 5:
			NavOptimize = (RadioButton)target;
			NavOptimize.Click += Nav_Click;
			break;
		case 6:
			NavGaming = (RadioButton)target;
			NavGaming.Click += NavCategory_Click;
			break;
		case 7:
			NavHardware = (RadioButton)target;
			NavHardware.Click += NavCategory_Click;
			break;
		case 8:
			NavPrivacy = (RadioButton)target;
			NavPrivacy.Click += NavCategory_Click;
			break;
		case 9:
			NavWindows = (RadioButton)target;
			NavWindows.Click += NavCategory_Click;
			break;
		case 10:
			NavStartup = (RadioButton)target;
			NavStartup.Click += Nav_Click;
			break;
		case 11:
			NavProcess = (RadioButton)target;
			NavProcess.Click += Nav_Click;
			break;
		case 12:
			NavNetwork = (RadioButton)target;
			NavNetwork.Click += Nav_Click;
			break;
		case 13:
			NavHosts = (RadioButton)target;
			NavHosts.Click += Nav_Click;
			break;
		case 14:
			NavPower = (RadioButton)target;
			NavPower.Click += Nav_Click;
			break;
		case 15:
			NavContext = (RadioButton)target;
			NavContext.Click += Nav_Click;
			break;
		case 16:
			NavDisk = (RadioButton)target;
			NavDisk.Click += Nav_Click;
			break;
		case 17:
			NavBrowser = (RadioButton)target;
			NavBrowser.Click += Nav_Click;
			break;
		case 18:
			NavJunkCleaner = (RadioButton)target;
			NavJunkCleaner.Click += Nav_Click;
			break;
		case 19:
			NavActivate = (RadioButton)target;
			NavActivate.Click += NavActivate_Click;
			break;
		case 20:
			NavBoost = (RadioButton)target;
			NavBoost.Click += NavBoost_Click;
			break;
		case 21:
			PageContainer = (Grid)target;
			break;
		case 22:
			PageOptimizer = (Grid)target;
			break;
		case 23:
			OptSubtitle = (TextBlock)target;
			break;
		case 24:
			CatAll = (RadioButton)target;
			CatAll.Click += Category_Click;
			break;
		case 25:
			CatAllCount = (TextBlock)target;
			break;
		case 26:
			CatGaming = (RadioButton)target;
			CatGaming.Click += Category_Click;
			break;
		case 27:
			CatGamingCount = (TextBlock)target;
			break;
		case 28:
			CatHardware = (RadioButton)target;
			CatHardware.Click += Category_Click;
			break;
		case 29:
			CatHardwareCount = (TextBlock)target;
			break;
		case 30:
			CatPrivacy = (RadioButton)target;
			CatPrivacy.Click += Category_Click;
			break;
		case 31:
			CatPrivacyCount = (TextBlock)target;
			break;
		case 32:
			CatWindows = (RadioButton)target;
			CatWindows.Click += Category_Click;
			break;
		case 33:
			CatWindowsCount = (TextBlock)target;
			break;
		case 34:
			CategoryCardsControl = (ItemsControl)target;
			break;
		case 37:
			FilterBtn = (Button)target;
			FilterBtn.Click += FilterBtn_Click;
			break;
		case 38:
			SearchPlaceholder = (TextBlock)target;
			break;
		case 39:
			SearchBox = (TextBox)target;
			SearchBox.TextChanged += SearchBox_TextChanged;
			break;
		case 40:
			SearchClearBtn = (Button)target;
			SearchClearBtn.Click += SearchClear_Click;
			break;
		case 41:
			((Button)target).Click += ApplyAll_Click;
			break;
		case 42:
			((Button)target).Click += ApplySelected_Click;
			break;
		case 43:
			((Button)target).Click += RestoreAll_Click;
			break;
		case 44:
			PageActivate = (Grid)target;
			break;
		case 45:
			ActivateSubtitle = (TextBlock)target;
			break;
		case 46:
			ActivateKeyBox = (TextBox)target;
			ActivateKeyBox.TextChanged += ActivateKeyBox_TextChanged;
			break;
		case 47:
			ActivateStatus = (TextBlock)target;
			break;
		case 48:
			ActivateBtn = (Button)target;
			ActivateBtn.Click += ActivateBtn_Click;
			break;
		case 49:
			TrialBanner = (Border)target;
			break;
		case 50:
			TrialBannerTitle = (TextBlock)target;
			break;
		case 51:
			TrialBannerSub = (TextBlock)target;
			break;
		case 52:
			LicenseStatusValue = (TextBlock)target;
			break;
		case 53:
			LicenseTierValue = (TextBlock)target;
			break;
		case 54:
			LicenseExpiryValue = (TextBlock)target;
			break;
		case 55:
			PageJunkCleaner = (Grid)target;
			break;
		case 56:
			JunkResultBorder = (Border)target;
			break;
		case 57:
			JunkLastResult = (TextBlock)target;
			break;
		case 58:
			JunkCleanBtn = (Button)target;
			JunkCleanBtn.Click += CleanJunk_Click;
			break;
		case 59:
			PageStartup = (Grid)target;
			break;
		case 60:
			StartupRefreshBtn = (Button)target;
			StartupRefreshBtn.Click += StartupRefresh_Click;
			break;
		case 61:
			StartupList = (ItemsControl)target;
			break;
		case 62:
			PageProcess = (Grid)target;
			break;
		case 63:
			ProcessKillBtn = (Button)target;
			ProcessKillBtn.Click += ProcessKill_Click;
			break;
		case 64:
			ProcessRefreshBtn = (Button)target;
			ProcessRefreshBtn.Click += ProcessRefresh_Click;
			break;
		case 65:
			ProcessList = (ListBox)target;
			break;
		case 66:
			PageNetwork = (Grid)target;
			break;
		case 67:
			NetworkFlushBtn = (Button)target;
			NetworkFlushBtn.Click += NetworkFlush_Click;
			break;
		case 68:
			NetworkResetBtn = (Button)target;
			NetworkResetBtn.Click += NetworkReset_Click;
			break;
		case 69:
			NetworkReleaseBtn = (Button)target;
			NetworkReleaseBtn.Click += NetworkRelease_Click;
			break;
		case 70:
			NetworkResult = (TextBlock)target;
			break;
		case 71:
			PageHosts = (Grid)target;
			break;
		case 72:
			HostsSaveBtn = (Button)target;
			HostsSaveBtn.Click += HostsSave_Click;
			break;
		case 73:
			HostsEditor = (TextBox)target;
			break;
		case 74:
			HostsStatus = (TextBlock)target;
			break;
		case 75:
			PagePower = (Grid)target;
			break;
		case 76:
			PowerPlanList = (ItemsControl)target;
			break;
		case 77:
			PowerHighPerfBtn = (Button)target;
			PowerHighPerfBtn.Click += PowerHighPerf_Click;
			break;
		case 78:
			PowerUltimateBtn = (Button)target;
			PowerUltimateBtn.Click += PowerUltimate_Click;
			break;
		case 79:
			PowerStatus = (TextBlock)target;
			break;
		case 80:
			PageContext = (Grid)target;
			break;
		case 81:
			ContextMenuList = (ItemsControl)target;
			break;
		case 82:
			ContextRefreshBtn = (Button)target;
			ContextRefreshBtn.Click += ContextRefresh_Click;
			break;
		case 83:
			PageDisk = (Grid)target;
			break;
		case 84:
			DiskRefreshBtn = (Button)target;
			DiskRefreshBtn.Click += DiskRefresh_Click;
			break;
		case 85:
			DiskDriveList = (ItemsControl)target;
			break;
		case 86:
			PageBrowser = (Grid)target;
			break;
		case 87:
			BrowserChromeBtn = (Button)target;
			BrowserChromeBtn.Click += BrowserChrome_Click;
			break;
		case 88:
			BrowserFirefoxBtn = (Button)target;
			BrowserFirefoxBtn.Click += BrowserFirefox_Click;
			break;
		case 89:
			BrowserEdgeBtn = (Button)target;
			BrowserEdgeBtn.Click += BrowserEdge_Click;
			break;
		case 90:
			BrowserResult = (TextBlock)target;
			break;
		case 91:
			PageBoost = (Grid)target;
			break;
		case 92:
			BoostGamingBtn = (Button)target;
			BoostGamingBtn.Click += GamingBoost_Click;
			break;
		case 93:
			BoostHardwareBtn = (Button)target;
			BoostHardwareBtn.Click += HardwareBoost_Click;
			break;
		case 94:
			BoostPrivacyBtn = (Button)target;
			BoostPrivacyBtn.Click += PrivacyBoost_Click;
			break;
		case 95:
			BoostWindowsBtn = (Button)target;
			BoostWindowsBtn.Click += WindowsBoost_Click;
			break;
		case 96:
			StatusBarText = (TextBlock)target;
			break;
		case 97:
			((Button)target).Click += OpenDiscord_Click;
			break;
		case 98:
			StatusBarVersion = (TextBlock)target;
			break;
		case 99:
			((Button)target).Click += CheckUpdate_Click;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.17.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 35:
			((Button)target).Click += CatApply_Click;
			break;
		case 36:
			((Button)target).Click += CatRestore_Click;
			break;
		}
	}
}
