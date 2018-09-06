using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha {

/// <summary>
/// IEnumerable拡張
/// </summary>
public static class IEnumerableExtension
{
	/// <summary>
	/// 全要素に処理を行う
	/// </summary>
	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (T element in source)
		{
			action(element);
		}
	}
}

}//namespace Musha