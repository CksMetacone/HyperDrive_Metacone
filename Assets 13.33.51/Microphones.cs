using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using System;
using UnityEngine.UI;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEngine.Networking;
using System.Net.Sockets;
//using System.Linq;
using System.IO.Compression;

public class Microphones : MonoBehaviour
{
	public void SaveData(List<float> dataToSave)
	{
		string jsonString = JsonUtility.ToJson(new DataWrapper(dataToSave.ToArray()));
		File.WriteAllText(dataPath, jsonString);
	}

	public List<float> LoadData()
	{
		if (File.Exists(dataPath))
		{
			string jsonString = File.ReadAllText(dataPath);
			DataWrapper loadedData = JsonUtility.FromJson<DataWrapper>(jsonString);
			return new List<float>(loadedData.data);
		}

		// Dosya yoksa boş bir dosya oluşturup boş list dönüyoruz.
		File.WriteAllText(dataPath, JsonUtility.ToJson(new DataWrapper(new float[0])));
		return new List<float>();
	}
	
	void Start()
	{

		Application.targetFrameRate = 90;
		//Screen.SetResolution(Screen.currentResolution.width / 2, Screen.currentResolution.height / 2, true);

		//UnityEngine.Debug.unityLogger.logEnabled = false;

		dataPath = Path.Combine("/Users/metacone/Desktop/Sesrecord/", "SaveData1.json");
#if UNITY_IOS
		if (Application.platform == RuntimePlatform.IPhonePlayer)
			dataPath = Path.Combine(Application.persistentDataPath, "SaveData1.json");
#endif
		StringPublickey = PlayerPrefs.GetString("publickey");

		if (!string.IsNullOrEmpty(StringPublickey) && StringPublickey.Length >= 10)
		{
			StringPublickey = (string)StringPublickey.Substring(0, 10);
		}
		StringPublickey = "9huQYFXX5w";

		Public_Key_text.text = StringPublickey;

		// Eğer dosya daha önce oluşturulmamışsa
		if (!File.Exists(dataPath))
		{
			SaveData(ortalamadataList);
		}
		else
		{

			UnityEngine.Debug.Log("Dosya zaten mevcut, üzerine yazılmayacak.");
		}

		cb_TrainActive_Button = Train_Active_Button.colors;
		cb_Train_Button = Train_Button.colors;
		cb_Learning_Button = Learn_Button.colors;

		if (Microphone.devices.Length > 0)
		{

			int minFreq, maxFreq, freq;
			Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);
			freq = 44100;//Mathf.Min(44100, maxFreq);
			//UnityEngine.Debug.Log("minfreq" + minFreq + "Maxfreq" + maxFreq);
			//UnityEngine.Debug.Log("devices" + Microphone.devices[0]);

			source = GetComponent<AudioSource>();
			source.outputAudioMixerGroup = mix_Microphone;
			source.clip = Microphone.Start(null, true, 20, freq);
			source.loop = true;

			while (!(Microphone.GetPosition(null) > 0)) { }
			source.Play();

			StartCoroutine(StartingCoroutine());

		}
		else
		{
			UnityEngine.Debug.Log("No Mic connected!");
		}

	}

	IEnumerator StartingCoroutine()
	{

		//Stopwatch stopwatch = new Stopwatch();

		// Zamanı başlat
		//stopwatch.Start();
		//Login işlemi burada yapılmalı
		LoginData loginingData = new LoginData()
		{
			Id = "0",
			publickey= StringPublickey
		};
		LoginResponse loginResponseData = null;
		string jsonDataLogin = JsonUtility.ToJson(loginingData);
		StartCoroutine(SendLoginData(jsonDataLogin));
		//Coroutine tamamlanana kadar bekleyin
		while (!isCoroutineFinished)
		{
			yield return null;
			if (Data_Event_Mic_text.text != "Data")
			{

				// Convert the JSON response to a LoginResponse object using JsonUtility
				loginResponseData = JsonUtility.FromJson<LoginResponse>(Data_Event_Mic_text.text);

				isCoroutineFinished = true;
				Data_Event_Mic_text.text = "Data";
			}
		}
		isCoroutineFinished = false;

		ID_Device = loginResponseData.id;
		bool parseSuccess = int.TryParse(ID_Device, out ID_Device_int);
		if (!parseSuccess)
		{
			// Dönüşüm başarısız oldu, hata işleme kodunu buraya yazın.
			Console.WriteLine("ID_Device başarısız");
			This_text.text = "ID_Device başarısız";
		}
		First_Voice_Shot = loginResponseData.first_Shot_Mic;
		Train_Mod = loginResponseData.outp_Mic + 1;
		CamSc.fshot = loginResponseData.first_Shot_Cam;
		CamSc.Train_Out = loginResponseData.outp_Cam + 1;
		CurrentPercentNo = Train_Mod - 1;

		if(First_Voice_Shot>0)
		{ 
			// Veriyi yükle ve konsola yaz
			ortalamadataList = LoadData();
			foreach (float data in ortalamadataList)
			{
				UnityEngine.Debug.Log(data);
			}
		}
		string dnslabel = "dnslabel" + ID_Device;
		// Geçen süreyi milisaniye cinsinden yazdır
		//UnityEngine.Debug.Log("dns: "+ dnslabel/*+" Login Time: " + stopwatch.ElapsedMilliseconds + " ms"*/);
		This_text.text = dnslabel;

		bool connectionSuccess = false;
		bool attemptComplete = false;
		if (client == null)
		{
			client = new TcpClient();
		}
		client.BeginConnect(dnslabel + ".westeurope.azurecontainer.io", 12888, ar =>
		//client.BeginConnect("192.168.1.50", 12888, ar =>
		{
			try
			{
				ar.AsyncWaitHandle.WaitOne();

				client.EndConnect(ar);
				connectionSuccess = true;
				if (connectionSuccess)
				{
					UnityEngine.Debug.Log("Succ serv.");
					// Server'a veri gönder
					var stream = client.GetStream();
					byte[] publicKeyBytes = Encoding.UTF8.GetBytes(StringPublickey); // string'i byte dizisine çevir
					byte[] dataToSend = new byte[publicKeyBytes.Length + 3]; // +3: 2 for 03/03 and 1 for length
					dataToSend[0] = 0x03;
					dataToSend[1] = 0x03;
					dataToSend[2] = (byte)publicKeyBytes.Length;
					// Geri kalan byte'ları kopyala
					Array.Copy(publicKeyBytes, 0, dataToSend, 3, publicKeyBytes.Length);

					stream.Write(dataToSend, 0, dataToSend.Length); // byte dizisini server'a gönder

					byte[] response = new byte[2]; // sunucudan beklenen yanıt boyutu
					int bytesRead = stream.Read(response, 0, response.Length);
					if (bytesRead == response.Length && response[0] == 0x03 && response[1] == 0x03)
					{
						UnityEngine.Debug.Log("Serv expect.");
						Connection_Error = 0;
					}
					else
					{
						Connection_Error = 1;
						UnityEngine.Debug.LogError("Unexpected serv.");
					}
				}

			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError($"Connection error: {ex.Message}");
				Connection_Error = 1;
				//This_text.text = "Coonection ERRORr";
			}
			finally
			{
				attemptComplete = true;
			}
		}, null);

		float timePassed = 0f;

		while (!attemptComplete && timePassed < connectionTimeout)
		{
			timePassed += Time.deltaTime;
			yield return new WaitForSecondsRealtime(0.1f);
		}

		if (!attemptComplete)
		{
			UnityEngine.Debug.LogError("Conn tim out.");
			Connection_Error = 1;
			This_text.text = "Conn tim out";
			if (client.Connected)
				client.Close(); // Eğer bağlantı hala açıksa, kapat
		}
		else if(Connection_Error ==0)
		{
			UnityEngine.Debug.Log("Conn ok.");
			This_text.text = "Conn ok .";
			Connection_Error = 0;
			cam_start.gameObject.SetActive(true);
			cam_select.gameObject.SetActive(true);

		}
		else
        {
			UnityEngine.Debug.Log("Conn er.");
			This_text.text = "Conn er";
			if (client.Connected)
				client.Close(); // Eğer bağlantı hala açıksa, kapat
		}
	}

	public TMP_Dropdown cam_select;
	public Button cam_start;
	public static int Connection_Error = 1; 
	private float connectionTimeout = 4.0f; // 20 saniye

	IEnumerator SendLoginData(string jsonData/*, Action<float[]> callback*/)
	{
		//var apiUrl = "http://metlog.cuaecegjg7fjahgn.westeurope.azurecontainer.io/LoginApp";
		//var apiUrl = "http://192.168.1.2:5243/LoginApp";
		//var apiUrl = "http://localhost:5243/LoginApp";
		var apiUrl = "http://metdnslog.cvathgdgesddffgx.westeurope.azurecontainer.io/LoginApp";

		byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonData);

		using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
		{
			www.uploadHandler = new UploadHandlerRaw(jsonBytes);
			www.downloadHandler = new DownloadHandlerBuffer();
			www.SetRequestHeader("Content-Type", "application/json");

			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success)
			{
				UnityEngine.Debug.Log(www.error);
				//Connection_Error = 1;
				//This_text.text = "Coonection Http ERROR";
			}
			else
			{
				//UnityEngine.Debug.Log("Form Login complete!");
				string responseJson = www.downloadHandler.text;

				UnityEngine.Debug.Log("Response JSON: " + responseJson);
				Data_Event_Mic_text.SetText(responseJson);

			}
		} // using bloğunun sonun
	}

	bool isCoroutineFinished = false;
	public static byte[] Compress(byte[] data)
	{
		using (var output = new MemoryStream())
		{
			using (var compressor = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
			{
				compressor.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}
	}
	private float[] SendTCPmsg(byte sourceType, byte taskType, List<float> data)
	{
		if (client == null || !client.Connected)
		{
			UnityEngine.Debug.LogError("Connection lost!");
			Connection_Error = 1;
			This_text.text = "Coonection TCP ERROR";
			return null;
		}
		try
		{
			NetworkStream stream = client.GetStream();

			// Girdi olarak gelen int listesini byte dizisine dönüştür.
			byte[] byteData = new byte[data.Count * sizeof(float)];
			Buffer.BlockCopy(data.ToArray(), 0, byteData, 0, byteData.Length);

			byte[] compressedData = Compress(byteData);

			//UnityEngine.Debug.Log("byteData"+ byteData.Length+"compressedData" +compressedData.Length);

			//Buffer.BlockCopy(data.ToArray(), 0, byteData, 0, byteData.Length);

			byte[] firstshotBytes = BitConverter.GetBytes(First_Voice_Shot);
			byte[] trainmodBytes = BitConverter.GetBytes(CurrentPercentNo);
			byte[] IDbytes = BitConverter.GetBytes(ID_Device_int);


			/*UInt16 checksum = ComputeChecksum(byteData);
			byte[] checksumBytes = BitConverter.GetBytes(checksum);
			byte[] byteDataWithChecksum = new byte[byteData.Length + checksumBytes.Length];
			Array.Copy(byteData, byteDataWithChecksum, byteData.Length);
			Array.Copy(checksumBytes, 0, byteDataWithChecksum, byteData.Length, checksumBytes.Length);
			*/
			UInt16 checksum = ComputeChecksum(compressedData);
			byte[] checksumBytes = BitConverter.GetBytes(checksum);
			byte[] byteDataWithChecksum = new byte[compressedData.Length + checksumBytes.Length];
			Array.Copy(compressedData, byteDataWithChecksum, compressedData.Length);
			Array.Copy(checksumBytes, 0, byteDataWithChecksum, compressedData.Length, checksumBytes.Length);
			// Uzunluk, tip ve kaynak bilgisini ekleyin
			byte[] fullPacket = new byte[1 + 1 + 2 + 4 + 4 + byteDataWithChecksum.Length];
			fullPacket[0] = sourceType;
			fullPacket[1] = taskType;
			fullPacket[2] = IDbytes[0];
			fullPacket[3] = IDbytes[1];
			fullPacket[4] = firstshotBytes[0];
			fullPacket[5] = firstshotBytes[1];
			fullPacket[6] = trainmodBytes[0];
			fullPacket[7] = trainmodBytes[1];
			byte[] lengthBytes = BitConverter.GetBytes(byteDataWithChecksum.Length);
			Array.Copy(lengthBytes, 0, fullPacket, 8, 4); // 4 byte uzunluk bilgisi
			Array.Copy(byteDataWithChecksum, 0, fullPacket, 12, byteDataWithChecksum.Length);

			// Bu byte dizisini TCP üzerinden gönder.
			//NetworkStream stream = client.GetStream();
			stream.Write(fullPacket, 0, fullPacket.Length);
			//UnityEngine.Debug.Log("csbytes: " + checksumBytes[0] + checksumBytes[1]);
			//UnityEngine.Debug.Log("Send_mic:" + fullPacket.Length + "-" + sourceType + taskType);
			// Receive Data (istek üzerine veri almak için, bu kısmı değiştirmedim)
			//byteData = new Byte[256];
			//String responseData = String.Empty;
			//Int32 bytes = stream.Read(byteData, 0, byteData.Length);
			//responseData = System.Text.Encoding.ASCII.GetString(byteData, 0, bytes);
			//UnityEngine.Debug.Log("Rec_Mic: " + responseData);
			//string responseDataHex = BitConverter.ToString(byteData, 0, bytes);
			//UnityEngine.Debug.Log("Rec_Mic (Hex): " + responseDataHex);
			byte[] header = new byte[2]; // 2 byte data + 4 byte uzunluk
			int bytesRead = stream.Read(header, 0, header.Length);
			if (header[1] == 2)
			{
				byte[] floatlgth = new byte[4]; // 2 byte data + 4 byte uzunluk
				int bytesRead2 = stream.Read(floatlgth, 0, floatlgth.Length);
				// 4 byte uzunluk bilgisini çıkar ve float dizisinin uzunluğunu hesapla
				int floatArrayLength = BitConverter.ToInt32(floatlgth, 0);

				// float dizisi boyutunu byte cinsinden hesapla
				int byteDataLength = floatArrayLength * 4;
				byte[] byteData2 = new byte[byteDataLength];

				int bytesReadForData = stream.Read(byteData2, 0, byteData2.Length);
				if (bytesReadForData < byteDataLength)
				{
					//UnityEngine.Debug.Log("Incomplete mic data received.");
					return new float[0]; // boş bir float dizisi döndür
				}

				// byte dizisini float dizisine dönüştür
				float[] result = new float[floatArrayLength];
				for (int i = 0; i < byteData2.Length; i += 4)
				{
					result[i / 4] = BitConverter.ToSingle(byteData2, i);
				}

				//UnityEngine.Debug.Log("RecMictask: " + string.Join(", ", result));
				return result;
			}
			else
			{
				/*byte[] state = new byte[2]; // 2 byte data + 4 byte uzunluk
				stream.Read(state, 0, state.Length);
				if (state[1] == 1)
				{
					byte[] fshoting = new byte[2]; // 2 byte data 
					stream.Read(fshoting, 0, fshoting.Length);
					//fshot = BitConverter.ToInt16(fshoting, 0);
					byte[] outing = new byte[2]; // 2 byte data 
					stream.Read(outing, 0, outing.Length);
					//Microphones.CurrentPercentNo = BitConverter.ToInt16(outing, 0);
					UnityEngine.Debug.Log("str_mic: " + BitConverter.ToInt16(fshoting, 0) + BitConverter.ToInt16(outing, 0));
				}
				//UnityEngine.Debug.Log("RecCamTrain: ");*/
				float[] result = new float[1];
				result[0] = 0.0f;
				return result;
			}


		}
		catch (Exception ex)
		{
			// Burada hata mesajını yakalayıp, logluyoruz. 
			UnityEngine.Debug.LogError($"Send/Receive error: {ex.Message}");

			Connection_Error = 1;
			This_text.text = "Coonection TCP ERROR";
			return null;
		}
	}

	private float[] SendTCPTask(byte sourceType, byte taskType)
	{
		if (client == null || !client.Connected)
		{
			UnityEngine.Debug.LogError("Connection lost!");
			Connection_Error = 1;
			This_text.text = "Coonection TCP ERROR";
			return null;
		}
		try
		{
			NetworkStream stream = client.GetStream();

			byte[] firstshotBytes = BitConverter.GetBytes(First_Voice_Shot);
			byte[] trainmodBytes = BitConverter.GetBytes(CurrentPercentNo);
			byte[] IDbytes = BitConverter.GetBytes(ID_Device_int);
			// Uzunluk, tip ve kaynak bilgisini ekleyin
			byte[] fullPacket = new byte[1 + 1 + 2 + 4 + 4]; // sourceType + taskType + length + byteData
			fullPacket[0] = sourceType;
			fullPacket[1] = taskType;
			fullPacket[2] = IDbytes[0];
			fullPacket[3] = IDbytes[1];
			fullPacket[4] = firstshotBytes[0];
			fullPacket[5] = firstshotBytes[1];
			fullPacket[6] = trainmodBytes[0];
			fullPacket[7] = trainmodBytes[1];
			byte[] lengthBytes = new byte[4];
			Array.Copy(lengthBytes, 0, fullPacket, 8, 4); // 4 byte uzunluk bilgisi

			// Bu byte dizisini TCP üzerinden gönder.
			
			stream.Write(fullPacket, 0, fullPacket.Length);
			//UnityEngine.Debug.Log("Sent_mic: sourceType: " + sourceType + " and taskType: " + taskType);

			// Sunucudan gelen veriyi oku
			byte[] header = new byte[2]; // 2 byte data + 4 byte uzunluk
			int bytesRead = stream.Read(header, 0, header.Length);
			if (header[1] == 2)
			{
				byte[] floatlgth = new byte[4]; // 2 byte data + 4 byte uzunluk
				int bytesRead2 = stream.Read(floatlgth, 0, floatlgth.Length);
				// 4 byte uzunluk bilgisini çıkar ve float dizisinin uzunluğunu hesapla
				int floatArrayLength = BitConverter.ToInt32(floatlgth, 0);

				// float dizisi boyutunu byte cinsinden hesapla
				int byteDataLength = floatArrayLength * 4;
				byte[] byteData2 = new byte[byteDataLength];

				int bytesReadForData = stream.Read(byteData2, 0, byteData2.Length);
				if (bytesReadForData < byteDataLength)
				{
					//UnityEngine.Debug.Log("Incomplete mic data received.");
					return new float[0]; // boş bir float dizisi döndür
				}

				// byte dizisini float dizisine dönüştür
				float[] result = new float[floatArrayLength];
				for (int i = 0; i < byteData2.Length; i += 4)
				{
					result[i / 4] = BitConverter.ToSingle(byteData2, i);
				}

				//UnityEngine.Debug.Log("RecMictask: " + string.Join(", ", result));
				return result;
			}
			else
			{
				byte[] state = new byte[2]; // 2 byte data + 4 byte uzunluk
				stream.Read(state, 0, state.Length);
				if (state[1] == 1)
				{
					byte[] fshoting = new byte[2]; // 2 byte data + 4 byte uzunluk
					stream.Read(fshoting, 0, fshoting.Length);
					//First_Voice_Shot = BitConverter.ToInt16(fshoting, 0);
					byte[] outing = new byte[2]; // 2 byte data + 4 byte uzunluk
					stream.Read(outing, 0, outing.Length);
					//CurrentPercentNo = BitConverter.ToInt16(outing, 0);
					//UnityEngine.Debug.Log("stream_mic: " + BitConverter.ToInt16(fshoting, 0) + BitConverter.ToInt16(outing, 0));
				}
				//UnityEngine.Debug.Log("RecCamTrain: ");
				float[] result = new float[1];
				result[0] = 0.0f;
				return result;
			}

		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError($"Send/Receive error: {ex.Message}");
			Connection_Error = 1;
			This_text.text = "Coonection TCP ERROR";
			return null; // Hata durumunda null döndür
		}
	}

	private IEnumerator Mic_Learning_Tcp()
	{
		//Stopwatch stopwatch = new Stopwatch();
		// Zamanı başlat
		//stopwatch.Start();

		float[] SimilarPercent = SendTCPmsg(0, 0, full_data_list);
		if (SimilarPercent == null)
		{
			UnityEngine.Debug.Log("SimilarPercentnull");
			// Hata işleme kısmı
			yield break;
		}
		else if(SimilarPercent[0]== 0.0f)
        {
			//UnityEngine.Debug.Log("Mic_Sim_Per 0");
			yield return null;
		}
		else
		{
			//UnityEngine.Debug.Log("HandleSimilarPercent");
			// Sonuçla ilgili diğer işlemler
			HandleSimilarPercent(SimilarPercent);

			// Zamanı durdur
			//stopwatch.Stop();

			// Geçen süreyi milisaniye cinsinden yazdır
			//UnityEngine.Debug.Log("Voi_Lear_T: " + stopwatch.ElapsedMilliseconds);
			yield return null;
		}


	}

	IEnumerator MyVoice_Moving_routine()
	{
		//UnityEngine.Debug.Log("Voice Det1");
		Mic_Training_Tcp();

		yield break;

	}

	//public static int Change_Mic = 0;
	void Analyze_Chunk()
	{
		// fill data with FFT info
		double Threshold_voice = 0;
		//List<double> new_data = new List<double>();

		float[] spectrum = new float[Array_Sized];
		source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
		count_m++;
		Rndtime++;
		lastavecount++; //eski dataları ortalama toplama için
		
		if (count_m > 1024) //600 lik boyut için 
			count_m = 1;
		for (int i = 1; i < Array_Sized/*Array_Sized*/; i++)
		{
			//else
			// c = new Color(0.0f, 0.0f, 255.0f);
			double val;
			//if(Changed == 0)
			val = Math.Abs(spectrum[i]) * 7000.0f;
			//else
			//val = Math.Abs(spectrum[i]) * 20000.0f;

			if (val > 500)
				val = 500;
			else if (val < 0)
				val = 0;

			if (RndtimeOK == 1)
				Threshold_voice = val - last_data[i];

			//if (Threshold_voice>20)
			//Debug.Log("Threshold" + Threshold_voice.ToString() + "val" + val.ToString() + "i: " + i+"LD"+ last_data[i].ToString());
			//Debug.Log("LastData" + last_data[i].ToString(s));

			if ((Threshold_voice > 150) && (Changed == 0)) //&& (change3 == 0))
			{
				Changed = 1;//Bu kısım ile data toplanmaya başlar ve ? ms data toplanır.
							//Change_Mic = 1;
							//button3.BackColor = Color.Red;
				
				val = last_data[i-1];
				i = 1;
				//watch.Start();
				//watch.Restart();
				//UnityEngine.Debug.Log("treshold");
				lastavecount = 0;//eski dataları topla

				micc = Microphone.GetPosition(null);
				CamSc.Microphone_Activate = 1;

			}
			Avarage[i] = val;//+= val;
			if (Changed == 0)//Boştayken örnek son veriyi toplar.
			{
				if ((lastavecount > 1) && (Rndtime == 46)) //eski örneği almak için ortalamasını alır. 
				{
					Rndtime = 0;
					RndtimeOK = 1;
					last_data[i] = (float)(Avarage[i]/* * 1.0f // * 10.0f*/); //eski data olarak yazar.
																			  //Debug.Log("LD" + last_data[i].ToString());
				}
			}
			if (Changed == 1)
			{
				if ((Avarage[i] > last_data[i]) && (Avarage[i] > 1))
					Avarage[i] = Avarage[i] * 25f;
				else
					Avarage[i] = 0;
				//Show_spectrom((float)Avarage[i], i, 1f);
				//new_data.Add(Avarage[i]);
				//full_data[fd100 * Array_Sized + i] = (Avarage[i]) / 255.0f;

				//full_data_list.Add((int)(Avarage[i] * 4.0f)); //* 1000.0f / 255.0f)); // bu kısım list integer haline getirildi.
				full_data_list.Add((float)(Avarage[i] / 255.0f));

				lastavecount = 0;//eski dataları topla
				//UnityEngine.Debug.Log("full_data_list");
			}
			Avarage[i] = 0;
		}
		if (lastavecount > 1)
			lastavecount = 0;

		if ((fd100 < 99)&& (Changed == 1)) //100 sefer datayı say
		{
			fd100++;  //100 sefer datasyı toplayabilmek için
						//recordingTime += Time.deltaTime;
		}
		else if ((fd100 >= 99) && (Changed == 1))
		{
			Changed = 0;  //50 sefer kaydedildi. şimdi başa al
			Rndtime = 0;
			//button3.BackColor = Color.Blue;
			fd100 = 0;
			//if (Change_Mic == 1)
			{
				Changed_OK = 1; //datayı işleyebilirsin.
								//Change_Mic = 0; //ve yeni öğrenme için fırsat ver.
								//UnityEngine.Debug.Log("Ses Eğitimi Başladı");
			}
			
		}
	}

	//void FixedUpdate()//Update()
	void Update()
	{
		if (Connection_Error == 0)
		{
			timingdelta = Time.deltaTime;
			timer += timingdelta;
			if (isTimeHappy)//zaman saymaya başlar
			{
				Happytimer += timingdelta; // Zamanlayıcıyı artır

				if (Happytimer >= waitHappyTimer)   // Eğer bekleme süresi dolduysa
				{
					Happytimer = 0f; // Zamanlayıcıyı sıfırla
					isTimeHappy = false;

					Happy_Button.gameObject.SetActive(false);
					Sad_Button.gameObject.SetActive(false);
					full_data_list.Clear();
					Changed_OK = 0;
					Happy_Ok = 0;
					MachineCtrl.Have_a_Sound = 0;
				}

			}
			if (Happy_Button.gameObject.activeSelf)
			{
				Happy_Button.transform.GetChild(0).Rotate(Vector3.up, rotationSpeed);
				Sad_Button.transform.GetChild(0).Rotate(Vector3.up, rotationSpeed);
			}

			if (timer >= analyzeInterval)
			{
				timer = 0;
				//Analyze_Chunk();

				if ((MachineCtrl.Mic_Closed == 0) && (Changed_OK == 0))
				{
					//UnityEngine.Debug.Log("Analyzechunk");
					//Stopwatch watch = new Stopwatch();
					//watch.Start();
					Analyze_Chunk();
					//watch.Stop();
					//Mic_Timer_text.SetText(watch.Elapsed.TotalMilliseconds.ToString());
				}
				if ((Changed_OK == 1) && (Happy_Ok == 0) && (MachineCtrl.Have_a_Sound == 0))
				{

					//UnityEngine.Debug.Log("Changed_OK"+ full_data_list.Count.ToString());
					//Full data gönderilecek. //prepare için diğerlerinde data göndermeye gerek yok.
					//List<int> processedData = full_data_list.Select(d => (int)(d * 1000)).ToList();
					Stopwatch watch = new Stopwatch();
					watch.Start();

					StartCoroutine(Mic_Learning_Tcp());
					

					full_data_list.Clear();
					Changed_OK = 0;//Changed_ok daha geç sıfırlanması gerekebilir kontrol edilmeli. 
								   //her sesi analiz et.//anlamlı kontrol et.
					watch.Stop();
					//UnityEngine.Debug.Log("SesWtime" + watch.Elapsed.TotalMilliseconds.ToString());

					if (First_Voice_Shot == 0)
						//StartCoroutine(Mic_Learning_Tcp());
					//StartCoroutine(MyVoice_routine()); //Learning
					//else
					{
						//UnityEngine.Debug.Log("Have_a_Sound0");
						MachineCtrl.Have_a_Sound = 1;
					}
				}
				if ((MachineCtrl.Have_a_Sound == 0) && (MachineCtrl.Have_a_Moving == 1) && (Happy_Ok == 0))
				{//Hareket varmı?
				 //Stopwatch watch = new Stopwatch();
				 //watch.Start();
					//UnityEngine.Debug.Log("Movingroutine");
					MachineCtrl.Have_a_Moving = 0;
					StartCoroutine(MyVoice_Moving_routine());//Training
															 //watch.Stop();
															 //Mic_Timer_text.SetText(watch.Elapsed.TotalMilliseconds.ToString());
				}

				if ((MachineCtrl.Have_a_Sound_not_Moving == 1) && (First_Voice_Shot > 0) && (Happy_Ok == 0))//Ses var 2 sn içinde hareket yok ise
				{//sesi analiz et, uygun outputu bul.
					MachineCtrl.Have_a_Sound_not_Moving = 0;
					//Buraya eski kamera görüntüleri gösterilmesi için komut yazılabilir. 
					//StartCoroutine(MyVoice_routine());
				}
			}
		}
	}
	private void Mic_Training_Tcp()
	{
		//Stopwatch watch = new Stopwatch();
		//watch.Start();

		float[] rettcp = SendTCPTask(0, 1);
		if (rettcp == null)
		{
			// Hata işleme kısmı
			UnityEngine.Debug.Log("rettcpnull");
			return;
		}
		// Sonuçla ilgili diğer işlemler
		//watch.Stop();
		//UnityEngine.Debug.Log("F_Voi_Smic_Tra" + First_Voice_Shot+ "MicTtime" + watch.Elapsed.TotalMilliseconds.ToString());
		if (First_Voice_Shot == (Train_Mod - 1))
			First_Voice_Shot++;

		if ((first_recorded == false) && (First_Voice_Shot == 1))
		{
			first_recorded = true;
			write_first(0);
		}
		
		//UnityEngine.Debug.Log("MicWtime" + watch.Elapsed.TotalMilliseconds.ToString());

	}
	private string BuildTextString(string baseString, float[] SimilarPercent)
	{
		for (int oo = 1; oo < First_Voice_Shot; oo++)
		{
			baseString += string.Format("Perc:{0,12:#.00000} ", SimilarPercent[oo]) + "Out: " + oo.ToString();
		}
		return baseString;
	}

	private void HandleCurrentPercentNo()
	{

		CurrentPercent = Percentage_Accuracy;
		if (Percentage_output == 20 || CurrentPercentNo != Percentage_output)
		{
			CurrentPercentNo = 0;
			This_text.text = "New Data20 ?";
			Happy_Ok = 1;
			ActivateHappySadButtons();
			EnsureOrtalamaDataSize();

		}
		else
		{
			CurrentPercentNo = Percentage_output;
			if (CurrentPercentNo >= ortalamadataList.Count ||
				(ortalamadataList[CurrentPercentNo] > 0 && (ortalamadataList[CurrentPercentNo] - CurrentPercent) > 0.005))
			{
				This_text.text = "New Data ?";
				Happy_Ok = 1;
				ActivateHappySadButtons();
				EnsureOrtalamaDataSize();
			}
			else
			{
				if (AgainCount > 0)
				{
					AgainCount++;
					CurrentPercentNo = First_Voice_Shot;
				}
				if (AgainCount > 3)
					AgainCount = 0;

				MachineCtrl.Have_a_Sound = 1;
				EnsureOrtalamaDataSize();
				if (ortalamadataList[CurrentPercentNo] != 0)
				{
					ortalamadataList[CurrentPercentNo] += CurrentPercent;
					ortalamadataList[CurrentPercentNo] /= 2;
				}
				else
					ortalamadataList[CurrentPercentNo] = CurrentPercent;
			}
		}
		SaveData(ortalamadataList);
	}

	private void ActivateHappySadButtons()
	{
		if (AgainCount == 0)
		{
			Happy_Button.gameObject.SetActive(true);
			Sad_Button.gameObject.SetActive(true);
			isTimeHappy = true;
			Happytimer = 0;
		}
	}

	private void EnsureOrtalamaDataSize()
	{
		if (CurrentPercentNo >= ortalamadataList.Count)
		{
			ortalamadataList.Add(0f);
			UnityEngine.Debug.Log("ortadatList size ensured: " + ortalamadataList.Count);
		}
	}
	private void EvaluatePercentageAccuracy(float[] SimilarPercent)
	{
		if (ortalamadataList.Count > 0 && ortalamadataList[0] > 0)
		{
			if ((ortalamadataList[0] - 0.007) < SimilarPercent[0])
			{
				Percentage_output = 0;
			}
			else
			{
				Percentage_output = 20;

			}
		}
		else
			Percentage_output = 0;

		int timer_2 = 0;
		for (int oo = 1; oo < First_Voice_Shot; oo++)
		{
			if (((ortalamadataList[oo] - 0.005f) < SimilarPercent[oo]) && (Percentage_output == 20))
			{
				if ((timer_2 == 0) || ((timer_2 == 1) && (Percentage_Accuracy < SimilarPercent[oo])))
				{
					Percentage_Accuracy = SimilarPercent[oo];
					Percentage_output = oo;
				}
				timer_2 = 1;
			}
			else if ((Percentage_Accuracy < SimilarPercent[oo]) && (Percentage_output != 20))
			{
				Percentage_Accuracy = SimilarPercent[oo];
				Percentage_output = oo;
			}
		}

	}
	private void HandleSimilarPercent(float[] SimilarPercent)
	{
		string TextString = string.Format("Perc:{0,12:#.00000} ", SimilarPercent[0]) + "Out0: ";
		UnityEngine.Debug.Log("First_Voice_Shotmic" + First_Voice_Shot + "Simiperlength" + SimilarPercent.Length);

		EnsureOrtalamaDataSize();

		Percentage_Accuracy = SimilarPercent[0];
		EvaluatePercentageAccuracy(SimilarPercent);

		TextString = BuildTextString(TextString, SimilarPercent);

		Percantage_text.SetText(TextString);

		HandleCurrentPercentNo();

	}

	bool first_recorded = false;
	
	private UInt16 ComputeChecksum(byte[] data)
	{
		UInt32 checksum = 0;
		foreach (byte b in data)
		{
			checksum += b;
		}
		return (UInt16)(checksum & 0xFFFF);  // sadece alt 2 byte'ı al
	}
	
	int AgainCount = 0;

	public void Happy_ButtonClick()
	{
		Happy_Button.gameObject.SetActive(false);
		Sad_Button.gameObject.SetActive(false);

		//if (AgainCount == 0)//3 kereden fazla tekrarda sorgulamaya geçer.
		{
			ChangeOf_Voice_new = 1; //yeni data var eğer eskilerden yüksek artış olan yoksa.
			Train_Mod = First_Voice_Shot+1;
			CurrentPercentNo = Train_Mod - 1;//Train_Mod - 1;
			AgainCount = 1;
			write_first(First_Voice_Shot );

		}
		/*else
		{
			//AgainCount++;
			CurrentPercentNo = First_Voice_Shot-1;//Train_Mod - 1;
		}*/
		UnityEngine.Debug.Log("VoiceHappy" + Percentage_output.ToString() + "No:" + CurrentPercentNo.ToString());
		if (CurrentPercentNo >= ortalamadataList.Count)
		{
			ortalamadataList.Add(CurrentPercent);
			UnityEngine.Debug.Log("4ortadatList" + ortalamadataList.Count);
		}
		else
		{
			ortalamadataList[CurrentPercentNo] = CurrentPercent;
		}

		MachineCtrl.Have_a_Sound = 1;

		This_text.text = "--";
		full_data_list.Clear();
		Changed_OK = 0;
		if (Happy_Ok > 0)
			Happy_Ok = 0;

		

	}

	public void Sad_ButtonClick()
	{

		Happy_Button.gameObject.SetActive(false);
		Sad_Button.gameObject.SetActive(false);

		if (Happy_Ok == 1)//ses sinyali geldi ve bekliyor ise
		{
			
			/*if (AgainCount > 0) //3 kere içinde ise en son veridedir. 
			{
				
				Train_Mod = First_Voice_Shot;
				CurrentPercentNo = Train_Mod - 1;

			}
			else*/
				CurrentPercentNo = Percentage_output;// Train_Mod - 1;

			if (CurrentPercentNo != 20)
				ortalamadataList[CurrentPercentNo] = CurrentPercent;
			else
			{
				CurrentPercentNo = 0;
				ortalamadataList[0] = CurrentPercent;
			}

			if (CurrentPercentNo >= ortalamadataList.Count)
			{
				ortalamadataList.Add(0f);
				UnityEngine.Debug.Log("5ortadatList" + ortalamadataList.Count);
			}
			UnityEngine.Debug.Log("VoiceHappy" + Percentage_output.ToString() + "No:" + CurrentPercentNo.ToString());

			MachineCtrl.Have_a_Sound = 1;

			This_text.text = "--";
			full_data_list.Clear();
			Changed_OK = 0;
			if (Happy_Ok > 0)
				Happy_Ok = 0;

			
		}
		else
		{
			ortalamadataList[CurrentPercentNo] = CurrentPercent + 0.1f;
		}
	}

	public void Train_ButtonClick()
	{

		Train_Mod++;

		if (Train_Mod > 5)
			Train_Mod = 1;

		Train_text.text = "Train Mod: " + Train_Mod.ToString();

	}

	public void Learn_ButtonClick()
	{
		cb_Learning_Button.normalColor = Color.green;
		//Learn_Mod = 1;
	}

	public void Train_Active_ButtonClick()
	{

		cb_TrainActive_Button.normalColor = Color.green;


		if (Train_Activate == 0)
			Train_Activate = 1;
		else
			Train_Activate = 0;

		Train_Activate_text.text = "Activate: " + Train_Activate.ToString();

	}

	public struct Complex
	{
		/// <summary>
		/// Real Part
		/// </summary>
		public float X;
		/// <summary>
		/// Imaginary Part
		/// </summary>
		public float Y;
	}

	public static void FFT(bool forward, int m, Complex[] data)
	{
		int n, i, i1, j, k, i2, l, l1, l2;
		float c1, c2, tx, ty, t1, t2, u1, u2, z;

		// Calculate the number of points
		n = 1;
		for (i = 0; i < m; i++)
			n *= 2;

		// Do the bit reversal
		i2 = n >> 1;
		j = 0;
		for (i = 0; i < n - 1; i++)
		{
			if (i < j)
			{
				tx = data[i].X;
				ty = data[i].Y;
				data[i].X = data[j].X;
				data[i].Y = data[j].Y;
				data[j].X = tx;
				data[j].Y = ty;
			}
			k = i2;

			while (k <= j)
			{
				j -= k;
				k >>= 1;
			}
			j += k;
		}

		// Compute the FFT 
		c1 = -1.0f;
		c2 = 0.0f;
		l2 = 1;
		for (l = 0; l < m; l++)
		{
			l1 = l2;
			l2 <<= 1;
			u1 = 1.0f;
			u2 = 0.0f;
			for (j = 0; j < l1; j++)
			{
				for (i = j; i < n; i += l2)
				{
					i1 = i + l1;
					t1 = u1 * data[i1].X - u2 * data[i1].Y;
					t2 = u1 * data[i1].Y + u2 * data[i1].X;
					data[i1].X = data[i].X - t1;
					data[i1].Y = data[i].Y - t2;
					data[i].X += t1;
					data[i].Y += t2;
				}
				z = u1 * c1 - u2 * c2;
				u2 = u1 * c2 + u2 * c1;
				u1 = z;
			}
			c2 = (float)Math.Sqrt((1.0f - c1) / 2.0f);
			if (forward)
				c2 = -c2;
			c1 = (float)Math.Sqrt((1.0f + c1) / 2.0f);
		}

		// Scaling for forward transform 
		if (forward)
		{
			for (i = 0; i < n; i++)
			{
				data[i].X /= n;
				data[i].Y /= n;
			}
		}
	}

	const int HEADER_SIZE = 44;
	// WAV file format from
	static void WriteWAVFile(AudioClip clip, string filePath)
	{
		float[] clipData = new float[clip.samples];

		//Create the file.
		using (Stream fs = File.Create(filePath))
		{
			int frequency = clip.frequency;
			int numOfChannels = clip.channels;
			int samples = clip.samples;
			fs.Seek(0, SeekOrigin.Begin);

			//Header

			// Chunk ID
			byte[] riff = Encoding.ASCII.GetBytes("RIFF");
			fs.Write(riff, 0, 4);

			// ChunkSize
			byte[] chunkSize = BitConverter.GetBytes((HEADER_SIZE + clipData.Length) - 8);
			fs.Write(chunkSize, 0, 4);

			// Format
			byte[] wave = Encoding.ASCII.GetBytes("WAVE");
			fs.Write(wave, 0, 4);

			// Subchunk1ID
			byte[] fmt = Encoding.ASCII.GetBytes("fmt ");
			fs.Write(fmt, 0, 4);

			// Subchunk1Size
			byte[] subChunk1 = BitConverter.GetBytes(16);
			fs.Write(subChunk1, 0, 4);

			// AudioFormat
			byte[] audioFormat = BitConverter.GetBytes(1);
			fs.Write(audioFormat, 0, 2);

			// NumChannels
			byte[] numChannels = BitConverter.GetBytes(numOfChannels);
			fs.Write(numChannels, 0, 2);

			// SampleRate
			byte[] sampleRate = BitConverter.GetBytes(frequency);
			fs.Write(sampleRate, 0, 4);

			// ByteRate
			byte[] byteRate = BitConverter.GetBytes(frequency * numOfChannels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
			fs.Write(byteRate, 0, 4);

			// BlockAlign
			ushort blockAlign = (ushort)(numOfChannels * 2);
			fs.Write(BitConverter.GetBytes(blockAlign), 0, 2);

			// BitsPerSample
			ushort bps = 16;
			byte[] bitsPerSample = BitConverter.GetBytes(bps);
			fs.Write(bitsPerSample, 0, 2);

			// Subchunk2ID
			byte[] datastring = Encoding.ASCII.GetBytes("data");
			fs.Write(datastring, 0, 4);

			// Subchunk2Size
			byte[] subChunk2 = BitConverter.GetBytes(samples * numOfChannels * 2);
			fs.Write(subChunk2, 0, 4);

			// Data

			clip.GetData(clipData, 0);
			short[] intData = new short[clipData.Length];
			byte[] bytesData = new byte[clipData.Length * 2];

			int convertionFactor = 32767;

			for (int i = 0; i < clipData.Length; i++)
			{
				intData[i] = (short)(clipData[i] * convertionFactor);
				byte[] byteArr = new byte[2];
				byteArr = BitConverter.GetBytes(intData[i]);
				byteArr.CopyTo(bytesData, i * 2);
			}

			fs.Write(bytesData, 0, bytesData.Length);
		}
	}

	void Show_spectrom(float val, int i, float t)
	{

		//for (int i = 0; i < 512; i++)
		{


			if (val < 8)
			{
				//Debug.DrawLine(new Vector3(count_m, i - 1, 3), new Vector3(count_m, i, 3), Color.red, t);
			}
			else if (val >= 255)
				UnityEngine.Debug.DrawLine(new Vector3(count_m, i - 1, 3), new Vector3(count_m, i, 3), Color.cyan, t);
			else if ((val < 255) && (val >= 200))
				UnityEngine.Debug.DrawLine(new Vector3(count_m, i - 1, 3), new Vector3(count_m, i, 3), Color.black, t);
			else if ((val < 200) && (val >= 150))
				UnityEngine.Debug.DrawLine(new Vector3(count_m, i - 1, 3), new Vector3(count_m, i, 3), Color.grey, t);
			else if ((val < 150) && (val >= 100))
				UnityEngine.Debug.DrawLine(new Vector3(count_m, i - 1, 3), new Vector3(count_m, i, 3), Color.blue, t);
			else if ((val < 100) && (val >= 50))
				UnityEngine.Debug.DrawLine(new Vector3(count_m, i - 1, 3), new Vector3(count_m, i, 3), Color.magenta, t);
			else if ((val < 50) && (val >= 8))
				UnityEngine.Debug.DrawLine(new Vector3(count_m, i - 1, 3), new Vector3(count_m, i, 3), Color.green, t);
		}
	}

	void write_first(int obj)
	{
		//UnityEngine.Debug.Log("samples " + source.clip.samples + "channels" + source.clip.channels + "Get" + (Microphone.GetPosition(null)) + "micc" + micc);
		samplesData = new float[44100];//[source.clip.samples * source.clip.channels];


		source.clip.GetData(samplesData, micc - 1000);

		//var samples = samplesData.ToList();


		// Create the audio file after removing the silence
		AudioClip audioClip = AudioClip.Create("Audio", samplesData.Length, source.clip.channels, 44100, false);
		audioClip.SetData(samplesData, 0);

		// Assign Current Audio Clip to Audio Player
		//audioPlayer.audioClip = audioClip;
		//audioPlayer.UpdateClip();
		//Application.persistentDataPath
		string filePath = Path.Combine("/Users/metacone/Desktop/Sesrecord/", obj.ToString()/*+"_" + Train_Mod.ToString()*//*DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_ffff")*/ + ".wav");
		//recordingTime = 0;
		// Delete the file if it exists.
#if UNITY_IOS
		if (Application.platform == RuntimePlatform.IPhonePlayer)
			filePath = Path.Combine(Application.persistentDataPath, obj.ToString()/* + "_" + Train_Mod.ToString()*//*DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_ffff")*/ + ".wav");
#endif

		//string filePath = Path.Combine(Application.persistentDataPath, obj.ToString() + "_" + Train_Mod.ToString()/*DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_ffff")*/ + ".wav");
		if (File.Exists(filePath))
		{
			File.Delete(filePath);
		}
		try
		{
			WriteWAVFile(audioClip, filePath);
			//ConsoleText.text = "Audio Saved at: " + filePath;
			UnityEngine.Debug.Log("File Save Succ at " + filePath);
			This_text.text = "filesave";
		}
		catch (DirectoryNotFoundException)
		{
			UnityEngine.Debug.LogError("Pers Data not!");
			This_text.text = "file er"+ filePath;
		}
	}

	public Button Train_Button, Learn_Button, Train_Active_Button;
	public Button Happy_Button, Sad_Button;
	public TextMeshProUGUI This_text;
	//public TextMeshProUGUI Analyze_text;
	public TextMeshProUGUI Percantage_text;
	public TextMeshProUGUI Result_text;
	public TextMeshProUGUI Train_text;
	public TextMeshProUGUI Train_Activate_text;
	//public TextMeshProUGUI Learning_text;
	public TextMeshProUGUI Mic_Timer_text;
	public TextMeshProUGUI Public_Key_text;
	/*public TextMeshProUGUI BufCapture_text;
	public TextMeshProUGUI BufSize_text;
	public TextMeshProUGUI Unanalyzed_text;*/
	AudioSource source;
	public AudioMixerGroup mix_Microphone, mix_Master;
	ColorBlock cb_TrainActive_Button, cb_Train_Button, cb_Learning_Button;

	public const int Array_Sized = 1024;
	float[] last_data = new float[Array_Sized];
	int Changed = 0;
	//int change2 = 0;
	//int change3 = 0;
	[System.Serializable]
	private class DataWrapper
	{
		public float[] data;

		public DataWrapper(float[] _data)
		{
			data = _data;
		}
	}

	List<float> ortalamadataList = new List<float>();
	public static string StringPublickey;

	//int AveCount = 0;
	int lastavecount = 0;

	int fd100 = 0;
	int Changed_OK = 0;

	const int Voice_neuro = Array_Sized * 50;
	double[] full_data = new double[Voice_neuro];

	List<float> full_data_list = new List<float>();

	//double[] full_data2 = new double[Voice_neuro];
	double[] Avarage = new double[4096];

	double[][] Out_Backs = new double[10][];

	//	int ilksilinme = 1;
	//float ortalama = 0;

	int count_m = 0;
	int Train_Activate = 0;
	//int Learn_Mod = 0;
	int Train_Mod = 1;

	//	int Count = 0;
	float Percentage_Accuracy = 0;
	int Percentage_output = 0;
	//	int Percentage_output2 = 0;
	public static TcpClient client;

	int Rndtime = 0;
	int RndtimeOK = 0;

	static float[] samplesData;// = new float[88200000];
							   //	static float[] samplesData1;
							   //	private float recordingTime = 0f;
							   //public int timeToRecord = 20;
	int micc = 0;

	public TextMeshProUGUI Data_Event_Mic_text;

	private float timer = 0.0f;
	private float analyzeInterval = 0.002f; // 20 miliseconds

	//yeni veri geldiğinde değişim çok az onu incele.
	//veri değişimi olduğunda sorgula. (ilk 5 değişim diye sınırlandırabiliriz.)

	int Happy_Ok = 0;
	float rotationSpeed = 1f;
	float timingdelta = 0;

	float Happytimer = 0f;
	float waitHappyTimer = 5f;
	private bool isTimeHappy = false;

	public List<List<List<double>>> S_NeuroCom_Voice = new List<List<List<double>>>();
	public int First_Voice_Shot = 0;
	double[] Const_Neighb_Voice = new double[5] { 1.0, 0.5, 0.25, 0.1, 0.05 };
	public static string ID_Device;
	public static int ID_Device_int;

	//	float LastPercent = 0;
	float CurrentPercent = 0;
	public static int CurrentPercentNo = 0;
	public static int LastPercentNo = 0;
	public static int ChangeOf_Voice_new = 0;
	//public static int ChangeOf_Voice_Last = 0;
	//public static int ChangeOf_Voice_Last_No = 0;
	//float[] ortalama = new float[20];

	public string dataPath;

	[System.Serializable]
	public class FloatArrayWrapper
	{
		public float[] array;
	}

	public class TeachingData
	{
		public string Id;
		public string publickey;
		public List<double> Data;
		public int Teach;
		public int Outp;
		public int First_Shot;
	}

	public class LoginData
	{
		public string Id;
		public string publickey;
	}

	public class LoginDataIn
	{
		public string Id;
		public string publickey;
		public int Outp_Cam;
		public int First_Shot_Cam;
		public int Outp_Mic;
		public int First_Shot_Mic;
	}
	public class LoginResponse
	{
		public string id;
		public string publickey;
		public int first_Shot_Cam;
		public int outp_Cam;
		public int first_Shot_Mic;
		public int outp_Mic;
	}
}