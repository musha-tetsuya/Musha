using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaSystem {

/// <summary>
/// 表示領域設定
/// </summary>
[AddComponentMenu("MushaSystem/UI/ViewportRectSetter")]
public class ViewportRectSetter : MonoBehaviour
{
	private const float LANDSCAPE_BEZELLESS_ASPECT = 724f / 354f;
	private const float PORTRAIT_BEZELLESS_ASPECT = 375f / 734f;
	/// <summary>
	/// 配置定数
	/// </summary>
	private static readonly Dictionary<TextAnchor, Vector2> anchorPosition = new Dictionary<TextAnchor, Vector2>
	{
		{ TextAnchor.UpperLeft,    new Vector2(0.0f, 1.0f) },
		{ TextAnchor.UpperCenter,  new Vector2(0.5f, 1.0f) },
		{ TextAnchor.UpperRight,   new Vector2(1.0f, 1.0f) },
		{ TextAnchor.MiddleLeft,   new Vector2(0.0f, 0.5f) },
		{ TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f) },
		{ TextAnchor.MiddleRight,  new Vector2(1.0f, 0.5f) },
		{ TextAnchor.LowerLeft,    new Vector2(0.0f, 0.0f) },
		{ TextAnchor.LowerCenter,  new Vector2(0.5f, 0.0f) },
		{ TextAnchor.LowerRight,   new Vector2(1.0f, 0.0f) },
	};
	/// <summary>
	/// 画面サイズ
	/// </summary>
	[SerializeField]
	protected Vector2 screenSize = Vector2.one;
	/// <summary>
	/// 配置
	/// </summary>
	[SerializeField]
	private TextAnchor anchor = TextAnchor.MiddleCenter;
	/// <summary>
	/// 表示領域を設定する対象のカメラ
	/// </summary>
	[SerializeField]
	private Camera targetCamera = null;
	/// <summary>
	/// 親
	/// </summary>
	[SerializeField]
	private ViewportRectSetter parent = null;
	/// <summary>
	/// 表示領域
	/// </summary>
	private Rect viewportRect = new Rect(0, 0, 1, 1);
	/// <summary>
	/// 子供
	/// </summary>
	private List<ViewportRectSetter> children = new List<ViewportRectSetter>();

	/// <summary>
	/// Awake
	/// </summary>
	private void Awake()
	{
		if (this.parent == null)
		{
			//画面サイズ変化時イベントを登録
			ScreenManager.AddChangeScreenSizeEvent(this.OnChangeScreenSize);
		}
		else if (!this.parent.children.Contains(this))
		{
			//親に自分を登録
			this.parent.children.Add(this);
		}
	}

	/// <summary>
	/// OnEnable
	/// </summary>
	private void OnEnable()
	{
		if (this.parent == null)
		{
			//アクティブ復帰時には画面サイズ変化時イベントを呼ぶ
			this.OnChangeScreenSize();
		}
	}

	/// <summary>
	/// OnDestroy
	/// </summary>
	private void OnDestroy()
	{
		if (this.parent == null)
		{
			//画面サイズ変化時イベントを除去
			ScreenManager.RemoveCangeScreenSizeEvent(this.OnChangeScreenSize);
		}
	}

	/// <summary>
	/// 画面サイズに変更があった時に呼ばれる
	/// </summary>
	private void OnChangeScreenSize()
	{
		Vector2 realScreenSize = ScreenManager.GetRealScreenSize();
		Rect safeArea = ScreenManager.GetSafeArea();
		Rect safeAreaRect = new Rect(
			position: safeArea.position / realScreenSize,
			size: safeArea.size / realScreenSize);
		this.UpdateViewportRect(safeArea.size, safeAreaRect);
	}

	/// <summary>
	/// 表示領域更新
	/// </summary>
	private void UpdateViewportRect(Vector2 parentScreenSize, Rect parentViewportRect)
	{
		//画面サイズチェック
		if (this.screenSize.x <= 0
		||  this.screenSize.y <= 0
		||  parentScreenSize.x <= 0
		||  parentScreenSize.y <= 0)
		{
			Debug.LogErrorFormat("無効な画面サイズ this = {0}, parent = {1}", this.screenSize, parentScreenSize);
			return;
		}

		//画面比
		float parentAspect = parentScreenSize.x / parentScreenSize.y;
		float myAspect = this.screenSize.x / this.screenSize.y;

		//親の横幅が大きいので左右に黒帯
		if (myAspect < parentAspect)
		{
			this.viewportRect.width = parentViewportRect.width * (myAspect / parentAspect);
			this.viewportRect.height = parentViewportRect.height;
			this.viewportRect.x = parentViewportRect.x + (parentViewportRect.width - this.viewportRect.width) * anchorPosition[this.anchor].x;
			this.viewportRect.y = parentViewportRect.y;
		}
		//親の縦幅が大きいので上下に黒帯
		else
		{
			this.viewportRect.width = parentViewportRect.width;
			this.viewportRect.height = parentViewportRect.height * (parentAspect / myAspect);
			this.viewportRect.x = parentViewportRect.x;
			this.viewportRect.y = parentViewportRect.y + (parentViewportRect.height - this.viewportRect.height) * anchorPosition[this.anchor].y;
		}

		//設定反映
		if (this.targetCamera != null)
		{
			this.targetCamera.rect = this.viewportRect;
		}

		//子供を更新
		for (int i = 0, imax = this.children.Count; i < imax; i++)
		{
			this.children[i].UpdateViewportRect(this.screenSize, this.viewportRect);
		}
	}
}

}//namespace MushaSystem
