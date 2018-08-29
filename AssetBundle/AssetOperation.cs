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
	protected AssetBundleRequest request = null;
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	protected Action onLoad = null;

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
	public virtual Status GetStatus()
	{
		return this.request == null ? Status.None
			 : this.request.isDone  ? Status.isLoaded
			 :						  Status.isLoading;
	}

	/// <summary>
	/// 読み込み開始
	/// </summary>
	public abstract void Load(AssetBundle assetBundle);

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
				if (this is SceneAssetOperation)
				{
					string[] allScenePaths = (this as SceneAssetOperation).GetAllScenePaths();
					foreach (var scenePath in allScenePaths)
					{
						EditorGUILayout.TextField(typeof(string).Name, scenePath);
					}
				}
				else
				{
					EditorGUILayout.TextField(this.assetName);
					foreach (var asset in this.request.allAssets)
					{
						EditorGUILayout.TextField(asset.GetType().Name, asset.name);
					}
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
	/// 単体アセット取得
	/// </summary>
	public T GetAsset()
	{
		Debug.AssertFormat(this.GetStatus() == Status.isLoaded, "読み込みが完了していません。{0}:{1}", this.GetType(), this.assetName);
		Debug.AssertFormat(this.request.asset != null, "{0}は{1}型のアセットではありません。", this.assetName, typeof(T));
		return (T)this.request.asset;
	}

	/// <summary>
	/// 読み込み完了時コールバックの追加
	/// </summary>
	public void AddCallBack(Action<T> onLoad)
	{
		if (onLoad != null)
		{
			this.onLoad += () => onLoad(this.GetAsset());
		}
	}

	/// <summary>
	/// 読み込み開始
	/// </summary>
	public override void Load(AssetBundle assetBundle)
	{
		if (this.request == null)
		{
			if (assetBundle.Contains(this.assetName))
			{
				this.request = assetBundle.LoadAssetAsync<T>(this.assetName);
				this.request.completed += (op) =>
				{
					this.onLoad.SafetyInvoke();
					this.onLoad = null;
				};
			}
			else
			{
				Debug.LogWarningFormat("AssetBundle={0}にassetName={1}は含まれていません", assetBundle.name, this.assetName);
			}
		}
	}
}

/// <summary>
/// アセット配列管理クラス
/// </summary>
protected abstract class AssetsOperation<T> : AssetOperationBase where T : UnityEngine.Object
{
	/// <summary>
	/// construct
	/// </summary>
	protected AssetsOperation(string assetName, Action<T[]> onLoad)
		: base(assetName, typeof(T))
	{
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// アセット配列取得
	/// </summary>
	public T[] GetAllAssets()
	{
		Debug.AssertFormat(this.GetStatus() == Status.isLoaded, "読み込みが完了していません。{0}:{1}", this.GetType(), this.assetName);
		Debug.AssertFormat(this.request.allAssets.Length > 0, "{0}型のアセットが含まれていません。{1}:{2}", typeof(T), this.GetType(), this.assetName);
		return Array.ConvertAll(this.request.allAssets, x => x as T);
	}

	/// <summary>
	/// 読み込み完了時コールバックの追加
	/// </summary>
	public void AddCallBack(Action<T[]> onLoad)
	{
		if (onLoad != null)
		{
			this.onLoad += () => onLoad(this.GetAllAssets());
		}
	}
}

/// <summary>
/// 全体アセット管理クラス
/// </summary>
protected class AllAssetsOperation<T> : AssetsOperation<T> where T : UnityEngine.Object
{
	/// <summary>
	/// construct
	/// </summary>
	public AllAssetsOperation(Action<T[]> onLoad)
		: base(null, onLoad)
	{
	}

	/// <summary>
	/// 読み込み開始
	/// </summary>
	public override void Load(AssetBundle assetBundle)
	{
		if (this.request == null)
		{
			this.request = assetBundle.LoadAllAssetsAsync<T>();
			this.request.completed += (op) =>
			{
				this.onLoad.SafetyInvoke();
				this.onLoad = null;
			};
		}
	}
}

/// <summary>
/// サブアセット管理クラス
/// </summary>
protected class SubAssetsOperation<T> : AssetsOperation<T> where T : UnityEngine.Object
{
	/// <summary>
	/// construct
	/// </summary>
	public SubAssetsOperation(string assetName, Action<T[]> onLoad)
		: base(assetName, onLoad)
	{
	}

	/// <summary>
	/// 読み込み開始
	/// </summary>
	public override void Load(AssetBundle assetBundle)
	{
		if (this.request == null)
		{
			if (assetBundle.Contains(this.assetName))
			{
				this.request = assetBundle.LoadAssetWithSubAssetsAsync<T>(this.assetName);
				this.request.completed += (op) =>
				{
					this.onLoad.SafetyInvoke();
					this.onLoad = null;
				};
			}
			else
			{
				Debug.LogWarningFormat("AssetBundle={0}にassetName={1}は含まれていません", assetBundle.name, this.assetName);
			}
		}
	}
}

/// <summary>
/// シーンアセット管理クラス
/// </summary>
protected class SceneAssetOperation : AssetOperationBase
{
	/// <summary>
	/// シーンパス配列
	/// </summary>
	private string[] allScenePaths = null;

	/// <summary>
	/// construct
	/// </summary>
	public SceneAssetOperation(Action<string[]> onLoad)
		: base(null, typeof(string[]))
	{
		this.AddCallBack(onLoad);
	}

	/// <summary>
	/// desturct
	/// </summary>
	~SceneAssetOperation()
	{
		this.allScenePaths = null;
	}

	/// <summary>
	/// 状態取得
	/// </summary>
	public override Status GetStatus()
	{
		return (this.allScenePaths == null) ? Status.isLoading : Status.isLoaded;
	}

	/// <summary>
	/// シーンパス配列取得
	/// </summary>
	public string[] GetAllScenePaths()
	{
		return this.allScenePaths;
	}

	/// <summary>
	/// 読み込み完了時コールバックの追加
	/// </summary>
	public void AddCallBack(Action<string[]> onLoad)
	{
		if (onLoad != null)
		{
			this.onLoad += () => onLoad(this.allScenePaths);
		}
	}

	/// <summary>
	/// 読み込み開始
	/// </summary>
	public override void Load(AssetBundle assetBundle)
	{
		if (this.allScenePaths == null)
		{
			this.allScenePaths = assetBundle.GetAllScenePaths();
			this.onLoad.SafetyInvoke();
			this.onLoad = null;
		}
	}
}

}
}//namespace MushaSystem