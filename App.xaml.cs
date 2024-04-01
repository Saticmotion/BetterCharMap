using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace BetterCharMap;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern IntPtr SetForegroundWindow(IntPtr hwnd);

	System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

	public App()
	{
		using var iconStream = GetResourceStream(new Uri("pack://application:,,,/BetterCharMap;component/Resources/dragon.ico")).Stream;
		notifyIcon.Icon = new System.Drawing.Icon(iconStream);
		notifyIcon.Visible = true;
		notifyIcon.Click += NotifyIconClick;
	}

	void NotifyIconClick(object sender, EventArgs e)
	{
		var contextMenu = new ContextMenu();
		var menuItemCloseApp = new MenuItem();
		menuItemCloseApp.Header = "Exit BetterCharMap";
		menuItemCloseApp.Click += CloseApplication;
		contextMenu.Items.Add(menuItemCloseApp);
		contextMenu.IsOpen = true;
			
		// Get context menu handle and bring it to the foreground
		if (PresentationSource.FromVisual(contextMenu) is HwndSource hwndSource)
		{
			_ = SetForegroundWindow(hwndSource.Handle);
		}
	}

	private void CloseApplication(object sender, RoutedEventArgs e)
	{
		Shutdown();
	}
}