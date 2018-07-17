#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MushaEditor {

/// <summary>
/// EditorPrefsProperty
/// </summary>
public abstract class EditorPrefsProperty<T>
{
	/// <summary>
	/// EditorPrefsキー
	/// </summary>
	protected string key = null;
	/// <summary>
	/// キーに対する値が無かった場合のデフォルト値
	/// </summary>
	protected T defaultValue = default(T);
	/// <summary>
	/// デフォルト値を返却する追加条件
	/// </summary>
	protected Func<T, bool> condition = null;
	/// <summary>
	/// 値
	/// </summary>
	protected abstract T val_ { get; set; }
	/// <summary>
	/// 値
	/// </summary>
	public T val
	{
		get
		{
			if (EditorPrefs.HasKey(this.key))
			{
				if (this.condition == null || this.condition(this.val_))
				{
					return this.val_;
				}
			}
			return this.defaultValue;
		}
		set
		{
			this.val_ = value;
		}
	}
}

/// <summary>
/// string値のEditorPrefs
/// </summary>
public class EditorPrefsString : EditorPrefsProperty<string>
{
	/// <summary>
	/// construct
	/// </summary>
	public EditorPrefsString(string key, string defaultValue, Func<string, bool> condition = null)
	{
		this.key = key;
		this.defaultValue = defaultValue;
		this.condition = condition;
	}

	/// <summary>
	/// 値
	/// </summary>
	protected override string val_
	{
		get { return EditorPrefs.GetString(this.key); }
		set { EditorPrefs.SetString(this.key, value); }
	}
}

}//namespace MushaEditor
#endif