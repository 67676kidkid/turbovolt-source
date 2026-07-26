using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace WpfApp2.Optimizations.OptimizationHelpers;

public class KeyHoldTracker : IDisposable
{
	private readonly Dictionary<Key, long> _pressedKeys = new Dictionary<Key, long>();

	private readonly Stopwatch _stopwatch = new Stopwatch();

	private readonly DispatcherTimer _pollTimer = new DispatcherTimer();

	private static readonly Key[] AllKeys = Enum.GetValues<Key>();

	public string CurrentKey { get; private set; } = "";


	public long CurrentHoldMs { get; private set; }

	public event Action<string, long>? KeyDown;

	public event Action<string, long>? KeyHeld;

	public event Action<string, long>? KeyUp;

	public KeyHoldTracker()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		_pollTimer.Interval = TimeSpan.FromMilliseconds(16L, 0L);
		_pollTimer.Tick += delegate
		{
			PollKeys();
		};
		_stopwatch.Start();
	}

	public void Start()
	{
		_pollTimer.Start();
	}

	public void Stop()
	{
		_pollTimer.Stop();
	}

	private void PollKeys()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		Key[] allKeys = AllKeys;
		for (int i = 0; i < allKeys.Length; i++)
		{
			Key val = allKeys[i];
			if ((int)val == 0)
			{
				continue;
			}
			bool flag = (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(val)) & 0x8000) != 0;
			bool flag2 = _pressedKeys.ContainsKey(val);
			if (flag && !flag2)
			{
				_pressedKeys[val] = _stopwatch.ElapsedMilliseconds;
				CurrentKey = ((object)val).ToString();
				CurrentHoldMs = 0L;
				this.KeyDown?.Invoke(((object)val).ToString(), 0L);
			}
			else if (flag && flag2)
			{
				CurrentKey = ((object)val).ToString();
				CurrentHoldMs = _stopwatch.ElapsedMilliseconds - _pressedKeys[val];
				this.KeyHeld?.Invoke(((object)val).ToString(), CurrentHoldMs);
			}
			else if (!flag && flag2)
			{
				long num = _pressedKeys[val];
				long arg = _stopwatch.ElapsedMilliseconds - num;
				_pressedKeys.Remove(val);
				this.KeyUp?.Invoke(((object)val).ToString(), arg);
				if (CurrentKey == ((object)val).ToString())
				{
					CurrentKey = "";
					CurrentHoldMs = 0L;
				}
			}
		}
	}

	public void Dispose()
	{
		Stop();
		_pollTimer.Stop();
		_pressedKeys.Clear();
	}

	[DllImport("user32.dll")]
	private static extern short GetAsyncKeyState(int vKey);
}
