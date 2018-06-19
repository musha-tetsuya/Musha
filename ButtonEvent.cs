using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MushaEngine {

/// <summary>
/// ボタンイベント
/// </summary>
[AddComponentMenu("MushaEngine/ButtonTest")]
public class ButtonEvent : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
	/// <summary>
	/// 状態
	/// </summary>
	public enum State
	{
		None	 = 0,
		Pressing = 1 << 0,
		Began	 = 1 << 1,
		Long	 = 1 << 2,
		Released = 1 << 3,
		Cancel	 = 1 << 4,
	}

	/// <summary>
	/// マルチタッチを許可するかどうか
	/// </summary>
	[SerializeField]public bool allowMultiTouch = false;
	/// <summary>
	/// 長押し判定までの時間
	/// </summary>
	[SerializeField]public float longPressedTime = 0.5f;
	/// <summary>
	/// 押した瞬間に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onPressed = null;
	/// <summary>
	/// 長押し状態になった時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onLongPressed = null;
	/// <summary>
	/// クリック成立時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onClick = null;
	/// <summary>
	/// ショートクリック成立時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onShortClick = null;
	/// <summary>
	/// ロングクリック成立時に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onLongClick = null;
	/// <summary>
	/// キャンセル発生時（押したまま範囲外に出た時）に呼ばれるイベント
	/// </summary>
	[SerializeField]public UnityEvent onCancel = null;

	/// <summary>
	/// 状態
	/// </summary>
	private State state = State.None;
	/// <summary>
	/// 長押し判定コルーチン
	/// </summary>
	private Coroutine longPressedCoroutine = null;
	
	/// <summary>
	/// 押された瞬間
	/// </summary>
	public bool isPressed		{ get { return this.state == (State.Pressing | State.Began); } }
	/// <summary>
	/// 押されている間
	/// </summary>
	public bool isPressing		{ get { return (this.state & State.Pressing) > 0; } }
	/// <summary>
	/// 長押し状態かどうか
	/// </summary>
	public bool isLong			{ get { return (this.state & State.Long) > 0; } }
	/// <summary>
	/// 長押し状態になった瞬間
	/// </summary>
	public bool isLongPressed	{ get { return this.state == (State.Long | State.Pressing | State.Began); } }
	/// <summary>
	/// クリック成立時
	/// </summary>
	public bool isClick			{ get { return (this.state & State.Released) > 0; } }
	/// <summary>
	/// キャンセル発生時
	/// </summary>
	public bool isCancel		{ get { return this.state == State.Cancel; } }

	/// <summary>
	/// ボタンを押した時
	/// </summary>
	public void OnPointerDown(PointerEventData eventData)
	{
		//既に何らかの状態に入ってる時は受け付けない
		if (this.state != State.None) return;
		//マルチタッチ判定
		if (!this.allowMultiTouch && IsMultiTouch()) return;
		
		//押下成立（１フレームだけBegan状態を通知）
		this.state |= State.Pressing;
		this.state |= State.Began;
		StartCoroutine(this.WaitForEndOfFrameAction(() =>
		{
			this.state &= ~State.Began;
		}));

		//イベント実行
		if (this.onPressed != null)
		{
			this.onPressed.Invoke();
		}

		//長押し判定開始
		this.longPressedCoroutine = StartCoroutine(this.WaitForLongPressedTimeAction(() =>
		{
			//長押し成立（１フレームだけBegan状態を通知）
			this.state |= State.Long;
			this.state |= State.Began;
			StartCoroutine(this.WaitForEndOfFrameAction(() =>
			{
				this.state &= ~State.Began;
			}));

			//イベント実行
			if (this.onLongPressed != null)
			{
				this.onLongPressed.Invoke();
			}

			//コルーチン終了
			this.longPressedCoroutine = null;
		}));
	}

	/// <summary>
	/// ボタンを離した時
	/// </summary>
	public void OnPointerUp(PointerEventData eventData)
	{
		//押下中しか受け付けない
		if (!this.isPressing) return;

		//クリック成立（通知は１フレームだけ）
		this.state &= ~State.Pressing;
		this.state |= State.Released;
		StartCoroutine(this.WaitForEndOfFrameAction(() =>
		{
			this.state = State.None;
		}));

		//長押し判定の中断
		if (this.longPressedCoroutine != null)
		{
			StopCoroutine(this.longPressedCoroutine);
			this.longPressedCoroutine = null;
		}

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

	/// <summary>
	/// ボタンの範囲外に出た時
	/// </summary>
	public void OnPointerExit(PointerEventData eventData)
	{
		//押下中しか受け付けない
		if (!this.isPressing) return;

		//キャンセル発生（通知は１フレームだけ）
		this.state = State.Cancel;
		StartCoroutine(this.WaitForEndOfFrameAction(() =>
		{
			this.state = State.None;
		}));

		//長押し判定の中断
		if (this.longPressedCoroutine != null)
		{
			StopCoroutine(this.longPressedCoroutine);
			this.longPressedCoroutine = null;
		}

		//イベント実行
		if (this.onCancel != null)
		{
			this.onCancel.Invoke();
		}
	}

	/// <summary>
	/// OnDisable
	/// </summary>
	private void OnDisable()
	{
		this.state = State.None;
		this.longPressedCoroutine = null;
	}

	/// <summary>
	/// フレーム終了時にアクション実行
	/// </summary>
	private IEnumerator WaitForEndOfFrameAction(Action action)
	{
		yield return new WaitForEndOfFrame();
		action();
	}

	/// <summary>
	/// 長押しまでの時間を待ってフレーム終了時にアクション実行
	/// </summary>
	private IEnumerator WaitForLongPressedTimeAction(Action action)
	{
		yield return new WaitForSeconds(this.longPressedTime);
		yield return this.WaitForEndOfFrameAction(action);
	}

	/// <summary>
	/// マルチタッチかどうか
	/// </summary>
	public static bool IsMultiTouch()
	{
		return Input.touchSupported && Input.touchCount > 1;
	}
}

}//namespace MushaEngine
