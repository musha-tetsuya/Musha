using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MushaEngine {

/// <summary>
/// アセット読み込みタスク基底
/// </summary>
public abstract class AssetLoadTaskBase
{
	/// <summary>
	/// 読み込み中かどうか
	/// </sumamry>
	public bool isLoading { get; private set; }
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
	/// construct
	/// </summary>
	protected AssetLoadTaskBase(string assetBundleName, string assetName, Type assetType)
	{
		this.assetBundleName = assetBundleName;
		this.assetName = assetName;
		this.assetType = assetType;
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
		this.isLoading = true;
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
			EditorGUILayout.Popup(this.isLoading ? 1 : 0, new string[] { "None", "IsLoading" }, GUILayout.Width(100));
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
	public AssetLoadTask(string assetBundleName, string assetName, Action<T> onLoad)
		: base(assetBundleName, assetName, typeof(T))
	{
		this.onLoad = onLoad;
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
	public AllAssetsLoadTask(string assetBundleName, Action<T[]> onLoad)
		: base(assetBundleName, null, typeof(T))
	{
		this.onLoad = onLoad;
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
	public SubAssetsLoadTask(string assetBundleName, string assetName, Action<T[]> onLoad)
		: base(assetBundleName, assetName, typeof(T))
	{
		this.onLoad = onLoad;
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


}//namespace MushaEngine