using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MushaEngine {

/// <summary>
/// アセットバンドル読み込みクラス
/// </summary>
[AddComponentMenu("MushaEngine/AssetBundleLoader")]
public partial class AssetBundleLoader : MonoBehaviour
{
	/// <summary>
	/// リソースリスト
	/// </summary>
	protected Dictionary<string, AssetBundleOperation> resourceList = new Dictionary<string, AssetBundleOperation>();
	/// <summary>
	/// サーバーのアセットバンドルディレクトリURL
	/// </summary>
	protected string serverAssetBundleDirectoryUrl = null;
	/// <summary>
	/// ローカルのリソースリストパス
	/// </summary>
	protected string localResourceListPath = null;
	/// <summary>
	/// サーバーのリソースリストURL
	/// </summary>
	protected string serverResourceListUrl = null;

	/// <summary>
	/// Awake
	/// </summary>
	protected virtual void Awake()
	{
#if UNITY_EDITOR && !STREAMINGASSETS_SERVER
		this.SetServerAssetBundleDirectoryUrl("file://" + Application.dataPath.Replace("Assets", "Server/" + Define.assetBundleDirectoryName));
#else
		this.SetServerAssetBundleDirectoryUrl("file://" + Application.streamingAssetsPath + "/" + Define.assetBundleDirectoryName);
#endif
	}

	/// <summary>
	/// サーバーのアセットバンドルディレクトリURLを設定する
	/// </summary>
	public void SetServerAssetBundleDirectoryUrl(string url)
	{
		this.serverAssetBundleDirectoryUrl = url;
		this.serverResourceListUrl = url + "/ResourceList.csv";
		this.localResourceListPath = Define.GetLocalAssetBundleDirectoryPath() + "/ResourceList.dat";
	}

	/// <summary>
	/// セットアップ
	/// </summary>
	public void Setup(Action onFinished)
	{
		//ローカルのリソースリスト読み込み
		this.LoadResourceList();

		//サーバーから最新のリソースリストを取得して更新
		StartCoroutine(this.DownloadResourceList(onFinished));
	}

	/// <summary>
	/// リソースリスト保存
	/// </summary>
	private void SaveResourceList()
	{
		//ディレクトリ作成
		Directory.CreateDirectory(Path.GetDirectoryName(this.localResourceListPath));

		//ファイル書き込み
		using (var stream = new FileStream(this.localResourceListPath, FileMode.Create, FileAccess.Write))
		using (var writer = new BinaryWriter(stream))
		{
			foreach (var data in this.resourceList.Values)
			{
				data.Save(writer);
			}
		}
	}

	/// <summary>
	/// リソースリスト読み込み
	/// </summary>
	protected void LoadResourceList()
	{
		//ファイル存在チェック
		if (File.Exists(this.localResourceListPath))
		{
			//ファイル読み込み
			using (var stream = new MemoryStream(File.ReadAllBytes(this.localResourceListPath)))
			using (var reader = new BinaryReader(stream))
			{
				while (!reader.IsEnd())
				{
					var data = new AssetBundleOperation(reader);
					this.resourceList.Add(data.name, data);
				}
			}
		}
	}

	/// <summary>
	/// リソースリストのダウンロード
	/// </summary>
	protected IEnumerator DownloadResourceList(Action onFinished = null)
	{
		//CSVダウンロード
		using (var www = new WWW(this.serverResourceListUrl))
		{
			//ダウンロード完了を待つ
			yield return www.WaitOrTimeout();

			//タイムアウト
			if (!www.isDone)
			{
				Debug.LogError("タイムアウト");
			}
			//エラー
			else if (!string.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
			}
			else
			{
				//CSV読み込み
				using (var stream = new MemoryStream(www.bytes))
				using (var reader = new StreamReader(stream))
				{
					string line = null;
					while ((line = reader.ReadLine()) != null)
					{
						string[] lineSplit = line.Split(',');
						string name = lineSplit[0];

						if (this.resourceList.ContainsKey(name))
						{
							this.resourceList[name].UpdateFromCsv(lineSplit);
						}
						else
						{
							var data = new AssetBundleOperation(lineSplit);
							this.resourceList.Add(name, data);
						}
					}
				}

				//更新内容を保存
				this.SaveResourceList();

				//コールバック実行
				onFinished.SafetyInvoke();
			}
		}	
	}

	/// <summary>
	/// 単体アセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="assetName">読み込むアセット名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadAsset<T>(string assetBundleName, string assetName, Action<T> onLoad = null) where T : UnityEngine.Object
	{
		if (!this.resourceList.ContainsKey(assetBundleName))
		{
			Debug.LogWarningFormat("リソースリストに無いアセットバンドルです：assetBundleName={0}", assetBundleName);
			onLoad.SafetyInvoke(null);
			return;
		}

		var data = this.resourceList[assetBundleName];
		var assetOperation = data.FindAssetOperation<AssetOperation<T>>(assetName);

		//初めての読み込み
		if (assetOperation == null)
		{
			//アセット管理データ作成
			assetOperation = new AssetOperation<T>(assetName, onLoad);
			data.AddAssetOperation(assetOperation);
			//読み込み開始
			this.UpdateAssetBundleOperation(data);
		}
		//ロード済み
		else if (assetOperation.GetStatus() == AssetOperationBase.Status.isLoaded)
		{
			//１フレーム後にコールバック実行
			StartCoroutine(CoroutineUtility.WaitForFrameAction(1, () =>
			{
				onLoad.SafetyInvoke(assetOperation.GetAsset<T>());
			}));
		}
		//ロード中
		else
		{
			//コールバック追加
			assetOperation.AddCallBack(onLoad);
		}
	}

	/// <summary>
	/// 単体アセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="assetName">読み込むアセット名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadAsset(string assetBundleName, string assetName, Action<UnityEngine.Object> onLoad = null)
	{
		this.LoadAsset<UnityEngine.Object>(assetBundleName, assetName, onLoad);
	}

	/// <summary>
	/// 全アセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadAllAssets<T>(string assetBundleName, Action<T[]> onLoad = null) where T : UnityEngine.Object
	{
		if (!this.resourceList.ContainsKey(assetBundleName))
		{
			Debug.LogWarningFormat("リストに無いアセットバンドルです：assetBundleName={0}", assetBundleName);
			onLoad.SafetyInvoke(null);
			return;
		}

		var data = this.resourceList[assetBundleName];
		var assetOperation = data.FindAssetOperation<AllAssetsOperation<T>>();

		//初めての読み込み
		if (assetOperation == null)
		{
			//アセット管理データ作成
			assetOperation = new AllAssetsOperation<T>(onLoad);
			data.AddAssetOperation(assetOperation);
			//読み込み開始
			this.UpdateAssetBundleOperation(data);
		}
		//ロード済み
		else if (assetOperation.GetStatus() == AssetOperationBase.Status.isLoaded)
		{
			//１フレーム後にコールバック実行
			StartCoroutine(CoroutineUtility.WaitForFrameAction(1, () =>
			{
				onLoad.SafetyInvoke(assetOperation.GetAllAssets<T>());
			}));
		}
		//ロード中
		else
		{
			//コールバック追加
			assetOperation.AddCallBack(onLoad);
		}
	}

	/// <summary>
	/// 全アセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadAllAssets(string assetBundleName, Action<UnityEngine.Object[]> onLoad = null)
	{
		this.LoadAllAssets<UnityEngine.Object>(assetBundleName, onLoad);
	}

	/// <summary>
	/// サブアセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="assetName">読み込むアセット名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadSubAssets<T>(string assetBundleName, string assetName, Action<T[]> onLoad = null) where T : UnityEngine.Object
	{
		if (!this.resourceList.ContainsKey(assetBundleName))
		{
			Debug.LogWarningFormat("リストに無いアセットバンドルです：assetBundleName={0}", assetBundleName);
			onLoad.SafetyInvoke(null);
			return;
		}

		var data = this.resourceList[assetBundleName];
		var assetOperation = data.FindAssetOperation<SubAssetsOperation<T>>(assetName);

		//初めての読み込み
		if (assetOperation == null)
		{
			//アセット管理データ作成
			assetOperation = new SubAssetsOperation<T>(assetName, onLoad);
			data.AddAssetOperation(assetOperation);
			//読み込み開始
			this.UpdateAssetBundleOperation(data);
		}
		//ロード済み
		else if (assetOperation.GetStatus() == AssetOperationBase.Status.isLoaded)
		{
			//１フレーム後にコールバック実行
			StartCoroutine(CoroutineUtility.WaitForFrameAction(1, () =>
			{
				onLoad.SafetyInvoke(assetOperation.GetAllAssets<T>());
			}));
		}
		//ロード中
		else
		{
			//コールバック追加
			assetOperation.AddCallBack(onLoad);
		}
	}

	/// <summary>
	/// サブアセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="assetName">読み込むアセット名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadSubAssets(string assetBundleName, string assetName, Action<UnityEngine.Object[]> onLoad = null)
	{
		this.LoadSubAssets<UnityEngine.Object>(assetBundleName, assetName, onLoad);
	}

	/// <summary>
	/// 指定アセットバンドルの破棄
	/// </summary>
	public void UnloadAssetBundle(string assetBundleName)
	{
		if (!this.resourceList.ContainsKey(assetBundleName))
		{
			Debug.LogWarningFormat("リストに無いアセットバンドルです：assetBundleName={0}", assetBundleName);
			return;
		}
	
		this.resourceList[assetBundleName].Unload();
	}

	/// <summary>
	/// 全アセットバンドルの破棄
	/// </summary>
	public void UnloadAll()
	{
		foreach (var data in this.resourceList.Values)
		{
			data.Unload();
		}
	}

	/// <summary>
	/// 指定のアセットバンドルがアンロード可能かどうか
	/// </summary>
	public bool IsUnloadable(string assetBundleName)
	{
		if (!this.resourceList.ContainsKey(assetBundleName))
		{
			Debug.LogWarningFormat("リストに無いアセットバンドルです：assetBundleName={0}", assetBundleName);
			return false;
		}

		return this.resourceList[assetBundleName].IsUnloadable();
	}

	/// <summary>
	/// 全アセットバンドルのアンロードが可能かどうか
	/// </summary>
	public bool IsUnloadableAll()
	{
		return !this.resourceList.Values.Any(x => !x.IsUnloadable());
	}

	/// <summary>
	/// アセットバンドルの状態に応じた処理
	/// </summary>
	private void UpdateAssetBundleOperation(AssetBundleOperation data)
	{
		var status = data.GetStatus();

		switch (status)
		{
		//ダウンロードが必要
		case AssetBundleOperation.Status.isNeedDownload:
		{
			//ダウンロード開始
			data.DownloadAssetBundle(this, this.serverAssetBundleDirectoryUrl, () =>
			{
				this.SaveResourceList();
				this.UpdateAssetBundleOperation(data);
			});
		}
		break;

		//ダウンロード済み
		case AssetBundleOperation.Status.isDownloaded:
		{
			//読み込み開始
			data.LoadAssetBundle(() =>
			{
				this.UpdateAssetBundleOperation(data);
			});
		}
		break;

		//読み込み済み
		case AssetBundleOperation.Status.isLoaded:
		{
			data.LoadAsset();
		}
		break;
		}
	}

#if UNITY_EDITOR
	#region インスペクター表示
	/// <summary>
	/// インスペクターGUI：リソースリスト折り畳み表示用
	/// </summary>
	private bool foldoutResourceList = false;
	/// <summary>
	/// インスペクターGUI：読み込み済みアセットバンドル折り畳み表示用
	/// </summary>
	private bool foldoutLoadedAssetBundles = false;
	
	/// <summary>
	/// インスペクターGUI描画
	/// </summary>
	public void OnInspectorGUI()
	{
		EditorGUI.indentLevel = 0;

		//リソースリスト一覧表示
		this.foldoutResourceList = EditorGUILayout.Foldout(this.foldoutResourceList, "ResourceList : Count=" + this.resourceList.Count);
		if (this.foldoutResourceList)
		{
			if (this.resourceList.Count == 0)
			{
				EditorGUI.indentLevel = 1;
				EditorGUILayout.LabelField("Empty");
			}
			else
			{
				foreach (var data in this.resourceList.Values)
				{
					EditorGUI.indentLevel = 1;
					data.DrawInspectorGUI();
				}
			}
		}

		EditorGUI.indentLevel = 0;

		//読み込み済みアセットバンドル一覧表示
		this.foldoutLoadedAssetBundles = EditorGUILayout.Foldout(this.foldoutLoadedAssetBundles, "LoadedAssetBundles");
		if (this.foldoutLoadedAssetBundles)
		{
			var loadedAssetBundles = this.resourceList.Values.Where(x => x.GetStatus() == AssetBundleOperation.Status.isLoaded);
			if (loadedAssetBundles.Count() == 0)
			{
				EditorGUI.indentLevel = 1;
				EditorGUILayout.TextField(null);
			}
			else
			{
				foreach (var data in loadedAssetBundles)
				{
					EditorGUI.indentLevel = 1;
					EditorGUILayout.TextField(data.name);
				}
			}
		}
	}
	#endregion
#endif

}

}//namespace MushaEngine

#if UNITY_EDITOR
namespace MushaEditor {

/// <summary>
/// AssetBundleLoaderカスタムインスペクター
/// </summary>
[CustomEditor(typeof(MushaEngine.AssetBundleLoader))]
public class AssetBundleLoaderInspector : Editor
{
	/// <summary>
	/// インスペクターGUI描画
	/// </summary>
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		(this.target as MushaEngine.AssetBundleLoader).OnInspectorGUI();
	}
}

}//namespace MushaEditor
#endif