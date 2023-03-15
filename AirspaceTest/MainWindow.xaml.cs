using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UnitedSets.Windows.Flyout.OutOfBoundsFlyout;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement.Core;
using WinUIEx;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AirspaceTest {
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : WinUIEx.WindowEx {
		public MainWindow() {
			this.InitializeComponent();
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
			GetParent(gridMain,5).PreviewKeyDown += Grid_PreviewKeyDown;
			
			
			Activated -= MainWindow_Activated;
			this.CenterOnScreen(800, 400);
			instance = this;
			OutOfBoundsFlyoutSystem.Initialize();
			txtLog.GotFocus += (_, _) => LogPaused = true;
			txtLog.LostFocus += (_, _) => LogPaused = false;
		}
		private static MainWindow instance;
		public async Task test() {
			int cnt = 0;
			var btn = new Button { Content = "Hi", };
			btn.Click += (_, _) => btn.Content = "Hi" + cnt++;
			log($"Showing flyout at: {WinWrapper.Cursor.Position}");
			await OutOfBoundsFlyoutSystem.ShowAsync(
				new Microsoft.UI.Xaml.Controls.Flyout {
					Content = btn
				}
				,
				WinWrapper.Cursor.Position
			);
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
			if (!LogPaused) {
				instance.DispatcherQueue.EnqueueAsync(() =>
					instance.txtLog.Text = logText.ToString()
				);
			}

		}
		private static volatile bool LogPaused;
		private static StringBuilder logText = new();
		public static void SetStatus(string msg) {
			instance.DispatcherQueue.EnqueueAsync(() =>
			instance.txtStatus.Text = msg
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
		}



	}
}
