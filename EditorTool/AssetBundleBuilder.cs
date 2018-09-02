#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Musha.EditorTool {

/// <summary>
/// AssetBundleビルダー
/// </summary>
public class AssetBundleBuilder : EditorWindow
{
	/// <summary>
	/// アセットバンドル名付与対象のアセット群
	/// </summary>
	private IEnumerable<UnityEngine.Object> addNameTargets = null;
	/// <summary>
	/// AssetBundle保存先
	/// </summary>
	private EditorPrefsString destPath = null;
	/// <summary>
	/// AssetBundle出力先
	/// </summary>
	private string destSubPath = null;
	/// <summary>
	/// アセットバンドル名付与対象表示のスクロール位置
	/// </summary>
	private Vector2 scrollPosition = Vector2.zero;
	/// <summary>
	/// ビルドターゲット
	/// </summary>
	private BuildTarget buildTarget =
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
	/// GUIウィンドウを開く
	/// </summary>
	[MenuItem("Musha/AssetBundleBuilder")]
	private static void Open()
	{
		EditorWindow.GetWindow<AssetBundleBuilder>();
	}

	/// <summary>
	/// 初期化処理
	/// </summary>
	private void OnEnable()
	{
		this.destPath = new EditorPrefsString(GetType().FullName + ".destPath", Application.dataPath, Directory.Exists);
		this.destSubPath = this.destPath.val + "/" + Define.assetBundleDirectoryName;
		this.OnSelectionChange();
	}

	/// <summary>
	/// 選択内容に変化があった時
	/// </summary>
	private void OnSelectionChange()
	{
		//現在選択中のオブジェクトの中からアセットバンドル名を付与出来るものだけ抽出する
		this.addNameTargets = Selection.objects
			.Where(AssetDatabase.Contains)
			.GroupBy(x => AssetDatabase.GetAssetPath(x))
			.Select(x => x.First())
			.Where(x =>
			{
				string assetPath = AssetDatabase.GetAssetPath(x);
				string extension = Path.GetExtension(assetPath);
				return !extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
			});

		this.Repaint();
	}

	/// <summary>
	/// OnGUI
	/// </summary>
	private void OnGUI()
	{
		GUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("変更", GUILayout.ExpandWidth(false)))
			{
				string path = EditorUtility.SaveFolderPanel("AssetBundle保存先の選択", this.destPath.val, "");
				if (!string.IsNullOrEmpty(path))
				{
					//保存先決定
					this.destPath.val = path;
					this.destSubPath = this.destPath + "/" + Define.assetBundleDirectoryName;
				}
			}

			GUILayout.Label("出力先", GUILayout.ExpandWidth(false));

			this.destSubPath = GUILayout.TextField(this.destSubPath);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("AssetBundleビルド", GUILayout.Width(150)))
			{
				this.AddAssetBundleName();
				this.BuildAssetBundle();
			}

			EditorGUI.BeginDisabledGroup(this.addNameTargets.Count() == 0);
			{
				if (GUILayout.Button("AssetBundle名を付与", GUILayout.Width(150)))
				{
					this.AddAssetBundleName();
				}
			}
			EditorGUI.EndDisabledGroup();
		}
		GUILayout.EndHorizontal();

		EditorGUILayout.HelpBox("アセットバンドル名を付与したいアセットがあればProjectウィンドウ内から選択して下さい。", MessageType.Info);

		this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);
		{
			foreach (var asset in this.addNameTargets)
			{
				GUILayout.Label(AssetDatabase.GetAssetPath(asset));
			}
		}
		GUILayout.EndScrollView();
	}

	/// <summary>
	/// 選択しているアセットにアセットバンドル名を付与する
	/// </summary>
	private void AddAssetBundleName()
	{
		foreach (var asset in this.addNameTargets)
		{
			//アセットパス
			string assetPath = AssetDatabase.GetAssetPath(asset);
			//拡張子
			string extension = Path.GetExtension(assetPath);
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

	/// <summary>
	/// アセットバンドルビルド
	/// </summary>
	private void BuildAssetBundle()
	{
		//保存先ディレクトリを作成
		Directory.CreateDirectory(this.destSubPath);

		//アセットバンドルビルド実行（マニフェスト情報取得）
		var manifest = BuildPipeline.BuildAssetBundles(this.destSubPath, BuildAssetBundleOptions.None, this.buildTarget);
		if (manifest == null)
		{
			//アセットバンドル名が付与されていなかったりした場合はnullになるのでreturn
			Debug.LogError("BuildAssetBundle failed.");
			return;
		}

		//ResourceList.csvを作成
		using (var writer = new StreamWriter(this.destSubPath + "/ResourceList.csv", false, Encoding.UTF8))
		{
			foreach (var assetBundleName in manifest.GetAllAssetBundles())
			{
				string assetBundlePath = this.destSubPath + "/" + assetBundleName;
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
		Debug.Log("BuildAssetBundle success.");
	}
}

}//namespace Musha.EditorTool
#endif