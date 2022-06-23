using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsPathsManipulation
{
	public class NativeMethods
	{
		internal struct WINDOWPLACEMENT
		{
			public int length;

			public int flags;

			public ShowWindowCommands showCmd;

			public Point ptMinPosition;

			public Point ptMaxPosition;

			public Rectangle rcNormalPosition;
		}

		internal struct TRACKMOUSEEVENT
		{
			public int cbSize;

			public int dwFlags;

			public IntPtr hwndTrack;

			public int dwHoverTime;

			public TRACKMOUSEEVENT(IntPtr hWnd)
			{
				this.cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT));
				this.hwndTrack = hWnd;
				this.dwHoverTime = SystemInformation.MouseHoverTime;
				this.dwFlags = 1;
			}
		}

		public enum ShowWindowCommands
		{
			Hide = 0,
			Normal = 1,
			Minimized = 2,
			Maximized = 3
		}

		internal struct PictDescBitmap
		{
			internal int cbSizeOfStruct;

			internal int pictureType;

			internal IntPtr hBitmap;

			internal IntPtr hPalette;

			internal int unused;

			internal static PictDescBitmap Default
			{
				get
				{
					PictDescBitmap result = default(PictDescBitmap);
					result.cbSizeOfStruct = 20;
					result.pictureType = 1;
					result.hBitmap = IntPtr.Zero;
					result.hPalette = IntPtr.Zero;
					return result;
				}
			}
		}

		internal struct BITMAPINFO
		{
			internal int biSize;

			internal int biWidth;

			internal int biHeight;

			internal short biPlanes;

			internal short biBitCount;

			internal int biCompression;

			internal int biSizeImage;

			internal int biXPelsPerMeter;

			internal int biYPelsPerMeter;

			internal int biClrUsed;

			internal int biClrImportant;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
			internal byte[] bmiColors;

			internal static BITMAPINFO Default
			{
				get
				{
					BITMAPINFO result = default(BITMAPINFO);
					result.biSize = 40;
					result.biPlanes = 1;
					return result;
				}
			}
		}

		public const int SW_SHOWNORMAL = 1;

		public const int SW_SHOWMINIMIZED = 2;

		public const int SW_SHOWMAXIMIZED = 3;

		public const int SW_RESTORE = 9;

		internal const int MiniDumpWithFullMemory = 2;

		private const int TME_HOVER = 1;

		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		internal static extern void CopyMemory(IntPtr Destination, IntPtr Source, uint Length);

		[DllImport("oleaut32.dll", CharSet = CharSet.Ansi)]
		internal static extern int OleCreatePictureIndirect(ref PictDescBitmap pictdesc, ref Guid iid, bool fOwn, [MarshalAs(UnmanagedType.Interface)] out object ppVoid);

		[DllImport("gdi32.dll", SetLastError = true)]
		internal static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

		[DllImport("Kernel32.Dll")]
		internal static extern uint GetOEMCP();

		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
		internal static extern int PathMatchSpec(string pszFile, string pszSpec);

		[DllImport("user32.dll")]
		internal static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		internal static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		internal static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		[DllImport("user32")]
		internal static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		internal static extern IntPtr GetParent(IntPtr hWnd);

		[DllImport("kernel32.dll")]
		private static extern uint GetCurrentThreadId();

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll")]
		private static extern uint GetCurrentProcessId();

		[DllImport("Dbghelp.dll")]
		internal static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeHandle hFile, int DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.U4)]
		internal static extern int GetLongPathName([MarshalAs(UnmanagedType.LPTStr)] string lpszShortPath, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszLongPath, [MarshalAs(UnmanagedType.U4)] int cchBuffer);

		internal static string DumpProcess()
		{
			string dumpFilePath = Path.GetTempFileName();
			Thread thread = new Thread((ThreadStart)delegate
			{
				FileStream fileStream = new FileStream(dumpFilePath, FileMode.Create);
				NativeMethods.MiniDumpWriteDump(NativeMethods.GetCurrentProcess(), NativeMethods.GetCurrentProcessId(), fileStream.SafeFileHandle, 2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				fileStream.Close();
			});
			thread.Start();
			thread.Join();
			return dumpFilePath;
		}
	}
}
