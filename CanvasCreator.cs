using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Musha
{
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(CanvasScaler))]
	[RequireComponent(typeof(GraphicRaycaster))]
	public class CanvasCreator : MonoBehaviour
	{
		public Canvas mCanvas { get; private set; }
		public CanvasScaler mCanvasScaler { get; private set; }
		protected virtual RenderMode mRenderMode { get { return RenderMode.ScreenSpaceCamera; } }

		void Reset()
		{
			gameObject.layer = LayerMask.NameToLayer("UI");

			mCanvas = GetComponent<Canvas>();
			mCanvas.renderMode = mRenderMode;
			mCanvas.planeDistance = 0;

			mCanvasScaler = GetComponent<CanvasScaler>();
			mCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			mCanvasScaler.referenceResolution = new Vector2(Define.DISP_W, Define.DISP_H);
			mCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
		}

		protected virtual void Awake()
		{
			Reset();
		}
	}
}
