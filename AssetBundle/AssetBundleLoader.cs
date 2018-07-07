using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;

namespace MushaEngine {

/// <summary>
/// アセットバンドル読み込みクラス
/// </summary>
public partial class AssetBundleLoader : MonoBehaviour
{
	/// <summary>
	/// アセットバンドルデータリスト
	/// </summary>
	protected AssetBundleDataList dataList = new AssetBundleDataList();
	/// <summary>
	/// サーバーのアセットバンドルディレクトリURL
	/// </summary>
	protected string serverAssetBundleDirectoryUrl = null;

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
		this.dataList.filePath = Define.GetLocalAssetBundleDirectoryPath() + "/AssetBundleDataList.dat";
		this.dataList.csvUrl = this.serverAssetBundleDirectoryUrl + "/AssetBundleDataList.csv";
	}

	/// <summary>
	/// セットアップ
	/// </summary>
	public void Setup(Action onFinished)
	{
		//ローカルのアセットバンドルリストデータ読み込み
		this.dataList.Load();

		//サーバーから最新のアセットバンドルリストを取得して更新
		StartCoroutine(this.dataList.Download(onFinished));
	}

	/// <summary>
	/// 単体アセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="assetName">読み込むアセット名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadAsset(string assetBundleName, string assetName, Action<UnityEngine.Object> onLoad)
	{
		var data = this.dataList[assetBundleName];
		var request = data.FindRequestData<AssetRequest>(assetName);

		//初めての読み込みリクエスト
		if (request == null)
		{
			//リクエスト作成
			data.requestList.Add(new AssetRequest(assetName, onLoad));
			//アセットバンドル読み込み
			this.UpdateAssetBundleData(data);
		}
		//ロード済み
		else if (request.isLoaded)
		{
			//コールバック実行
			if (onLoad != null)
			{
				onLoad(request.asset);
			}
		}
		//ロード中
		else
		{
			//コールバック追加
			request.AddCallBack(onLoad);
		}
	}

	/// <summary>
	/// 全アセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadAllAssets(string assetBundleName, Action<UnityEngine.Object[]> onLoad)
	{
		var data = this.dataList[assetBundleName];
		var request = data.FindRequestData<AllAssetsRequest>();

		//初めての読み込みリクエスト
		if (request == null)
		{
			//リクエスト作成
			data.requestList.Add(new AllAssetsRequest(onLoad));
			//アセットバンドル読み込み
			this.UpdateAssetBundleData(data);
		}
		//ロード済み
		else if (request.isLoaded)
		{
			//コールバック実行
			if (onLoad != null)
			{
				onLoad(request.allAssets);
			}
		}
		//ロード中
		else
		{
			//コールバック追加
			request.AddCallBack(onLoad);
		}
	}

	/// <summary>
	/// サブアセット読み込み
	/// </summary>
	/// <param name="assetBundleName">アセットバンドル名</param>
	/// <param name="assetName">読み込むアセット名</param>
	/// <param name="onLoad">読み込み完了時コールバック</param>
	public void LoadAssetWithSubAssets(string assetBundleName, string assetName, Action<UnityEngine.Object[]> onLoad)
	{
		var data = this.dataList[assetBundleName];
		var request = data.FindRequestData<SubAssetsRequest>();

		//初めての読み込みリクエスト
		if (request == null)
		{
			//リクエスト作成
			data.requestList.Add(new SubAssetsRequest(assetName, onLoad));
			//アセットバンドル読み込み
			this.UpdateAssetBundleData(data);
		}
		//ロード済み
		else if (request.isLoaded)
		{
			//コールバック実行
			if (onLoad != null)
			{
				onLoad(request.allAssets);
			}
		}
		//ロード中
		else
		{
			//コールバック追加
			request.AddCallBack(onLoad);
		}
	}

	/// <summary>
	/// アセットバンドルの状態に応じた処理
	/// </summary>
	private void UpdateAssetBundleData(AssetBundleData data)
	{
		switch (data.GetStatus())
		{
		//ダウンロードが必要
		case AssetBundleData.Status.isNeedDownload:
		{
			//ダウンロード開始
			StartCoroutine(data.DownloadAssetBundle(this.serverAssetBundleDirectoryUrl, () =>
			{
				this.dataList.Save();
				this.UpdateAssetBundleData(data);
			}));
		}
		break;

		//ダウンロード済み
		case AssetBundleData.Status.isDownloaded:
		{
			//読み込み開始
			StartCoroutine(data.LoadAssetBundle(() =>
			{
				this.UpdateAssetBundleData(data);
			}));
		}
		break;

		//読み込み済み
		case AssetBundleData.Status.isLoaded:
		{
			for (int i = 0, imax = data.requestList.Count; i < imax; i++)
			{
				var request = data.requestList[i];
				if (request.isNeedLoad)
				{
					//必要なアセットの読み込みを開始する
					StartCoroutine(request.Load(data.assetBundle));
				}
			}
		}
		break;
		}
	}
}

}//namespace MushaEngine