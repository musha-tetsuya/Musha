#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace MushaEditor {
	
/// <summary>
/// Defineシンボル操作
/// partialでコンストラクタを定義し、シンボルを追加可能
/// </summary>
public partial class DefineSymbolSetting : EditorWindow
{
	/// <summary>
	/// ウィンドウに表示するシンボル一覧
	/// partialコンストラクタでシンボル追加可能
	/// </summary>
	private Dictionary<string, bool> symbolList = new Dictionary<string, bool>
	{
		{ "STREAMINGASSETS_SERVER", false },
	};

	/// <summary>
	/// ウィンドウを開く
	/// </summary>
	[MenuItem("MushaEditor/DefineSymbolSetting")]
	private static void Open()
	{
		EditorWindow.GetWindow<DefineSymbolSetting>();
	}

	/// <summary>
	/// OnEnable（起動時やシンボル反映して再コンパイルかかった時）
	/// </summary>
	private void OnEnable()
	{
		//設定されているシンボル文字列を取得
		string symbolLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

		//現在有効状態のシンボル
		string[] currentEnabledSymbols = null;
		if (!string.IsNullOrEmpty(symbolLine))
		{
			//「；」で分割
			currentEnabledSymbols = symbolLine.Split(';');

			//GUI表示するシンボルリストの内容を決定
			foreach (var symbol in currentEnabledSymbols)
			{
				if (this.symbolList.ContainsKey(symbol))
				{
					this.symbolList[symbol] = true;
				}
				else
				{
					this.symbolList.Add(symbol, true);
				}
			}
		}
	}
	/// <summary>
	/// GUI描画
	/// </summary>
	private void OnGUI()
	{
		//反映ボタン
		if (GUILayout.Button("反映"))
		{
			//有効になっているシンボルを「;」区切りのstringにする
			var enabledSymbols = this.symbolList.Where(x => x.Value).ToArray();
			string symbolLine = null;
			for (int i = 0; i < enabledSymbols.Length; i++)
			{
				//２つ目以降は「;」で区切る
				if (i > 0)
				{
					symbolLine += ";";
				}
				symbolLine += enabledSymbols[i].Key;
			}

			//シンボルを反映
			PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbolLine);
			Debug.Log(symbolLine);
		}

		//トグル描画
		foreach (var key in this.symbolList.Keys.ToArray())
		{
			this.symbolList[key] = GUILayout.Toggle(this.symbolList[key], key, GUILayout.ExpandWidth(false));
		}
	}
}

}//namespace MushaEditor
#endif