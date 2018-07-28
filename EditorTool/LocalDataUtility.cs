#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace MushaSystem.EditorTool {

/// <summary>
/// ローカルデータ取り扱いクラス
/// </summary>
public class LocalDataUtility
{
	/// <summary>
	/// ローカルデータディレクトリを開く
	/// </summary>
	[MenuItem("MushaSystem/Open LocalDataDirectory")]
	private static void OpenLocalDataDirectory()
	{
		Directory.CreateDirectory(Define.GetLocalDataDirectoryPath());
		Process.Start(Define.GetLocalDataDirectoryPath());
	}
}

}//namespace MushaSystem.EditorTool
#endif