using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using WpfApp2.Optimizations.Customization.CustomizationSettings;
using WpfApp2.Optimizations.OptimizationHelpers;

namespace TurboVolt;

public class OptimizationItem : INotifyPropertyChanged
{
	private readonly OptimizationManager _manager;

	private readonly OptimizationHelper _opt;

	private readonly bool _isPremium;

	private readonly string _category;

	private bool _isEnabled;

	private bool _isUnlocked = true;

	public string Name => _opt.Name;

	public string Description => _opt.Description;

	public string Category => _opt.Category;

	public string TweakCategory => _category;

	public bool IsPremium => _isPremium;

	public OptimizationHelper Opt => _opt;

	public Geometry CategoryIcon => _category switch
	{
		"gaming" => (Application.Current.TryFindResource("GamepadIcon") as Geometry) ?? new EllipseGeometry(), 
		"hardware" => (Application.Current.TryFindResource("CpuIcon") as Geometry) ?? new EllipseGeometry(), 
		"privacy" => (Application.Current.TryFindResource("ShieldIcon") as Geometry) ?? new EllipseGeometry(), 
		"windows" => (Application.Current.TryFindResource("WindowIcon") as Geometry) ?? new EllipseGeometry(), 
		_ => new EllipseGeometry(), 
	};

	public SolidColorBrush CategoryColor => _category switch
	{
		"gaming" => new SolidColorBrush(Color.FromRgb(95, 159, 95)), 
		"hardware" => new SolidColorBrush(Color.FromRgb(95, 175, 207)), 
		"privacy" => new SolidColorBrush(Color.FromRgb(207, 143, 95)), 
		"windows" => new SolidColorBrush(Color.FromRgb(207, 143, 207)), 
		_ => new SolidColorBrush(Color.FromRgb(136, 136, 136)), 
	};

	public bool IsUnlocked
	{
		get
		{
			return _isUnlocked;
		}
		set
		{
			_isUnlocked = value;
			OnPropertyChanged("IsUnlocked");
			OnPropertyChanged("PremiumBadgeVisibility");
			OnPropertyChanged("LockedBadgeVisibility");
		}
	}

	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (_isEnabled != value && (_isUnlocked || !value))
			{
				_isEnabled = value;
				_manager.SetToggle(_opt.Name, value);
				OnPropertyChanged("IsEnabled");
			}
		}
	}

	public Visibility PremiumBadgeVisibility
	{
		get
		{
			if (!_isPremium || !_isUnlocked)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility LockedBadgeVisibility
	{
		get
		{
			if (!_isPremium || _isUnlocked)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public OptimizationItem(string name, string desc, string cat, OptimizationManager manager, OptimizationHelper opt, bool premium = false, string tweakCategory = "windows")
	{
		_manager = manager;
		_opt = opt;
		_isPremium = premium;
		_category = tweakCategory;
		_isUnlocked = !premium;
	}

	public void RefreshLockState(bool hasPremium)
	{
		IsUnlocked = !_isPremium || hasPremium;
	}

	public void SyncToggleToManager()
	{
		_manager.SetToggle(_opt.Name, _isEnabled);
	}

	public void SetEnabledDirect(bool value)
	{
		_isEnabled = value;
		OnPropertyChanged("IsEnabled");
	}

	protected void OnPropertyChanged([CallerMemberName] string? name = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
