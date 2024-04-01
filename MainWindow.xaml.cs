using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Forms;

namespace BetterCharMap;

public partial class MainWindow : Window
{
	private HwndSource _source;
	private const int HOTKEY_ID = 9000;

	public FilterableUnicodeChars filterableUnicodeChars { get; set; }
	private KeyboardListener listener = new KeyboardListener();

	private UniformGrid grid;

	private string filter = "";
	private int selectedIndex;
	private Color highlightColor = Color.FromRgb(100, 100, 100);

	public MainWindow()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		IsVisibleChanged += OnIsVisibleChanged;
		CharGridControl.ItemContainerGenerator.StatusChanged += ResetHighlightToFirst;
		CharGridControl.Loaded += OnCharGridLoaded;
	}

	private void OnIsVisibleChanged(object o, DependencyPropertyChangedEventArgs e)
	{
		if ((bool)e.NewValue)
		{
			GetCaretPos();
			//GetCaretPos2();
			listener.KeyDown += Filter;
			listener.KeyDown += OnKeyDown;
		}
		else
		{
			listener.KeyDown -= Filter;
			listener.KeyDown -= OnKeyDown;
		}
	}

	private void OnCharGridLoaded(object sender, EventArgs eventArgs)
	{
		grid = FindUniformGrid(CharGridControl);
	}

	private UniformGrid FindUniformGrid(DependencyObject parent)
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(parent, i);

			if (child is UniformGrid uniformGrid)
			{
				return uniformGrid;
			}

			UniformGrid foundGrid = FindUniformGrid(child);
			if (foundGrid != null)
			{
				return foundGrid;
			}
		}

		return null;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		filterableUnicodeChars = new FilterableUnicodeChars();
		DataContext = filterableUnicodeChars;
		HideWindow();
	}

	private bool Filter(object sender, RawKeyEventArgs args)
	{
		if (args.Key == Key.Back)
		{
			if (filter.Length == 0)
			{
				HideWindow();
			}
			else
			{
				filter = filter.Substring(0, filter.Length - 1);
			}
		}
		filter += args.character;
		filterableUnicodeChars.filter = filter;

		return true;
	}

	private void ResetHighlightToFirst(object sender, EventArgs eventArgs)
	{
		selectedIndex = 0;

		var contentPresenter = GetContentPresenterInItemsControl(CharGridControl, 0);
		if (contentPresenter == null) return;

		contentPresenter.Loaded += (_, _) =>
		{
			if (VisualTreeHelper.GetChildrenCount(contentPresenter) <= 0) return;

			var textBlock = VisualTreeHelper.GetChild(contentPresenter, 0) as TextBlock;
			textBlock.Background = new SolidColorBrush(highlightColor);
		};
	}

	private void HideWindow()
	{
		filter = "";
		//NOTE(Simon): Need to do this explicitly, as the binding is not updated. Presumably because we're hiding before the binding can happen
		filterableUnicodeChars.filter = "";
		Hide();
	}

	private void HighlightSelectedIndex()
	{
		var contentPresenter = GetContentPresenterInItemsControl(CharGridControl, selectedIndex);
		if (contentPresenter == null) return;

		if (VisualTreeHelper.GetChildrenCount(contentPresenter) <= 0) return;

		var textBlock = VisualTreeHelper.GetChild(contentPresenter, 0) as TextBlock;
		textBlock.Background = new SolidColorBrush(highlightColor);
	}

	private void UnHighlightSelectedIndex()
	{
		var contentPresenter = GetContentPresenterInItemsControl(CharGridControl, selectedIndex);
		if (contentPresenter == null) return;

		if (VisualTreeHelper.GetChildrenCount(contentPresenter) <= 0) return;

		var textBlock = VisualTreeHelper.GetChild(contentPresenter, 0) as TextBlock;
		textBlock.Background = null;
	}

	private ContentPresenter GetContentPresenterInItemsControl(ItemsControl control, int index)
	{
		if (control.Items.Count <= 0) return null;

		var childContainer = control.ItemContainerGenerator.ContainerFromIndex(index);

		if (childContainer == null || childContainer is not UIElement) return null;

		return (ContentPresenter)childContainer;
	}

	private bool OnKeyDown(object sender, RawKeyEventArgs e)
	{
		if (!IsVisible)
		{
			return true;
		}

		UnHighlightSelectedIndex();

		switch (e.Key)
		{
			case Key.Left:
				selectedIndex = Math.Max(selectedIndex - 1, 0);
				HighlightSelectedIndex();
				break;
			case Key.Right:
				selectedIndex = Math.Min(selectedIndex + 1, grid.Children.Count - 1);
				HighlightSelectedIndex();
				break;
			case Key.Up:
				selectedIndex = Math.Max(selectedIndex - grid.Columns, 0);
				HighlightSelectedIndex();
				break;
			case Key.Down:
				selectedIndex = Math.Min(selectedIndex + grid.Columns, grid.Children.Count - 1);
				HighlightSelectedIndex();
				break;
			case Key.Enter:
				//SendBackSpaces(filter.Length);
				SendUnicodeCharacter(filterableUnicodeChars.displayList[selectedIndex].codepoint);
				HighlightSelectedIndex();
				break;
			case Key.Escape:
				HideWindow();
				break;
		}
		return false;
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);

		var helper = new WindowInteropHelper(this);
		_source = HwndSource.FromHwnd(helper.Handle);
		_source.AddHook(HwndHook);
		RegisterHotKey();
	}

	private void RegisterHotKey()
	{
		var helper = new WindowInteropHelper(this);
		const uint VK_F10 = 0x79;
		const uint MOD_CTRL = 0x0002;

		if (!Win32.RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CTRL, VK_F10))
		{
			// handle error
		}
	}

	private void UnregisterHotKey()
	{
		var helper = new WindowInteropHelper(this);
		Win32.UnregisterHotKey(helper.Handle, HOTKEY_ID);
	}

	private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
	{
		const int WM_HOTKEY = 0x0312;

		switch (msg)
		{
			case WM_HOTKEY:
				switch (wParam.ToInt32())
				{
					case HOTKEY_ID:
						OnHotKeyPressed();
						handled = true;
						break;
				}
				break;
		}

		return IntPtr.Zero;
	}

	private void OnHotKeyPressed()
	{
		Show();
	}

	static void SendUnicodeCharacter(int character)
	{
		Win32.Input[] inputs;

		if (character < 0x10000)
		{
			inputs = new Win32.Input[1];
			inputs[0] = CreateUnicodeInput(character);
		}
		else
		{
			inputs = new Win32.Input[2];
			int highSurrogate = 0xD800 + ((character - 0x10000) >> 10);
			int lowSurrogate = 0xDC00 + ((character - 0x10000) & 0x3FF);
			inputs[0] = CreateUnicodeInput(highSurrogate);
			inputs[1] = CreateUnicodeInput(lowSurrogate);
		}

		Win32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Win32.Input)));
	}

	static Win32.Input CreateUnicodeInput(int character)
	{
		return new Win32.Input
		{
			Type = Win32.InputType.InputKeyboard,
			Data = new Win32.InputUnion
			{
				Keyboard = new Win32.KeyboardInput
				{
					Vk = 0,
					Scan = (ushort)character,
					Flags = Win32.KeyEventF.Unicode,
					Time = 0,
					DwExtraInfo = IntPtr.Zero
				}
			}
		};
	}


	[DllImport("user32.dll", SetLastError = true)]
	static extern int SendMessage(IntPtr hWnd, UInt32 Msg, UIntPtr wParam, IntPtr lParam);

	public const int WM_IME_REQUEST = 0x0288;
	public int IMR_QUERYCHARPOSITION = 0x0006;

	void GetCaretPos()
	{
		return;

		//NOTE(Simon): Although the code below seems correct to me, it doesn't work.

		var hwnd = Win32.GetForegroundWindow();
		var charPos = new Win32.ImeCharPosition();
		charPos.size = Marshal.SizeOf(charPos);
		var charPosPtr = Marshal.AllocHGlobal(Marshal.SizeOf(charPos));

		try
		{
			Marshal.StructureToPtr(charPos, charPosPtr, false);

			var success = SendMessage(hwnd, WM_IME_REQUEST, (UIntPtr)IMR_QUERYCHARPOSITION, charPosPtr);

			Console.WriteLine(Marshal.GetLastWin32Error());

			if (success == 0) return;

			var charPosition = Marshal.PtrToStructure<Win32.ImeCharPosition>(charPosPtr);
		}
		finally
		{
			Marshal.FreeHGlobal(charPosPtr);
		}
	}
}