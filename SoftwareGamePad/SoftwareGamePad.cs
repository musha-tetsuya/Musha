using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MushaEngine {

/// <summary>
/// ソフトウェアゲームパッド
/// </summary>
[AddComponentMenu("MushaEngine/SoftwareGamePad")]
public class SoftwareGamePad : MonoBehaviour
{
	/// <summary>
	/// ボタンタイプ
	/// </summary>
	public enum ButtonType
	{
		A,
		B,
		X,
		Y,
		Up,
		Down,
		Left,
		Right,
		Start,
		Select,
	}

	/// <summary>
	/// イベントリスナー
	/// </summary>
	public interface IListner
	{
		void OnPressed(ButtonType buttonType);
		void OnLongPressed(ButtonType buttonType);
		void OnClick(ButtonType buttonType);
		void OnShortClick(ButtonType buttonType);
		void OnLongClick(ButtonType buttonType);
		void OnCancel(ButtonType buttonType);
	}

	[SerializeField]
	private RectTransform canvasTransform = null;

	[SerializeField]
	private RectTransform groupLeft = null;
	[SerializeField]
	private RectTransform groupRight = null;

	[SerializeField]
	private Vector2 padSize = new Vector2(8.6f, 4.3f);

	[SerializeField]
	private ButtonEvent[] buttonEvent = null;

	private ButtonType state = 0;
	private List<IListner> listnerList = new List<IListner>();
	private Vector2 beforeCanvasSize = Vector2.zero;

	/// <summary>
	/// Start
	/// </summary>
	private void Start()
	{
		this.UpdatePadScale();
		this.RaycastIgnoreTransparent();
	}

#if UNITY_EDITOR
	/// <summary>
	/// Update
	/// </summary>
	private void Update()
	{
		this.UpdatePadScale();
	}
#endif

	/// <summary>
	/// 画面サイズに合わせてパッドのスケールを更新
	/// </summary>
	private void UpdatePadScale()
	{
		//前回からキャンバスサイズに変更があったかチェック
		if (this.canvasTransform.sizeDelta != this.beforeCanvasSize)
		{
			//現在のキャンバスサイズを保存
			this.beforeCanvasSize = this.canvasTransform.sizeDelta;

			//現在のキャンバスサイズの場合、パッドのスケールをどれぐらいにするべきか計算（最大１）
			var scale = this.canvasTransform.sizeDelta / this.padSize;
			float padScaleValue = Mathf.Min(scale.x, scale.y, 1f);
			this.groupLeft.localScale = this.groupRight.localScale = new Vector3(padScaleValue, padScaleValue, 1f);
		}
	}

	/// <summary>
	/// ボタンの透明部分へのレイキャストを無視する
	/// </summary>
	private void RaycastIgnoreTransparent()
	{
		int[] ids =
		{
			(int)ButtonType.A,
			(int)ButtonType.B,
			(int)ButtonType.X,
			(int)ButtonType.Y,
			(int)ButtonType.Start,
			(int)ButtonType.Select,
		};

		foreach (int i in ids)
		{
			this.buttonEvent[i].GetComponent<Image>().alphaHitTestMinimumThreshold = 1f;
		}
	}

	/// <summary>
	/// リスナー登録
	/// </summary>
	/// <param name="listner">登録するリスナー</param>
	public void AddListner(IListner listner)
	{
		if (!this.listnerList.Contains(listner))
		{
			this.listnerList.Add(listner);
		}
	}

	/// <summary>
	/// リスナー除去
	/// </summary>
	/// <param name="listner">除去するリスナー</param>
	public void RemoveListner(IListner listner)
	{
		this.listnerList.Remove(listner);
	}

	/// <summary>
	/// ボタンイベント取得
	/// </summary>
	/// <param name="buttonType">取得したいボタンイベントのタイプ</param>
	public ButtonEvent GetButtonEvent(ButtonType buttonType)
	{
		return this.buttonEvent[(int)buttonType];
	}

	/// <summary>
	/// ボタンが押された瞬間に呼ばれる
	/// </summary>
	/// <param name="buttonType">押されたボタンの識別ID</param>
	public void OnPressed(int buttonType)
	{
		for (int i = 0, imax = this.listnerList.Count; i < imax; i++)
		{
			this.listnerList[i].OnPressed((ButtonType)buttonType);
		}
	}

	/// <summary>
	/// ボタンが長押し状態になった瞬間に呼ばれる
	/// </summary>
	/// <param name="buttonType">長押し状態になったボタンの識別ID</param>
	public void OnLongPressed(int buttonType)
	{
		for (int i = 0, imax = this.listnerList.Count; i < imax; i++)
		{
			this.listnerList[i].OnLongPressed((ButtonType)buttonType);
		}
	}

	/// <summary>
	/// ボタンがクリックされた瞬間に呼ばれる
	/// </summary>
	/// <param name="buttonType">クリックされたボタンの識別ID</param>
	public void OnClick(int buttonType)
	{
		for (int i = 0, imax = this.listnerList.Count; i < imax; i++)
		{
			this.listnerList[i].OnClick((ButtonType)buttonType);
		}
	}

	/// <summary>
	/// ボタンがショートクリックされた瞬間に呼ばれる
	/// </summary>
	/// <param name="buttonType">ショートクリックされたボタンの識別ID</param>
	public void OnShortClick(int buttonType)
	{
		for (int i = 0, imax = this.listnerList.Count; i < imax; i++)
		{
			this.listnerList[i].OnShortClick((ButtonType)buttonType);
		}
	}

	/// <summary>
	/// ボタンがロングクリックされた瞬間に呼ばれる
	/// </summary>
	/// <param name="buttonType">ロングクリックされたボタンの識別ID</param>
	public void OnLongClick(int buttonType)
	{
		for (int i = 0, imax = this.listnerList.Count; i < imax; i++)
		{
			this.listnerList[i].OnLongClick((ButtonType)buttonType);
		}
	}

	public void OnCancel(int buttonType)
	{
		for (int i = 0, imax = this.listnerList.Count; i < imax; i++)
		{
			this.listnerList[i].OnCancel((ButtonType)buttonType);
		}
	}


}

}//namespace MushaEngine
