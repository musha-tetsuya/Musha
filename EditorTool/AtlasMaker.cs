#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace MushaEditor {

/// <summary>
/// アトラスメーカー
/// </summary>
public class AtlasMaker : EditorWindow
{
	/// <summary>
	/// テクスチャ間のpadding
	/// </summary>
	private int padding = 0;
	/// <summary>
	/// アトラス化対象のテクスチャリスト
	/// </summary>
	private List<AtlasParts> atlasPartsList = new List<AtlasParts>();

	[MenuItem("MushaEditor/AtlasMaker")]
	private static void Open()
	{
		GetWindow<AtlasMaker>();
	}

	/// <summary>
	/// OnGUI
	/// </summary>
	private void OnGUI()
	{
		GUILayout.BeginHorizontal();
		{
			EditorGUI.BeginDisabledGroup(this.atlasPartsList.Count == 0 || !this.atlasPartsList.Exists(x => x.IsValid()));
			{
				//保存ボタン
				if (GUILayout.Button("Save", GUILayout.Width(60)))
				{
					string path = EditorUtility.SaveFilePanelInProject("Save Atlas", "", "png", "");
					if (!string.IsNullOrEmpty(path))
					{
						Save(path);
					}
				}
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.Label("Padding", GUILayout.Width(91));
			
			//Padding
			this.padding = EditorGUILayout.IntSlider(this.padding, 0, 10, GUILayout.Width(169));
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		{
			EditorGUI.BeginDisabledGroup(this.atlasPartsList.Count == 0);
			{
				//クリアボタン
				if (GUILayout.Button("Clear", GUILayout.Width(60)))
				{
					for (int i = 0; i < this.atlasPartsList.Count; i++)
					{
						this.atlasPartsList[i].Delete();
					}
					this.atlasPartsList.Clear();
				}
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.Label("Select Texture", GUILayout.Width(91));

			//アトラスに含めたいテクスチャをD&Dするエリア
			Texture2D tex = (Texture2D)EditorGUILayout.ObjectField(null, typeof(Texture2D), false, GUILayout.Width(187));
			if (tex != null)
			{
				if (this.atlasPartsList.Exists(x => x.Equals(tex)))
				{
					Debug.LogWarningFormat("Texture2D'{0}' is already exist.", tex.name);
				}
				else
				{
					this.atlasPartsList.Add(new AtlasParts(tex));
				}
			}
		}
		GUILayout.EndHorizontal();

		//水平線
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

		if (this.atlasPartsList.Count > 0)
		{
			//全アトラスパーツ表示
			for (int i = 0; i < this.atlasPartsList.Count; i++)
			{
				if (!this.atlasPartsList[i].OnGUI())
				{
					this.atlasPartsList[i].Delete();
					this.atlasPartsList.RemoveAt(i);
					break;
				}
			}
		}
	}

	/// <summary>
	/// 保存
	/// </summary>
	private void Save(string path)
	{
		//テクスチャをパッキング
		Texture2D atlasTex = null;
		SpriteMetaData[] spriteSheet = null;
		PackTextures(out atlasTex, out spriteSheet);

		//テクスチャ書き出し
		byte[] bytes = atlasTex.EncodeToPNG();
		File.WriteAllBytes(path, bytes);

		//インポーター設定
		var importer = (TextureImporter)AssetImporter.GetAtPath(path);
		if (importer == null)
		{
			//インポーターが存在していなかったら作成する
			AssetDatabase.ImportAsset(path);
			importer = (TextureImporter)AssetImporter.GetAtPath(path);
		}
		importer.textureType = TextureImporterType.Sprite;
		importer.spriteImportMode = SpriteImportMode.Multiple;
		importer.alphaIsTransparency = true;
		importer.spritesheet = spriteSheet;
		importer.SaveAndReimport();

		for (int i = 0; i < this.atlasPartsList.Count; i++)
		{
			this.atlasPartsList[i].Delete();
		}
		this.atlasPartsList.Clear();
		this.atlasPartsList.Add(new AtlasParts(AssetDatabase.LoadAssetAtPath<Texture2D>(path)));

		//メモリ破棄
		DestroyImmediate(atlasTex);
		atlasTex = null;
		spriteSheet = null;
		bytes = null;
		importer = null;
	}

	/// <summary>
	/// テクスチャパッキング
	/// </summary>
	private void PackTextures(out Texture2D atlasTex, out SpriteMetaData[] spriteSheet)
	{
		var enabledTexList = new List<Texture2D>();
		
		for (int i = 0; i < this.atlasPartsList.Count; i++)
		{
			//パッキングするので読み書き可能に変更
			this.atlasPartsList[i].SetReadable(true);

			//パッキングされるテクスチャたちを取得
			enabledTexList.AddRange(this.atlasPartsList[i].GetEnabledTexture());
		}

		//テクスチャをパッキング
		var packedTex = new Texture2D(1, 1);
		var packedRects = packedTex.PackTextures(enabledTexList.ToArray(), this.padding, 2048);

		//パッキング終わったので読み書き設定を元に戻す
		for (int i = 0; i < this.atlasPartsList.Count; i++)
		{
			this.atlasPartsList[i].RevertReadable();
		}

		//アトラステクスチャ作成（EncodeToPNGするのでフォーマットをARGB32に変更）
		atlasTex = new Texture2D(packedTex.width, packedTex.height, TextureFormat.ARGB32, false);
		atlasTex.SetPixels(packedTex.GetPixels());
		atlasTex.Apply();

		//スプライトシート作成
		spriteSheet = new SpriteMetaData[packedRects.Length];
		for (int i = 0; i < spriteSheet.Length; i++)
		{
			spriteSheet[i].name = enabledTexList[i].name;
			spriteSheet[i].rect.x = packedRects[i].x * atlasTex.width;
			spriteSheet[i].rect.y = packedRects[i].y * atlasTex.height;
			spriteSheet[i].rect.width = packedRects[i].width * atlasTex.width;
			spriteSheet[i].rect.height = packedRects[i].height * atlasTex.height;
		}

		//メモリ破棄
		enabledTexList.Clear();
		enabledTexList = null;
		DestroyImmediate(packedTex);
		packedTex = null;
		packedRects = null;
	}
}

/// <summary>
/// アトラスパーツ
/// </summary>
public class AtlasParts
{
	private bool m_isReadable = false;
	private bool m_isMultiple = false;
	private Texture2D m_mainTexture = null;
	private List<Texture2D> m_textureList = new List<Texture2D>();
	private Dictionary<Texture2D, bool> m_textureEnabled = new Dictionary<Texture2D, bool>();
	private TextureImporter m_importer = null;

	//OnGUI用
	private bool m_isFoldout = true;
	private GUIStyle m_foldoutStyle = null;
	private GUIStyle m_boxStyle = null;
	private GUIStyle m_buttonStyle = null;
	
	/// <summary>
	/// construct
	/// </summary>
	private AtlasParts()
	{
		m_foldoutStyle = new GUIStyle(EditorStyles.foldout);
		m_foldoutStyle.stretchWidth = false;
		m_foldoutStyle.margin.right = 154;

		m_boxStyle = new GUIStyle(GUI.skin.box);
		m_boxStyle.alignment = TextAnchor.MiddleLeft;
		m_boxStyle.richText = true;
		m_boxStyle.margin.left = 4;

		m_buttonStyle = new GUIStyle(GUI.skin.button);
		m_buttonStyle.margin.left = 0;
		m_buttonStyle.margin.right = 0;
	}

	public AtlasParts(Texture2D texture) : this()
	{
		m_mainTexture = texture;
		m_importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(m_mainTexture));
		m_isReadable = m_importer.isReadable;
		m_isMultiple = (m_importer.textureType == TextureImporterType.Sprite)
					&& (m_importer.spriteImportMode == SpriteImportMode.Multiple)
					&& (m_importer.spritesheet != null)
					&& (m_importer.spritesheet.Length > 0);

		if (m_isMultiple)
		{
			//GetPixelsするので読み書き可能に変更する
			if (!m_isReadable)
			{
				m_importer.isReadable = true;
				m_importer.SaveAndReimport();
			}

			foreach (var data in m_importer.spritesheet)
			{
				int x = (int)data.rect.x;
				int y = (int)data.rect.y;
				int w = (int)data.rect.width;
				int h = (int)data.rect.height;

				Texture2D tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
				tex.name = data.name;
				tex.SetPixels(m_mainTexture.GetPixels(x, y, w, h));
				tex.Apply();
				
				m_textureList.Add(tex);
				m_textureEnabled.Add(tex, true);
			}

			//GetPixels終わったので読み書き設定を元に戻す
			if (!m_isReadable)
			{
				m_importer.isReadable = m_isReadable;
				m_importer.SaveAndReimport();
			}
		}
		else
		{
			m_textureList.Add(m_mainTexture);
			m_textureEnabled.Add(m_mainTexture, true);
		}
	}

	/// <summary>
	/// 破棄
	/// </summary>
	public void Delete()
	{
		if (m_textureList != null)
		{
			if (m_isMultiple)
			{
				for (int i = 0; i < m_textureList.Count; i++)
				{
					Object.DestroyImmediate(m_textureList[i]);
					m_textureList[i] = null;
				}
			}
			m_textureList.Clear();
		}

		if (m_textureEnabled != null)
		{
			m_textureEnabled.Clear();
		}

		m_mainTexture = null;
		m_textureList = null;
		m_textureEnabled = null;
		m_importer = null;
		m_foldoutStyle = null;
		m_boxStyle = null;
		m_buttonStyle = null;
	}

	/// <summary>
	/// パーツとして有効か
	/// </summary>
	public bool IsValid()
	{
		return m_textureEnabled.ContainsValue(true);
	}

	/// <summary>
	/// メインテクスチャが同じなら等しい
	/// </summary>
	public bool Equals(Texture2D texture)
	{
		return m_mainTexture == texture;
	}

	/// <summary>
	/// インポーターの読み書き設定を変更する
	/// </summary>
	public void SetReadable(bool readable)
	{
		if (!m_isMultiple && m_importer.isReadable != readable)
		{
			m_importer.isReadable = readable;
			m_importer.SaveAndReimport();
		}
	}

	/// <summary>
	/// 読み書き設定を元の状態に戻す
	/// </summary>
	public void RevertReadable()
	{
		SetReadable(m_isReadable);
	}

	/// <summary>
	/// 有効なテクスチャを取得
	/// </summary>
	public List<Texture2D> GetEnabledTexture()
	{
		return m_textureList.FindAll(x => m_textureEnabled[x]);
	}

	/// <summary>
	/// OnGUI
	/// </summary>
	public bool OnGUI()
	{
		if (m_isMultiple)
		{
			GUILayout.BeginHorizontal();
			{
				//折りたたみ
				m_isFoldout = EditorGUILayout.Foldout(m_isFoldout, "", m_foldoutStyle);

				//メインテクスチャ表示（無効な場合はグレー表示）
				GUI.color = IsValid() ? Color.white : Color.gray;
				{
					
					var rect = GUILayoutUtility.GetLastRect();
					rect.x = 18;
					rect.width = 185;
					EditorGUI.ObjectField(rect, m_mainTexture, typeof(Texture2D), false);
				}
				GUI.color = Color.white;

				//除去ボタン
				if (GUILayout.Button("Remove", GUILayout.Width(60)))
				{
					return false;
				}

				//一括ON/OFFボタン
				if (m_isFoldout)
				{
					var rect = GUILayoutUtility.GetLastRect();
					rect.x += 66;
					rect.width = 88;
					GUI.Box(rect, "ALL", m_boxStyle);
					
					GUILayout.Space(30);

					if (GUILayout.Button("on", m_buttonStyle, GUILayout.Width(30)))
					{
						foreach (var tex in m_textureList)
						{
							m_textureEnabled[tex] = true;
						}
					}
					
					if (GUILayout.Button("off", m_buttonStyle, GUILayout.Width(30)))
					{
						foreach (var tex in m_textureList)
						{
							m_textureEnabled[tex] = false;
						}
					}
				}
			}
			GUILayout.EndHorizontal();

			//折りたたみ内容表示
			if (m_isFoldout)
			{
				//インデント追加
				EditorGUI.indentLevel++;
				
				//全テクスチャ一覧表示
				for (int i = 0; i < m_textureList.Count; i++)
				{
					var tex = m_textureList[i];
					GUILayout.BeginHorizontal();
					{
						//テクスチャ表示（アトラスに含めない場合はグレー表示）
						GUI.color = m_textureEnabled[tex] ? Color.white : Color.gray;
						{
							EditorGUILayout.ObjectField(tex, typeof(Texture2D), false, GUILayout.Width(200));
						}
						GUI.color = Color.white;

						//アトラスに含めるかどうかのチェックボックス
						m_textureEnabled[tex] = GUILayout.Toggle(m_textureEnabled[tex], "");
					}
					GUILayout.EndHorizontal();
				}

				//追加したインデントを戻す
				EditorGUI.indentLevel--;
			}
		}
		else
		{
			GUILayout.BeginHorizontal();
			{
				//メインテクスチャ表示
				EditorGUILayout.ObjectField(m_mainTexture, typeof(Texture2D), false, GUILayout.Width(200));

				//除去ボタン
				if (GUILayout.Button("Remove", GUILayout.Width(60)))
				{
					return false;
				}
			}
			GUILayout.EndHorizontal();
		}

		return true;
	}
}

}//namespace MushaEditor
#endif