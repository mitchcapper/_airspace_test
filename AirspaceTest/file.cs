using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Drawing;
using Colors = System.Drawing.Color;
using System.Threading.Tasks;
using TransparentWinUIWindowLib;
//using UnitedSets.Classes;
using WinUIEx;
using WinWrapper;
using SysDiaProcess = System.Diagnostics.Process;//grrrrrrrrrrrrrr
using Window = WinWrapper.Window;
using WinUIPoint = Windows.Foundation.Point;
using Microsoft.UI.Xaml.Media;
using AirspaceTest;
using Windows.Win32;
using CWP_FLAGS = Windows.Win32.UI.WindowsAndMessaging.CWP_FLAGS;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Diagnostics;

namespace UnitedSets.NotWindows.Flyout.OutOfBoundsFlyout {
	static class OutOfBoundsFlyoutSystem {
		public static void Initialize(bool addBorder) {
			Instance = new(addBorder);
		}
		public static void Dispose() {
			Instance.Dispose();
		}
		static OutOfBoundsFlyoutHost Instance;
		static private Window appWindow;
		private class OutOfBoundsFlyoutHost : WindowEx, IDisposable {
			public readonly SwapChainPanel swapChainPanel;
			readonly TransparentWindowManager trans_mgr;
			public readonly Window Window;

			public OutOfBoundsFlyoutHost(bool addBorder) {
				swapChainPanel = new();
				trans_mgr = new(this, swapChainPanel, false);
				if (addBorder)
					swapChainPanel.Children.Add(new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, Margin = new(5), BorderThickness = new(2), BorderBrush = new SolidColorBrush(MainWindow.ConvertFromDrawColor(Colors.Red)) });
				//IsMinimizable = false;
				//IsMaximizable = false;
				appWindow = Window.FromWindowHandle(SysDiaProcess.GetCurrentProcess().MainWindowHandle);

				IsResizable = false;
				IsTitleBarVisible = false;
				WindowContent = swapChainPanel;
				Window = Window.FromWindowHandle(this.GetWindowHandle());
				//Window.SetTopMost();
				trans_mgr.AfterInitialize();
				Activate();
				this.Hide();

			}

