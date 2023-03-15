using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class OurDbg {
	public static void Log(String msg, [System.Runtime.CompilerServices.CallerFilePath] string source_file_path = "",[System.Runtime.CompilerServices.CallerMemberName] string member_name = "") => Debug.WriteLine(BLog(msg,source_file_path,member_name));
	public static string BLog(String msg, [System.Runtime.CompilerServices.CallerFilePath] string source_file_path = "", [System.Runtime.CompilerServices.CallerMemberName] string member_name = "") => $"{DateTime.Now.ToString("mm:ss.ffff")} {GetCallerIdent(member_name, source_file_path)}: {msg}";
	public static string GetCallerIdent([System.Runtime.CompilerServices.CallerMemberName] string member_name = "", [System.Runtime.CompilerServices.CallerFilePath] string source_file_path = "") {
		return GetFileNameFromFullPath(source_file_path) + "::" + member_name;

	}
	public static String GetFileNameFromFullPath(String source_file_path) {
		var file = source_file_path.Substring(source_file_path.LastIndexOf('\\') + 1);
		return file;
	}
}

