using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaSystem {

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

}//namespace MushaEngine

namespace Musha {

/// <summary>
/// Define
/// partialでstaticコンストラクタを定義し変数値変更が可能
/// </summary>
public partial class Define
{


	//インスタンス
	private static readonly Define Instance = new Define();

	//partialでstaticコンストラクタを定義し変数値の変更が可能
	public static readonly int FRAME_RATE = 30;//フレームレート
	public static readonly Vector2 SCREEN_SIZE = new Vector2(1080f, 1920f);//画面サイズ

	//partialコンストラクタで設定できる変数
	public static float ASPECT = 1f;
	private float DispW = 1080f;
	private float DispH = 1920f;
	private int MaxFinger = 2;
	private float TapCancelRange = 32f;
	private string ServerURL = "http://www.hogehoge.jp/";
	private float DownloadTimeout = 10f;
	private float AssetManagerWorkLimit = 0.5f / 30;

	//partialコンストラクタで設定してはいけない変数
	private string ServerAssetPath = null;
	private string LocalAssetPath = null;
	private string LocalDataPath = null;
	
	/// <summary>
	/// 画面サイズ（横）
	/// </summary>
	public static float DISP_W { get { return Instance.DispW; } }
	/// <summary>
	/// 画面サイズ（縦）
	/// </summary>
	public static float DISP_H { get { return Instance.DispH; } }
	/// <summary>
	/// システムフェードのカメラ深度
	/// </summary>
	public const float CAMERADEPTH_SYSFADE = 90f;
	/// <summary>
	/// 操作を受け付ける指本数
	/// </summary>
	public static int MAX_FINGER { get { return Instance.MaxFinger; } }
	/// <summary>
	/// 指移動値がこの値を超えたらタップをキャンセルする
	/// </summary>
	public static float TAP_CANCEL_RANGE { get { return Instance.TapCancelRange; } }
	/// <summary>
	/// ダウンロードのタイムアウト時間
	/// </summary>
	public static float DOWNLOAD_TIMEOUT { get { return Instance.DownloadTimeout; } }
	/// <summary>
	/// AssetManager処理時間制限
	/// </summary>
	public static float ASSETMANAGER_WORKLIMIT { get { return Instance.AssetManagerWorkLimit; } }
	/// <summary>
	/// サーバーURL
	/// </summary>
	public static string SERVER_URL { get { return Instance.ServerURL; } }
	/// <summary>
	/// リソースバージョン
	/// </summary>
	public static string RESOURCE_VERSION = null;
	/// <summary>
	/// プラットフォーム名
	/// </summary>
	public const string PLATFORM_NAME =
#if UNITY_ANDROID
		"Android";
#elif UNITY_IOS
		"iOS";
#else
		"Windows";
#endif
	/// <summary>
	/// サーバーのアセットパス
	/// </summary>
	public static string SERVER_ASSET_PATH
	{
		get
		{
			return Instance.ServerAssetPath ?? (Instance.ServerAssetPath =
#if INPKG_SERVER && !UNITY_EDITOR && UNITY_ANDROID
				"jar:file://" + Application.dataPath + "!/assets/" + PLATFORM_NAME + "/"
#elif INPKG_SERVER
				"file://" + Application.streamingAssetsPath + "/" + PLATFORM_NAME + "/"
#elif UNITY_EDITOR
				"file://" + Application.dataPath.Replace("Assets", "Server/" + PLATFORM_NAME + "/")
#else
				SERVER_URL + "Assets/" + RESOURCE_VERSION + "/" + PLATFORM_NAME + "/"
#endif
			);
		}
	}
	/// <summary>
	/// ダウンロードしたアセットバンドルの保存場所
	/// </summary>
	public static string LOCAL_ASSET_PATH
	{
		get
		{
			return Instance.LocalAssetPath ?? (Instance.LocalAssetPath =
#if UNITY_EDITOR
				LOCAL_DATA_PATH
#elif UNITY_IOS
				Application.temporaryCachePath + "/"
#else
				Application.persistentDataPath + "/"
#endif
			);
		}
	}
	/// <summary>
	/// ローカルデータの保存先。セーブデータなど。
	/// </summary>
	public static string LOCAL_DATA_PATH
	{
		get
		{
			return Instance.LocalDataPath ?? (Instance.LocalDataPath =
#if UNITY_EDITOR
				Application.dataPath.Replace("Assets", "LocalData/")
#else
				Application.persistentDataPath + "/"
#endif
			);
		}
	}
	/// <summary>
	/// メッセージボックスプレハブのパス
	/// </summary>
	public const string MESSAGE_BOX_RESOURCE_PATH = "prefab/MessageBox";
}

}//namespace Musha
