#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using ParseDictionary = System.Collections.Generic.Dictionary<System.Type, System.Func<string, object>>;

namespace MushaSystem.EditorTool {

/// <summary>
/// マスターデータメーカー
/// </summary>
public class MasterDataMaker : EditorWindow
{
	/// <summary>
	/// CSファイルテンプレート
	/// </summary>
	private const string CS_TEMPLATE =
		"using System;"
	+ "\nusing System.Collections;"
	+ "\nusing System.Collections.Generic;"
	+ "\nusing UnityEngine;"
	+ "\n"
	+ "\npublic class {0} : ScriptableObject"
	+ "\n{{"
	+ "\n    public List<Param> param = new List<Param>();"
	+ "\n"
	+ "\n    [Serializable]"
	+ "\n    public class Param"
	+ "\n    {{{1}"
	+ "\n    }}"
	+ "\n}}"
	;
	/// <summary>
	/// string値を指定の型の値に変換する辞書
	/// </summary>
	private static readonly ParseDictionary parse = new ParseDictionary()
	{
		{ typeof(Boolean),	(s) => Boolean.Parse(s)	},
		{ typeof(Char),		(s) => Char.Parse(s)	},
		{ typeof(SByte),	(s) => SByte.Parse(s)	},
		{ typeof(Byte),		(s) => Byte.Parse(s)	},
		{ typeof(Int16),	(s) => Int16.Parse(s)	},
		{ typeof(UInt16),	(s) => UInt16.Parse(s)	},
		{ typeof(Int32),	(s) => Int32.Parse(s)	},
		{ typeof(UInt32),	(s) => UInt32.Parse(s)	},
		{ typeof(Int64),	(s) => Int64.Parse(s)	},
		{ typeof(UInt64),	(s) => UInt64.Parse(s)	},
		{ typeof(Single),	(s) => Single.Parse(s)	},
		{ typeof(Double),	(s) => Double.Parse(s)	},
		{ typeof(DateTime),	(s) => DateTime.Parse(s)},
		{ typeof(String),	(s) => s				},
	};
	/// <summary>
	/// 変換対象のCSVパスリスト
	/// </summary>
	private List<string> targetCsvPathList = new List<string>();
	/// <summary>
	/// 選択中CSVパス一覧表示のスクロール位置
	/// </summary>
	private Vector2 scrollPosition = Vector2.zero;
	/// <summary>
	/// CSVディレクトリパス
	/// </summary>
	private EditorPrefsString sourceCsvDirectoryPath = null;
	/// <summary>
	/// CSファイル保存先
	/// </summary>
	private EditorPrefsString destCsDirectoryPath = null;
	/// <summary>
	/// ScriptableObject保存先
	/// </summary>
	private EditorPrefsString destScriptableObjectDirectoryPath = null;

	/// <summary>
	/// ウィンドウを開く
	/// </summary>
	[MenuItem("MushaSystem/MasterDataMaker")]
	private static void Open()
	{
		EditorWindow.GetWindow<MasterDataMaker>();
	}

	/// <summary>
	/// 初期化処理
	/// </summary>
	private void OnEnable()
	{
		this.sourceCsvDirectoryPath = new EditorPrefsString(GetType().FullName + ".sourceCsvDirectoryPath", Application.dataPath, Directory.Exists);
		this.destCsDirectoryPath = new EditorPrefsString(GetType().FullName + ".destCsDirectoryPath", Application.dataPath);
		this.destScriptableObjectDirectoryPath = new EditorPrefsString(GetType().FullName + ".destScriptableObjectDirectoryPath", null, (val) => Directory.Exists(Application.dataPath + val));
	}

