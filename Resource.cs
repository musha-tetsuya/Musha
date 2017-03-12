using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Musha
{
	/// <summary>
	/// リソース
	/// </summary>
	public abstract class Resource : Dictionary<System.Type, Dictionary<string, Object>>
	{
		//----	field	-----------------------------------------------------------------------------------
		public bool mDontClear;//trueだとResourceManager.ClearAll()で破棄されない。共通UI(メッセージボックス等)でtrueにすると良いかも。
		public string mName { get; private set; }
		//----	construct	-------------------------------------------------------------------------------
		protected Resource(string name, params Object[] objs)
		{
			mName = name;
			foreach (var obj in objs)
			{
				var type = obj.GetType();
				if (!ContainsKey(type))
				{
					Add(type, new Dictionary<string, Object>());
				}
				//Debug.Log(type + ":" + obj.name);
				this[type].Add(obj.name, obj);
			}
		}
		//----	method	-----------------------------------------------------------------------------------
		/// <summary>
		/// 破棄
		/// </summary>
		public virtual new void Clear()
		{
			foreach (var dic in Values)
			{
				dic.Clear();
			}
			base.Clear();
		}
		/// <summary>
		/// 要素取得
		/// </summary>
		public T Get<T>(string name) where T : Object
		{
			return (T)this[typeof(T)][name];
		}
		/// <summary>
		/// 先頭要素取得
		/// </summary>
		public T GetFirst<T>() where T : Object
		{
			foreach (var obj in this[typeof(T)])
			{
				return (T)obj.Value;
			}
			return null;
		}
	}
	/// <summary>
	/// アセットバンドルリソース
	/// </summary>
	public class AssetBundleResource : Resource
	{
		//----	field	-----------------------------------------------------------------------------------
		private AssetBundle mAssetBundle;
		//----	construct	-------------------------------------------------------------------------------
		private AssetBundleResource(string assetName, AssetBundle assetBundle, Object[] objs)
			: base(assetName, objs)
		{
			mAssetBundle = assetBundle;
		}
		//----	method	-----------------------------------------------------------------------------------
		/// <summary>
		/// 破棄
		/// </summary>
		public override void Clear()
		{
			if (mAssetBundle != null)
			{
				mAssetBundle.Unload(true);
				mAssetBundle = null;
			}
			base.Clear();
		}
		/// <summary>
		/// 生成タスク
		/// </summary>
		public class CreateTask : AssetManager.Task
		{
			//----	field	-----------------------------------------------------------------------------------
			private bool mAutoUnload;
			private string mEncryptName;
			private AssetBundleCreateRequest mCreateRequest;
			private AssetBundleRequest mThawingRequest;
			public Resource mResource { get; private set; }
			public System.Action<Resource> OnEnd;
			//----	construct	-------------------------------------------------------------------------------
			public CreateTask(string assetName, string encryptName, bool autoUnload)
				: base(assetName)
			{
				mAutoUnload = autoUnload;
				mEncryptName = encryptName;
				UpdateFunc = Update_Start;
			}
			//----	method	-----------------------------------------------------------------------------------
			/// <summary>
			/// 破棄
			/// </summary>
			public override void Delete()
			{
				UpdateFunc = null;
				if (mCreateRequest != null)
				{
					if (mCreateRequest.assetBundle != null && mAutoUnload)
					{
						mCreateRequest.assetBundle.Unload(false);
					}
					mCreateRequest = null;
				}
				mThawingRequest = null;
				mResource = null;
			}
			/// <summary>
			/// 開始
			/// </summary>
			private void Update_Start()
			{
				//アセットバンドル作成開始
				mCreateRequest = AssetBundle.LoadFromFileAsync(Define.LOCAL_ASSET_PATH + mEncryptName);
				UpdateFunc = Update_WaitCreate;
			}
			/// <summary>
			/// アセットバンドル作成待ち
			/// </summary>
			private void Update_WaitCreate()
			{
				if (mCreateRequest.isDone || mCreateRequest.progress >= 1.0f)
				{
					//アセットバンドル解凍開始
					mThawingRequest = mCreateRequest.assetBundle.LoadAllAssetsAsync();
					UpdateFunc = Update_WaitThawing;
				}
			}
			/// <summary>
			/// アセットバンドル解凍待ち
			/// </summary>
			private void Update_WaitThawing()
			{
				if (mThawingRequest.isDone || mThawingRequest.progress >= 1.0f)
				{
					//完了
					IsEnd = true;
					UpdateFunc = null;
					//リソース作成
					mResource = new AssetBundleResource(Name, mCreateRequest.assetBundle, mThawingRequest.allAssets);
					//不要メモリ削除
					if (mAutoUnload)
					{
						mCreateRequest.assetBundle.Unload(false);
					}
					mCreateRequest = null;
					mThawingRequest = null;
					//コールバック実行
					if (OnEnd != null)
					{
						OnEnd(mResource);
						OnEnd = null;
					}
				}
			}
		}
	}
	/// <summary>
	/// インパッケージリソース
	/// </summary>
	public class InPackageResource : Resource
	{
		//----	field	-----------------------------------------------------------------------------------
		//----	construct	-------------------------------------------------------------------------------
		private InPackageResource(string path, Object obj)
			: base(path, obj)
		{

		}
		//----	method	-----------------------------------------------------------------------------------
		/// <summary>
		/// 破棄
		/// </summary>
		public override void Clear()
		{
			base.Clear();
			Resources.UnloadUnusedAssets();
		}
		/// <summary>
		/// 生成タスク
		/// </summary>
		public class CreateTask : AssetManager.Task
		{
			//----	field	-----------------------------------------------------------------------------------
			private string mPath;
			private ResourceRequest mResourceRequest;
			public Resource mResource { get; private set; }
			public System.Action<Resource> OnEnd;
			//----	construct	-------------------------------------------------------------------------------
			public CreateTask(string path)
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
				UpdateFunc = null;
				mResource = null;
			}
			/// <summary>
			/// 読み込み開始
			/// </summary>
			private void Update_Start()
			{
				mResourceRequest = Resources.LoadAsync(Name);
				UpdateFunc = Update_Wait;
			}
			/// <summary>
			/// 読み込み待ち
			/// </summary>
			private void Update_Wait()
			{
				if (mResourceRequest.isDone || mResourceRequest.progress >= 1.0f)
				{
					//完了
					IsEnd = true;
					UpdateFunc = null;
					//リソース作成
					mResource = new InPackageResource(Name, mResourceRequest.asset);
					//コールバック実行
					if (OnEnd != null)
					{
						OnEnd(mResource);
						OnEnd = null;
					}
				}
			}
		}
	}
}
