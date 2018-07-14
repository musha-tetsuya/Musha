using System;
using UnityEngine;

namespace MushaEngine {
public partial class AssetBundleLoader : MonoBehaviour {

/// <summary>
/// アセット管理クラス
/// </summary>
protected abstract class AssetOperationBase
{
	/// <summary>
	/// 状態
	/// </summary>
	public enum Status
	{
		None,
		isLoading,
		isLoaded,
	}

	/// <summary>
	/// アセット名
	/// </summary>
	public string assetName { get; protected set; }
	/// <summary>
	/// AssetBundleRequest
	/// </summary>
	private AssetBundleRequest request = null;
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	private Action onLoad = null;

	/// <summary>
	/// destruct
	/// </summary>
	~AssetOperationBase()
	{	
		this.assetName = null;
		this.request = null;
		this.onLoad = null;
	}

	/// <summary>
	/// 状態取得
	/// </summary>
	public Status GetStatus()
	{
		return this.request == null ? Status.None
			 : this.request.isDone  ? Status.isLoaded
			 :						  Status.isLoading;
	}

	/// <summary>
	/// 単体アセット取得
	/// </summary>
	public T GetAsset<T>() where T : UnityEngine.Object
	{
		return (this.GetStatus() == Status.isLoaded) ? (T)this.request.asset : null;
	}

	/// <summary>
	/// アセット配列取得
	/// </summary>
	public T[] GetAllAssets<T>() where T : UnityEngine.Object
	{
		return (this.GetStatus() == Status.isLoaded) ? Array.ConvertAll(this.request.allAssets, x => x as T) : null;
	}

	/// <summary>
	/// 読み込み完了時コールバックの追加
	/// </summary>
	public void AddCallBack<T>(Action<T> onLoad) where T : UnityEngine.Object
	{
		if (onLoad != null)
		{
			this.onLoad += () => onLoad(this.GetAsset<T>());
		}
	}

	/// <summary>
	/// 読み込み完了時コールバックの追加
	/// </summary>
	public void AddCallBack<T>(Action<T[]> onLoad) where T : UnityEngine.Object
	{
		if (onLoad != null)
		{
			this.onLoad += () => onLoad(this.GetAllAssets<T>());
		}
	}

	/// <summary>
	/// 読み込み開始
	/// </summary>
	public void Load(AssetBundle assetBundle)
	{
		if (this.request == null && this.CreateAssetBundleRequest(assetBundle, out this.request))
		{
			this.request.completed += (op) =>
			{
				if (this.onLoad != null)
				{
					this.onLoad();
					this.onLoad = null;
				}
			};
		}
	}

	/// <summary>
	/// AssetBundleRequestの作成
	/// </summary>
	protected abstract bool CreateAssetBundleRequest(AssetBundle assetBundle, out AssetBundleRequest request);
}

/// <summary>
/// 単体アセット管理クラス
/// </summary>
protected class AssetOperation<T> : AssetOperationBase where T : UnityEngine.Object
{
	/// <summary>
	/// construct
	/// </summary>
	public AssetOperation(string assetName, Action<T> onLoad)
	{
		this.assetName = assetName;
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// AssetBundleRequestの作成
	/// </summary>
	protected override bool CreateAssetBundleRequest(AssetBundle assetBundle, out AssetBundleRequest request)
	{
		if (assetBundle.Contains(this.assetName))
		{
			request = assetBundle.LoadAssetAsync<T>(this.assetName);
			return true;
		}
		else
		{
			Debug.LogWarningFormat("AssetBundle={0}にassetName={1}は含まれていません", assetBundle.name, this.assetName);
			request = null;
			return false;
		}
	}
}

/// <summary>
/// 全体アセット管理クラス
/// </summary>
protected class AllAssetsOperation<T> : AssetOperationBase where T : UnityEngine.Object
{
	/// <summary>
	/// construct
	/// </summary>
	public AllAssetsOperation(Action<T[]> onLoad)
	{
		this.assetName = null;
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// AssetBundleRequestの作成
	/// </summary>
	protected override bool CreateAssetBundleRequest(AssetBundle assetBundle, out AssetBundleRequest request)
	{
		request = assetBundle.LoadAllAssetsAsync<T>();
		return true;
	}
}

/// <summary>
/// サブアセット管理クラス
/// </summary>
protected class SubAssetsOperation<T> : AssetOperationBase where T : UnityEngine.Object
{
	/// <summary>
	/// construct
	/// </summary>
	public SubAssetsOperation(string assetName, Action<T[]> onLoad)
	{
		this.assetName = assetName;
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// AssetBundleRequestの作成
	/// </summary>
	protected override bool CreateAssetBundleRequest(AssetBundle assetBundle, out AssetBundleRequest request)
	{
		if (assetBundle.Contains(this.assetName))
		{
			request = assetBundle.LoadAssetWithSubAssetsAsync<T>(this.assetName);
			return true;
		}
		else
		{
			Debug.LogWarningFormat("AssetBundle={0}にassetName={1}は含まれていません", assetBundle.name, this.assetName);
			request = null;
			return false;
		}
	}
}

}
}//namespace MushaEngine