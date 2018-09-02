using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Musha {

/// <summary>
/// キャンバスサイズに合わせてスケールを調整するクラス
/// </summary>
[AddComponentMenu("Musha/UI/CanvasContentScaler")]
[RequireComponent(typeof(CanvasScaler))]
public class CanvasContentScaler : UIBehaviour
{
	/// <summary>
	/// RectTransform
	/// </summary>
	private RectTransform m_rectTransform = null;
	/// <summary>
	/// CanvasScaler
	/// </summary>
	private CanvasScaler m_canvasScaler = null;

	/// <summary>
	/// Awake
	/// </summary>
	protected override void Awake()
	{
		base.Awake();

		//画面サイズ変化時イベントを登録
		ScreenManager.AddChangeScreenSizeEvent(this.OnChangeScreenSize);
	}

	/// <summary>
	/// OnEnable
	/// </summary>
	protected override void OnEnable()
	{
		base.OnEnable();

		//アクティブ復帰時には画面サイズ変化時イベントを呼ぶ
		this.OnChangeScreenSize();
	}

	/// <summary>
	/// OnDestroy
	/// </summary>
	protected override void OnDestroy()
	{
		//画面サイズ変化時イベントを除去
		ScreenManager.RemoveCangeScreenSizeEvent(this.OnChangeScreenSize);

		base.OnDestroy();
	}

	/// <summary>
	/// RectTransform
	/// </summary>
	private RectTransform rectTransform
	{
		get { return this.m_rectTransform ?? (this.m_rectTransform = this.GetComponent<RectTransform>()); }
	}

	/// <summary>
	/// CanvasScaler
	/// </summary>
	private CanvasScaler canvasScaler
	{
		get { return this.m_canvasScaler ?? (this.m_canvasScaler = this.GetComponent<CanvasScaler>()); }
	}

	/// <summary>
	/// 画面サイズ変化時イベント
	/// </summary>
	private void OnChangeScreenSize()
	{
		Vector2 realScreenSize = ScreenManager.GetRealScreenSize();
		this.canvasScaler.matchWidthOrHeight = (realScreenSize.x < realScreenSize.y) ? 0f : 1f;
	}

	/// <summary>
	/// キャンバスサイズ変化時イベント
	/// </summary>
	protected override void OnRectTransformDimensionsChange()
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
	}
}

}//namespace Musha