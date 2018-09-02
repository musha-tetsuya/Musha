using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Musha {

/// <summary>
/// ボタンイベント
/// </summary>
[AddComponentMenu("Musha/UI/ButtonEvent")]
public class ButtonEvent : MonoBehaviour, IPointerDownHandler, IPointerExitHandler, IPointerClickHandler
{
	/// <summary>
	/// 状態
	/// </summary>
	private enum State
	{
		None		= 0,
		Long		= 1 << 0,
		Pressing	= 1 << 1,
		Began		= 1 << 2,
		Click		= 1 << 3,
		Cancel		= 1 << 4,
	}

	/// <summary>
	/// マルチタッチを許可するかどうか
	/// </summary>
	[SerializeField]public bool allowMultiTouch = false;
	/// <summary>
	/// 入力を受け付けるマウスボタンタイプ
	/// </summary>
	[EnumFlags(typeof(PointerEventData.InputButton))]
	[SerializeField]public int enabledInputButton = 1 << (int)PointerEventData.InputButton.Left;
	/// <summary>
	/// 長押し＆連射開始までの待ち時間
	/// </summary>
	[SerializeField]public float waitLongMode = 1.0f;
	/// <summary>
	/// 連射間隔（フレーム）
	/// </summary>
	[SerializeField]public int repeatedInterval = 0;
	/// <summary>
	/// 押した瞬間に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onPressed = null;
	/// <summary>
	/// 長押し成立した瞬間に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onLongPressed = null;
	/// <summary>
	/// クリックが成立した時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onClick = null;
	/// <summary>
	/// ショートクリックが成立した時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onShortClick = null;
	/// <summary>
	/// ロングクリックが成立した時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onLongClick = null;
	/// <summary>
	/// キャンセル発生時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onCancel = null;

	/// <summary>
	/// 状態
	/// </summary>
	private State state = State.None;
	/// <summary>
	/// 押した指番号
	/// </summary>
	private List<int> pointerIdList = new List<int>();

	/// <summary>
	/// マルチタッチかどうか
	/// </summary>
	public static bool isMultiTouch
	{
		get { return Input.touchCount > 1; }
	}
	/// <summary>
	/// 長押し状態
	/// </summary>
	public bool isLong
	{
		get { return (this.state & State.Long) > 0; }
	}
	/// <summary>
	/// 押されている間
	/// </summary>
	public bool isPressing
	{
		get { return (this.state & State.Pressing) > 0; }
	}
	/// <summary>
	/// 押された瞬間１フレームだけ通知
	/// </summary>
	public bool isPressed
	{
		get { return this.state == (State.Pressing | State.Began); }
	}
	/// <summary>
	/// 長押し成立した瞬間 or 連射時
	/// </summary>
	public bool isNextPressed
	{
		get { return this.state == (State.Long | State.Pressing | State.Began); }
	}
	/// <summary>
	/// クリック成立時１フレームだけ通知
	/// </summary>
	public bool isClick
	{
		get { return (this.state & State.Click) > 0; }
	}
	/// <summary>
	/// ショートクリック成立時１フレームだけ通知
	/// </summary>
	public bool isShortClick
	{
		get { return !this.isLong && this.isClick; }
	}
	/// <summary>
	/// ロングクリック成立時１フレームだけ通知
	/// </summary>
	public bool isLongClick
	{
		get { return this.isLong && this.isClick; }
	}
	/// <summary>
	/// キャンセル発生時１フレームだけ通知
	/// </summary>
	public bool isCancel
	{
		get { return this.state == State.Cancel; }
	}

	/// <summary>
	/// ボタンを押した時
	/// </summary>
	public void OnPointerDown(PointerEventData eventData)
	{
		//マルチタッチチェック
		if (!this.allowMultiTouch && isMultiTouch) return;

		//マウス入力の場合、有効なマウスボタンタイプかどうかをチェック
		if (eventData.pointerId < 0 && !IsEnabledInputButton(eventData.button)) return;

		//ボタンを押した指番号を管理
		if (!this.pointerIdList.Contains(eventData.pointerId))
		{
			this.pointerIdList.Add(eventData.pointerId);
		}

		//最初の１本目の指しか受け付けない
		if (this.pointerIdList.Count == 1)
		{
			//押下成立
			StartCoroutine(OnPressed());
		}
	}

	/// <summary>
	/// 押下成立時
	/// </summary>
	private IEnumerator OnPressed()
	{
		//押下成立
		this.state |= State.Pressing | State.Began;

		//押下イベント実行
		if (this.onPressed != null)
		{
			this.onPressed.Invoke();
		}

		//１フレーム後に通知を解除
		yield return new WaitForEndOfFrame();
		this.state &= ~State.Began;

		//長押し成立までの時間を待つ
		if (this.waitLongMode > Time.deltaTime)
		{
			yield return new WaitForSeconds(this.waitLongMode - Time.deltaTime);
			yield return new WaitForEndOfFrame();
		}

		for (int i = 0; ; i++)
		{
			//長押し（連射）成立
			this.state |= State.Long | State.Pressing | State.Began;

			if (i == 0)
			{
				//長押しイベント実行
				if (this.onLongPressed != null)
				{
					this.onLongPressed.Invoke();
				}
			}

			//連射イベント実行判定
			bool isRepeatPressed = this.repeatedInterval > 0;
			if (isRepeatPressed)
			{
				//連射イベント実行
				if (this.onPressed != null)
				{
					this.onPressed.Invoke();
				}
			}

			//１フレーム後に通知を解除
			yield return new WaitForEndOfFrame();
			this.state &= ~State.Began;

			if (isRepeatPressed && this.repeatedInterval > 0)
			{
				//次の連射実行を待つ
				for (int j = 1, jmax = this.repeatedInterval; j < jmax; j++)
				{
					yield return new WaitForEndOfFrame();
				}
			}
			else
			{
				break;
			}
		}
	}

	/// <summary>
	/// ボタンの範囲外に出た時
	/// </summary>
	public void OnPointerExit(PointerEventData eventData)
	{
		//何も管理していないのでreturn
		if (this.pointerIdList.Count == 0) return;

		//マウスの場合-1しか来ない
		if (eventData.pointerId == -1)
		{
			//管理している指番号を全部除去
			this.pointerIdList.Clear();
		}
		//マウス以外の場合
		else
		{
			//範囲外に出た指番号をリストから除去
			this.pointerIdList.Remove(eventData.pointerId);
		}

		//全ての指がボタンの範囲外に出た
		if (this.pointerIdList.Count == 0)
		{
			//長押し判定の中断
			StopAllCoroutines();

			//キャンセル発生（１フレームだけ通知）
			this.state = State.Cancel;
			StartCoroutine(CoroutineUtility.WaitForEndOfFrameAction(() =>
			{
				this.state = State.None;
			}));

			//イベント実行
			if (this.onCancel != null)
			{
				this.onCancel.Invoke();
			}
		}
	}

	/// <summary>
	/// クリック成立時
	/// </summary>
	public void OnPointerClick(PointerEventData eventData)
	{
		//クリック成立した指番号をリストから除去
		if (this.pointerIdList.Remove(eventData.pointerId))
		{
			//最後の指の時だけクリック成立
			if (this.pointerIdList.Count == 0)
			{
				//長押し判定の中断
				StopAllCoroutines();

				//クリック成立（１フレームだけ通知）
				this.state |= State.Click;
				this.state &= ~State.Pressing;
				StartCoroutine(CoroutineUtility.WaitForEndOfFrameAction(() =>
				{
					this.state = State.None;
				}));

				//イベント実行
				if (this.onClick != null)
				{
					this.onClick.Invoke();
				}
				if (!this.isLong)
				{
					if (this.onShortClick != null)
					{
						this.onShortClick.Invoke();
					}
				}
				else
				{
					if (this.onLongClick != null)
					{
						this.onLongClick.Invoke();
					}
				}
			}
		}
	}

	/// <summary>
	/// OnDisable
	/// </summary>
	private void OnDisable()
	{
		this.pointerIdList.Clear();
		this.state = State.None;
	}

	/// <summary>
	/// 有効なボタンタイプかどうか
	/// </summary>
	private bool IsEnabledInputButton(PointerEventData.InputButton button)
	{
		return (this.enabledInputButton & (1 << (int)button)) > 0;
	}
}

}//namespace Musha