using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Musha {

/// <summary>
/// SEトラック
/// </summary>
[AddComponentMenu("Musha/Sound/SeTrack")]
public class SeTrack : MonoBehaviour
{
	/// <summary>
	/// AudioClip
	/// </summary>
	[SerializeField]public AudioClip clip = null;
	/// <summary>
	/// AudioSourceリスト（配列数分だけ同時発音出来る）
	/// </summary>
	[SerializeField]private AudioSource[] audioSource = new AudioSource[0];
	/// <summary>
	/// 音量
	/// </summary>
	[Range(0, 1)]
	[SerializeField]private float m_volume = 1.0f;

	/// <summary>
	/// SoundManager
	/// </summary>
	[NonSerialized]public SoundManager soundManager = null;
	/// <summary>
	/// フリーなAudioSource番号
	/// </summary>
	private int freeNum = 0;
	/// <summary>
	/// マスター音量（SoundManagerがあるならSoundManagerに従う）
	/// </summary>
	private float masterVolume { get { return this.soundManager ? this.soundManager.masterVolume : 1.0f; } }
	/// <summary>
	/// 音量
	/// </summary>
	public float volume
	{
		get
		{
			return this.m_volume;
		}
		set
		{
			this.m_volume = Mathf.Clamp01(value);
			this.ApplyAudioVolume();
		}
	}
	/// <summary>
	/// 同時発音数
	/// </summary>
	public int polyphonySize { get { return this.audioSource.Length; } }

	/// <summary>
	/// OnDestroy
	/// </summary>
	private void OnDestroy()
	{
		this.clip = null;
		this.audioSource = null;
		this.soundManager = null;
	}

	/// <summary>
	/// 同時発音数を設定する
	/// </summary>
	public void SetPolyphonySize(int size)
	{
		this.freeNum = 0;

		//現在のAudioSourceリストから重複を除いた配列を一時確保
		var beforeAudioSource = this.audioSource.Distinct().ToArray();
		//AudioSourceリストをリサイズ
		this.audioSource = new AudioSource[size];
		//リサイズしたAudioSourceリストに一時確保した内容をコピー
		for (int i = 0; i < size; i++)
		{
			if (i < beforeAudioSource.Length)
			{
				this.audioSource[i] = beforeAudioSource[i];
			}
		}

		//子供の中でまだAudioSourceリストに含まれていないものを取得
		var freeChildren = this.GetComponentsInChildren<AudioSource>().Except(audioSource).ToList();

		//AudioSourceリストに空きがあるなら
		if (this.audioSource.Any(x => x == null))
		{
			for (int i = 0; i < size; i++)
			{
				if (this.audioSource[i] == null)
				{
					if (freeChildren.Count > 0)
					{
						//子供をリストの空き部分で管理
						this.audioSource[i] = freeChildren[0];
						freeChildren.RemoveAt(0);
					}
					else
					{
						//新規にAudioSourceを作成してリストの空きを埋める
						var gobj = new GameObject("AudioSource " + i);
						this.audioSource[i] = gobj.AddComponent<AudioSource>();
						this.audioSource[i].transform.SetParent(this.transform);
						this.audioSource[i].loop = false;
						this.audioSource[i].playOnAwake = false;
					}
				}
			}
		}

		//AudioSourceリストで管理出来ない子供を破棄する
		for (int i = 0, imax = freeChildren.Count; i < imax; i++)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				DestroyImmediate(freeChildren[i].gameObject);
				continue;
			}
#endif
			Destroy(freeChildren[i].gameObject);
		}

		//不要になった変数の解放
		beforeAudioSource = null;
		freeChildren.Clear();
		freeChildren = null;
	}

	/// <summary>
	/// 現在の音量とマスター音量からオーディオ出力音量を決定する
	/// </summary>
	public void ApplyAudioVolume()
	{
		for (int i = 0, imax = this.polyphonySize; i < imax; i++)
		{
			if (this.audioSource[i] != null)
			{
				this.audioSource[i].volume = this.volume * this.masterVolume;
			}
		}
	}

	/// <summary>
	/// 再生
	/// </summary>
	public void Play()
	{
		if (this.freeNum < this.polyphonySize)
		{
			if (this.audioSource[this.freeNum] == null)
			{
				Debug.LogWarningFormat("can't play se. {0}/audioSource[{1}] is null.", this.gameObject.GetPath(), this.freeNum);
				return;
			}

			//フリーなAudioSourceで再生する
			this.audioSource[this.freeNum].clip = this.clip;
			this.audioSource[this.freeNum].Stop();
			this.audioSource[this.freeNum].Play();

			//使用中になったので次のAudioSourceをフリーにする
			this.freeNum = (this.freeNum + 1) % this.polyphonySize;
		}
	}

	/// <summary>
	/// 停止
	/// </summary>
	public void Stop()
	{
		//全て停止
		for (int i = 0, imax = this.polyphonySize; i < imax; i++)
		{
			if (this.audioSource[i] != null)
			{
				this.audioSource[i].Stop();
			}
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// SeTrackのカスタムインスペクター
	/// </summary>
	[CustomEditor(typeof(SeTrack))]
	private class SeTrackInspector : Editor
	{
		/// <summary>
		/// インスペクター描画
		/// </summary>
		public override void OnInspectorGUI()
		{
			var target = (SeTrack)this.target;

			base.OnInspectorGUI();

			//オーディオ音量への反映
			target.ApplyAudioVolume();

			//SoundManager表示
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("SoundManager", target.soundManager, typeof(SoundManager), true);
			EditorGUI.EndDisabledGroup();

			//同時発音数反映ボタン
			if (GUILayout.Button("ApplyPolyphonySize", GUILayout.ExpandWidth(false)))
			{
				target.SetPolyphonySize(target.polyphonySize);
			}

			EditorGUI.BeginDisabledGroup(!Application.isPlaying);
			GUILayout.BeginHorizontal();
			{
				//再生ボタン
				if (GUILayout.Button("Play"))
				{
					target.Play();
				}
				//停止ボタン
				if (GUILayout.Button("Stop"))
				{
					target.Stop();
				}
			}
			GUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
		}
	}
#endif
}

}//namespace Musha