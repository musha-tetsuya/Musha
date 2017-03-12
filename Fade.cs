using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha
{
	/// <summary>
	/// フェード
	/// </summary>
	[RequireComponent(typeof(Camera))]
	public class Fade : MonoBehaviour
	{
		//----	field	-----------------------------------------------------------------------------------
		private bool mFadeIn;
		private float mTimeCount;
		private float mTimeMax;
		private Texture2D mTex;
		private Material mMat;
		private Color mColor;
		public Camera mCamera { get; private set; }
		private System.Action OnEnd;
		//----	method	-----------------------------------------------------------------------------------
		/// <summary>
		/// 起動時
		/// </summary>
		private void Awake()
		{
			mCamera = GetComponent<Camera>();
			mCamera.clearFlags = CameraClearFlags.Depth;
			mCamera.cullingMask = 0;
			mCamera.enabled = false;
			Camera.onPostRender += Run;

			var shader = Shader.Find("Particles/Alpha Blended");
			mMat = new Material(shader);
			mTex = new Texture2D(1, 1);
			mTex.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f));
			mTex.Apply();
			mMat.color = Color.black;
			mColor = Color.black;
		}
		/// <summary>
		/// 処理
		/// </summary>
		private void Run(Camera cam)
		{
			if (cam == mCamera)
			{
				float t = Mathf.Clamp01(mTimeCount / mTimeMax);
				mColor.a = mFadeIn ? 1f - t : t;
				mMat.SetColor("_TintColor", mColor);
				Graphics.Blit(mTex, mMat);
				if (mTimeCount < mTimeMax)
				{
					mTimeCount += Time.deltaTime;
				}
				else if (OnEnd != null)
				{
					OnEnd();
					OnEnd = null;
				}
			}
		}
		/// <summary>
		/// フェードイン開始
		/// </summary>
		public void In(float time = 0.5f, System.Action onEnd = null)
		{
			Set(true, time, onEnd + (() => { mCamera.enabled = false; }));
		}
		/// <summary>
		/// フェードアウト開始
		/// </summary>
		public void Out(float time = 0.5f, System.Action onEnd = null)
		{
			Set(false, time, onEnd);
		}
		/// <summary>
		/// フェード設定
		/// </summary>
		private void Set(bool fadeIn, float time, System.Action onEnd)
		{
			mFadeIn = fadeIn;
			mCamera.enabled = true;
			mTimeCount = (time <= 0f) ? 1f : 0f;
			mTimeMax = (time <= 0f) ? 1f : time;
			OnEnd = onEnd;
		}
	}
}
