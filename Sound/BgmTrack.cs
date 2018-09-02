using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Musha {

/// <summary>
/// BGMトラック
/// </summary>
[AddComponentMenu("Musha/Sound/BgmTrack")]
[RequireComponent(typeof(AudioSource))]
public class BgmTrack : MonoBehaviour
{
	/// <summary>
	/// AudioSource
	/// </summary>
	[SerializeField]private AudioSource audioSource = null;
	/// <summary>
	/// SoundManager
	/// </summary>
	[SerializeField]private SoundManager soundManager = null;
	/// <summary>
	/// 音量
	/// </summary>
	[Range(0, 1)]
	[SerializeField]private float m_volume = 1.0f;
	/// <summary>
	/// ループ開始位置
	/// </summary>
	[SerializeField]public int loopStart = 0;
	/// <summary>
	/// ループ終了位置
	/// </summary>
	[SerializeField]public int loopEnd = 0;

	/// <summary>
	/// AudioClip
	/// </summary>
	public AudioClip clip
	{
		get
		{
			return this.audioSource.clip;
		}
		set
		{
			//クリップが変更されたらAudioSourceをストップする
			if (value == null || this.audioSource.clip != value)
			{
				this.Stop();
			}

			this.audioSource.clip = value;
		}
	}
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
	/// ループさせるかどうか
	/// </summary>
	public bool loop
	{
		get { return this.audioSource.loop; }
		set { this.audioSource.loop = value; }
	}
	/// <summary>
	/// TimeSamples
	/// </summary>
	public int timeSamples
	{
		get { return this.audioSource.timeSamples; }
		set { this.audioSource.timeSamples = value; }
	}
	/// <summary>
	/// 停止中かどうか
	/// </summary>
	public bool isStop { get { return !this.isPlaying && !this.isPause; } }
	/// <summary>
	/// 再生中かどうか
	/// </summary>
	public bool isPlaying { get { return this.audioSource.isPlaying; } }
	/// <summary>
	/// 一時停止中かどうか
	/// </summary>
	public bool isPause { get; private set; }
	/// <summary>
	/// マスター音量（SoundManagerがあるならSoundManagerに従う）
	/// </summary>
	private float masterVolume { get { return this.soundManager ? this.soundManager.masterVolume : 1.0f; } }

	/// <summary>
	/// Reset
	/// </summary>
	/// <remarks>Only called by editor mode</remarks>
	private void Reset()
	{
		this.audioSource = this.GetComponent<AudioSource>();
		this.audioSource.loop = true;
		this.audioSource.playOnAwake = false;
	}

	/// <summary>
	/// Awake
	/// </summary>
	private void Awake()
	{
		if (!this.audioSource)
		{
			this.audioSource = this.GetComponent<AudioSource>();
			this.audioSource.loop = true;
			this.audioSource.playOnAwake = false;
		}
	}

	/// <summary>
	/// Update
	/// </summary>
	private void Update()
	{
		//ループ監視
		if (this.isPlaying && this.loop && this.loopEnd > this.loopStart)
		{
			//ループ終了位置を超えたら
			int overSamples = this.timeSamples - this.loopEnd;
			if (overSamples >= 0)
			{
				//ループ開始位置に戻す
				this.timeSamples = this.loopStart + overSamples;
			}
		}
	}

	/// <summary>
	/// 現在の音量とマスター音量からオーディオ出力音量を決定する
	/// </summary>
	public void ApplyAudioVolume()
	{
		this.audioSource.volume = this.volume * this.masterVolume;
	}

	/// <summary>
	/// 再生
	/// </summary>
	public void Play()
	{
		if (this.clip != null && !this.isPlaying)
		{
			this.isPause = false;
			this.audioSource.Play();
		}
	}

	/// <summary>
	/// 一時停止
	/// </summary>
	public void Pause()
	{
		if (this.clip != null)
		{
			this.isPause = true;
			this.audioSource.Pause();
		}
	}

	/// <summary>
	/// 停止
	/// </summary>
	public void Stop()
	{
		if (this.clip != null)
		{
			this.isPause = false;
			this.timeSamples = 0;
			this.audioSource.Stop();
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// BgmTrackのカスタムインスペクター
	/// </summary>
	[CustomEditor(typeof(BgmTrack))]
	private class BgmTrackInspector : Editor
	{
		/// <summary>
		/// 状態
		/// </summary>
		private string[] statusNames =
		{
			"isStop",
			"isPlaying",
			"isPause",
		};

		private SerializedProperty audioSource = null;
		private SerializedProperty soundManager = null;
		private SerializedProperty volume = null;
		private SerializedProperty loopStart = null;
		private SerializedProperty loopEnd = null;

		/// <summary>
		/// OnEnable
		/// </summary>
		private void OnEnable()
		{
			this.audioSource = this.serializedObject.FindProperty("audioSource");
			this.soundManager = this.serializedObject.FindProperty("soundManager");
			this.volume = this.serializedObject.FindProperty("m_volume");
			this.loopStart = this.serializedObject.FindProperty("loopStart");
			this.loopEnd = this.serializedObject.FindProperty("loopEnd");
		}

		/// <summary>
		/// インスペクター描画
		/// </summary>
		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();

			var target = (BgmTrack)this.target;

			//AudioSource表示
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(this.audioSource);
			EditorGUI.EndDisabledGroup();

			//SoundManager表示
			EditorGUILayout.PropertyField(this.soundManager);
			
			//状態表示
			int statusNum = target.isPause ? 2 : target.isPlaying ? 1 : 0;
			EditorGUILayout.Popup("Status", statusNum, this.statusNames);

			//音量表示
			EditorGUILayout.PropertyField(this.volume);
			target.ApplyAudioVolume();//オーディオ音量への反映

			//最大サンプル値
			var maxSamples = target.audioSource.clip ? target.audioSource.clip.samples - 1 : 0;

			//ループ開始位置表示
			loopStart.intValue = EditorGUILayout.IntSlider("LoopStart", loopStart.intValue, 0, maxSamples);

			//ループ終了位置表示
			loopEnd.intValue = EditorGUILayout.IntSlider("LoopEnd", loopEnd.intValue, 0, maxSamples);

			EditorGUI.BeginDisabledGroup(!Application.isPlaying);
			{
				//現在のサンプル値を表示
				var beforeSamples = target.audioSource.timeSamples;
				var afterSamples = EditorGUILayout.IntSlider("TimeSamples", beforeSamples, 0, maxSamples);
				if (beforeSamples != afterSamples)
				{
					//サンプル位置の変更を反映する
					target.audioSource.timeSamples = afterSamples;
				}

				//再生中は現在サンプル値が常に変化するので再描画する
				if (target.isPlaying)
				{
					this.Repaint();
				}
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!Application.isPlaying);
			GUILayout.BeginHorizontal();
			{
				//再生ボタン
				EditorGUI.BeginDisabledGroup(target.isPlaying);
				if (GUILayout.Button("Play"))
				{
					target.Play();
				}
				EditorGUI.EndDisabledGroup();

				//一時停止ボタン
				EditorGUI.BeginDisabledGroup(!target.isPlaying);
				if (GUILayout.Button("Pause"))
				{
					target.Pause();
				}
				EditorGUI.EndDisabledGroup();

				//停止ボタン
				EditorGUI.BeginDisabledGroup(target.isStop);
				if (GUILayout.Button("Stop"))
				{
					target.Stop();
				}
				EditorGUI.EndDisabledGroup();
			}
			GUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			
			this.serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}

}//namespace Musha

