using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace TurboVolt;

public class CategoryCardData : INotifyPropertyChanged
{
	private string _badgeText = "0/0";

	private double _progressWidth;

	public string CategoryName { get; set; } = "";


	public string DisplayName { get; set; } = "";


	public Geometry? Icon { get; set; }

	public SolidColorBrush? AccentBrush { get; set; }

	public ObservableCollection<OptimizationItem> Items { get; set; } = new ObservableCollection<OptimizationItem>();


	public ObservableCollection<OptimizationItem> AllItems { get; set; } = new ObservableCollection<OptimizationItem>();


	public string BadgeText
	{
		get
		{
			return _badgeText;
		}
		set
		{
			_badgeText = value;
			OnPropertyChanged("BadgeText");
		}
	}

	public double ProgressWidth
	{
		get
		{
			return _progressWidth;
		}
		set
		{
			_progressWidth = value;
			OnPropertyChanged("ProgressWidth");
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? n = null)
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
	}
}
