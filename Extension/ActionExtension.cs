using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaEngine {

/// <summary>
/// Action拡張
/// </summary>
public static class ActionExtension
{
	/// <summary>
	/// Nullチェック付きInvoke
	/// </summary>
	public static void SafetyInvoke(this Action action)
	{
		if (action != null)
		{
			action.Invoke();
		}
	}

	/// <summary>
	/// Nullチェック付きInvoke
	/// </summary>
	public static void SafetyInvoke<T>(this Action<T> action, T obj)
	{
		if (action != null)
		{
			action.Invoke(obj);
		}
	}
}

}//namespace MushaEngine