using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static BetterCharMap.InterceptKeys;

//NOTE(Simon): From https://stackoverflow.com/revisions/1639331/15
namespace BetterCharMap;

public class KeyboardListener : IDisposable
{
	public event RawKeyEventHandler KeyDown;
	public event RawKeyEventHandler KeyUp;
	private IntPtr hookId = IntPtr.Zero;
	private delegate bool KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode);
	private KeyboardCallbackAsync hookedKeyboardCallbackAsync;
	private InterceptKeys.LowLevelKeyboardProc hookedLowLevelKeyboardProc;

	public KeyboardListener()
	{
		//NOTE(Simon): We have to store the HookCallback, so that it is not garbage collected runtime
		hookedLowLevelKeyboardProc = (InterceptKeys.LowLevelKeyboardProc)LowLevelKeyboardProc;
		hookId = InterceptKeys.SetHook(hookedLowLevelKeyboardProc);
		hookedKeyboardCallbackAsync = KeyboardListener_KeyboardCallbackAsync;
	}

	~KeyboardListener()
	{
		Dispose();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam)
	{
		var kbStruct = Marshal.PtrToStructure<KeyboardHookStruct>(lParam);

		bool continueProcessing = true;
		if (nCode >= 0 && (kbStruct.flags & 0x00000010) == 0)
		{
			if (wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_KEYDOWN 
				|| wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_KEYUP 
				|| wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_SYSKEYDOWN 
				|| wParam.ToUInt32() == (int)InterceptKeys.KeyEvent.WM_SYSKEYUP)
			{
				continueProcessing = hookedKeyboardCallbackAsync.Invoke((InterceptKeys.KeyEvent)wParam.ToUInt32(), Marshal.ReadInt32(lParam));
			}
		}

		return continueProcessing ? InterceptKeys.CallNextHookEx(hookId, nCode, wParam, lParam) : (IntPtr)1;
	}

	bool KeyboardListener_KeyboardCallbackAsync(InterceptKeys.KeyEvent keyEvent, int vkCode)
	{
		return keyEvent switch
		{
			InterceptKeys.KeyEvent.WM_KEYDOWN when KeyDown != null => KeyDown.Invoke(this, new RawKeyEventArgs(vkCode, false)),
			InterceptKeys.KeyEvent.WM_SYSKEYDOWN when KeyDown != null => KeyDown.Invoke(this, new RawKeyEventArgs(vkCode, true)),
			InterceptKeys.KeyEvent.WM_KEYUP when KeyUp != null => KeyUp.Invoke(this, new RawKeyEventArgs(vkCode, false)),
			InterceptKeys.KeyEvent.WM_SYSKEYUP when KeyUp != null => KeyUp.Invoke(this, new RawKeyEventArgs(vkCode, true)),
			_ => true
		};
	}

	public void Dispose()
	{
		InterceptKeys.UnhookWindowsHookEx(hookId);
	}
}

public class RawKeyEventArgs : EventArgs
{
	public int VKCode;
	public Key Key;
	public bool IsSysKey;
	public string character;

	public RawKeyEventArgs(int VKCode, bool isSysKey)
	{
		this.VKCode = VKCode;
		this.IsSysKey = isSysKey;
		this.Key = KeyInterop.KeyFromVirtualKey(VKCode);
		string keyString = Key.ToString();
		if (keyString.Length == 1)
		{
			character = keyString;
		}
		else if (Key == Key.Space)
		{
			character = " ";
		}
		else
		{
			character = "";
		}
	}
}

public delegate bool RawKeyEventHandler(object sender, RawKeyEventArgs args);

internal static class InterceptKeys
{
	public delegate IntPtr LowLevelKeyboardProc(int nCode, UIntPtr wParam, IntPtr lParam);
	public static int WH_KEYBOARD_LL = 13;

	public enum KeyEvent : int
	{
		WM_KEYDOWN = 256,
		WM_KEYUP = 257,
		WM_SYSKEYUP = 261,
		WM_SYSKEYDOWN = 260
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct KeyboardHookStruct
	{
		public int vkCode;
		public int scanCode;
		public int flags;
		public int time;
		public IntPtr dwExtraInfo;
	}

	public static IntPtr SetHook(LowLevelKeyboardProc proc)
	{
		using var curProcess = Process.GetCurrentProcess();
		using var curModule = curProcess.MainModule;

		return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnhookWindowsHookEx(IntPtr hhk);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, UIntPtr wParam, IntPtr lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr GetModuleHandle(string lpModuleName);
}