			public void Dispose() {
				Instance.Close();
				trans_mgr.Cleanup();
			}
		}
		public static double widthScale;
		public static double heightScale;
		private enum CUR_OVER { Us, Caller, External }
		private static bool firstRun = true;
		private static Interop.UIAutomationClient.IUIAutomationElement ElementFromCursor() {
			// Convert mouse position from System.Drawing.Point to System.Windows.Point.
			var auto = new Interop.UIAutomationClient.CUIAutomation();
			//var desktop = auto.GetRootElement();
			var element = auto.ElementFromPoint(new Interop.UIAutomationClient.tagPOINT { x = Cursor.Position.X, y = Cursor.Position.Y });


			return element;
		}
		public const bool REAL_BOY_MODE = true;
		public static async Task ShowAsync(FlyoutBase Flyout, Point pt, FlyoutPlacementMode placementMode = FlyoutPlacementMode.Auto) {
			if (firstRun) {
				//appWindow = Window.FromWindowHandle(SysDiaProcess.GetCurrentProcess().MainWindowHandle);
			}

			firstRun = false;
			var instance = Instance;
			var window = instance.Window;
			var displayBounds = Display.FromPoint(pt).WorkingAreaBounds;
			window.Bounds = new() {
				X = displayBounds.X,
				Y = displayBounds.Y,
				Width = displayBounds.Width,
				Height = displayBounds.Height
			};
			instance.Activate();
			await Task.Delay(50);//critical for sizing to be right, could cache scale info per monitor and watch for dpi changes
			widthScale = window.ClientBounds.Width / instance.swapChainPanel.ActualWidth;
			heightScale = window.ClientBounds.Height / instance.swapChainPanel.ActualHeight;
			var loc = new WinUIPoint(pt.X - displayBounds.X, pt.Y - displayBounds.Y);
			var scaled = loc;
			scaled.X /= widthScale;
			scaled.Y /= heightScale;
			window.ToString();

			Log($"Showing at: {loc} (scaled: {scaled}) Win bounds: {window.Bounds} (client: {window.ClientBounds}) SwapC Size: {instance.swapChainPanel.ActualSize}  scale: {widthScale:0.000}x{heightScale:0.000}");

			Flyout.ShowAt(instance.swapChainPanel, new() {
				ShowMode = FlyoutShowMode.Standard,
				Placement = placementMode,
				Position = scaled
			});

			bool Opening = true;
			void closedEv(object? o, object e) => Opening = false;
			Flyout.Closed += closedEv;
			Point lastPos = new(-1, -1);
			var uiThread = PInvoke.GetCurrentThreadId();
			bool weVisible = true;
			var ourColor = Colors.LightGreen;
			var notUsColor = Colors.LightYellow;
			var appColor = Colors.LightPink;
			CUR_OVER over;
			var usHandle = window.Handle.Value;
			var callerHandle = appWindow.Handle.Value;
			long totalTicks = 0;
			var totalLoop = 0;
			while (Opening) {
				var startTick = Environment.TickCount;
				if (!REAL_BOY_MODE) {
					var pos = Cursor.Position;
					//window.SetExStyleFlag(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_TRANSPARENT, true);
					//window.SetExStyleFlag(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_TRANSPARENT, false);
					weVisible = true;
					var under = Window.FromLocation(pos);
					var underHdl = under.Root.Handle.Value;
					if (underHdl == usHandle)
						over = CUR_OVER.Us;
					else if (underHdl == callerHandle)
						over = CUR_OVER.Caller;
					else
						over = CUR_OVER.External;

					var flags = CWP_FLAGS.CWP_ALL;
					var flags4 = CWP_FLAGS.CWP_SKIPINVISIBLE | CWP_FLAGS.CWP_SKIPDISABLED | CWP_FLAGS.CWP_SKIPTRANSPARENT;

					var handlesToTry = new List<IntPtr>(new[] { usHandle, callerHandle });

					Windows.Win32.Foundation.BOOL ThreadEnumCallback(Windows.Win32.Foundation.HWND wind, Windows.Win32.Foundation.LPARAM vall) {
						handlesToTry.Add(wind.Value);
						return true;
					}
					//var it = new Windows.Win32.UI.WindowsAndMessaging.WNDENUMPROC;
					PInvoke.EnumThreadWindows(uiThread, ThreadEnumCallback, 0);
					handlesToTry = handlesToTry.Distinct().ToList();
					await Task.Delay(100);

					var res = new List<IntPtr>();
					var child = Window.FromWindowHandle(PInvoke.ChildWindowFromPointEx((Windows.Win32.Foundation.HWND)usHandle, pos, flags));
					foreach (var hdl in handlesToTry) {
						var child1 = PInvoke.ChildWindowFromPointEx((Windows.Win32.Foundation.HWND)hdl, pos, flags).Value;
						var child4 = PInvoke.ChildWindowFromPointEx((Windows.Win32.Foundation.HWND)hdl, pos, flags4).Value;
						var child2 = PInvoke.ChildWindowFromPoint((Windows.Win32.Foundation.HWND)hdl, pos).Value;
						var child3 = PInvoke.RealChildWindowFromPoint((Windows.Win32.Foundation.HWND)hdl, pos).Value;
						res.AddRange(new[] { child1, child2, child3, child4 });
						//Window.FromWindowHandle(
					}
					//            var child = Window.FromWindowHandle( PInvoke.ChildWindowFromPointEx((Windows.Win32.Foundation.HWND)usHandle,pos, flags));
					//var child4 = Window.FromWindowHandle( PInvoke.ChildWindowFromPointEx((Windows.Win32.Foundation.HWND)usHandle,pos, flags4));
					//var child2 = Window.FromWindowHandle( PInvoke.ChildWindowFromPoint((Windows.Win32.Foundation.HWND)usHandle,pos));
					//var child3 = Window.FromWindowHandle( PInvoke.RealChildWindowFromPoint((Windows.Win32.Foundation.HWND)usHandle,pos));  {child2.Handle.Value} {child3.Handle.Value} {child4.Handle.Value}
					var distinctKids = res.Distinct().Select(Window.FromWindowHandle).ToArray();
					var elem = ElementFromCursor();
					var uiaWidth = elem.CurrentBoundingRectangle.right - elem.CurrentBoundingRectangle.left;
					var uiaHeight = elem.CurrentBoundingRectangle.bottom - elem.CurrentBoundingRectangle.top;
					var msgr = $" UIA element: {elem.CurrentName,10} control: {elem.CurrentControlType} size: {uiaWidth,4},{uiaHeight,4}  ||  threadHandles: {handlesToTry.Count} have {res.Distinct().Count()} kids At point: {pos,15}: Over: {over,8} {under.Root.ToString().Substring(0, 50)} norm child: {child.Handle.Value} RES: {String.Join(" # ", distinctKids)} : {child.Root} flags: {flags} ### {under}";
					msgr = msgr.Replace("Microsoft.UI.Content.", "M.").Replace("DesktopChildSiteBridge", "DCSBridge");
					SetStatus(msgr, over switch { CUR_OVER.Us => ourColor, CUR_OVER.Caller => appColor, CUR_OVER.External => notUsColor, _ => throw new NotImplementedException() });
					if (under.Root == window) {

					} else {
						//Window.SetExStyleFlag(WINDOW_EX_STYLE.WS_EX_TRANSPARENT, true);
						weVisible = false;
						//Debug.WriteLine("Transparent");
					}
				} else {
					try {
						if (!weVisible)
							window.SetExStyleFlag(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_TRANSPARENT, false);
						var elem = ElementFromCursor();
						var bounding = elem.CurrentBoundingRectangle;
						var width = bounding.right - bounding.left;
						var notOverMenu = width > 700 && elem.CurrentName == "Close";
						if (notOverMenu) {
							window.SetExStyleFlag(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_TRANSPARENT, true);
							if (weVisible) {
								
								SetStatus($"{DateTime.Now}: Gone transparent", notUsColor);
							}
							weVisible = false;
						} else {
							if (!weVisible) {
								SetStatus($"{DateTime.Now}: Hi There you on us:) in {totalLoop} we have spent {totalTicks} ms processing (avg: {totalTicks/(double)totalLoop:0.00})", ourColor);
								weVisible = true;
								window.SetExStyleFlag(Windows.Win32.UI.WindowsAndMessaging.WINDOW_EX_STYLE.WS_EX_TRANSPARENT, false);
							}
						}
					} catch {
						await Task.Delay(100);//uia will fail for example over admin windows, of course could just assume not over menu then:)
					}

				}
				totalLoop++;

				totalTicks += Environment.TickCount - startTick;
				await Task.Delay(50);
			}
			instance.Hide();
			//{

			//    var hwnd1 = PInvoke.ChildWindowFromPointEx(,pt,CWP_FLAGS.);
			//    if (hwnd1 == instance.Window)
			//        instance.Window.SetExStyleFlag(WINDOW_EX_STYLE.WS_EX_TRANSPARENT, false); //assuming we were transparent
			//    var hwnd2 = Window.FromLocation(Cursor.Position);
			//    if (hwnd2 != instance.Window) //ok we are not actually over us lets go transparent
			//        instance.Window.SetExStyleFlag(WINDOW_EX_STYLE.WS_EX_TRANSPARENT, true);
			//    await Task.Delay(100);
			//}
		}
		private static void SetStatus(string str, Colors? color = null) => MainWindow.SetStatus(str, color);
		private static void Log(string str) => MainWindow.log(str);
	}
}
