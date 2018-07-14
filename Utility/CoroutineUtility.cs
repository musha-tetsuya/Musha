using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaEngine {

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
}

}//namespace MushaEngine
