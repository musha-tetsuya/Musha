using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MushaSystem {
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
	/// アセットタイプ
	/// </summary>
	public Type type { get; private set; }
	/// <summary>
	/// アセット名
	/// </summary>
	public string assetName { get; private set; }
	/// <summary>
	/// AssetBundleRequest
	/// </summary>
	private AssetBundleRequest request = null;
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	private Action onLoad = null;

	/// <summary>
	/// construct
	/// </summary>
	protected AssetOperationBase(string assetName, Type type)
	{
		this.assetName = assetName;
		this.type = type;
	}

	/// <summary>
	/// destruct
	/// </summary>
	~AssetOperationBase()
	{
		this.type = null;
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
		Debug.AssertFormat(this.GetStatus() == Status.isLoaded, "読み込みが完了していません。{0}:{1}", this.GetType(), this.assetName);
		Debug.AssertFormat(this.request.asset != null, "{0}は{1}型のアセットではありません。", this.assetName, typeof(T));
		return (T)this.request.asset;
	}

	/// <summary>
	/// アセット配列取得
	/// </summary>
	public T[] GetAllAssets<T>() where T : UnityEngine.Object
	{
		Debug.AssertFormat(this.GetStatus() == Status.isLoaded, "読み込みが完了していません。{0}:{1}", this.GetType(), this.assetName);
		Debug.AssertFormat(this.request.allAssets.Length > 0, "{0}型のアセットが含まれていません。{1}:{2}", typeof(T), this.GetType(), this.assetName);
		return Array.ConvertAll(this.request.allAssets, x => x as T);
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

#if UNITY_EDITOR
	#region InspectorGUI
	/// <summary>
	/// InspectorGUI：折り畳み表示用
	/// </summary>
	/// <remarks>Editor Only</remarks>
	protected bool foldout = false;

	/// <summary>
	/// InspectorGUI描画
	/// </summary>
	/// <remarks>Editor Only</remarks>
	public void OnInspectorGUI(int index)
	{
		GUILayout.BeginHorizontal();
		{
			string typeName = this.GetType().Name.Replace("`1", null) + string.Format("<{0}>", this.type.Name);
			this.foldout = EditorGUILayout.Foldout(this.foldout, string.Format("{0}:{1}", index, typeName));
			EditorGUILayout.EnumPopup(this.GetStatus(), GUILayout.Width(120));
		}
		GUILayout.EndHorizontal();

		if (this.foldout)
		{
			if (this.GetStatus() == Status.isLoaded)
			{
				EditorGUILayout.TextField(this.assetName);
				foreach (var asset in this.request.allAssets)
				{
					EditorGUILayout.TextField(asset.GetType().Name, asset.name);
				}
			}
			else
			{
				EditorGUILayout.LabelField("Not Loaded.");
			}
		}
	}
	#endregion
#endif
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
		: base(assetName, typeof(T))
	{
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
		: base(null, typeof(T))
	{
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
		: base(assetName, typeof(T))
	{
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
}//namespace MushaSystem