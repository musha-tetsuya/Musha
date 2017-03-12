using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha
{
	/// <summary>
	/// シーン基礎
	/// </summary>
	public class BaseScene : MonoBehaviour
	{
		/// <summary>
		/// Awake
		/// </summary>
		protected virtual void Awake()
		{
			Sys.Create();
		}
	}
}
