using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Musha
{
	/// <summary>
	/// メッセージボックス
	/// </summary>
	public partial class MessageBox : MonoBehaviour
	{
		//----	field	-------------------------------------------------------------------
		private Resource mResource;
		private Text mMessage;
		private Button mButtonOk;
		private Button mButtonYes;
		private Button mButtonNo;
		private System.Action OnOk;
		private System.Action OnYes;
		private System.Action OnNo;
		private bool mTapOk;
		private bool mTapYes;
		private bool mTapNo;
		private bool mAutoDestroy = true;
		//----	method	-------------------------------------------------------------------
		/// <summary>
		/// 生成
		/// </summary>
		private static void Create(System.Action<MessageBox> onCreate)
		{
			//リソース読み込み
			Sys.AssetManager.LoadAsyncInPackage(Define.MESSAGE_BOX_RESOURCE_PATH, resource =>
			{
				//UI生成
				Sys.AssetManager.InstantiateAsync(resource.Get<GameObject>("MessageBox"), gobj =>
				{
					//UIをオーバーレイキャンバスの下に格納
					gobj.transform.SetParent(Sys.OverlayCanvas.transform);
					gobj.transform.localPosition = Vector3.zero;
					gobj.transform.localScale = Vector3.one;
					//メッセージボックス作成
					var mb = gobj.AddComponent<MessageBox>();
					mb.mResource = resource;
					//コールバック実行
					onCreate(mb);
				},
				true);
			}, 
			true);
		}
		/// <summary>
		/// 開始
		/// </summary>
		private void Start()
		{
			if (mButtonOk != null)
			{
				mButtonOk.onClick.AddListener(() => mTapOk = true);
			}
			if (mButtonYes != null)
			{
				mButtonYes.onClick.AddListener(() => mTapYes = true);
			}
			if (mButtonNo != null)
			{
				mButtonNo.onClick.AddListener(() => mTapNo = true);
			}
		}
		/// <summary>
		/// 破棄
		/// </summary>
		private void OnDestroy()
		{
			Sys.AssetManager.Clear(mResource);
		}
		/// <summary>
		/// 処理
		/// </summary>
		private void Update()
		{
			if (mTapOk)
			{
				if (OnOk != null)
				{
					OnOk();
					OnOk = null;
				}
				if (mAutoDestroy)
				{
					Destroy(gameObject);
				}
			}
			else if (mTapYes)
			{
				if (OnYes != null)
				{
					OnYes();
					OnYes = null;
				}
				if (mAutoDestroy)
				{
					Destroy(gameObject);
				}
			}
			else if (mTapNo)
			{
				if (OnNo != null)
				{
					OnNo();
					OnNo = null;
				}
				if (mAutoDestroy)
				{
					Destroy(gameObject);
				}
			}
		}
		/// <summary>
		/// 後処理
		/// </summary>
		private void LateUpdate()
		{
			mTapOk = false;
			mTapYes = false;
			mTapNo = false;
		}
		/// <summary>
		/// 開く
		/// </summary>
		public static void Open(string msg, System.Action onOk, bool autoDestroy = true)
		{
			Create(mb =>
			{
				mb.mAutoDestroy = autoDestroy;
				mb.OnOk = onOk;
				if (mb.mMessage != null)
				{
					mb.mMessage.text = msg;
				}
				if (mb.mButtonYes != null)
				{
					mb.mButtonYes.gameObject.SetActive(false);
				}
				if (mb.mButtonNo != null)
				{
					mb.mButtonNo.gameObject.SetActive(false);
				}
			});
		}
		/// <summary>
		/// Yes/Noメッセージボックスを開く
		/// </summary>
		public static void OpenYesNo(string msg, System.Action onYes, System.Action onNo, bool autoDestroy = true)
		{
			Create(mb =>
			{
				mb.mAutoDestroy = autoDestroy;
				mb.OnYes = onYes;
				mb.OnNo = onNo;
				if (mb.mMessage != null)
				{
					mb.mMessage.text = msg;
				}
				if (mb.mButtonOk != null)
				{
					mb.mButtonOk.gameObject.SetActive(false);
				}
			});
		}
	}
}
