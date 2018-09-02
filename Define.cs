using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha {

/// <summary>
/// Define
/// </summary>
public class Define
{
	/// <summary>
	/// ローカルデータディレクトリ名
	/// </summary>
	public const string localDataDirectoryName = "LocalData";

	/// <summary>
	/// アセットバンドルディレクトリ名
	/// </summary>
	public const string assetBundleDirectoryName =
#if UNITY_ANDROID
		"AssetBundleAndroid";
#elif UNITY_IOS
		"AssetBundleIos";
#elif UNITY_STANDALONE_WIN
		"AssetBundleWindows";
#elif UNITY_STANDALONE_OSX
		"AssetBundleOSX";
#endif

	/// <summary>
	/// ローカルデータディレクトリパス
	/// </summary>
	private static string localDataDirectoryPath = null;
	/// <summary>
	/// ローカルデータディレクトリパス取得
	/// </summary>
	public static string GetLocalDataDirectoryPath()
	{
		return localDataDirectoryPath ?? (localDataDirectoryPath =
#if UNITY_EDITOR
			Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets")) + localDataDirectoryName
#elif UNITY_STANDALONE
			Application.dataPath + "/" + localDataDirectoryName
#elif UNITY_ANDROID
			Application.persistentDataPath + "/" + localDataDirectoryName
#elif UNITY_IOS
			Application.persistentDataPath + "/" + localDataDirectoryName
#endif
			);
	}

	/// <summary>
	/// ローカルのアセットバンドルディレクトリパス
	/// </summary>
	private static string localAssetBundleDirectoryPath = null;
	/// <summary>
	/// ローカルのアセットバンドルディレクトリパス取得
	/// </summary>
	public static string GetLocalAssetBundleDirectoryPath()
	{
		return localAssetBundleDirectoryPath ?? (localAssetBundleDirectoryPath = GetLocalDataDirectoryPath() + "/" + assetBundleDirectoryName);
	}
}

}//namespace Musha
