using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaSystem {

/// <summary>
/// フェード
/// </summary>
[AddComponentMenu("MushaSystem/UI/Fade")]
[RequireComponent(typeof(Camera))]
public class Fade : MonoBehaviour
{
	/// <summary>
	/// フェード時間の長さ
	/// </summary>
	[SerializeField]public float duration = 0.5f;
	/// <summary>
	/// フェード色
	/// </summary>
	[SerializeField]public Color color = Color.black;

	/// <summary>
	/// イン動作かアウト動作か
	/// </summary>
	private bool isIn = false;
	/// <summary>
	/// 時間カウント
	/// </summary>
	private float timeCount = 0f;
	/// <summary>
	/// カメラ
	/// </summary>
	private new Camera camera = null;
	/// <summary>
	/// テクスチャ
	/// </summary>
	private Texture2D tex = null;
	/// <summary>
	/// テクスチャを描画する際のマテリアル
	/// </summary>
	private Material mat = null;
	/// <summary>
	/// フェード終了時コールバック
	/// </summary>
	private event Action onEnd = null;

	/// <summary>
	/// Awake
	/// </summary>
	private void Awake()
	{
		//処理が走らないようenabledをfalseに
		this.enabled = false;

		//カメラ取得
		this.camera = this.GetComponent<Camera>();

		//テクスチャ作成
		this.tex = new Texture2D(1, 1);
		this.tex.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.5f));
		this.tex.Apply();

		//マテリアル作成
		this.mat = new Material(Shader.Find("Particles/Alpha Blended"));
	}

	/// <summary>
	/// フェード処理
	/// </summary>
	private void OnPostRender()
	{
		//カラーのα値を決定
		float t = (this.duration > 0f) ? Mathf.Clamp01(this.timeCount / this.duration) : 1f;
		this.color.a = this.isIn ? 1 - t : t;

		//マテリアルにカラーを設定
		this.mat.SetColor("_TintColor", this.color);

		//まだフェード途中
		if (this.timeCount < this.duration)
		{
			this.timeCount += Time.deltaTime;
		}
		//フェード完了
		else
		{
			//アウト処理の場合、完了後の画面を継続させるためenabledはtrueのまま
			this.enabled = !this.isIn;
			
			//フェード完了時コールバック実行
			this.onEnd.SafetyInvoke();
			this.onEnd = null;
		}

		//フェード描画
		Graphics.Blit(this.tex, this.camera.activeTexture, this.mat);
	}

	/// <summary>
	/// フェードイン開始
	/// </summary>
	public void In(Action onEnd = null)
	{
		this.enabled = true;
		this.isIn = true;
		this.timeCount = 0f;
		this.onEnd = onEnd;
	}

	/// <summary>
	/// フェードアウト開始
	/// </summary>
	public void Out(Action onEnd = null)
	{
		this.enabled = true;
		this.isIn = false;
		this.timeCount = 0f;
		this.onEnd = onEnd;
	}
}

}//namespace MushaSystem
