using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class DebugLogFile
{
	string			logdir = null;
	string			filename = null;
	StringBuilder	sb = null;
	
	/// <summary>
	/// デバッグログファイルオープン
	/// </summary>
	public DebugLogFile(string _filename)
	{
		// ログディレクトリ
		logdir = System.IO.Path.GetDirectoryName(Application.dataPath) + @"\_Log\";
		if (Directory.Exists(logdir) == false)
		{
			Directory.CreateDirectory(logdir);
		}
		
		// ログファイルが多すぎたらエラー出しておく
		string[] files = Directory.GetFiles(logdir);
		if (files.Length > 500)
		{
			Debug.Log("too much debug-log file.");
		}
		filename = _filename;
		
		sb = new StringBuilder(4096);
		sb.AppendLine( "");
		sb.AppendLine($"★★★★★★★★★★★★★★★★★★★★★★");
		sb.AppendLine($"★ LogStart {DateTime.Now.ToString()} ★");
		sb.AppendLine($"★★★★★★★★★★★★★★★★★★★★★★");
		sb.AppendLine( "");
		
		Debug.Log($"[DebugLogFile] open {logdir}{filename}");
	}
	
	/// <summary>
	/// 文字列追加
	/// </summary>
	public void Append(string str)
	{
		if (sb == null)
		{
			Debug.LogError("debug-log file not open.");
			return;
		}
		sb.Append(str);
	}
	
	/// <summary>
	/// 文字列追加（＋改行）
	/// </summary>
	public void AppendLine(string str)
	{
		if (sb == null)
		{
			Debug.LogError("debug-log file not open.");
			return;
		}
		sb.AppendLine(str);
	}
	
	/// <summary>
	/// ログ書き出し
	/// </summary>
	public void Flush()
	{
		if (sb == null)
		{
			Debug.LogError("debug-log file not open.");
			return;
		}
		if (sb.Length == 0)
		{
			return;
		}
		
		using (StreamWriter sw = new StreamWriter(logdir + filename, true))
		{
			sw.Write(sb.ToString());
			sw.Flush();
			sw.Close();
			
			sb.Clear();
		}
	}
}
