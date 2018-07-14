using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaEngine {

/// <summary>
/// WWW拡張
/// </summary>
public static class WWWExtension
{
	/// <summary>
	/// 完了待ち（タイムアウト付き）
	/// </summary>
	public static IEnumerator WaitOrTimeout(this WWW www, float timeout = 30f)
	{
		timeout += Time.realtimeSinceStartup;

		while (!www.isDone && Time.realtimeSinceStartup < timeout)
		{
			yield return null;
		}
	}
}

}//namespace MushaEngine