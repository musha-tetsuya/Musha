using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha {

/// <summary>
/// 表示領域設定
/// </summary>
public class ViewportRectSetter : MonoBehaviour
{
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
	protected Vector2 screenSize = Define.SCREEN_SIZE;
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
	/// 前回の実機画面サイズ
	/// </summary>
	private Vector2 beforeRealScreenSize = Vector2.zero;


	/// <summary>
	/// Awake
	/// </summary>
	private void Awake()
	{
		//親に自分を登録
		if (this.parent != null && !this.parent.children.Contains(this))
		{
			this.parent.children.Add(this);
		}
	}

	/// <summary>
	/// Update
	/// </summary>
	private void Update()
	{
		//親がいない場合、実機画面を親として自分を更新する
		if (this.parent == null)
		{
			//実機画面サイズに変化があった場合だけ更新
			var parentScreenSize = GetRealScreenSize();
			if (parentScreenSize != beforeRealScreenSize)
			{
				this.UpdateViewportRect(parentScreenSize, new Rect(0, 0, 1, 1));
			}
		}
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

	/// <summary>
	/// 実機画面サイズ取得
	/// </summary>
	private static Vector2 GetRealScreenSize()
	{
		Vector2 size = Vector2.zero;
#if UNITY_EDITOR
		var res = UnityEditor.UnityStats.screenRes.Split('x');
		size.x = int.Parse(res[0]);
		size.y = int.Parse(res[1]);
#else
		size.x = Screen.width;
		size.y = Screen.height;
#endif
		return size;
	}
}

}//namespace Musha
