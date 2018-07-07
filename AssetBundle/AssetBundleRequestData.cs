using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaEngine {
public partial class AssetBundleLoader : MonoBehaviour {

/// <summary>
/// アセット読み込みリクエスト基底
/// </summary>
protected abstract class AssetBundleRequestData
{
	/// <summary>
	/// AssetBundleRequest
	/// </summary>
	private AssetBundleRequest request = null;
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	private Action onLoad = null;
	/// <summary>
	/// 読み込むアセット名
	/// </summary>
	public string assetName { get; protected set; }
	/// <summary>
	/// 読み込みが必要かどうか
	/// </summary>
	public bool isNeedLoad
	{
		get { return this.request == null; }
	}
	/// <summary>
	/// ロード済みか
	/// </summary>
	public bool isLoaded
	{
		get { return this.request != null && this.request.isDone; }
	}
	/// <summary>
	/// 読み込んだアセット
	/// </summary>
	public UnityEngine.Object asset
	{
		get { return this.isLoaded ? this.request.asset : null; }
	}
	/// <summary>
	/// 読み込んだアセット
	/// </summary>
	public UnityEngine.Object[] allAssets
	{
		get { return this.isLoaded ? this.request.allAssets : null; }
	}

	/// <summary>
	/// コールバック追加
	/// </summary>
	public void AddCallBack(Action<UnityEngine.Object> onLoad)
	{
		if (onLoad != null)
		{
			this.onLoad += () => onLoad(this.asset);
		}
	}

	/// <summary>
	/// コールバック追加
	/// </summary>
	public void AddCallBack(Action<UnityEngine.Object[]> onLoad)
	{
		if (onLoad != null)
		{
			this.onLoad += () => onLoad(this.allAssets);
		}
	}

	/// <summary>
	/// アセット読み込み
	/// </summary>
	public virtual IEnumerator Load(AssetBundle assetBundle)
	{
		Debug.AssertFormat(this.request == null,
			"リクエストは生成済みです。\n" +
			"Type = {0}\n" +
			"AssetBundle.name = {1}\n" +
			"assetName = {2}",
			this.GetType(),
			assetBundle.name,
			this.assetName);

		//アセット読み込みリクエスト生成
		this.request = this.CreateAssetBundleRequest(assetBundle);
		//読み込み待ち
		yield return this.request;
		//コールバック
		if (this.onLoad != null)
		{
			this.onLoad();
			this.onLoad = null;
		}
	}

	/// <summary>
	/// AssetBundleRequest生成
	/// </summary>
	protected abstract AssetBundleRequest CreateAssetBundleRequest(AssetBundle assetBundle);
}

/// <summary>
/// 単体アセット読み込みリクエスト
/// </summary>
protected class AssetRequest : AssetBundleRequestData
{
	/// <summary>
	/// construct
	/// </summary>
	public AssetRequest(string assetName, Action<UnityEngine.Object> onLoad)
	{
		this.assetName = assetName;
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// AssetBundleRequest生成
	/// </summary>
	protected override AssetBundleRequest CreateAssetBundleRequest(AssetBundle assetBundle)
	{
		return assetBundle.LoadAssetAsync(this.assetName);
	}
}

/// <summary>
/// 全体アセット読み込みリクエスト
/// </summary>
protected class AllAssetsRequest : AssetBundleRequestData
{
	/// <summary>
	/// construct
	/// </summary>
	public AllAssetsRequest(Action<UnityEngine.Object[]> onLoad)
	{
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// AssetBundleRequest生成
	/// </summary>
	protected override AssetBundleRequest CreateAssetBundleRequest(AssetBundle assetBundle)
	{
		return assetBundle.LoadAllAssetsAsync();
	}
}

/// <summary>
/// サブアセット読み込みリクエスト
/// </summary>
protected class SubAssetsRequest : AssetBundleRequestData
{
	/// <summary>
	/// construct
	/// </summary>
	public SubAssetsRequest(string assetName, Action<UnityEngine.Object[]> onLoad)
	{
		this.assetName = assetName;
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// AssetBundleRequest生成
	/// </summary>
	protected override AssetBundleRequest CreateAssetBundleRequest(AssetBundle assetBundle)
	{
		return assetBundle.LoadAssetWithSubAssetsAsync(this.assetName);
	}
}

}
}//namespace MushaEngine