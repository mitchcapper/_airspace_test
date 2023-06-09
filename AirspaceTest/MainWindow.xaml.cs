﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UnitedSets.NotWindows.Flyout.OutOfBoundsFlyout;
using Windows.UI;
using WinUIEx;
using WinWrapper;
using Colors = System.Drawing.Color;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AirspaceTest {
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : WinUIEx.WindowEx {
		public MainWindow() {
			MinHeight = MinWidth = 0;
			this.InitializeComponent();
			//this.MoveAndResize(0, 0, 50, 50);
			Activated += MainWindow_Activated;
			Closed += MainWindow_Closed;

		}

		private void MainWindow_Closed(object sender, WindowEventArgs args) {
			OutOfBoundsFlyoutSystem.Dispose();
		}
		public FrameworkElement GetParent(FrameworkElement elem, int depth) {
			if (depth == 0)
				return elem;
			var par = VisualTreeHelper.GetParent(elem);
			if (par is FrameworkElement nelem)
				return GetParent(nelem, depth - 1);
			else
				throw new Exception("IT is not a frameworkelement: " + par);
		}
		private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args) {
			await Task.Delay(500);
			GetParent(gridMain, 5).PreviewKeyDown += Grid_PreviewKeyDown;


			Activated -= MainWindow_Activated;
			this.CenterOnScreen(900, 500);
			
			
			instance = this;
			OutOfBoundsFlyoutSystem.Initialize(true);
			txtLog.GotFocus += (_, _) => LogPaused = true;
			txtLog.LostFocus += (_, _) => LogPaused = false;
		}
		private static MainWindow instance;
		public static async Task test() {
			int cnt = 0;
			var btn = new Button { Content = "Hi", };
			btn.Click += (_, _) => btn.Content = "Hi" + cnt++;
			log($"Showing flyout at: {WinWrapper.Cursor.Position}");
			await OutOfBoundsFlyoutSystem.ShowAsync(
				new Microsoft.UI.Xaml.Controls.Flyout {
					Content = btn
				}
				, WinWrapper.Cursor.Position

			);//
		}
		public static void log(string msg, Exception? ex = null, [System.Runtime.CompilerServices.CallerFilePath] string source_file_path = "", [System.Runtime.CompilerServices.CallerMemberName] string member_name = "") {
			string GetCallerIdent([System.Runtime.CompilerServices.CallerMemberName] string member_name = "", [System.Runtime.CompilerServices.CallerFilePath] string source_file_path = "") {
				return GetFileNameFromFullPath(source_file_path) + "::" + member_name;

			}
			String GetFileNameFromFullPath(String source_file_path) {
				var file = source_file_path.Substring(source_file_path.LastIndexOf('\\') + 1);
				return file;
			}
			var str = $"{DateTime.Now.ToString("mm:ss.ffff")} {GetCallerIdent(member_name, source_file_path)}: {msg ?? ""}";
			logText.Insert(0, str + "\r\n");
			if (ex != null)
				str += $" Exception {ex.GetType()}: {ex.ToString}";
			if (instance == null)
				Debug.WriteLine(str);
			else if (!LogPaused) {
				instance.DispatcherQueue.EnqueueAsync(() =>
					instance.txtLog.Text = logText.ToString()
				);
			}

		}
		private static volatile bool LogPaused;
		private static StringBuilder logText = new();
		private Color lastColor;
		private static System.Drawing.ColorConverter ColorConvert = new();
		public static Color ConvertToColor(String colorStr) => ConvertFromDrawColor((System.Drawing.Color)ColorConvert.ConvertFromString(colorStr));
		public static Color ConvertFromDrawColor(System.Drawing.Color dcolor) => Color.FromArgb(dcolor.A, dcolor.R, dcolor.G, dcolor.B);

		private static ConcurrentDictionary<Colors, SolidColorBrush> colorToBrush = new();
		public static void SetStatus(string msg, Colors? color = null) {

			if (instance == null) {
				log(msg);

				return;
			}
			var bgColor = color ?? Colors.White;
			if (!colorToBrush.TryGetValue(bgColor, out var brush))
				colorToBrush[bgColor] = brush = new SolidColorBrush(ConvertFromDrawColor(bgColor));

			instance.DispatcherQueue.EnqueueAsync(() => {
				instance.txtStatus.Text = msg;
				instance.txtStatus.Background = brush;

			}
			);
		}
		private void myButton_Click(object sender, RoutedEventArgs e) {
			test();
			myButton.Content = "Clicked";
		}
		public int hoct = 0;
		private void btnHo_Click(object sender, RoutedEventArgs e) {
			btnHo.Content = "Ho" + hoct++;
		}

		private void Grid_PreviewKeyDown(object sender, KeyRoutedEventArgs e) {
			if (e.Key == Windows.System.VirtualKey.Shift)
				log(WinWrapper.Cursor.Position.ToString());
			if (e.Key == Windows.System.VirtualKey.Control && Keyboard.IsRightControlDown)
				test();

		}



	}
}
