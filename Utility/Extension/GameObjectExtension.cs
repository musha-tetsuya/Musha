using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushaSystem {

/// <summary>
/// GameObject拡張
/// </summary>
public static class GameObjectExtension
{
	/// <summary>
	/// Hierarchyパスを取得
	/// </summary>
	public static string GetPath(this GameObject gobj)
	{
		string path = gobj.name;
		var parent = gobj.transform.parent;
		while (parent != null)
		{
			path = parent.name + "/" + path;
			parent = parent.parent;
		}
		return path;
	}
}

}//namespace MushaSystem