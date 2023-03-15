using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Drawing;
using System.Threading.Tasks;
using TransparentWinUIWindowLib;
//using UnitedSets.Classes;
using WinUIEx;
using WinWrapper;
using Window = WinWrapper.Window;
using WinUIPoint = Windows.Foundation.Point;
using Microsoft.UI.Xaml.Media;
using AirspaceTest;

namespace UnitedSets.NotWindows.Flyout.OutOfBoundsFlyout {
	static class OutOfBoundsFlyoutSystem {
		public static void Initialize(bool addBorder) {
			Instance = new(addBorder);
		}
		public static void Dispose() {
			Instance.Dispose();
		}
		static OutOfBoundsFlyoutHost Instance;
		private class OutOfBoundsFlyoutHost : WindowEx, IDisposable {
			public readonly SwapChainPanel swapChainPanel;
			readonly TransparentWindowManager trans_mgr;
			public readonly Window Window;
			public OutOfBoundsFlyoutHost(bool addBorder) {
				swapChainPanel = new();
				trans_mgr = new(this, swapChainPanel, false);
				if (addBorder)
					swapChainPanel.Children.Add(new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, Margin = new(5), BorderThickness = new(2), BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)) });
				//IsMinimizable = false;
				//IsMaximizable = false;
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
		public static async Task ShowAsync(FlyoutBase Flyout, Point pt, FlyoutPlacementMode placementMode = FlyoutPlacementMode.Auto) {
			var instance = Instance;
			var Window = instance.Window;
			var displayBounds = Display.FromPoint(pt).WorkingAreaBounds;
			Window.Bounds = new() {
				X = displayBounds.X,
				Y = displayBounds.Y,
				Width = displayBounds.Width,
				Height = displayBounds.Height
			};
			instance.Activate();
			await Task.Delay(50);//critical for sizing to be right, could cache scale info per monitor and watch for dpi changes
			widthScale = Window.ClientBounds.Width / instance.swapChainPanel.ActualWidth;
			heightScale = Window.ClientBounds.Height / instance.swapChainPanel.ActualHeight;
			var loc = new WinUIPoint(pt.X - displayBounds.X, pt.Y - displayBounds.Y);
			var scaled = loc;
			scaled.X /= widthScale;
			scaled.Y /= heightScale;

			Log($"Showing at: {loc} (scaled: {scaled}) Win bounds: {Window.Bounds} (client: {Window.ClientBounds}) SwapC Size: {instance.swapChainPanel.ActualSize}  scale: {widthScale:0.000}x{heightScale:0.000}");

			Flyout.ShowAt(instance.swapChainPanel, new() {
				ShowMode = FlyoutShowMode.Standard,
				Placement = placementMode,
				Position = scaled
			});

			bool Opening = true;
			void closedEv(object? o, object e) => Opening = false;
			Flyout.Closed += closedEv;
			Point lastPos = new(-1, -1);
			//var ourProcId = Window.ThreadProcessId;
			bool weVisible = true;
			while (Opening) {
				var pos = Cursor.Position;

				//Window.SetExStyleFlag(WINDOW_EX_STYLE.WS_EX_TRANSPARENT, false);
				weVisible = true;
				var under = Window.FromLocation(pos);
				SetStatus(under.Root.ToString());
				if (under.Root == Window) {

				} else {
					//Window.SetExStyleFlag(WINDOW_EX_STYLE.WS_EX_TRANSPARENT, true);
					weVisible = false;
					//Debug.WriteLine("Transparent");
				}
				await Task.Delay(100);
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
		private static void SetStatus(string str) => MainWindow.SetStatus(str);
		private static void Log(string str) => MainWindow.log(str);
	}
}
