using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaSystem {

/// <summary>
/// スクリーンマネージャ
/// </summary>
[AddComponentMenu("MushaSystem/UI/ScreenManager")]
public class ScreenManager : MonoBehaviour
{
	/// <summary>
	/// 横持ちベゼルレスのセーフエリアアスペクト比(iPhoneX)
	/// </summary>
	private const float LANDSCAPE_BEZELLESS_ASPECT = 724f / 354f;
	/// <summary>
	/// 縦持ちベゼルレスのセーフエリアアスペクト比(iPhoneX)
	/// </summary>
	private const float PORTRAIT_BEZELLESS_ASPECT = 375f / 734f;
	/// <summary>
	/// インスタンス（シングルトン）
	/// </summary>
	private static ScreenManager instance = null;
	/// <summary>
	/// 画面サイズ変更時イベント
	/// </summary>
	private event Action onChangeScreenSize = null;
	/// <summary>
	/// １フレーム前の実機画面サイズ
	/// </summary>
	private Vector2? beforeRealScreenSize = null;

	/// <summary>
	/// インスタンス生成
	/// </summary>
	public static void CreateInstance()
	{
		if (instance == null)
		{
			new GameObject(typeof(ScreenManager).Name, typeof(ScreenManager));
		}
	}

	/// <summary>
	/// Awake
	/// </summary>
	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(this.gameObject);
		}
		else
		{
			Destroy(this.gameObject);
		}
	}

	/// <summary>
	/// Update
	/// </summary>
	private void Update()
	{
		//現在の実機画面サイズ
		Vector2 realScreenSize = GetRealScreenSize();

		//１フレーム前と実機画面サイズに変化があったか？
		if (realScreenSize != this.beforeRealScreenSize)
		{
			//実機画面サイズを保存
			this.beforeRealScreenSize = realScreenSize;

			//画面サイズ変化時イベント発行
			this.onChangeScreenSize.SafetyInvoke();
		}
	}

	/// <summary>
	/// 画面サイズ変化時イベントを登録する
	/// </summary>
	public static void AddChangeScreenSizeEvent(Action callback)
	{
		CreateInstance();
		instance.onChangeScreenSize += callback;
	}

	/// <summary>
	/// 画面サイズ変化時イベントを除去する
	/// </summary>
	public static void RemoveCangeScreenSizeEvent(Action callback)
	{
		if (instance != null)
		{
			instance.onChangeScreenSize -= callback;
		}
	}

	/// <summary>
	/// 実機画面サイズ取得
	/// </summary>
	public static Vector2 GetRealScreenSize()
	{
		var size = new Vector2(Screen.width, Screen.height);
#if UNITY_EDITOR
		string[] res = UnityEditor.UnityStats.screenRes.Split('x');
		size.x = int.Parse(res[0]);
		size.y = int.Parse(res[1]);
#endif
		return size;
	}

	/// <summary>
	/// セーフエリア取得
	/// </summary>
	public static Rect GetSafeArea()
	{
		Rect safeArea = Screen.safeArea;
#if UNITY_EDITOR
		Vector2 realScreenSize = GetRealScreenSize();
		float realAspect = realScreenSize.x / realScreenSize.y;

		//横持ちベゼルレスの場合
		if (realAspect > LANDSCAPE_BEZELLESS_ASPECT)
		{
			safeArea.size *= (LANDSCAPE_BEZELLESS_ASPECT / realAspect);
			safeArea.position = new Vector2((realScreenSize.x - safeArea.width) * 0.5f, (realScreenSize.y - safeArea.height));
		}
		//縦持ちベゼルレスの場合
		else if (realAspect < PORTRAIT_BEZELLESS_ASPECT)
		{
			safeArea.size = new Vector2(safeArea.width, safeArea.height * (realAspect / PORTRAIT_BEZELLESS_ASPECT));
			safeArea.position = new Vector2(0f, (realScreenSize.y - safeArea.height) * 0.5f);
		}
#endif
		return safeArea;
	}
}

}//namespace MushaSystem