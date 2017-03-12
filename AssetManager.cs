using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Musha
{
	/// <summary>
	/// アセット管理
	/// </summary>
	public partial class AssetManager : MonoBehaviour
	{
		//----	enum	-----------------------------------------------------------------------------------
		private enum ERRORCODE
		{
			NONE = 0,
			SERVER_CRC_DL,		//サーバーCRCダウンロードエラー
			SERVER_CRC_DATA,	//サーバーCRC内容エラー
			SERVER_CRC_LINE,	//サーバーCRC内容エラー
			SERVER_CRC_PARSE,	//サーバーCRC変換エラー
			ASSET_NOT_EXIST,	//サーバーに存在しないアセットを読み込もうとした
			ASSET_DL,			//アセットダウンロードエラー
			ASSET_DL_TIMEOUT,	//アセットダウンロードタイムアウトエラー
		}
		//----	field	-----------------------------------------------------------------------------------
		private bool IsReady;
		private float LimitTime = 0.5f / Define.FRAMERATE;
		private string LocalCRCPath;	//ローカルCRCバイナリのパス
		private System.Action OnStop;
		private List<Task.Request> RequestList = new List<Task.Request>();
		private List<Task> TaskList = new List<Task>();
		private List<Resource> ResourceList = new List<Resource>();
		private Dictionary<string, AssetData> AssetList = new Dictionary<string, AssetData>();
		//----	method	-----------------------------------------------------------------------------------
		/// <summary>
		/// 起動時
		/// </summary>
		private void Awake()
		{
			LocalCRCPath = Define.LOCAL_DATA_PATH + "list.bytes";
		}
		/// <summary>
		/// 破棄
		/// </summary>
		private void OnDestroy()
		{
			ClearAll(true);
			RequestList.Clear();
			TaskList.Clear();
			ResourceList.Clear();
			AssetList.Clear();
		}
		/// <summary>
		/// 初期化
		/// </summary>
#if USE_ASSETBUNDLE
		private IEnumerator Start()
#else 
		private void Start()
#endif
		{
#if USE_ASSETBUNDLE
			//リソースバージョンを通信取得するまで待機
			while (string.IsNullOrEmpty(Define.RESOURCE_VERSION))
			{
				yield return null;
			}

			AssetList.Clear();
			{
				//サーバーのCRCデータ読み込み
				using (WWW www = new WWW(Define.SERVER_ASSET_PATH + "list.csv"))
				{
					//csvダウンロード待ち
					yield return www;

					//ダウンロードエラー
					if (!string.IsNullOrEmpty(www.error))
					{
						Debug.LogError("サーバーCRCのダウンロードに失敗しました\n" + www.error);
						MessageBox.Open(GetErrorMsg(ERRORCODE.SERVER_CRC_DL), null, false);
						yield break;
					}

					//csv内容エラー
					if (string.IsNullOrEmpty(www.text))
					{
						Debug.LogError("サーバーCRCの内容が空です");
						MessageBox.Open(GetErrorMsg(ERRORCODE.SERVER_CRC_DATA), null, false);
						yield break;
					}

					//csv読み取り
					string[] line = www.text.Split('\n');
					foreach (var str in line)
					{
						if (!string.IsNullOrEmpty(str))
						{
							string[] token = str.Split(',');

							//csv内容エラー
							if (token.Length != 2)
							{
								Debug.LogError("サーバーCRCの内容が不正です\n" + str);
								MessageBox.Open(GetErrorMsg(ERRORCODE.SERVER_CRC_LINE), null, false);
								yield break;
							}

							//CRC変換エラー
							long crc = -1;
							if (!long.TryParse(token[1], out crc))
							{
								Debug.LogError("CRCをlongに変換出来ませんでした\n" + token[1]);
								MessageBox.Open(GetErrorMsg(ERRORCODE.SERVER_CRC_PARSE), null, false);
								yield break;
							}
						
							AssetList.Add(token[0], new AssetData(token[0], crc));
						}
					}
				}

				//ローカルのCRCデータ読み込み
				if (File.Exists(LocalCRCPath))
				{
					using (FileStream fs = new FileStream(LocalCRCPath, FileMode.Open, FileAccess.Read))
					using (BinaryReader br = new BinaryReader(fs))
					{
						while (br.BaseStream.Position < br.BaseStream.Length)
						{
							string assetName = br.ReadString();
							long localCRC = br.ReadInt64();
							if (AssetList.ContainsKey(assetName))
							{
								AssetList[assetName].LocalCRC = localCRC;
							}
						}
					}
				}
			}
#endif
			//準備完了
			IsReady = true;

			//共通アセット読み込み
			SendMessage("LoadCommonAsset", SendMessageOptions.DontRequireReceiver);

			//リクエスト実行
			for (int i = 0; i < RequestList.Count; i++)
			{
				RequestList[i].Call();
			}
			RequestList.Clear();
		}
		/// <summary>
		/// エラーメッセージ
		/// </summary>
		private static string GetErrorMsg(ERRORCODE errorcode)
		{
			switch (errorcode)
			{
				case ERRORCODE.ASSET_DL_TIMEOUT:
				return "assetmanager timeout";
				default:
				return "assetmanager error (" + (int)errorcode + ")";
			}
		}
		/// <summary>
		/// 処理中
		/// </summary>
		public bool IsBusy
		{
			get { return !IsReady || 0 < RequestList.Count + TaskList.Count; }
		}
		/// <summary>
		/// 処理
		/// </summary>
		public void Run()
		{
			//・タスクが残っている
			//・エラーを起こしていない
			//・先頭のタスクが未完了
			while (TaskList.Count > 0 && !TaskList[0].IsError && !TaskList[0].IsEnd)
			{
				//先頭のタスクを処理
				var task = TaskList[0];
				task.Update();
				//タスク完了したか？
				if (task.IsEnd)
				{
					//タスクをリストから除去
					task.Delete();
					TaskList.Remove(task);
					//強制終了時のコールバック実行
					if (OnStop != null)
					{
						OnStop();
						OnStop = null;
					}
				}
				//時間制限に達した
				if (Sys.TimeSinceUpdateStart >= LimitTime)
				{
					break;
				}
			}
		}
		/// <summary>
		/// 読み込み依頼
		/// </summary>
		public AssetBundleResource.CreateTask LoadAsync(string assetName, System.Action<Resource> onEnd, bool autoUnload = false)
		{
			//準備中
			if (!IsReady)
			{
				//リクエストを積んでreturn
				RequestList.Add(new Task.Request(LoadAsync, assetName, onEnd, autoUnload));
				return null;
			}

			//サーバーに存在しないファイルを読み込もうとした
			if (!AssetList.ContainsKey(assetName))
			{
				IsReady = false;
				Debug.LogError(assetName + "はサーバーに存在しません");
				MessageBox.Open(GetErrorMsg(ERRORCODE.ASSET_NOT_EXIST), null, false);
				return null;
			}

			//リソースを検索
			Resource resource = ResourceList.Find(obj => (obj is AssetBundleResource) && (obj.mName == assetName));
			//まだ読み込んでない
			if (resource == null)
			{
				//既に生成依頼済みかチェック
				AssetBundleResource.CreateTask task = (AssetBundleResource.CreateTask)TaskList.Find(obj => (obj is AssetBundleResource.CreateTask) && (obj.Name == assetName));
				//新規依頼
				if (task == null)
				{
					Download(assetName);
					task = new AssetBundleResource.CreateTask(assetName, AssetList[assetName].EncryptName, autoUnload);
					task.OnEnd = ResourceList.Add;
					task.OnEnd += onEnd;
					TaskList.Add(task);
				}
				//依頼済み
				else
				{
					//コールバックを追加
					task.OnEnd += onEnd;
				}
				return task;
			}
			//既に読み込み済み
			else
			{
				//コールバックだけ実行
				if (onEnd != null)
				{
					onEnd(resource);
					onEnd = null;
				}
				return null;
			}
		}
		/// <summary>
		/// Assets/Resourcesからの読み込み
		/// </summary>
		public void LoadAsyncInPackage(string path, System.Action<Resource> onEnd, bool insert = false)
		{
			//リソースを検索
			Resource resource = ResourceList.Find(obj => (obj is InPackageResource) && (obj.mName == path));
			//まだ読み込んでない
			if (resource == null)
			{
				//既に生成依頼済みかチェック
				InPackageResource.CreateTask task = (InPackageResource.CreateTask)TaskList.Find(obj => (obj is InPackageResource.CreateTask) && (obj.Name == path));
				//新規依頼
				if (task == null)
				{
					task = new InPackageResource.CreateTask(path);
					task.OnEnd = ResourceList.Add;
					task.OnEnd += onEnd;
					//挿入
					if (insert)
					{
						TaskList.Insert(0, task);
					}
					//末尾に追加
					else
					{
						TaskList.Add(task);
					}
				}
				//依頼済み
				else
				{
					//コールバックを追加
					task.OnEnd += onEnd;
				}
			}
			//既に読み込み済み
			else
			{
				//コールバックだけ実行
				if (onEnd != null)
				{
					onEnd(resource);
					onEnd = null;
				}
			}
		}
		/// <summary>
		/// GameObject非同期生成
		/// </summary>
		public void InstantiateAsync(Object obj, System.Action<GameObject> onEnd, bool insert = false)
		{
			InstantiateTask task = new InstantiateTask(obj);
			task.OnEnd = onEnd;
			if (insert)
			{
				TaskList.Insert(0, task);
			}
			else
			{
				TaskList.Add(task);
			}
		}
		/// <summary>
		/// 処理中断
		/// </summary>
		public void StopAsync(System.Action onStop)
		{
			//コールバック設定
			OnStop = onStop;
			//中断させるものが無い
			if (TaskList.Count == 0)
			{
				//即コールバック実行
				if (OnStop != null)
				{
					OnStop();
					OnStop = null;
				}
			}
			//先頭以外を削除
			else
			{
				while (TaskList.Count > 1)
				{
					TaskList[1].Delete();
					TaskList.RemoveAt(1);
				}
			}
		}
		/// <summary>
		/// 指定破棄
		/// </summary>
		public void Clear(Resource resource)
		{
			resource.Clear();
			ResourceList.Remove(resource);
		}
		/// <summary>
		/// 全破棄
		/// </summary>
		public void ClearAll(bool forceClear = false)
		{
			for (int i = 0; i < ResourceList.Count; i++)
			{
				if (!ResourceList[i].mDontClear || forceClear)
				{
					Clear(ResourceList[i]);
					i--;
				}
			}
		}
		/// <summary>
		/// 一括ダウンロード開始
		/// </summary>
		public void AllDownload()
		{
			foreach (var key in AssetList.Keys)
			{
				Download(key);
			}
		}
		/// <summary>
		/// 指定ダウンロード
		/// </summary>
		private void Download(string assetName)
		{
			//CRCに差異がある or ファイルが存在しない
			if (!AssetList[assetName].CheckCRC() || !AssetList[assetName].CheckExist())
			{
				//まだダウンロード依頼していないなら
				if (!TaskList.Exists(obj => (obj is DownloadTask) && (obj.Name == assetName)))
				{
					//ダウンロード開始
					DownloadTask task = new DownloadTask(assetName);
					task.OnEnd = Save;
					TaskList.Add(task);
				}
			}
		}
		/// <summary>
		/// ダウンロードしたアセットを保存
		/// </summary>
		private void Save(string assetName, byte[] bytes)
		{
			//アセットバイナリ保存＆CRC更新
			AssetList[assetName].Save(bytes);

			//CRCデータを保存
			using (FileStream fs = new FileStream(LocalCRCPath, FileMode.Create, FileAccess.Write))
			using (BinaryWriter bw = new BinaryWriter(fs))
			{
				foreach (var obj in AssetList)
				{
					bw.Write(obj.Key);
					bw.Write(obj.Value.LocalCRC);
				}
			}
		}
		/// <summary>
		/// 一括ダウンロードが必要かチェック
		/// </summary>
		public bool CheckAllDownload()
		{
			foreach (var obj in AssetList.Values)
				if (!obj.CheckCRC() || !obj.CheckExist())
					return true;
			return false;
		}
		/// <summary>
		/// タスク数取得
		/// </summary>
		public int GetTaskCount()
		{
			return TaskList.Count;
		}
		/// <summary>
		/// 読み込み依頼
		/// </summary>
		private void LoadAsync(object[] args)
		{
			LoadAsync((string)args[0], (System.Action<Resource>)args[1], (bool)args[2]);
		}
		/// <summary>
		/// アセットデータ
		/// </summary>
		public class AssetData
		{
			//----	field	-----------------------------------------------------------------------------------
			private static byte[] Password = System.Text.Encoding.UTF8.GetBytes("xU99jXuCPmV3TJcv");
			public long LocalCRC;
			public long ServerCRC { get; private set; }
			public string EncryptName { get; private set; }
			//----	construct	-------------------------------------------------------------------------------
			public AssetData(string name, long servercrc)
			{
				LocalCRC = -1;
				ServerCRC = servercrc;
				EncryptName = ToEncryptName(name);
			}
			//----	method	-----------------------------------------------------------------------------------
			/// <summary>
			/// ファイル名暗号化
			/// </summary>
			private static string ToEncryptName(string name)
			{
#if false
				//テスト用
				return name.Replace("/", "@");
#else
				byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
				int i = 0;
				while (i < nameBytes.Length)
				{
					for (int j = 0; j < Password.Length; j++)
					{
						if (i < nameBytes.Length)
						{
							nameBytes[i++] ^= Password[j];
						}
						else
						{
							break;
						}
					}
				}
				return System.BitConverter.ToString(nameBytes).Replace("-", "").ToLower();
#endif
			}
			/// <summary>
			/// CRCチェック
			/// </summary>
			public bool CheckCRC()
			{
				return LocalCRC == ServerCRC;
			}
			/// <summary>
			/// ファイル存在チェック
			/// </summary>
			public bool CheckExist()
			{
				return File.Exists(Define.LOCAL_ASSET_PATH + EncryptName);
			}
			/// <summary>
			/// ファイル保存＆CRC更新
			/// </summary>
			public void Save(byte[] bytes)
			{
				File.WriteAllBytes(Define.LOCAL_ASSET_PATH + EncryptName, bytes);
				LocalCRC = ServerCRC;
			}
		}
		/// <summary>
		/// タスク基底
		/// </summary>
		public abstract class Task
		{
			//----	field	-----------------------------------------------------------------------------------
			public bool IsError { get; protected set; }
			public bool IsEnd { get; protected set; }
			public string Name { get; private set; }
			protected System.Action UpdateFunc;
			//----	construct	-------------------------------------------------------------------------------
			protected Task(string name) 
			{ 
				Name = name;
			}
			//----	method	-----------------------------------------------------------------------------------
			/// <summary>
			/// 破棄
			/// </summary>
			public virtual void Delete()
			{

			}
			/// <summary>
			/// 処理
			/// </summary>
			public void Update()
			{
				if (UpdateFunc != null)
				{
					UpdateFunc();
				}
			}
			/// <summary>
			/// リクエスト
			/// </summary>
			public class Request
			{
				//----	field	-----------------------------------------------------------------------------------
				private System.Action<object[]> func;
				private object[] args;
				//----	construct	-------------------------------------------------------------------------------
				public Request(System.Action<object[]> _func, params object[] _args)
				{
					func = _func;
					args = _args;
				}
				//----	method	-----------------------------------------------------------------------------------
				/// <summary>
				/// リクエスト実行
				/// </summary>
				public void Call()
				{
					func(args);
				}
			}
		}
		/// <summary>
		/// ダウンロードタスク
		/// </summary>
		private class DownloadTask : Task
		{
			//----	field	-----------------------------------------------------------------------------------
			private WWW mWWW;
			private float mProgress;
			private float mTime;
			public System.Action<string, byte[]> OnEnd;
			//----	consturct	-------------------------------------------------------------------------------
			public DownloadTask(string name)
				: base(name)
			{
				UpdateFunc = Update_Start;
			}
			//----	method	-----------------------------------------------------------------------------------
			/// <summary>
			/// 破棄
			/// </summary>
			public override void Delete()
			{
				if (mWWW != null)
				{
					mWWW.Dispose();
					mWWW = null;
				}
			}
			/// <summary>
			/// ダウンロード開始
			/// </summary>
			private void Update_Start()
			{
				mWWW = new WWW(Define.SERVER_ASSET_PATH + Name);
				mProgress = 0;
				mTime = Time.realtimeSinceStartup;
				UpdateFunc = Update_Wait;
			}
			/// <summary>
			/// ダウンロード待ち
			/// </summary>
			private void Update_Wait()
			{
				//ダウンロード完了
				if (mWWW.isDone || mWWW.progress >= 1.0f)
				{
					//ダウンロードエラー
					if (!string.IsNullOrEmpty(mWWW.error))
					{
						//リトライさせる
						IsError = true;
						UpdateFunc = null;
						Debug.LogError(Name + "のダウンロードに失敗しました");
						MessageBox.Open(GetErrorMsg(ERRORCODE.ASSET_DL), () =>
						{
							IsError = false;
							mWWW.Dispose();
							mWWW = null;
							UpdateFunc = Update_Start;
						});
						return;
					}

					if (OnEnd != null)
					{
						OnEnd(Name, mWWW.bytes);
						OnEnd = null;
					}
					IsEnd = true;
					UpdateFunc = null;
				}
				//ダウンロード中
				else
				{
					//進行中
					if (mProgress < mWWW.progress)
					{
						mProgress = mWWW.progress;
						mTime = Time.realtimeSinceStartup;
					}
					//停止中
					else
					{
						//タイムアウト
						float elapsedTime = Time.realtimeSinceStartup - mTime;
						if (elapsedTime >= Define.DOWNLOAD_TIMEOUT)
						{
							//リトライさせる
							IsError = true;
							UpdateFunc = null;
							Debug.LogWarning(Name + "のダウンロードがタイムアウトしました");
							MessageBox.Open(GetErrorMsg(ERRORCODE.ASSET_DL_TIMEOUT), () =>
							{
								IsError = false;
								mTime = Time.realtimeSinceStartup;
								UpdateFunc = Update_Wait;
							});
						}
					}
				}
			}
		}
		/// <summary>
		/// リソース読み込みタスク
		/// </summary>
		private class ResourceRequestTask : Task
		{
			//----	field	-----------------------------------------------------------------------------------
			private ResourceRequest mRequest;
			public System.Action<Object> OnEnd;
			//----	construct	-------------------------------------------------------------------------------
			public ResourceRequestTask(string path)
				: base(path)
			{
				UpdateFunc = Update_Start;
			}
			//----	method	-----------------------------------------------------------------------------------
			/// <summary>
			/// 破棄
			/// </summary>
			public override void Delete()
			{
				mRequest = null;
			}
			/// <summary>
			/// 読み込み開始
			/// </summary>
			private void Update_Start()
			{
				mRequest = Resources.LoadAsync(Name);
				UpdateFunc = Update_Wait;
			}
			/// <summary>
			/// 読み込み待ち
			/// </summary>
			private void Update_Wait()
			{
				if (mRequest.isDone || mRequest.progress >= 1.0f)
				{
					if (OnEnd != null)
					{
						OnEnd(mRequest.asset);
						OnEnd = null;
					}
					IsEnd = true;
					UpdateFunc = null;
				}
			}
		}
		/// <summary>
		/// GameObject生成タスク
		/// </summary>
		private class InstantiateTask : Task
		{
			//----	field	-----------------------------------------------------------------------------------
			public Object mObj;
			public System.Action<GameObject> OnEnd;
			//----	construct	-------------------------------------------------------------------------------
			public InstantiateTask(Object obj)
				: base(null)
			{
				mObj = obj;
				UpdateFunc = Update_Instantiate;
			}
			//----	method	-----------------------------------------------------------------------------------
			/// <summary>
			/// GameObject生成
			/// </summary>
			private void Update_Instantiate()
			{
				if (OnEnd != null)
				{
					OnEnd((GameObject)Object.Instantiate(mObj));
					OnEnd = null;
				}
				IsEnd = true;
				UpdateFunc = null;
			}
		}
	}
}