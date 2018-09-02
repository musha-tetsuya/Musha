using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Musha {

/// <summary>
/// サウンドマネージャ
/// </summary>
[AddComponentMenu("Musha/Sound/SoundManager")]
public class SoundManager : MonoBehaviour
{
	/// <summary>
	/// マスター音量
	/// </summary>
	[Range(0, 1)]
	[SerializeField]private float m_masterVolume = 1.0f;
	/// <summary>
	/// BGMトラック
	/// </summary>
	[SerializeField]private BgmTrack m_bgmTrack = null;
	/// <summary>
	/// SEトラック
	/// </summary>
	private Dictionary<string, SeTrack> seTrackList = new Dictionary<string, SeTrack>();

	/// <summary>
	/// マスター音量
	/// </summary>
	public float masterVolume
	{
		get
		{
			return this.m_masterVolume;
		}
		set
		{
			this.m_masterVolume = Mathf.Clamp01(value);
			this.ApplyMasterVolume();
		}
	}
	/// <summary>
	/// BGMトラック
	/// </summary>
	public BgmTrack bgmTrack { get { return m_bgmTrack; } }
	
	/// <summary>
	/// マスター音量を適用する
	/// </summary>
	private void ApplyMasterVolume()
	{
		if (this.bgmTrack != null)
		{
			//BGMトラックへの音量反映
			this.bgmTrack.ApplyAudioVolume();
		}
		if (this.seTrackList != null)
		{
			//SEトラックへの音量反映
			foreach (var seTrack in this.seTrackList.Values)
			{
				seTrack.ApplyAudioVolume();
			}
		}
	}

	/// <summary>
	/// 管理しているSEトラックを取得する
	/// </summary>
	public SeTrack GetSeTrack(string key)
	{
		return this.seTrackList[key];
	}

	/// <summary>
	/// 管理するSEトラックを追加する
	/// </summary>
	/// <param name="key">管理キー</param>
	/// <param name="clip">SEクリップ</param>
	public void AddSeTrack(string key, AudioClip clip, int polyphonySize = 2)
	{
		if (!this.seTrackList.ContainsKey(key))
		{
			var track = new GameObject(string.Format("SeTrack[{0}]", key)).AddComponent<SeTrack>();
			track.transform.SetParent(this.transform);
			track.soundManager = this;
			track.clip = clip;
			track.SetPolyphonySize(polyphonySize);
			this.seTrackList.Add(key, track);
		}
	}

	/// <summary>
	/// SEトラックを管理から外す
	/// </summary>
	public void RemoveSeTrack(string key)
	{
		if (this.seTrackList.ContainsKey(key))
		{
			Destroy(this.seTrackList[key].gameObject);
			this.seTrackList.Remove(key);
		}
	}

	/// <summary>
	/// 全SEトラックを管理から外す
	/// </summary>
	public void RemoveAllSeTrack()
	{
		foreach (var seTrack in this.seTrackList.Values)
		{
			Destroy(seTrack.gameObject);
		}
		this.seTrackList.Clear();
	}

#if UNITY_EDITOR
	/// <summary>
	///	SoundManagerのカスタムインスペクター
	/// </summary>
	[CustomEditor(typeof(SoundManager))]
	private class SoundManagerInspector : Editor
	{
		/// <summary>
		/// SEトラックリスト折り畳み表示用
		/// </summary>
		private bool foldoutSeTrack = false;

		/// <summary>
		/// インスペクター描画
		/// </summary>
		public override void OnInspectorGUI()
		{
			var target = (SoundManager)this.target;

			EditorGUI.indentLevel = 0;

			base.OnInspectorGUI();

			//マスター音量の反映
			target.ApplyMasterVolume();

			if (target.seTrackList.Count == 0)
			{
				//管理SEトラック数が0ならDisable表示
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.LabelField("SeTrack");
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				//SEトラック折り畳み表示
				this.foldoutSeTrack = EditorGUILayout.Foldout(this.foldoutSeTrack, "SeTrack");
				if (this.foldoutSeTrack)
				{
					EditorGUI.indentLevel = 1;
					foreach (var x in target.seTrackList)
					{
						EditorGUILayout.ObjectField(x.Key, x.Value, typeof(SeTrack), true);
					}
				}
			}
		}
	}
#endif
}

}//namespace Musha