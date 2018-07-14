#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using MushaEngine;

namespace MushaEditor {

/// <summary>
/// AssetBundleビルダー
/// </summary>
public class AssetBundleBuilder
{
	/// <summary>
	/// ビルドターゲット
	/// </summary>
	private const BuildTarget BUILD_TARGET =
#if UNITY_ANDROID
		BuildTarget.Android;
#elif UNITY_IOS
		BuildTarget.iOS;
#elif UNITY_STANDALONE_WIN
		BuildTarget.StandaloneWindows;
#elif UNITY_STANDALONE_OSX
		BuildTarget.StandaloneOSX;
#endif

	/// <summary>
	/// AssetBundle保存先のEditorPrefsキー
	/// </summary>
	private static readonly string destPathKey = typeof(AssetBundleBuilder).FullName + ".destPath";
	/// <summary>
	/// AssetBundle保存先
	/// </summary>
	private static string destPath
	{
		get
		{
			return EditorPrefs.HasKey(destPathKey) && Directory.Exists(EditorPrefs.GetString(destPathKey))
				 ? EditorPrefs.GetString(destPathKey)
				 : (destPath = Application.dataPath);
		}
		set
		{
			EditorPrefs.SetString(destPathKey, value);
		}
	}

	/// <summary>
	/// AssetBundleビルド
	/// Projectウィンドウ内で選択しているアセットにはその階層名のアセットバンドル名を付ける
	/// </summary>
	[MenuItem("MushaEditor/BuildAssetBundle")]
	[MenuItem("Assets/MushaEditor/BuildAssetBundle")]
	private static void BuildAssetBundle()
	{
		string path = EditorUtility.SaveFolderPanel("AssetBundle保存先の選択", destPath, "");
		if (string.IsNullOrEmpty(path))
		{
			return;
		}

		//保存先決定
		destPath = path;
		//保存先内に専用ディレクトリを作成
		string destSubPath = destPath + "/" + Define.assetBundleDirectoryName;
		Directory.CreateDirectory(destSubPath);

		//選択したアセットにアセットバンドル名を設定する
		foreach (var asset in Selection.objects.Where(AssetDatabase.Contains))
		{
			//アセットパス
			string assetPath = AssetDatabase.GetAssetPath(asset);
			//拡張子
			string extension = Path.GetExtension(assetPath);

			//.csファイルはアセットバンドル化出来ない
			if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarningFormat("{0}をアセットバンドルにすることは出来ません", assetPath);
			}
			else
			{
				//アセットパスをアセットバンドル名にする
				string assetBundleName = assetPath;
				if (!string.IsNullOrEmpty(extension))
				{
					//拡張子は取り除く
					assetBundleName = assetBundleName.Replace(extension, null);
				}

				//インポータに設定
				var importer = AssetImporter.GetAtPath(assetPath);
				importer.assetBundleName = assetBundleName;
				importer.SaveAndReimport();
			}
		}

		//アセットバンドルビルド実行（マニフェスト情報取得）
		var manifest = BuildPipeline.BuildAssetBundles(destSubPath, BuildAssetBundleOptions.None, BUILD_TARGET);

		//ResourceList.csvを作成
		using (var writer = new StreamWriter(destSubPath + "/ResourceList.csv", false, Encoding.UTF8))
		{
			foreach (var assetBundleName in manifest.GetAllAssetBundles())
			{
				string assetBundlePath = destSubPath + "/" + assetBundleName;
				uint crc = 0;
				
				if (BuildPipeline.GetCRCForAssetBundle(assetBundlePath, out crc))
				{
					long fileSize = new FileInfo(assetBundlePath).Length;

					List<string> dataList = new List<string>();
					dataList.Add(assetBundleName);		//アセットバンドル名
					dataList.Add(crc.ToString());		//CRC値
					dataList.Add(fileSize.ToString());	//ファイルサイズ

					//「,」区切りでCSVに書き込む
					writer.WriteLine(string.Join(",", dataList.ToArray()));
				}
			}
		}

		AssetDatabase.Refresh();
		Debug.Log("BuildAssetBundle finished.");
	}
}

}//namespace MushaEditor
#endif