	/// <summary>
	/// OnGUI
	/// </summary>
	private void OnGUI()
	{
		GUILayout.Label("CSVファイルの選択");

		GUILayout.BeginHorizontal();
		{
			//CSVファイル追加ボタン
			if (GUILayout.Button("ファイル追加", GUILayout.Width(150)))
			{
				string path = EditorUtility.OpenFilePanelWithFilters("CSVファイルの選択", this.sourceCsvDirectoryPath.val, new string[] { "CSV files", "csv" });
				if (!string.IsNullOrEmpty(path))
				{
					//デフォルトCSVディレクトリパス変更
					this.sourceCsvDirectoryPath.val = Path.GetDirectoryName(path);
					//変換対象CSV追加
					if (!this.targetCsvPathList.Contains(path))
					{
						this.targetCsvPathList.Add(path);
					}
				}
			}

			//CSVフォルダ追加ボタン
			if (GUILayout.Button("フォルダ追加", GUILayout.Width(150)))
			{
				string path = EditorUtility.OpenFolderPanel("CSVフォルダの選択", this.sourceCsvDirectoryPath.val, "");
				if (!string.IsNullOrEmpty(path))
				{
					//デフォルトCSVディレクトリパス変更
					this.sourceCsvDirectoryPath.val = path;
					//ディレクトリ内CSVファイルを変換対象に追加
					this.targetCsvPathList = this.targetCsvPathList
						.Union(Directory.GetFiles(path, "*.csv", SearchOption.AllDirectories).Select(x => x.Replace("\\", "/")))
						.Union(Directory.GetFiles(path, "*.CSV", SearchOption.AllDirectories).Select(x => x.Replace("\\", "/")))
						.ToList();
				}
			}
		}
		GUILayout.EndHorizontal();

		if (this.targetCsvPathList.Count > 0)
		{
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, GUILayout.MaxHeight(150));
			{
				//変換対象CSV一覧表示
				for (int i = 0; i < this.targetCsvPathList.Count; i++)
				{
					GUILayout.BeginHorizontal();
					{
						//除去ボタン
						if (GUILayout.Button("×", GUILayout.ExpandWidth(false)))
						{
							this.targetCsvPathList.RemoveAt(i);
							break;
						}

						//CSVパス表示
						GUILayout.Label(this.targetCsvPathList[i]);
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndScrollView();

			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
		}

		GUILayout.BeginHorizontal();
		EditorGUI.BeginDisabledGroup(this.targetCsvPathList.Count == 0);
		{
			if (GUILayout.Button("CSファイルに変換", GUILayout.Width(150)))
			{
				string path = EditorUtility.SaveFolderPanel("CSファイル保存先選択", this.destCsDirectoryPath.val, "");
				if (!string.IsNullOrEmpty(path))
				{
					//保存先決定
					this.destCsDirectoryPath.val = path;
					//CSファイルに変換
					this.CsvToCs();
				}
			}

			if (GUILayout.Button("ScriptableObjectに変換", GUILayout.Width(150)))
			{
				string path = EditorUtility.SaveFolderPanel("ScriptableObject保存先選択", Application.dataPath + this.destScriptableObjectDirectoryPath.val, "");
				if (!string.IsNullOrEmpty(path))
				{
					if (!path.Contains(Application.dataPath))
					{
						Debug.LogError("プロジェクト内のフォルダを選択して下さい");
					}
					else
					{
						//保存先決定
						this.destScriptableObjectDirectoryPath.val = path.Replace(Application.dataPath, "");
						//ScriptableObjectに変換
						this.CsvToScriptableObject();
					}
				}
			}
		}
		EditorGUI.EndDisabledGroup();
		GUILayout.EndHorizontal();
	}

	/// <summary>
	/// CSVをCSに変換
	/// </summary>
	private void CsvToCs()
	{
		foreach (var csvPath in this.targetCsvPathList)
		{
			//作成するクラス名とCSファイルのパス
			string className = Path.GetFileNameWithoutExtension(csvPath);
			string csPath = string.Format("{0}/{1}.cs", this.destCsDirectoryPath.val, className);

			//CSV読み込み＆CSファイル作成
			using (var reader = new StreamReader(csvPath))
			using (var writer = new StreamWriter(csPath, false, Encoding.UTF8))
			{
				//1行目：変数名
				string[] fieldName = reader.ReadLine().Split(',');
				//2行目：変数の型
				string[] fieldType = reader.ReadLine().Split(',');
				//変数テキスト作成
				string fieldText = null;
				for (int i = 0; i < fieldName.Length; i++)
				{
					fieldText += string.Format("\n        public {0} {1};", fieldType[i], fieldName[i]);
				}
				//CSファイルに書き込み
				writer.Write(string.Format(CS_TEMPLATE, className, fieldText));
			}
		}

		AssetDatabase.Refresh();
	}

	/// <summary>
	/// CSVをScriptableObjectに変換
	/// </summary>
	private void CsvToScriptableObject()
	{
		foreach (var csvPath in this.targetCsvPathList)
		{
			//生成するScriptableObjectクラス名
			string className = Path.GetFileNameWithoutExtension(csvPath);
			//生成するScriptableObjectの型をチェック
			var classType = Type.GetType(className);
			if (classType == null)
			{
				Debug.LogErrorFormat("class {0} が存在しません。"
								+ "\n・CSVからCSファイルの作成"
								+ "\n・CSファイルが参照可能かどうか"
								, className);
				continue;
			}

			//CSV読み込み
			using (var reader = new StreamReader(csvPath))
			{
				//ScriptableObject生成
				var scriptableObject = ScriptableObject.CreateInstance(classType);
				//パラメータリスト
				var paramList = (IList)classType.GetField("param").GetValue(scriptableObject);
				//パラメータの型
				var paramType = Type.GetType(className + "+Param");
				//1行目：変数名
				string[] fieldName = reader.ReadLine().Split(',');
				//2行目：変数の型（読み捨て）
				reader.ReadLine();
				//3行目～：変数の値を読み込み
				while (!reader.EndOfStream)
				{
					//パラメータインスタンス生成
					var paramInstance = Activator.CreateInstance(paramType);
					//変数の値
					string[] fieldValues = reader.ReadLine().Split(',');
					for (int i = 0; i < fieldValues.Length; i++)
					{
						//変数名からパラメータの変数情報取得
						var fieldInfo = paramType.GetField(fieldName[i]);
						//文字列を変数値に変換
						var fieldValue = parse[fieldInfo.FieldType](fieldValues[i]);
						//変数値を設定
						fieldInfo.SetValue(paramInstance, fieldValue);
					}
					//パラメータリストに追加
					paramList.Add(paramInstance);
				}
				//ScriptableObject保存
				string scriptableObjectPath = string.Format("Assets{0}/{1}.asset", this.destScriptableObjectDirectoryPath.val, className);
				AssetDatabase.CreateAsset(scriptableObject, scriptableObjectPath);
			}
		}
	}
}

}//namespace MushaSystem.EditorTool
#endif