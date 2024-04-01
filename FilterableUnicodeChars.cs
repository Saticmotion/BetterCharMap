using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BetterCharMap;

//TODO(Simon): Can this be struct? Or something even smarter, to make filtering faster.
public class UnicodeChar
{
	public string character { get; set; }
	public int codepoint;
	public string codepointString;
	public string description;
	public string alternativeDescription;
}

public class FilterableUnicodeChars : INotifyPropertyChanged
{
	public string filter
	{
		get => _filter;
		set
		{
			if (value == _filter)
			{
				return;
			}

			_filter = value;
			Filter();
			OnPropertyChanged(null);
		}
	}
	// ReSharper disable once UnusedMember.Global Used by XAML
	public int filteredCount => chars.Count;
	// ReSharper disable once UnusedMember.Global Used by XAML
	public List<UnicodeChar> displayList => chars.Take(250).ToList();
	public List<UnicodeChar> chars { get; private set; }

	private string _filter;
	private readonly List<UnicodeChar> allchars;

	public FilterableUnicodeChars()
	{
		chars = new List<UnicodeChar>();
		allchars = new List<UnicodeChar>();
		using var stream = new StreamReader(Application.GetResourceStream(new Uri("pack://application:,,,/BetterCharMap;component/Resources/UnicodeData.txt")).Stream);

		string line;
		while ((line = stream.ReadLine()) != null)
		{
			string[] split = line.Split(';');
			int codePoint = Int32.Parse(split[0], System.Globalization.NumberStyles.HexNumber);

			allchars.Add(new UnicodeChar
			{
				character = Char.ConvertFromUtf32(codePoint), 
				codepoint = codePoint, 
				codepointString = split[0],
				description = split[1], 
				alternativeDescription = split[2]
			});
		}
	}

	private void Filter()
	{
		chars ??= [];

		chars.Clear();

		//NOTE(Simon): If the filter string is a valid hex-encoded character number, also check the first column
		if (Int64.TryParse(filter, System.Globalization.NumberStyles.HexNumber, null, out _))
		{
			//chars.AddRange(allchars.Where(item => item.charValue.Contains(filter)));
		}

		chars.AddRange(allchars.Where(item => item.description.Contains(filter) || item.description.Contains(filter)));
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;
		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}