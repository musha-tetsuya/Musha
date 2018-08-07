using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MushaSystem {

/// <summary>
/// キャンバスサイズに合わせてスケールを調整するクラス
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class CanvasContentScaler : MonoBehaviour
{
	/// <summary>
	/// RectTransform
	/// </summary>
	private RectTransform rectTransform = null;
	/// <summary>
	/// CanvasScaler
	/// </summary>
	private CanvasScaler canvasScaler = null;

	/// <summary>
	/// Awake
	/// </summary>
	private void Awake()
	{
		this.rectTransform = this.GetComponent<RectTransform>();
		this.canvasScaler = this.GetComponentInParent<CanvasScaler>();

		//画面サイズ変化時イベントを登録
		ScreenManager.AddChangeScreenSizeEvent(this.OnChangeScreenSize);
	}

	/// <summary>
	/// 画面サイズ変化時イベント
	/// </summary>
	private void OnChangeScreenSize()
	{
		//キャンバスサイズ変化時イベントを登録
		Canvas.willRenderCanvases += this.OnChangeCanvasSize;
	}

	/// <summary>
	/// キャンバスサイズ変化時イベント
	/// </summary>
	private void OnChangeCanvasSize()
	{
		//キャンバス内に収めるため子供のスケール値を調整
		var childScale = new Vector3(
			Mathf.Min(1f, this.rectTransform.sizeDelta.x / this.canvasScaler.referenceResolution.x),
			Mathf.Min(1f, this.rectTransform.sizeDelta.y / this.canvasScaler.referenceResolution.y),
			1f);

		foreach (Transform child in this.rectTransform)
		{
			child.localScale = childScale;
		}

		//キャンバスサイズ変化時イベントを除去
		Canvas.willRenderCanvases -= this.OnChangeCanvasSize;
	}
}

}//namespace MushaSystem