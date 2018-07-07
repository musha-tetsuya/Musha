using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MushaEngine {
public partial class AssetBundleLoader : MonoBehaviour {

/// <summary>
/// アセットバンドルデータ
/// </summary>
protected class AssetBundleData
{
	/// <summary>
	/// 状態
	/// </summary>
	public enum Status
	{
		None,
		isNeedDownload,
		isDownloading,
		isDownloaded,
		isLoading,
		isLoaded,
	}

	/// <summary>
	/// アセットバンドル実体
	/// </summary>
	public AssetBundle assetBundle { get; private set; }
	/// <summary>
	/// アセットバンドル名
	/// </summary>
	public string name { get; private set; }
	/// <summary>
	/// ローカルのアセットバンドルファイルパス
	/// </summary>
	private string path = null;
	/// <summary>
	/// ローカルのCRC値
	/// </summary>
	public uint localCRC { get; private set; }
	/// <summary>
	/// サーバーのCRC値
	/// </sumamry>
	public uint serverCRC { get; set; }
	/// <summary>
	/// 状態
	/// </summary>
	private Status status = Status.None;
	/// <summary>
	/// 読み込みリクエストリスト
	/// </summary>
	public List<AssetBundleRequestData> requestList { get; private set; }

	/// <summary>
	/// construct
	/// </summary>
	public AssetBundleData(string name, uint localCRC, uint serverCRC)
	{
		this.name = name;
		this.path = Define.GetLocalAssetBundleDirectoryPath() + "/" + this.name + ".dat";
		this.localCRC = localCRC;
		this.serverCRC = serverCRC;
		this.requestList = new List<AssetBundleRequestData>();
	}

	/// <summary>
	/// アセットバンドルの状態を取得する
	/// </summary>
	public Status GetStatus()
	{
		if (this.assetBundle != null)
		{
			return Status.isLoaded;
		}
		else if (this.status != Status.None)
		{
			return this.status;
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

	/// <summary>
	/// アセットバンドルのダウンロード
	/// </summary>
	public IEnumerator DownloadAssetBundle(string serverAssetBundleDirectoryUrl, Action onFinished)
	{
		//ダウンロード開始
		this.status = Status.isDownloading;
		using (var www = new WWW(serverAssetBundleDirectoryUrl + "/" + this.name))
		{
			//ダウンロード完了を待つ
			yield return www.Wait();
			this.status = Status.None;

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
				//ダウンロードしたアセットバンドルを保存するディレクトリを作成
				Directory.CreateDirectory(Path.GetDirectoryName(this.path));
				//ダウンロードしたアセットバンドルを保存
				File.WriteAllBytes(this.path, www.bytes);
				//ダウンロードしたのでCRC値を更新
				this.localCRC = this.serverCRC;
				//コールバック
				onFinished();
			}
		}
	}

	/// <summary>
	/// アセットバンドルの読み込み
	/// </summary>
	public IEnumerator LoadAssetBundle(Action onFinished)
	{
		//ステータスをロード中に
		this.status = Status.isLoading;
		//ローカルのファイルからアセットバンドルを読み込む
		var request = AssetBundle.LoadFromFileAsync(this.path);
		//読み込み待ち
		yield return request;
		this.status = Status.None;
		//読み込んだアセットバンドルを保持
		this.assetBundle = request.assetBundle;
		//コールバック
		onFinished();
	}

	/// <summary>
	/// 読み込みリクエスト検索
	/// </summary>
	public T FindRequestData<T>(string assetName = null) where T : AssetBundleRequestData
	{
		return (T)this.requestList.FirstOrDefault(x => x is T && x.assetName == assetName);
	}
}

}
}//namespace MushaEngine
