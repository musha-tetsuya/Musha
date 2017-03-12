using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha
{
	/// <summary>
	/// タッチ管理
	/// </summary>
	public class Touch : MonoBehaviour
	{
		//----  field   -----------------------------------------------------------------------------------
		private UnityEngine.Touch mTouch = new UnityEngine.Touch();
		private TouchData[] mTouchData = new TouchData[Define.MAX_FINGER];
		//----	method	-----------------------------------------------------------------------------------
		/// <summary>
		/// 初期化
		/// </summary>
		private void Awake()
		{
			for (int i = 0; i < mTouchData.Length; i++)
			{
				mTouchData[i] = new TouchData();
			}
		}
		/// <summary>
		/// 更新
		/// </summary>
		public void Run()
		{
			//毎フレームタッチ状態クリア
			for (int i = 0; i < mTouchData.Length; i++)
			{
				mTouchData[i].Update();
			}
#if (UNITY_EDITOR || UNITY_STANDALONE)
			//マウス情報をタッチ情報に落とし込む
			if (Input.GetMouseButtonDown(0))
			{
				mTouch.phase = TouchPhase.Began;
				mTouch.position = Input.mousePosition;
				mTouchData[0].Set(mTouch);
			}
			else if (Input.GetMouseButtonUp(0))
			{
				mTouch.phase = TouchPhase.Ended;
				mTouch.position = Input.mousePosition;
				mTouchData[0].Set(mTouch);
			}
			else if (Input.GetMouseButton(0))
			{
				mTouch.phase = TouchPhase.Stationary;
				mTouch.position = Input.mousePosition;
				mTouchData[0].Set(mTouch);
			}
#else
			//タッチ情報でデータを更新
			for (int i = 0; i < Input.touchCount; i++)
			{
				mTouch = UnityEngine.Input.touches[i];
				if (0 <= mTouch.fingerId && mTouch.fingerId < mTouchData.Length)
				{
					mTouchData[mTouch.fingerId].Set(mTouch);
				}
			}
#endif
		}
		/// <summary>
		/// タッチ開始判定
		/// </summary>
		public bool CheckEdge(int fingerId, float cx, float cy, float w, float h)
		{
			return CheckEdge(fingerId, new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h));
		}
		/// <summary>
		/// タッチ開始判定
		/// </summary>
		public bool CheckEdge(int fingerId, Rect chkArea)
		{
			return mTouchData[fingerId].CheckEdge(chkArea);
		}
		/// <summary>
		/// タッチ中判定
		/// </summary>
		public bool CheckPush(int fingerId, float cx, float cy, float w, float h)
		{
			return CheckPush(fingerId, new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h));
		}
		/// <summary>
		/// タッチ中判定
		/// </summary>
		public bool CheckPush(int fingerId, Rect chkArea)
		{
			return mTouchData[fingerId].CheckPush(chkArea);
		}
		/// <summary>
		/// タップ判定
		/// </summary>
		public bool CheckTap(int fingerId, float cx, float cy, float w, float h)
		{
			return CheckTap(fingerId, new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h));
		}
		/// <summary>
		/// タップ判定
		/// </summary>
		public bool CheckTap(int fingerId, Rect chkArea)
		{
			return mTouchData[fingerId].CheckTap(chkArea);
		}
		/// <summary>
		/// タッチ終了判定
		/// </summary>
		public bool CheckEnd(int fingerId, float cx, float cy, float w, float h)
		{
			return CheckEnd(fingerId, new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h));
		}
		/// <summary>
		/// タッチ終了判定
		/// </summary>
		public bool CheckEnd(int fingerId, Rect chkArea)
		{
			return mTouchData[fingerId].CheckEnd(chkArea);
		}
		/// <summary>
		/// タッチ開始位置
		/// </summary>
		public Vector2 StartPos(int fingerId)
		{
			return mTouchData[fingerId].StartPos;
		}
		/// <summary>
		/// タッチ位置
		/// </summary>
		public Vector2 Pos(int fingerId)
		{
			return mTouchData[fingerId].NowPos;
		}
		/// <summary>
		/// フレーム間移動量
		/// </summary>
		public Vector2 Delta(int fingerId)
		{
			return mTouchData[fingerId].Delta;
		}
	}
	/// <summary>
	/// タッチデータ
	/// </summary>
	public struct TouchData
	{
		//----	field	-----------------------------------------------------------------------------------
		private bool Edge;			//タッチ開始
		private bool Push;			//タッチ中
		private bool End;			//タッチ終了
		private bool TapCancel;		//タップキャンセル
		public Vector2 StartPos;	//タッチ開始位置
		private Vector2 BeforePos;	//直前のタッチ位置
		public Vector2 NowPos;		//現在のタッチ位置
		//----	method	-----------------------------------------------------------------------------------
		/// <summary>
		/// 更新
		/// </summary>
		public void Update()
		{
			//タッチ状態のクリア
			Edge = false;
			Push = false;
			End = false;
			BeforePos = NowPos;
			NowPos = Vector2.zero;
		}
		/// <summary>
		/// セット
		/// </summary>
		public void Set(UnityEngine.Touch touch)
		{
			//中心原点の仮想画面サイズの座標系に変換
			NowPos.x = (touch.position.x / Screen.width - 0.5f) * Define.DISP_W;
			NowPos.y = (touch.position.y / Screen.height - 0.5f) * Define.DISP_H;
			if (Sys.RealAspect < Define.ASPECT)
			{
				NowPos.y *= Define.ASPECT / Sys.RealAspect;
			}
			else
			{
				NowPos.x *= Sys.RealAspect / Define.ASPECT;
			}

			switch (touch.phase)
			{
				//タッチ開始
				case TouchPhase.Began:
				Edge = true;
				Push = true;
				TapCancel = false;
				StartPos = NowPos;
				break;
				//タッチ中
				case TouchPhase.Moved:
				case TouchPhase.Stationary:
				Push = true;
				//タッチ開始位置からの距離が特定距離を超えたらタップキャンセル。
				if (!TapCancel)
				{
					TapCancel = Define.TAP_CANCEL_RANGE < Vector3.Distance(StartPos, NowPos);
				}
				break;
				//タッチ終了
				case TouchPhase.Ended:
				case TouchPhase.Canceled:
				Push = true;
				End = true;
				break;
			}
		}
		/// <summary>
		/// タッチ開始判定
		/// </summary>
		public bool CheckEdge(Rect chkArea)
		{
			return Edge && chkArea.Contains(NowPos);
		}
		/// <summary>
		/// タッチ中判定
		/// </summary>
		public bool CheckPush(Rect chkArea)
		{
			return Push && chkArea.Contains(NowPos);
		}
		/// <summary>
		/// タップ判定
		/// </summary>
		public bool CheckTap(Rect chkArea)
		{
			return End && !TapCancel && chkArea.Contains(NowPos);
		}
		/// <summary>
		/// タッチ終了判定
		/// </summary>
		public bool CheckEnd(Rect chkArea)
		{
			return End && chkArea.Contains(NowPos);
		}
		/// <summary>
		/// フレーム間移動量
		/// </summary>
		public Vector2 Delta
		{
			get { return NowPos - BeforePos; }
		}
	}
}
