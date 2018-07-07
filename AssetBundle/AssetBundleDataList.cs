using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MushaEngine {
public partial class AssetBundleLoader : MonoBehaviour {

/// <summary>
/// アセットバンドルデータリスト
/// </summary>
protected class AssetBundleDataList : Dictionary<string, AssetBundleData>
{
	/// <summary>
	/// ファイルパス
	/// </summary>
	public string filePath = null;
	/// <summary>
	/// CSVのURL
	/// </summary>
	public string csvUrl = null;

	/// <summary>
	/// 書き込み
	/// </summary>
	public void Save()
	{
		//ディレクトリ作成
		Directory.CreateDirectory(Path.GetDirectoryName(this.filePath));

		//ファイル書き込み
		using (var stream = new FileStream(this.filePath, FileMode.Create, FileAccess.Write))
		using (var writer = new BinaryWriter(stream))
		{
			foreach (var data in this.Values)
			{
				writer.Write(data.name);
				writer.Write(data.localCRC);
				writer.Write(data.serverCRC);
			}
		}
	}

	/// <summary>
	/// 読み込み
	/// </summary>
	public void Load()
	{
		this.Clear();

		//ファイル存在チェック
		if (File.Exists(this.filePath))
		{
			//ファイル読み込み
			using (var stream = new MemoryStream(File.ReadAllBytes(this.filePath)))
			using (var reader = new BinaryReader(stream))
			{
				while (!reader.IsEnd())
				{
					var data = new AssetBundleData(
						name: reader.ReadString(),
						localCRC: reader.ReadUInt32(),
						serverCRC: reader.ReadUInt32());
					this.Add(data.name, data);
				}
			}
		}
	}

	/// <summary>
	/// ダウンロード
	/// </summary>
	public IEnumerator Download(Action onFinished)
	{
		//CSVダウンロード
		using (var www = new WWW(this.csvUrl))
		{
			//ダウンロード完了を待つ
			yield return www.Wait();

			//タイムアウト
			if (!www.isDone)
			{
				Debug.LogError("タイムアウト");
			}
			//エラー
			else if (!string.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
			}
			else
			{
				//CSV読み込み
				using (var stream = new MemoryStream(www.bytes))
				using (var reader = new StreamReader(stream))
				{
					string line = null;
					while ((line = reader.ReadLine()) != null)
					{
						string[] lineSplit = line.Split(',');
						string name = lineSplit[0];
						uint serverCRC = uint.Parse(lineSplit[1]);

						if (this.ContainsKey(name))
						{
							this[name].serverCRC = serverCRC;
						}
						else
						{
							var data = new AssetBundleData(
								name: name,
								localCRC: 0,
								serverCRC: serverCRC);
							this.Add(name, data);
						}
					}
				}

				//更新内容を保存
				this.Save();

				//コールバック実行
				if (onFinished != null)
				{
					onFinished();
				}
			}
		}	
	}
}

}
}//namespace MushaEngine