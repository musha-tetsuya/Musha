using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MushaSystem {

/// <summary>
/// アセット読み込みタスク基底
/// </summary>
public abstract class AssetLoadTaskBase
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
	/// アセットバンドル名
	/// </summary>
	public string assetBundleName { get; private set; }
	/// <summary>
	/// アセット名
	/// </summary>
	public string assetName { get; private set; }
	/// <summary>
	/// アセットタイプ
	/// </summary>
	public Type assetType { get; private set; }
	/// <summary>
	/// 状態
	/// </summary>
	public Status status { get; private set; }

	/// <summary>
	/// construct
	/// </summary>
	protected AssetLoadTaskBase(string assetBundleName, string assetName, Type assetType)
	{
		this.assetBundleName = assetBundleName;
		this.assetName = assetName;
		this.assetType = assetType;
		this.AddCallBack(() => this.status = Status.isLoaded);
	}

	/// <summary>
	/// destruct
	/// </summary>
	~AssetLoadTaskBase()
	{
		this.assetBundleName = null;
		this.assetName = null;
		this.assetType = null;
	}

	/// <summary>
	/// コールバック追加
	/// </summary>
	public abstract void AddCallBack(Action action);

	/// <summary>
	/// 読み込み処理
	/// </summary>
	public virtual void Load(AssetBundleLoader loader)
	{
		this.status = Status.isLoading;
	}

#if UNITY_EDITOR
	#region インスペクター表示
	/// <summary>
	/// InspectorGUI：アセット名折り畳み表示用
	/// </summary>
	/// <remarks>Editor Only</remarks>
	private bool foldout = false;

	/// <summary>
	/// InspectorGUI描画
	/// </summary>
	/// <remarks>Editor Only</remarks>
	public void OnInspectorGUI(int index)
	{
		GUILayout.BeginHorizontal();
		{
			string typeName = this.GetType().Name.Replace("`1", null) + string.Format("<{0}>", this.assetType.Name);
			this.foldout = EditorGUILayout.Foldout(this.foldout, string.Format("{0}:{1}", index, typeName));
			EditorGUILayout.EnumPopup(this.status, GUILayout.Width(100));
		}
		GUILayout.EndHorizontal();

		if (this.foldout)
		{
			EditorGUI.indentLevel++;
			EditorGUILayout.TextField("AssetBundleName", this.assetBundleName);
			EditorGUILayout.TextField("AssetName", this.assetName);
		}
	}
	#endregion
#endif
}

/// <summary>
/// 単体アセット読み込みタスク
/// </summary>
public class AssetLoadTask<T> : AssetLoadTaskBase where T : UnityEngine.Object
{
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	private Action<T> onLoad = null;

	/// <summary>
	/// construct
	/// </summary>
	public AssetLoadTask(string assetBundleName, string assetName, Action<T> onLoad = null)
		: base(assetBundleName, assetName, typeof(T))
	{
		if (onLoad != null)
		{
			this.onLoad += onLoad;
		}
	}

	/// <summary>
	/// destruct
	/// </summary>
	~AssetLoadTask()
	{
		this.onLoad = null;
	}

	/// <summary>
	/// コールバック追加
	/// </summary>
	public override void AddCallBack(Action action)
	{
		this.onLoad += (asset) => action();
	}

	/// <summary>
	/// 読み込み処理
	/// </summary>
	public override void Load(AssetBundleLoader loader)
	{
		base.Load(null);
		loader.LoadAsset<T>(this.assetBundleName, this.assetName, this.onLoad);
	}
}

/// <summary>
/// 全体アセット読み込みタスク
/// </summary>
public class AllAssetsLoadTask<T> : AssetLoadTaskBase where T : UnityEngine.Object
{
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	private Action<T[]> onLoad = null;

	/// <summary>
	/// construct
	/// </summary>
	public AllAssetsLoadTask(string assetBundleName, Action<T[]> onLoad = null)
		: base(assetBundleName, null, typeof(T))
	{
		if (onLoad != null)
		{
			this.onLoad += onLoad;
		}
	}

	/// <summary>
	/// destruct
	/// </summary>
	~AllAssetsLoadTask()
	{
		this.onLoad = null;
	}

	/// <summary>
	/// コールバック追加
	/// </summary>
	public override void AddCallBack(Action action)
	{
		this.onLoad += (assets) => action();
	}

	/// <summary>
	/// 読み込み処理
	/// </summary>
	public override void Load(AssetBundleLoader loader)
	{
		base.Load(null);
		loader.LoadAllAssets<T>(this.assetBundleName, this.onLoad);
	}
}

/// <summary>
/// サブアセット読み込みタスク
/// </summary>
public class SubAssetsLoadTask<T> : AssetLoadTaskBase where T : UnityEngine.Object
{
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	private Action<T[]> onLoad = null;

	/// <summary>
	/// construct
	/// </summary>
	public SubAssetsLoadTask(string assetBundleName, string assetName, Action<T[]> onLoad = null)
		: base(assetBundleName, assetName, typeof(T))
	{
		if (onLoad != null)
		{
			this.onLoad += onLoad;
		}
	}

	/// <summary>
	/// destruct
	/// </summary>
	~SubAssetsLoadTask()
	{
		this.onLoad = null;
	}

	/// <summary>
	/// コールバック追加
	/// </summary>
	public override void AddCallBack(Action action)
	{
		this.onLoad += (assets) => action();
	}

	/// <summary>
	/// 読み込み処理
	/// </summary>
	public override void Load(AssetBundleLoader loader)
	{
		base.Load(null);
		loader.LoadSubAssets<T>(this.assetBundleName, this.assetName, this.onLoad);
	}
}

/// <summary>
/// シーンアセット読み込みタスク
/// </summary>
public class SceneAssetLoadTask : AssetLoadTaskBase
{
	/// <summary>
	/// 読み込み完了時コールバック
	/// </summary>
	private Action<string[]> onLoad = null;

	/// <summary>
	/// construct
	/// </summary>
	public SceneAssetLoadTask(string assetBundleName, Action<string[]> onLoad = null)
		: base(assetBundleName, null, typeof(string[]))
	{
		if (onLoad != null)
		{
			this.onLoad += onLoad;
		}
	}

	/// <summary>
	/// destruct
	/// </summary>
	~SceneAssetLoadTask()
	{
		this.onLoad = null;
	}

	/// <summary>
	/// コールバック追加
	/// </summary>
	public override void AddCallBack(Action action)
	{
		this.onLoad += (scenePaths) => action();
	}

	/// <summary>
	/// 読み込み処理
	/// </summary>
	public override void Load(AssetBundleLoader loader)
	{
		base.Load(null);
		loader.LoadScenePaths(this.assetBundleName, this.onLoad);
	}
}

}//namespace MushaSystem