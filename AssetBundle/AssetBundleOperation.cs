using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Musha {
public partial class AssetBundleLoader : MonoBehaviour {

/// <summary>
/// アセットバンドル管理クラス
/// </summary>
protected class AssetBundleOperation
{
	/// <summary>
	/// 状態
	/// </summary>
	public enum Status
	{
		isNeedDownload,
		isDownloading,
		isDownloaded,
		isLoading,
		isLoaded,
	}

	/// <summary>
	/// AssetBundleCreateRequest
	/// </summary>
	private AssetBundleCreateRequest request = null;
	/// <summary>
	/// アセットバンドル名
	/// </summary>
	public string name { get; private set; }
	/// <summary>
	/// ローカルのアセットバンドルファイルパス
	/// </summary>
	public string path { get; private set; }
	/// <summary>
	/// ローカルのCRC値
	/// </summary>
	private uint localCRC = 0;
	/// <summary>
	/// サーバーのCRC値
	/// </sumamry>
	private uint serverCRC = 0;
	/// <sumamry>
	/// Unload可能かどうか
	/// </summary>
	public bool isDontUnload = false;
	/// <summary>
	/// ダウンローダー
	/// </summary>
	private MonoBehaviour downloader = null;
	/// <summary>
	/// ダウンロードコルーチン
	/// </summary>
	private Coroutine downloadCoroutine = null;
	/// <summary>
	/// アセット管理リスト
	/// </summary>
	private List<AssetOperationBase> assetOperationList = new List<AssetOperationBase>();

	/// <summary>
	/// construct：ローカルの保存バイナリデータからの作成
	/// </summary>
	public AssetBundleOperation(BinaryReader reader)
	{
		this.name = reader.ReadString();
		this.localCRC = reader.ReadUInt32();
		this.path = Define.GetLocalAssetBundleDirectoryPath() + "/" + this.name + ".dat";
	}

	/// <summary>
	/// construct：サーバーのCSVデータからの作成
	/// </summary>
	public AssetBundleOperation(string[] csv)
	{
		this.name = csv[0];
		this.serverCRC = uint.Parse(csv[1]);
		this.path = Define.GetLocalAssetBundleDirectoryPath() + "/" + this.name + ".dat";
	}

	/// <summary>
	/// サーバーのCSVデータで内容を更新
	/// </summary>
	public void UpdateFromCsv(string[] csv)
	{
		this.serverCRC = uint.Parse(csv[1]);
	}

	/// <summary>
	/// CRC値の更新
	/// </summary>
	public void UpdateCRC()
	{
		this.localCRC = this.serverCRC;
	}

	/// <summary>
	/// ローカルのバイナリデータへの書き込み
	/// </summary>
	public void Save(BinaryWriter writer)
	{
		writer.Write(this.name);
		writer.Write(this.localCRC);
	}

	/// <summary>
	/// アセットバンドルの状態を取得する
	/// </summary>
	public Status GetStatus()
	{
		if (this.request == null)
		{
			if (this.downloadCoroutine != null)
			{
				return Status.isDownloading;
			}
			else if (this.localCRC == 0 || this.localCRC != this.serverCRC || !File.Exists(this.path))
			{
				return Status.isNeedDownload;
			}
			else
			{
				return Status.isDownloaded;
			}
		}
		else
		{
			if (this.request.isDone)
			{
				return Status.isLoaded;
			}
			else
			{
				return Status.isLoading;
			}
		}
	}

	/// <summary>
	/// アセットバンドルのダウンロード
	/// </summary>
	public void DownloadAssetBundle(MonoBehaviour downloader, string serverAssetBundleDirectoryUrl, Action<byte[]> onFinished)
	{
		this.downloader = downloader;
		this.downloadCoroutine = downloader.StartCoroutine(this.DownloadAssetBundle(serverAssetBundleDirectoryUrl, onFinished));
	}

	/// <summary>
	/// アセットバンドルのダウンロード
	/// </summary>
	private IEnumerator DownloadAssetBundle(string serverAssetBundleDirectoryUrl, Action<byte[]> onFinished)
	{
		//ダウンロード開始
		using (var www = new WWW(serverAssetBundleDirectoryUrl + "/" + this.name))
		{
			//ダウンロード完了を待つ
			yield return www.WaitOrTimeout();

			//ダウンロード完了
			this.downloader = null;
			this.downloadCoroutine = null;

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
			//ダウンロード成功
			else
			{
				onFinished(www.bytes);
			}
		}
	}

	/// <summary>
	/// アセットバンドルの読み込み
	/// </summary>
	public void LoadAssetBundleFromFile(Action onFinished)
	{
		//ローカルファイルからアセットバンドルを読み込む
		this.request = AssetBundle.LoadFromFileAsync(this.path);
		//読み込み完了時処理
		this.request.completed += (op) =>
		{
			//コールバック実行
			onFinished();
		};
	}

	/// <summary>
	/// アセットバンドルの読み込み
	/// </summary>
	public void LoadAssetBundleFromMemory(byte[] bytes, Action onFinished)
	{
		//メモリからアセットバンドルを読み込む
		this.request = AssetBundle.LoadFromMemoryAsync(bytes);
		//読み込み完了時処理
		this.request.completed += (op) =>
		{
			//コールバック実行
			onFinished();
		};
	}

	/// <summary>
	///	アセットの読み込み
	/// </summary>
	public void LoadAsset()
	{
		for (int i = 0, imax = this.assetOperationList.Count; i < imax; i++)
		{
			this.assetOperationList[i].Load(this.request.assetBundle);
		}
	}

	/// <summary>
	/// アセットバンドルの破棄
	/// </summary>
	public void Unload()
	{
		if (isDontUnload) return;

		var status = this.GetStatus();

		switch (status)
		{
		//ダウンロード中
		case Status.isDownloading:
		{
			Debug.LogWarningFormat(
				"ダウンロードを中止します。\n" +
				"アセットバンドル名：{0}",
				this.name);

			//ダウンロードを止める
			this.downloader.StopCoroutine(this.downloadCoroutine);
			this.downloader = null;
			this.downloadCoroutine = null;

			//アセット管理データを全破棄
			this.assetOperationList.Clear();
		}
		break;

		//ロード中
		case Status.isLoading:
		{
			Debug.LogWarningFormat(
				"読み込み中のアセットバンドルは破棄出来ません。\n" +
				"アセットバンドル名：{0}",
				this.name);
		}
		break;

		//ロード済み
		case Status.isLoaded:
		{
			//読み込み中のアセットがないかチェック
			for (int i = 0, imax = this.assetOperationList.Count; i < imax; i++)
			{
				if (this.assetOperationList[i].GetStatus() == AssetOperationBase.Status.isLoading)
				{
					Debug.LogWarningFormat(
						"アセット読み込み中の為、このアセットバンドルは破棄出来ません。\n" +
						"アセットバンドル名：{0}\n" +
						"読み込み中アセットタイプ：{1}\n" +
						"読み込み中アセット名：{2}",
						this.name,
						this.assetOperationList[i].GetType(),
						this.assetOperationList[i].assetName);
					return;
				}
			}

			//アセット管理データを全破棄
			this.assetOperationList.Clear();

			//アセットバンドルを破棄
			this.request.assetBundle.Unload(true);
			this.request = null;
		}
		break;
		}
	}

	/// <summary>
	/// 処理中かどうか
	/// ※true時にはUnload出来ない
	/// </summary>
	public bool IsBusy()
	{
		var status = this.GetStatus();

		if (status == Status.isLoading)
		{
			return true;
		}
		else if (status == Status.isLoaded)
		{
			return this.assetOperationList.Exists(x => x.GetStatus() == AssetOperationBase.Status.isLoading);
		}
		else
		{
			return false;
		}
	}

	/// <summary>
	/// アセット管理データの追加
	/// </summary>
	public void AddAssetOperation(AssetOperationBase assetOperation)
	{
		this.assetOperationList.Add(assetOperation);
	}

	/// <summary>
	/// アセット管理データの検索
	/// </summary>
	public T FindAssetOperation<T>(string assetName = null) where T : AssetOperationBase
	{
		return (T)this.assetOperationList.FirstOrDefault(x => x is T && x.assetName == assetName);
	}

#if UNITY_EDITOR
	#region インスペクター表示
	/// <summary>
	/// InspectorGUI：折り畳み表示用
	/// </summary>
	/// <remarks>Editor Only</remarks>
	private bool foldout = false;
	/// <summary>
	/// InspectorGUI：アセット管理リスト折り畳み表示用
	/// </summary>
	/// <remarks>Editor Only</remarks>
	private bool foldoutAssetOperationList = false;

	/// <summary>
	/// InspectorGUI描画
	/// </summary>
	/// <remarks>Editor Only</remarks>
	public void OnInspectorGUI()
	{
		//折り畳み表示
		this.foldout = EditorGUILayout.Foldout(this.foldout, this.name);
		if (this.foldout)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.EnumPopup("Status", this.GetStatus());
			this.isDontUnload = EditorGUILayout.Toggle("Is Don't Unload", this.isDontUnload);
			EditorGUILayout.DoubleField("LocalCRC", this.localCRC);
			EditorGUILayout.DoubleField("ServerCRC", this.serverCRC);

			//アセット管理リストの折り畳み表示
			this.foldoutAssetOperationList = EditorGUILayout.Foldout(this.foldoutAssetOperationList, "AssetOperationList:Count=" + this.assetOperationList.Count);
			if (this.foldoutAssetOperationList)
			{
				if (this.assetOperationList.Count == 0)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField("empty");
				}
				else
				{
					int indentLevel = EditorGUI.indentLevel;

					for (int i = 0, imax = this.assetOperationList.Count; i < imax; i++)
					{
						EditorGUI.indentLevel = indentLevel + 1;
						this.assetOperationList[i].OnInspectorGUI(i);
					}
				}
			}
		}
	}
	#endregion
#endif
}

}
}//namespace Musha
