using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha
{
	/// <summary>
	/// システム
	/// </summary>
	public class Sys : MonoBehaviour
	{
		public static Sys Instance { get; private set; }
		//----  field   -----------------------------------------------------------------------------------
		public static CanvasCreator OverlayCanvas { get; private set; }
		public static Touch Touch { get; private set; }
		public static AssetManager AssetManager { get; private set; }
		public static Fade Fade { get; private set; }
		public static float UpdateStartTime { get; private set; }
		public static float TimeSinceUpdateStart { get { return Time.realtimeSinceStartup - UpdateStartTime; } }
		public static float RealAspect { get; private set; }
		public static Rect ViewportRect;
		private string RestartSceneName;
		//----  method  -----------------------------------------------------------------------------------
		/// <summary>
		/// 生成
		/// </summary>
		public static void Create()
		{
			if (Instance == null)
			{
				GameObject gobj = new GameObject("PNS.Sys");
				gobj.AddComponent<Sys>();
				DontDestroyOnLoad(gobj);
			}
		}
		/// <summary>
		/// 起動時
		/// </summary>
		private void Awake()
		{
			Debug.AssertFormat(Instance == null, GetType() + "::Instance is already exist.");
			Instance = this;
#if UNITY_EDITOR
			//ローカルデータの保存場所作成
			System.IO.Directory.CreateDirectory(Define.LOCAL_DATA_PATH.Remove(Define.LOCAL_DATA_PATH.Length - 1));
#endif
			//カメラの描画範囲設定
			Camera.onPreRender += (cam => cam.rect = ViewportRect);

			#region 背景塗り潰し設定
			{
				Camera cam = new GameObject("BgCamera").AddComponent<Camera>();
				cam.transform.SetParent(transform);
				cam.clearFlags = CameraClearFlags.SolidColor;
#if !UNITY_EDITOR
				cam.backgroundColor = Color.black;
#endif
				cam.cullingMask = 0;
				cam.depth = -100;
			}
			#endregion

			//オーバーレイキャンバス
			OverlayCanvas = new GameObject("OverlayCanvas").AddComponent<CanvasCreator>();
			OverlayCanvas.transform.SetParent(transform);
			OverlayCanvas.mCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
			//タッチ管理
			Touch = gameObject.AddComponent<Touch>();
			//アセット管理
			AssetManager = gameObject.AddComponent<AssetManager>();
			//フェード
			Fade = new GameObject("Fade").AddComponent<Fade>();
			Fade.transform.SetParent(transform);
			Fade.mCamera.depth = Define.CAMERADEPTH_SYSFADE;
		}
		/// <summary>
		/// 破棄
		/// </summary>
		private void OnDestroy()
		{
			Instance = null;
			if (!string.IsNullOrEmpty(RestartSceneName))
			{
				SceneChange(RestartSceneName);
			}
		}
		/// <summary>
		/// 処理
		/// </summary>
		private void Update()
		{
			UpdateStartTime = Time.realtimeSinceStartup;

			CalcViewportRect();
			Touch.Run();
			AssetManager.Run();
		}
		/// <summary>
		/// 描画範囲計算
		/// </summary>
		private void CalcViewportRect()
		{
			//実機のアスペクト比計算
			RealAspect = (float)Screen.width / Screen.height;
			
			//実機の縦幅が大きいので上下に黒帯
			if (RealAspect < Define.ASPECT)
			{
				ViewportRect.width = 1;
				ViewportRect.height = RealAspect / Define.ASPECT;
				ViewportRect.x = 0;
				ViewportRect.y = (1 - ViewportRect.height) * 0.5f;
			}
			//実機の横幅が大きいので左右に黒帯
			else
			{
				ViewportRect.width = Define.ASPECT / RealAspect;
				ViewportRect.height = 1;
				ViewportRect.x = (1 - ViewportRect.width) * 0.5f;
				ViewportRect.y = 0;
			}
		}
		/// <summary>
		/// シーン切り替え
		/// </summary>
		public static void SceneChange(string sceneName)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
		}
		/// <summary>
		/// 再起動
		/// </summary>
		public static void Restart(string restartSceneName)
		{
			if (Instance != null)
			{
				Instance.RestartSceneName = restartSceneName;
				Destroy(Instance.gameObject);
			}
		}
	}
}
