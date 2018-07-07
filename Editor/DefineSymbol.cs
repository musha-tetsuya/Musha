using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MushaEditor {
    
/// <summary>
/// Defineシンボル操作
/// partialでコンストラクタを定義し、シンボルを追加可能
/// </summary>
public partial class DefineSymbol : EditorWindow
{
    //----	field	-----------------------------------------------------------------------------------
    private bool[] mFlag;

    //partialコンストラクタでシンボル追加可能
    private List<string> mSymbolList = new List<string>
    {
        "STREAMINGASSETS_SERVER",
        "USE_ASSETBUNDLE",
        "INPKG_SERVER",
    };
    //----	method	-----------------------------------------------------------------------------------
    /// <summary>
    /// ウィンドウを開く
    /// </summary>
    [MenuItem("MushaEditor/DefineSymbol")]
    private static void Open()
    {
        EditorWindow.GetWindow<DefineSymbol>();
    }
    /// <summary>
    /// 起動時
    /// </summary>
    private void Awake()
    {
        //設定されているシンボル文字列
        string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

        //一時リスト
        var tmp = new List<string>();

        //「；」で分割したシンボルをリストアップ
        if (!string.IsNullOrEmpty(symbols))
        {
            tmp.AddRange(symbols.Split(';'));
        }

        //全シンボルをリスト化
        for (int i = 0; i < tmp.Count; i++)
            if (!mSymbolList.Contains(tmp[i]))
                mSymbolList.Add(tmp[i]);

        //フラグ作成
        mFlag = new bool[mSymbolList.Count];
        for (int i = 0; i < mSymbolList.Count; i++)
            mFlag[i] = tmp.Contains(mSymbolList[i]);
    }
    /// <summary>
    /// 描画
    /// </summary>
    private void OnGUI()
    {
        //反映ボタン
        if (GUILayout.Button("反映"))
        {
            string symbols = "";
            for (int i = 0; i < mFlag.Length; i++)
            {
                if (mFlag[i])
                {
                    //既にシンボルが設定されているので「;」で区切る
                    if (!string.IsNullOrEmpty(symbols))
                    {
                        symbols += ";";
                    }
                    //フラグが立っているのでシンボルを有効に
                    symbols += mSymbolList[i];
                }
            }
            //シンボルを反映
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
            Debug.Log(symbols);
        }
        //トグル描画
        for (int i = 0; i < mFlag.Length; i++)
        {
            mFlag[i] = GUILayout.Toggle(mFlag[i],mSymbolList[i],GUILayout.ExpandWidth(false));
        }
    }
}

}//namespace MushaEditor
