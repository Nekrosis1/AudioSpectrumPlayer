using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AudioSpectrumPlayer.Avalonia.Views;

public sealed class PlayPauseGlyphConverter : IValueConverter
{
	public static readonly PlayPauseGlyphConverter Instance = new();

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		bool isPlaying = value is bool b && b;
		return isPlaying ? "❚❚" : "▶";
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
