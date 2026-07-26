using System;
using System.IO;
using Serilog;

namespace WpfApp2.Optimizations.OptimizationHelpers;

public static class BrowserCleaner
{
	public static (int files, long bytes, string report) CleanChrome()
	{
		long num = 0L;
		int num2 = 0;
		string[] array = new string[4];
		_003C_003Ey__InlineArray6<string> buffer = default(_003C_003Ey__InlineArray6<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 1) = "Google";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 2) = "Chrome";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 5) = "Cache";
		array[0] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray6<string>, string>(in buffer, 6));
		_003C_003Ey__InlineArray6<string> buffer2 = default(_003C_003Ey__InlineArray6<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 1) = "Google";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 2) = "Chrome";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 5) = "Code Cache";
		array[1] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray6<string>, string>(in buffer2, 6));
		_003C_003Ey__InlineArray7<string> buffer3 = default(_003C_003Ey__InlineArray7<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer3, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer3, 1) = "Google";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer3, 2) = "Chrome";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer3, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer3, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer3, 5) = "Service Worker";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer3, 6) = "CacheStorage";
		array[2] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray7<string>, string>(in buffer3, 7));
		_003C_003Ey__InlineArray6<string> buffer4 = default(_003C_003Ey__InlineArray6<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer4, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer4, 1) = "Google";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer4, 2) = "Chrome";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer4, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer4, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer4, 5) = "Media Cache";
		array[3] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray6<string>, string>(in buffer4, 6));
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(int, long) tuple = CleanDir(array2[i]);
			num2 += tuple.Item1;
			num += tuple.Item2;
		}
		Log.Information("Chrome cleaned: {Files} files, {Bytes} MB", num2, num / 1024 / 1024);
		return (files: num2, bytes: num, report: $"Chrome: {num2} files, {num / 1024 / 1024} MB");
	}

	public static (int files, long bytes, string report) CleanFirefox()
	{
		long num = 0L;
		int num2 = 0;
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mozilla", "Firefox", "Profiles");
		if (Directory.Exists(path))
		{
			string[] directories = Directory.GetDirectories(path);
			foreach (string path2 in directories)
			{
				(int, long) tuple = CleanDir(Path.Combine(path2, "cache2"));
				num2 += tuple.Item1;
				num += tuple.Item2;
				tuple = CleanDir(Path.Combine(path2, "thumbnails"));
				num2 += tuple.Item1;
				num += tuple.Item2;
				tuple = CleanDir(Path.Combine(path2, "offlinedata"));
				num2 += tuple.Item1;
				num += tuple.Item2;
			}
		}
		Log.Information("Firefox cleaned: {Files} files, {Bytes} MB", num2, num / 1024 / 1024);
		return (files: num2, bytes: num, report: $"Firefox: {num2} files, {num / 1024 / 1024} MB");
	}

	public static (int files, long bytes, string report) CleanEdge()
	{
		long num = 0L;
		int num2 = 0;
		string[] array = new string[4];
		_003C_003Ey__InlineArray6<string> buffer = default(_003C_003Ey__InlineArray6<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 1) = "Microsoft";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 2) = "Edge";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer, 5) = "Cache";
		array[0] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray6<string>, string>(in buffer, 6));
		_003C_003Ey__InlineArray6<string> buffer2 = default(_003C_003Ey__InlineArray6<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 1) = "Microsoft";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 2) = "Edge";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer2, 5) = "Code Cache";
		array[1] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray6<string>, string>(in buffer2, 6));
		_003C_003Ey__InlineArray6<string> buffer3 = default(_003C_003Ey__InlineArray6<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer3, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer3, 1) = "Microsoft";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer3, 2) = "Edge";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer3, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer3, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray6<string>, string>(ref buffer3, 5) = "Media Cache";
		array[2] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray6<string>, string>(in buffer3, 6));
		_003C_003Ey__InlineArray7<string> buffer4 = default(_003C_003Ey__InlineArray7<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer4, 0) = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer4, 1) = "Microsoft";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer4, 2) = "Edge";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer4, 3) = "User Data";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer4, 4) = "Default";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer4, 5) = "Service Worker";
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray7<string>, string>(ref buffer4, 6) = "CacheStorage";
		array[3] = Path.Combine(global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray7<string>, string>(in buffer4, 7));
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(int, long) tuple = CleanDir(array2[i]);
			num2 += tuple.Item1;
			num += tuple.Item2;
		}
		Log.Information("Edge cleaned: {Files} files, {Bytes} MB", num2, num / 1024 / 1024);
		return (files: num2, bytes: num, report: $"Edge: {num2} files, {num / 1024 / 1024} MB");
	}

	private static (int files, long bytes) CleanDir(string dir)
	{
		int num = 0;
		long num2 = 0L;
		if (!Directory.Exists(dir))
		{
			return (files: 0, bytes: 0L);
		}
		try
		{
			string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
			foreach (string text in files)
			{
				try
				{
					FileInfo fileInfo = new FileInfo(text);
					num2 += fileInfo.Length;
					num++;
					File.Delete(text);
				}
				catch
				{
				}
			}
			files = Directory.GetDirectories(dir);
			foreach (string path in files)
			{
				try
				{
					Directory.Delete(path, recursive: true);
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return (files: num, bytes: num2);
	}

	public static (int files, long bytes, string report) CleanAll()
	{
		(int files, long bytes, string report) tuple = CleanChrome();
		(int, long, string) tuple2 = CleanFirefox();
		(int, long, string) tuple3 = CleanEdge();
		int num = tuple.files + tuple2.Item1 + tuple3.Item1;
		long num2 = tuple.bytes + tuple2.Item2 + tuple3.Item2;
		return (files: num, bytes: num2, report: $"All browsers: {num} files, {num2 / 1024 / 1024} MB");
	}
}
