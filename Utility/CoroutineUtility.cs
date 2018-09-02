using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha {

public static class CoroutineUtility
{
	/// <summary>
	/// 指定フレーム数待ってから処理を実行
	/// </summary>
	public static IEnumerator WaitForFrameAction(int frameCount, Action action)
	{
		for (int i = 0; i < frameCount; i++)
		{
			yield return null;
		}
		action();
	}

	/// <summary>
	/// EndOfFrameを待ってから処理を実行
	/// </summary>
	public static IEnumerator WaitForEndOfFrameAction(Action action)
	{
		yield return new WaitForEndOfFrame();
		action();
	}

	/// <summary>
	/// 条件を満たすまで待ってから処理を実行
	/// </summary>
	public static IEnumerator WaitUntilAction(Func<bool> predicate, Action action)
	{
		yield return new WaitUntil(predicate);
		action();
	}

	/// <summary>
	/// 条件を満たしている間待って処理を実行
	/// </summary>
	public static IEnumerator WaitWhileAction(Func<bool> predicate, Action action)
	{
		yield return new WaitWhile(predicate);
		action();
	}
}

}//namespace Musha
