using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
//using System.Text;
using System.Diagnostics;
//using UnityEngine.Networking;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Compression;

public class CamSc : MonoBehaviour
{
    void Start()
    {
        //Application.targetFrameRate = 10;

        theColor.highlightedColor = UnityEngine.Color.cyan;
        theColor.normalColor = UnityEngine.Color.cyan;
        theColor.pressedColor = UnityEngine.Color.cyan;
        theColor.disabledColor = UnityEngine.Color.cyan;
        theColor.selectedColor = UnityEngine.Color.cyan;
        theColor.colorMultiplier = 1;
        //theColor.normalColor = UnityEngine.Color.white;
        ControlTrain_Button.colors = theColor;

        //var cam_select = transform.GetComponent<Dropdown>();
        cam_select.options.Clear();

        for (int i = 0; i < WebCamTexture.devices.Length; i++)
            items.Add(WebCamTexture.devices[i].name);

        cam_select.AddOptions(items);

    }

    int index = 0;
    public void DropdownItemSelected()
    {
        index = cam_select.value;
        //UnityEngine.Debug.Log("value" + index);

    }

    public void SwamCam_Clicked()
    {
        /*currentCamIndex += 1;
        currentCamIndex %= WebCamTexture.devices.Length;
        UnityEngine.Debug.Log(WebCamTexture.devices[currentCamIndex].name) ;
        if (webcamTexture != null)
        {
            stopWebcam();
            StartStopCam_Clicked();
        }*/
    }

    public void StartStopCam_Clicked()
    {
        if (webcamTexture != null)
        {
            stopWebcam();
            startStopText.text = "Start Camera";
        }
        else
        {
            WebCamDevice device = WebCamTexture.devices[index];
            webcamTexture = new WebCamTexture(device.name, 640, 480, 2);

            if (device.isFrontFacing)
            {
                
                //display.rectTransform.localEulerAngles = new Vector3(0, 0, -90);
                display3.rectTransform.localEulerAngles = new Vector3(0, 0, -90);

            }
            else
            {
                //display.rectTransform.localEulerAngles = Vector3.zero;
                display3.rectTransform.localEulerAngles = Vector3.zero;
            }


            //UnityEngine.Debug.Log(device.name);
            display.texture = webcamTexture;

            webcamTexture.Play();
            startStopText.text = "Stop Camera";
            StartCoroutine("Capture");

            cam_select.gameObject.SetActive(false);
            cam_start.gameObject.SetActive(false);
            display3.gameObject.SetActive(true);
        }
    }

    private void stopWebcam()
    {
        display.texture = null;
        webcamTexture.Stop();
        webcamTexture = null;
        StopCoroutine("Capture");
    }

    private IEnumerator Capture()
    {
        while (true)
        {
            //yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(captureIntervalSeconds);
            if (webcamTexture.width != 640 || webcamTexture.height != 480)
            {
                UnityEngine.Debug.LogError($"Cam 640x480 değil: {webcamTexture.width}x{webcamTexture.height}");
                yield return null;
            }
            else
            {

                UnityEngine.Color32[] pixels = webcamTexture.GetPixels32();

                //UnityEngine.Debug.Log("widht" + webcamTexture.width + "heigh" + webcamTexture.height);

                if (pixels.Length == 0)
                    yield return null;

                byte_Color = Color32ArrayToByteArray(pixels);

                //byte_Color = Color32ArrayToByteArrayunsafe(pixels);

                if (byte_Color.Length < 2560 * 480)
                {
                    UnityEngine.Debug.LogError("byte_Color kısa!");
                    yield return null;
                }
                else
                {
                    framefull = 1;
                    yield return timer_Tick();
                }
            }

        }

    }

    private List<float> ConvertDoubleArrayToFloatList(double[,,] input)
    {
        List<float> output = new List<float>();

        int dim1 = input.GetLength(0);
        int dim2 = input.GetLength(1);
        int dim3 = input.GetLength(2);

        for (int i = 0; i < dim1; i++)
        {
            for (int j = 0; j < dim2; j++)
            {
                for (int k = 0; k < dim3; k++)
                {
                    output.Add((float)input[i, j, k]);
                }
            }
        }

        return output;
    }

    private UInt16 ComputeChecksum(byte[] data)
    {
        UInt32 checksum = 0;
        foreach (byte b in data)
        {
            checksum += b;
        }
        return (UInt16)(checksum & 0xFFFF);  // sadece alt 2 byte'ı al
    }
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
        if (Microphones.client == null || !Microphones.client.Connected)
        {
            UnityEngine.Debug.LogError("Connection lost!");
            return null;
        }
        try
        {
            NetworkStream stream = Microphones.client.GetStream();
            // Girdi olarak gelen int listesini byte dizisine dönüştür.
            byte[] byteData = new byte[data.Count * sizeof(float)];
            Buffer.BlockCopy(data.ToArray(), 0, byteData, 0, byteData.Length);

            byte[] compressedData = Compress(byteData);
            //UnityEngine.Debug.Log("byteData" + byteData.Length + "compressedData" + compressedData.Length);


            byte[] firstshotBytes = BitConverter.GetBytes(fshot);
            byte[] trainmodBytes = BitConverter.GetBytes(Microphones.CurrentPercentNo);
            byte[] IDbytes = BitConverter.GetBytes(Microphones.ID_Device_int);

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
            byte[] fullPacket = new byte[1 + 1 + 2 +4 + 4 + byteDataWithChecksum.Length];
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
            
            stream.Write(fullPacket, 0, fullPacket.Length);
            //UnityEngine.Debug.Log("csbytes: " + checksumBytes[0]+ checksumBytes[1]);
            //UnityEngine.Debug.Log("cs: " + checksum + "Sent: " + fullPacket.Length + " integers with sourceType: " + sourceType + " and taskType: " + taskType);
            //UnityEngine.Debug.Log("Send_cam:" + fullPacket.Length + "-" + sourceType + taskType);
            // Receive Data (istek üzerine veri almak için, bu kısmı değiştirmedim)
            //byteData = new Byte[256];
            //String responseData = String.Empty;
            //Int32 bytes = stream.Read(byteData, 0, byteData.Length);
            //responseData = System.Text.Encoding.ASCII.GetString(byteData, 0, bytes);
            //UnityEngine.Debug.Log("Rec_Cam: " + responseData);
            //string responseDataHex = BitConverter.ToString(byteData, 0, bytes);
            //UnityEngine.Debug.Log("Rec_Cam (Hex): " + responseDataHex);

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
                    //UnityEngine.Debug.Log("Incomplete cam data received.");
                    return new float[0]; // boş bir float dizisi döndür
                }

                // byte dizisini float dizisine dönüştür
                float[] result = new float[floatArrayLength];
                for (int i = 0; i < byteData2.Length; i += 4)
                {
                    result[i / 4] = BitConverter.ToSingle(byteData2, i);
                }

                //            UnityEngine.Debug.Log("RecCamtask: " + string.Join(", ", result));
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
                    UnityEngine.Debug.Log("stream_cam: " + BitConverter.ToInt16(fshoting, 0) + BitConverter.ToInt16(outing, 0));
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
            return null;
        }
    }

    private float[] SendTCPTask(byte sourceType, byte taskType)
    {
        if (Microphones.client == null || !Microphones.client.Connected)
        {
            UnityEngine.Debug.LogError("Connection lost!");
            return null;
        }
        try
        {
            NetworkStream stream = Microphones.client.GetStream();

            byte[] firstshotBytes = BitConverter.GetBytes(fshot);
            byte[] trainmodBytes = BitConverter.GetBytes(Microphones.CurrentPercentNo);
            byte[] IDbytes = BitConverter.GetBytes(Microphones.ID_Device_int);
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
            //UnityEngine.Debug.Log("Sendcam: sourceType: " + sourceType + " and taskType: " + taskType);

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
                    //UnityEngine.Debug.Log("Incomplete cam data received.");
                    return new float[0]; // boş bir float dizisi döndür
                }

                // byte dizisini float dizisine dönüştür
                float[] result = new float[floatArrayLength];
                for (int i = 0; i < byteData2.Length; i += 4)
                {
                    result[i / 4] = BitConverter.ToSingle(byteData2, i);
                }

    //            UnityEngine.Debug.Log("RecCamtask: " + string.Join(", ", result));
                return result;

            }
            else 
            {
                byte[] state = new byte[2]; // 2 byte data + 4 byte uzunluk
                stream.Read(state, 0, state.Length);
                if (state[1] == 1)
                {
                    byte[] fshoting = new byte[2]; // 2 byte data 
                    stream.Read(fshoting, 0, fshoting.Length);
                    //fshot = BitConverter.ToInt16(fshoting, 0);
                    byte[] outing = new byte[2]; // 2 byte data 
                    stream.Read(outing, 0, outing.Length);
                    //Microphones.CurrentPercentNo = BitConverter.ToInt16(outing, 0);
                    UnityEngine.Debug.Log("stream_cam: " + BitConverter.ToInt16(fshoting, 0) + BitConverter.ToInt16(outing, 0));
                }
                //UnityEngine.Debug.Log("RecCamTrain: ");
                float[] result = new float[1];
                result[0] = 0.0f;
                return result;
            }
        }
        catch (Exception ex)
        {
            // Burada hata mesajını yakalayıp, logluyoruz. 
            UnityEngine.Debug.LogError($"Send/Receive error: {ex.Message}");
            return null;
        }


    }

    private IEnumerator Cam_Learning_Tcp(List<float> outputfloat)
    {
        //Stopwatch stopwatch = new Stopwatch();
        // Zamanı başlat
        //stopwatch.Start();
        // ... işlemler ...

        float[] SimilarPercent = SendTCPmsg(1, 0, outputfloat);
        if (SimilarPercent == null)
        {
            UnityEngine.Debug.Log("SimilarPercentcamnull");
            // Hata işleme kısmı
            yield break;
        }
        else if (SimilarPercent[0] == 0.0f)
        {
            //UnityEngine.Debug.Log("similar 0");
            yield return null;
        }
        else
        {

            //UnityEngine.Debug.Log("fshotcam" + fshot);

            Percentage_Accuracy_Train = SimilarPercent[0];
            Percentage_output_train = 0;
            for (int oo = 1; oo < fshot; oo++)
            {
                if (Percentage_Accuracy_Train < SimilarPercent[oo])
                {
                    Different_Train_Output = SimilarPercent[oo] - Percentage_Accuracy_Train;
                    Percentage_Accuracy_Train = SimilarPercent[oo];
                    Percentage_output_train = oo;
                }
            }

            CurrentPercent = Percentage_Accuracy_Train;
            CurrentPercentNo = Percentage_output_train;
            Train_Out = CurrentPercentNo;
            Cam_Percantage_text.SetText("CAM_Curracc:" + Percentage_Accuracy_Train.ToString()
                + " CurrentNo " + Percentage_output_train
                + " Dif " + Different_Train_Output.ToString()
                + " TrainOut " + (fshot+1) /*+ " ORJ " + SimilarPercent[Train_Out-1]*/);
            
            if ((Percentage_output_train != (fshot-1 )) && (Moving_start == 0)) //hareket var ise 5 sn içinde bilgilendirebilirsin. 
            {
                Percantage_Cam = CurrentPercent;
                Out_Cam = CurrentPercentNo;
                Moving_start = 1;
            }

            // Zamanı durdur
            //stopwatch.Stop();

            // Geçen süreyi milisaniye cinsinden yazdır
            //UnityEngine.Debug.Log("Camlearning Time: " + stopwatch.ElapsedMilliseconds + " ms");
            yield return null;
        }
    }

    private void Cam_Training_Tcp()
    {
        float[] receivedData = SendTCPTask(1, 1);
        if (receivedData == null)
        {
            UnityEngine.Debug.Log("receivedDatanull");
            // Hata işleme kısmı
            return;
        }
        UnityEngine.Debug.Log("fshotcam_Training" + fshot);
        if ((fshot == (Train_Out - 1)))
            fshot++;
    }

    private void middlefilter()
    {
        Selected_Image(outputcc);
        //Selected_Image(lastoutput);
        output2 = this.Metacone_lib.ConvolutionLayerAva(SelectOutput);//this.ConvolutionLayer2(output2); 62 x 62
        output3 =this.Metacone_lib.MaxPoolingLayerAva(output2);//Train için hazır en az 4 farklı filtreye sokulup birleştirilmeli. //9 filtreye sokuluyor. //inp 80/60 out: 62/2+1
        //output4 = this.Metacone_lib.ConvolutionLayer32(output3); //3 * 240 * 180  //-2 * 3
        //output5 = this.Metacone_lib.FlatternLayerOneRange(output4); //0-30 arasında //0-1 arasında
                                                                    //UnityEngine.Debug.Log("Mov Det2");
    }

    private IEnumerator timer_Tick()
    {
        if ((framefull == 1)&&(Microphones.Connection_Error==0))
        {
            int Moving_Cam = 0;
            framefull = 0;
            pixelsf(byte_Color, pixelvalues);
            output = Metacone_lib.ConvolutionLayerNoFilter(pixelvalues);
            outputb = Metacone_lib.MaxPoolingLayerAva(output);//filtre yok
            outputc = Metacone_lib.ConvolutionLayerAva(outputb);//ortalama filtresi var
            outputd = Metacone_lib.MaxPoolingLayerAva(outputc);//filtre yok
            ChangeORG = outputd;
            if (First_Time == 0) //Sadece ilk kez last output olsun diye.
            {
                lastoutput = outputd;
                First_Time = 1;
            }
            outputcc = this.ConvolutionChange(outputd, lastoutput);//değişim olup olmadığı arastırılıyor. //output -2 = 
            lastoutput = ChangeORG;
            //if (MachineCtrl.Have_a_Sound == 1) //ses varmı kontrol et.
            //{
            if ((ChangeKonum[0, 0] >= 10) && (ChangeKonum[0, 0] <= 90) && (ChangeKonum[0, 1] <= 50) && (ChangeKonum[0, 1] >= 10))
            {
                Moving_Cam = 1; //Hareket olduğunu belirtir. 
                middlefilter(); //ara filtreleri ayarlar
                List<float> outputfloat = ConvertDoubleArrayToFloatList(output3);
                Stopwatch watch = new Stopwatch();
                watch.Start();
                yield return StartCoroutine(Cam_Learning_Tcp(outputfloat));
                //SendTCPmsg(1, 0, outputfloat);
                watch.Stop();
                //UnityEngine.Debug.Log("CamWtime" + watch.Elapsed.TotalMilliseconds.ToString());

            }
            if (MachineCtrl.Have_a_Sound == 1) //ses varmı kontrol et.
            {
                //UnityEngine.Debug.Log("Have_a_Sound1cam");
                if ((Moving_Cam == 1) || (Microphones.ChangeOf_Voice_new == 1)) //Hem ses hem hareket varsa traininge başlar.
                {
                    //UnityEngine.Debug.Log("Have_a_Sound0cam");
                    MachineCtrl.Have_a_Sound = 0;
                    MachineCtrl.Have_a_Moving = 1;//ses tarafını aktif et.
                    if (Microphones.ChangeOf_Voice_new == 1) //yeni veri var ise
                    {
                        Train_Out = fshot+1;//Microphones.CurrentPercentno gibi davranmalı
                        //Train_Out++;
                        //UnityEngine.Debug.Log("Cam_new" + Microphones.CurrentPercentNo.ToString());
                        Microphones.ChangeOf_Voice_new = 0;
                        Cam_Training_Tcp();
                    }
                    else if (Microphones.ChangeOf_Voice_new == 0)
                    {
                        //UnityEngine.Debug.Log("Cam_Last" + Microphones.CurrentPercentNo.ToString());
                        Cam_Training_Tcp();
                    }

                }
            }

            Panel_Camera();
            Moving_Cam = 0;
        }

        yield break;

    }
    private void Panel_Camera()
    {
        /*if (buton_Ok2 == 1)
        {
            show(SelectOutputlast, display3, img3);
        }
        else*/
        {
            show(SelectOutput, display3, img3);
        }
        //buton_Ok2 = 0;

        show(output3, display4, img4);
    }

    private static byte[] Color32ArrayToByteArray(Color32[] colors)
    {
        if (colors == null || colors.Length == 0)
            return null;

        int lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
        int length = lengthOfColor32 * colors.Length;
        byte[] bytes = new byte[length];

        GCHandle handle = default(GCHandle);
        try
        {
            handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
            IntPtr ptr = handle.AddrOfPinnedObject();
            Marshal.Copy(ptr, bytes, 0, length);
        }
        finally
        {
            if (handle != default(GCHandle))
                handle.Free();
        }

        return bytes;
    }

    private void pixelsf(byte[] bytes, double[,,] pixelvalues)
    {

        for (int y = 0; y < 480; y = y + 1)
        {
            for (int x = 0; x < 640; x = x + 1)
            {
                pixelvalues[2, x, y] = bytes[(y * 2560) + (x * 4) + 2];
                pixelvalues[1, x, y] = bytes[(y * 2560) + (x * 4) + 1];
                pixelvalues[0, x, y] = bytes[(y * 2560) + (x * 4)];
            }
        }

    }

    public double[,,] ConvolutionChange(double[,,] input, double[,,] inputEski)
    {
        double[,,] output = new double[input.GetLength(0), input.GetLength(1) - 2, input.GetLength(2) - 2];
        int ho = input.GetLength(1) - 5;
        int vo = input.GetLength(2) - 5;

        double totalX = 0;  // Hareketli nesnenin tüm x koordinatlarının toplamı
        double totalY = 0;  // Hareketli nesnenin tüm y koordinatlarının toplamı
        int totalPixels = 0;  // Hareketli nesnenin içindeki toplam piksel sayısı

        //Array.Copy(input, output, output.Length);???

        int CountChange = 0;
        ChangeKonum[0, 0] = 0;
        ChangeKonum[0, 1] = 0;
        ChangeKonum[1, 0] = 0;
        ChangeKonum[1, 1] = 0;

        for (int y = 1; y < vo - 2; y++)
        {
            int edgeStartX = -1;
            int edgeEndX = -1;

            for (int x = 1; x < ho - 2; x++)
            {
                // Assume the pixel has not changed
                bool hasPixelChanged = false;

                // Check if the current pixel or its neighbors have changed
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (HasPixelChanged(input, inputEski, x + dx, y + dy))
                        {
                            hasPixelChanged = true;

                            if ((CountChange == 0) && (x > 10) && (y < 60) && (y > 10))
                            {
                                ChangeKonum[0, 0] = x;
                                ChangeKonum[0, 1] = y;
                                CountChange++;
                            }
                            else if (ChangeKonum[0, 0] == x)
                            {
                                ChangeKonum[1, 0] = x;
                                ChangeKonum[1, 1] = y;
                            }
                            else if ((ChangeKonum[0, 0] + 1 == x) && (ChangeKonum[1, 1] < y))
                            {
                                ChangeKonum[1, 0] = x;
                                ChangeKonum[1, 1] = y;
                            }
                            if (CountChange > 0)
                                CountChange++;
                            break;
                        }
                    }

                    if (hasPixelChanged)
                        break;
                }

                if (hasPixelChanged)
                {
                    // If we haven't found a start edge yet, this is it
                    if (edgeStartX == -1)
                        edgeStartX = x;

                    // Update the end edge to the current position
                    edgeEndX = x;
                }


            }

            // Now edgeStartX and edgeEndX define the range of the edge in the x direction.
            // We can use these to decide whether to keep the original pixel value or set it to 0.
            for (int x = 1; x < ho - 2; x++)
            {
                for (int c = 0; c < 3; c++) // For each color channel
                {
                    if (x >= edgeStartX && x <= edgeEndX)
                    {
                        // Inside the edge, keep the original pixel value
                        output[c, x, y] = input[c, x, y];
                        totalX += x;
                        totalY += y;
                        totalPixels++;

                    }
                    else
                    {
                        // Outside the edge, pixel should be black
                        output[c, x, y] = 0;
                    }
                }
            }
        }
        Change_X_Label.SetText("CHANGE_ALL: " + CountChange);
        if ((CountChange > 1200) && (CountChange < 4000))
        {
            //Change_XY_Label.SetText("X: " + ChangeKonum[0, 0] + " Y: " + ChangeKonum[0, 1]);
            if (totalPixels > 0)
            {
                double centerX = totalX / totalPixels;  // Ağırlıklı merkezin x koordinatı
                double centerY = totalY / totalPixels;  // Ağırlıklı merkezin y koordinatı

                // centerX ve centerY şimdi hareketli nesnenin ağırlıklı merkezini temsil ediyor.
                // Bu değerleri kullanarak hareketli nesnenin merkezine yakın özellikleri yakalayabilirsiniz.
                if ((centerX > 32) && (centerY > 32))
                {
                    ChangeKonum[0, 0] = (int)centerX - 32;
                    ChangeKonum[0, 1] = (int)centerY - 32;

                    Change_XY_Label.SetText("X: " + ChangeKonum[0, 0] + " Y: " + ChangeKonum[0, 1]);
                }
                else
                {
                    ChangeKonum[0, 0] = 0;
                    ChangeKonum[0, 1] = 0;
                    ChangeKonum[1, 0] = 0;
                    ChangeKonum[1, 1] = 0;
                }

            }
        }
        else
        {
            ChangeKonum[0, 0] = 0;
            ChangeKonum[0, 1] = 0;
            ChangeKonum[1, 0] = 0;
            ChangeKonum[1, 1] = 0;

        }
        return output;
      
    }

    private bool HasPixelChanged(double[,,] input, double[,,] inputEski, int x, int y)
    {
        return Math.Abs(input[0, x, y] - inputEski[0, x, y]) > 15 &&
               Math.Abs(input[1, x, y] - inputEski[1, x, y]) > 15 &&
               Math.Abs(input[2, x, y] - inputEski[2, x, y]) > 15;
    }

    public void Selected_Image(double[,,] input)
    {

        int xkon, ykon;
        xkon = ChangeKonum[0, 0];
        ykon = ChangeKonum[0, 1];

        for (int i = xkon; i < xkon + 64; i++)
        {
            for (int j = ykon; j < ykon + 64; j++)
            {
                SelectOutput[0, i - xkon, j - ykon] = input[0, i, j];
                SelectOutput[1, i - xkon, j - ykon] = input[1, i, j];
                SelectOutput[2, i - xkon, j - ykon] = input[2, i, j];
            }
        }

    }

    public void Selected_Image2(int Konum1, int Konum2, double[,,] input)
    {
        int xkon, ykon;
        if (Konum1 > 25)
            xkon = Konum1 - 20;
        else
            xkon = Konum1 - 5;

        if (Konum2 > 25)
            ykon = Konum2 - 20;
        else
            ykon = Konum2 - 5;

        //int xkon = Konum1 - 5;
        //int ykon = Konum2 - 5;
        for (int i = xkon; i < xkon + 64; i++)
        {
            for (int j = ykon; j < ykon + 64; j++)
            {
                SelectOutput2[0, i - xkon, j - ykon] = input[0, i, j];
                SelectOutput2[1, i - xkon, j - ykon] = input[1, i, j];
                SelectOutput2[2, i - xkon, j - ykon] = input[2, i, j];
            }
        }

    }

    public void Copy_Image()
    {
        for (int i = 0; i < 64; i++)
        {
            for (int j = 0; j < 64; j++)
            {
                SelectOutputlast[0, i, j] = SelectOutput2[0, i, j];
                SelectOutputlast[1, i, j] = SelectOutput2[1, i, j];
                SelectOutputlast[2, i, j] = SelectOutput2[2, i, j];
            }
        }

    }

    private void show(double[,,] input, RawImage displays, Texture2D img)
    {
        if (img == null || input.GetLength(1) != img.width || input.GetLength(2) != img.height)
        {
            img = new Texture2D(input.GetLength(1), input.GetLength(2), TextureFormat.RGBA32, false);
        }

        Color32[] outColors1 = new Color32[input.GetLength(1) * input.GetLength(2)];


        for (int y = 0; y < input.GetLength(2); y++) //640
        {
            for (int x = 0; x < input.GetLength(1); x++) //480
            {

                outColors1[x + y * input.GetLength(1)].r = (byte)input[0, x, y];
                outColors1[x + y * input.GetLength(1)].g = (byte)input[1, x, y];
                outColors1[x + y * input.GetLength(1)].b = (byte)input[2, x, y];
                if((input[0, x, y] == 0)&&(input[1, x, y] == 0)&&(input[2, x, y] == 0))
                {
                    outColors1[x + y * input.GetLength(1)].a = (byte)0;
                }
                else
                    outColors1[x + y * input.GetLength(1)].a = (byte)255;

            }
        }

        img.SetPixels32(outColors1);
        img.Apply();
        displays.texture = img;
    }

    WebCamTexture webcamTexture;
    public RawImage display;
    //public RawImage display2;
    public RawImage display3;
    public RawImage display4;
    public RawImage display5;
    public TMP_Dropdown cam_select;
    public Button cam_start;
    //public RawImage display5;
    //Texture2D texture2D;
    Texture2D img3;
    Texture2D img4;
    Texture2D img5;
    //Texture2D img5;
    //Texture2D img1;
    public TMP_Text startStopText;
    public Button Train_Mod_Button, Train_Active_Button, ControlTrain_Button;
    public TextMeshProUGUI Cam_Percantage_text;
    public TextMeshProUGUI Train_text;
    public TextMeshProUGUI Train_Activate_text;
    public TextMeshProUGUI ControlTrain_text;
    public TextMeshProUGUI Cam_Timer_text;
    public TextMeshProUGUI Data_Event_text;

    //Camera
    Metacone_Class Metacone_lib = new Metacone_Class();
    int framefull = 0;
    const int INP = 2700;
    private static List<List<double>> spec_data; // columns are time points, rows are frequency points
                                                 //    private static int spec_width = 600;
    private static int spec_height;
    int pixelsPerBuffer;

    //private static Random rand = new Random();
    // sound card settings
    private int rate;
    private int buffer_update_hz;

    // spectrogram and FFT settings
    int fft_size;

    //int Changed = 0;
    //    int Changed_timer = 0;Have_a_Sound
    //    int Sescount = 0;
    List<string> items = new List<string>();

    public float captureIntervalSeconds = 0.5f;
    byte[] byte_Color;
    double[,,] pixelvalues = new double[3, 640, 480];

    double[,,] output = new double[3, 638, 478];
    double[,,] outputb = new double[3, 320, 240];
    double[,,] outputc = new double[3, 318, 238];
    double[,,] outputd = new double[3, 160, 120];

    double[,,] outputcc = new double[3, 158, 118];
    double[,,,] outputccR = new double[20, 3, 158, 118];
    double[,,] output2 = new double[3, 62, 62];
    double[,,] output3 = new double[3, 32, 32];
    double[,,] output3last = new double[3, 32, 32];
    double[,,] output4 = new double[3, 90, 90];
    double[] output5 = new double[3 * 90 * 90];
    double[] output_Background = new double[3 * 90 * 90];

    double[][] output_Backs = new double[10][];

    double[,,] ChangeORG = new double[3, 160, 120];
    double[,,] lastoutput = new double[3, 160, 120];


    double[,,] SelectOutput = new double[3, 64, 64];
    double[,,] SelectOutput2 = new double[3, 64, 64];
    double[,,] SelectOutputlast = new double[3, 64, 64];

    double[] Avarage = new double[2048];
    public TextMeshProUGUI Change_X_Label, Change_XY_Label;
    int[,] ChangeKonum = new int[5, 2];
    int[,] ChangeKonum2 = new int[5, 2];

    int First_Time = 0;
    //dynamic lastoutput;

    //int buton_Ok = 0;
    //int buton_Ok2 = 0;

    float[] float_Input = new float[30];
    float[] float_Output = new float[3];

    //    float Percentage_Accuracy = 0;
    //    int Percentage_output = 0;

    float Percentage_Accuracy_Train = 0;
    int Percentage_output_train = 0;
    //float Last_Percentage_Accuracy_Train = 0;

    float Different_Train_Output = 0;

    //    float Last_Percentage = 0;
    //    int last_Out_Result = 0;
    public static int Change_Cam = 0;
    public static float Percantage_Cam = 0;
    public static int Out_Cam = 0;
    public static int Moving_start = 0;

    public static int Microphone_Activate = 0;

    private ColorBlock theColor;
    //bool isCoroutineFinished = false;
    float[] outputData;

    string jsonData;

    public static int fshot = 0;
    float CurrentPercent = 0;
    int CurrentPercentNo = 0;

    //int Best_thing = 0;



    private double[,] filterMatrix =   //sharpen 3x3
    new double[,] { { 0, -1, 0, },
                        { -1,  5, -1, },
                        { 0, -1, 0, }, };

    int learning_Close = 0;
    public static int Train_Out = 1;

    public void Train_MOD_Button_Click()
    {

        if (Train_Out == 1)
        {
            //Train_MOD_Button.BackColor = Color.Yellow;
            Train_Out = 2;
            Train_text.text = "Train 2:";
        }
        else if (Train_Out == 2)
        {
            //Train_MOD_Button.BackColor = Color.Orange;
            Train_Out = 3;
            Train_text.text = "Train 3:";
        }
        else if (Train_Out == 3)
        {
            //Train_MOD_Button.BackColor = Color.Black;
            Train_Out = 4;
            Train_text.text = "Train 4:";
        }
        else
        {
            //Train_MOD_Button.BackColor = Color.Blue;
            Train_Out = 1;
            Train_text.text = "Train 1:";
        }
    }

    public void Training_Activate_Button_Click()
    {


        if (learning_Close == 0)
        {
            //Training_Button.BackColor = Color.Green;
            learning_Close = 1;
            Train_Activate_text.text = "TRAINING OPEN:";
        }
        else
        {
            learning_Close = 0;
            Train_Activate_text.text = "TRAINING CLOSE:";
            //Training_Button.BackColor = Color.Red;
        }

    }

    public void Control_Trains_Button_Click()
    {
        //buton_Ok = 1;
        ControlTrain_text.text = "Control Train :1 ";
        //Control_Trains_Button.BackColor = Color.Green;
    }

    [System.Serializable]
    public class FloatArray
    {
        public float[] array;
    }

    [System.Serializable]

    public class TeachingData
    {
        public string Id;
        public string publickey;
        public List<double> Data;
        public int Teach;
        public int Outp;
        public int First_Shot;
    }

    [System.Serializable]
    public class FloatArrayWrapper
    {
        public float[] array;
    }
}


public class Metacone_Class
{
    public List<List<List<double>>> S_NeuroCom = new List<List<List<double>>>();

    public int First_Shot = 0;
    double[] Const_Neighb = new double[5] { 1.0, 0.5, 0.25, 0.1, 0.05 };
    const int sizefull = 24300;//2700

    double[,,] outputc = new double[3, 638, 478];
    public double[,,] ConvolutionLayerNoFilter(double[,,] input)
    {
        int hoc = input.GetLength(1) - 2;//318;
        int voc = input.GetLength(2) - 2;//238;
        System.Threading.Tasks.Parallel.For(0, hoc, x =>
        //for (int x = 0; x < ho; x++)
        {
            for (int y = 0; y < voc; y++)
            {
                outputc[0, x, y] = (int)((input[0, x, y]));
                outputc[1, x, y] = (int)((input[1, x, y]));
                outputc[2, x, y] = (int)((input[2, x, y]));

            }
        });

        return outputc;
    }

    public double[,,] ConvolutionLayerAva(double[,,] input)  //input: 160 / 120 -> output 158 /118
    {
        double[,,] output = new double[input.GetLength(0), input.GetLength(1) - 2, input.GetLength(2) - 2];
        int ho = input.GetLength(1) - 2;
        int vo = input.GetLength(2) - 2;

        System.Threading.Tasks.Parallel.For(0, ho, x =>
        //for (int x = 0; x < input.GetLength(1) - 2; x++)
        {
            for (int y = 0; y < vo; y++)
            {
                int sum0 = 0;
                int sum1 = 0;
                int sum2 = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        sum0 += (int)((input[0, x + i, y + j]));
                        sum1 += (int)((input[1, x + i, y + j]));
                        sum2 += (int)((input[2, x + i, y + j]));

                    }
                }
                sum0 = sum0 / 9;
                sum1 = sum1 / 9;
                sum2 = sum2 / 9;
                if ((x > 1) && (y > 1))
                {
                    if (Math.Abs(sum0 - input[0, x - 1, y - 1]) < 30)
                        sum0 = (int)input[0, x - 1, y - 1];
                    if (Math.Abs(sum1 - input[0, x - 1, y - 1]) < 30)
                        sum1 = (int)input[1, x - 1, y - 1];
                    if (Math.Abs(sum2 - input[0, x - 1, y - 1]) < 30)
                        sum2 = (int)input[2, x - 1, y - 1];
                }

                output[0, x, y] = sum0;
                output[1, x, y] = sum1;
                output[2, x, y] = sum2;

            }
        });

        return output;
    }

    public double[,,] MaxPoolingLayerAva(double[,,] input)
    {
        //Formula for MaxPooling

        var newHeight = (input.GetLength(1) / 2) + 1; //318/2+1=160 -> 238/2+1 =120
        var newWidth = (input.GetLength(2) / 2) + 1;
        //output= new double[input.GetLength(0), input.GetLength(1), input.GetLength(2)]; for (int j = 0; j < input.GetLength(0); j++)
        double[,,] output = new double[input.GetLength(0), newHeight, newWidth];

        System.Threading.Tasks.Parallel.For(0, newHeight - 1, i =>
        //for (int i = 0; i < input.GetLength(1); i++)
        {
            double maxValue0;
            double maxValue1;
            double maxValue2;
            int k = i * 2;

            for (int j = 0; j < input.GetLength(2) - 1; j++)
            {
                maxValue0 = input[0, k, j] + input[0, k, j + 1] + input[0, k + 1, j] + input[0, k + 1, j + 1];
                maxValue0 /= 4;
                maxValue1 = input[1, k, j] + input[1, k, j + 1] + input[1, k + 1, j] + input[1, k + 1, j + 1];
                maxValue1 /= 4;
                maxValue2 = input[2, k, j] + input[2, k, j + 1] + input[2, k + 1, j] + input[2, k + 1, j + 1];
                maxValue2 /= 4;

                output[0, k / 2, j / 2] = maxValue0;
                output[1, k / 2, j / 2] = maxValue1;
                output[2, k / 2, j / 2] = maxValue2;

                j++;

            }

        });


        return output;
    }

    public double[,,] ConvolutionLayer32(double[,,] input)
    {
        int FilterCount = 3;
        double[,,] output = new double[input.GetLength(0), (input.GetLength(1) - 2) * (FilterCount), (input.GetLength(2) - 2) * (FilterCount)];
        int ho = input.GetLength(1) - 2;
        int vo = input.GetLength(2) - 2;

        System.Threading.Tasks.Parallel.For(0, ho, x =>
        //for (int x = 0; x < input.GetLength(1) - 2; x++)
        {
            for (int y = 0; y < vo; y++)
            {
                int sum0 = 0;
                int sum1 = 0;
                int sum2 = 0;
                int sum0_1 = 0;
                int sum1_1 = 0;
                int sum2_1 = 0;
                int sum0_2 = 0;
                int sum1_2 = 0;
                int sum2_2 = 0;
                int sum0_3 = 0;
                int sum1_3 = 0;
                int sum2_3 = 0;
                int sum0_4 = 0;
                int sum1_4 = 0;
                int sum2_4 = 0;
                int sum0_5 = 0;
                int sum1_5 = 0;
                int sum2_5 = 0;
                int sum0_6 = 0;
                int sum1_6 = 0;
                int sum2_6 = 0;
                int sum0_7 = 0;
                int sum1_7 = 0;
                int sum2_7 = 0;
                int sum0_8 = 0;
                int sum1_8 = 0;
                int sum2_8 = 0;

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        sum0 += (int)((input[0, x + i, y + j] * filterMatrix[i, j]));
                        sum1 += (int)((input[1, x + i, y + j] * filterMatrix[i, j]));
                        sum2 += (int)((input[2, x + i, y + j] * filterMatrix[i, j]));

                        sum0_1 += (int)((input[0, x + i, y + j] * filterMatrix2[i, j]));
                        sum1_1 += (int)((input[1, x + i, y + j] * filterMatrix2[i, j]));
                        sum2_1 += (int)((input[2, x + i, y + j] * filterMatrix2[i, j]));

                        sum0_2 += (int)((input[0, x + i, y + j] * filterMatrix3[i, j]));
                        sum1_2 += (int)((input[1, x + i, y + j] * filterMatrix3[i, j]));
                        sum2_2 += (int)((input[2, x + i, y + j] * filterMatrix3[i, j]));

                        sum0_3 += (int)((input[0, x + i, y + j] * filterMatrix4[i, j]));
                        sum1_3 += (int)((input[1, x + i, y + j] * filterMatrix4[i, j]));
                        sum2_3 += (int)((input[2, x + i, y + j] * filterMatrix4[i, j]));

                        sum0_4 += (int)((input[0, x + i, y + j] * filterMatrix5[i, j]));
                        sum1_4 += (int)((input[1, x + i, y + j] * filterMatrix5[i, j]));
                        sum2_4 += (int)((input[2, x + i, y + j] * filterMatrix5[i, j]));

                        sum0_5 += (int)((input[0, x + i, y + j] * filterMatrix6[i, j]));
                        sum1_5 += (int)((input[1, x + i, y + j] * filterMatrix6[i, j]));
                        sum2_5 += (int)((input[2, x + i, y + j] * filterMatrix6[i, j]));

                        sum0_6 += (int)((input[0, x + i, y + j] * filterKernel0[i, j]));
                        sum1_6 += (int)((input[1, x + i, y + j] * filterKernel0[i, j]));
                        sum2_6 += (int)((input[2, x + i, y + j] * filterKernel0[i, j]));

                        sum0_7 += (int)((input[0, x + i, y + j] * filterKernel1[i, j]));
                        sum1_7 += (int)((input[1, x + i, y + j] * filterKernel1[i, j]));
                        sum2_7 += (int)((input[2, x + i, y + j] * filterKernel1[i, j]));

                        sum0_8 += (int)((input[0, x + i, y + j] * filterKernel2[i, j]));
                        sum1_8 += (int)((input[1, x + i, y + j] * filterKernel2[i, j]));
                        sum2_8 += (int)((input[2, x + i, y + j] * filterKernel2[i, j]));

                    }
                }
                if (sum0 > 255)
                    sum0 = 255;
                else if (sum0 < 0)
                    sum0 = 0;
                if (sum1 > 255)
                    sum1 = 255;
                else if (sum1 < 0)
                    sum1 = 0;
                if (sum2 > 255)
                    sum2 = 255;
                else if (sum2 < 0)
                    sum2 = 0;

                if (sum0_1 > 255)
                    sum0_1 = 255;
                else if (sum0_1 < 0)
                    sum0_1 = 0;
                if (sum1_1 > 255)
                    sum1_1 = 255;
                else if (sum1_1 < 0)
                    sum1_1 = 0;
                if (sum2_1 > 255)
                    sum2_1 = 255;
                else if (sum2_1 < 0)
                    sum2_1 = 0;

                if (sum0_2 > 255)
                    sum0_2 = 255;
                else if (sum0_2 < 0)
                    sum0_2 = 0;
                if (sum1_2 > 255)
                    sum1_2 = 255;
                else if (sum1_2 < 0)
                    sum1_2 = 0;
                if (sum2_2 > 255)
                    sum2_2 = 255;
                else if (sum2_2 < 0)
                    sum2_2 = 0;

                if (sum0_3 > 255)
                    sum0_3 = 255;
                else if (sum0_3 < 0)
                    sum0_3 = 0;
                if (sum1_3 > 255)
                    sum1_3 = 255;
                else if (sum1_3 < 0)
                    sum1_3 = 0;
                if (sum2_3 > 255)
                    sum2_3 = 255;
                else if (sum2_3 < 0)
                    sum2_3 = 0;

                if (sum0_4 > 255)
                    sum0_4 = 255;
                else if (sum0_4 < 0)
                    sum0_4 = 0;
                if (sum1_4 > 255)
                    sum1_4 = 255;
                else if (sum1_4 < 0)
                    sum1_4 = 0;
                if (sum2_4 > 255)
                    sum2_4 = 255;
                else if (sum2_4 < 0)
                    sum2_4 = 0;

                if (sum0_5 > 255)
                    sum0_5 = 255;
                else if (sum0_5 < 0)
                    sum0_5 = 0;
                if (sum1_5 > 255)
                    sum1_5 = 255;
                else if (sum1_5 < 0)
                    sum1_5 = 0;
                if (sum2_5 > 255)
                    sum2_5 = 255;
                else if (sum2_5 < 0)
                    sum2_5 = 0;

                if (sum0_6 > 255)
                    sum0_6 = 255;
                else if (sum0_6 < 0)
                    sum0_6 = 0;
                if (sum1_6 > 255)
                    sum1_6 = 255;
                else if (sum1_6 < 0)
                    sum1_6 = 0;
                if (sum2_6 > 255)
                    sum2_6 = 255;
                else if (sum2_6 < 0)
                    sum2_6 = 0;

                if (sum0_7 > 255)
                    sum0_7 = 255;
                else if (sum0_7 < 0)
                    sum0_7 = 0;
                if (sum1_7 > 255)
                    sum1_7 = 255;
                else if (sum1_7 < 0)
                    sum1_7 = 0;
                if (sum2_7 > 255)
                    sum2_7 = 255;
                else if (sum2_7 < 0)
                    sum2_7 = 0;

                if (sum0_8 > 255)
                    sum0_8 = 255;
                else if (sum0_8 < 0)
                    sum0_8 = 0;
                if (sum1_8 > 255)
                    sum1_8 = 255;
                else if (sum1_8 < 0)
                    sum1_8 = 0;
                if (sum2_8 > 255)
                    sum2_8 = 255;
                else if (sum2_8 < 0)
                    sum2_8 = 0;

                output[0, x, y] = sum0;
                output[1, x, y] = sum1;
                output[2, x, y] = sum2;

                output[0, x + (input.GetLength(1) - 2), y] = sum0_1;
                output[1, x + (input.GetLength(1) - 2), y] = sum1_1;
                output[2, x + (input.GetLength(1) - 2), y] = sum2_1;

                output[0, x, y + (input.GetLength(2) - 2)] = sum0_2;
                output[1, x, y + (input.GetLength(2) - 2)] = sum1_2;
                output[2, x, y + (input.GetLength(2) - 2)] = sum2_2;

                output[0, x + (input.GetLength(1) - 2), y + (input.GetLength(2) - 2)] = sum0_3;
                output[1, x + (input.GetLength(1) - 2), y + (input.GetLength(2) - 2)] = sum1_3;
                output[2, x + (input.GetLength(1) - 2), y + (input.GetLength(2) - 2)] = sum2_3;

                output[0, x + (input.GetLength(1) - 2) * 2, y] = sum0_4;
                output[1, x + (input.GetLength(1) - 2) * 2, y] = sum1_4;
                output[2, x + (input.GetLength(1) - 2) * 2, y] = sum2_4;

                output[0, x, y + (input.GetLength(2) - 2) * 2] = sum0_5;
                output[1, x, y + (input.GetLength(2) - 2) * 2] = sum1_5;
                output[2, x, y + (input.GetLength(2) - 2) * 2] = sum2_5;

                output[0, x + (input.GetLength(1) - 2) * 2, y + (input.GetLength(2) - 2)] = sum0_6;
                output[1, x + (input.GetLength(1) - 2) * 2, y + (input.GetLength(2) - 2)] = sum1_6;
                output[2, x + (input.GetLength(1) - 2) * 2, y + (input.GetLength(2) - 2)] = sum2_6;

                output[0, x + (input.GetLength(1) - 2), y + (input.GetLength(2) - 2) * 2] = sum0_7;
                output[1, x + (input.GetLength(1) - 2), y + (input.GetLength(2) - 2) * 2] = sum1_7;
                output[2, x + (input.GetLength(1) - 2), y + (input.GetLength(2) - 2) * 2] = sum2_7;

                output[0, x + (input.GetLength(1) - 2) * 2, y + (input.GetLength(2) - 2) * 2] = sum0_8;
                output[1, x + (input.GetLength(1) - 2) * 2, y + (input.GetLength(2) - 2) * 2] = sum1_8;
                output[2, x + (input.GetLength(1) - 2) * 2, y + (input.GetLength(2) - 2) * 2] = sum2_8;


            }
        });

        return output;
    }

    public double[] FlatternLayerOneRange(double[,,] input)
    {

        int rgbChannel = input.GetLength(0);
        int rowPixel = input.GetLength(1);
        int columnPixel = input.GetLength(2);
        int length = rgbChannel * rowPixel * columnPixel;
        double[] output = new double[length];
        try
        {
            int count = 0;

            for (int d = 0; d < 3; d++)
            {
                for (int c = 0; c < 3; c++)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        for (int p = 0; p < 30; p++)
                        {
                            for (int l = 0; l < 30; l++)
                            {
                                output[count] = (double)((input[b, (d * 30) + p, (c * 30) + l] / 255.0f) /** 30.0f*/);///255.0f)*30.0);
                                count = count + 1;
                            }
                        }
                    }
                }
            }



            /*for (int i = 0; i < rgbChannel; i++)
            {
                for (int j = 0; j < rowPixel; j++)
                {
                    for (int k = 0; k < columnPixel; k++)
                    {
                        output[count] = (double)((input[i, j, k] / 255.0f) // 30.0f);///255.0f)*30.0);
                        count = count + 1;
                    }
                }
            }*/
        }
        catch //()
        {

        }
        return output;
    }

    private double[,] filterMatrix =   //sharpen 3x3
    new double[,] { { 0, -1, 0, },
                        { -1,  5, -1, },
                        { 0, -1, 0, }, };

    private double[,] filterMatrix2 =
    new double[,] { { -0.2, 0.0, 0.5, },
                        { -0.2, 0.2, 0.2, },
                        { -0.5, 0.0, 0.2, }, };

    private double[,] filterMatrix3 = //sharpen 3x3 
    new double[,] { { 0, -2, 0, },
                        { -2,  11, -2, },
                        { 0, -1, 0, }, };

    private double[,] filterMatrix4 = //yatay sobel 
   new double[,] { { -1, 0, 1, },
                        { -2,  0, 2, },
                        { -1, 0, 1, }, };


    private double[,] filterMatrix5 = //dikey sobel
   new double[,] { { -1, -2, -1, },
                        { 0,  0, 0, },
                        { 1, 2, 1, }, };

    private double[,] filterMatrix42 = //yatay sobel 
   new double[,] { { -0.5, 0, 0.5, },
                        { -1,  0, 1, },
                        { -0.5, 0, 0.5, }, };


    private double[,] filterMatrix52 = //dikey sobel
   new double[,] { { -0.5, -1, -0.5, },
                        { 0,  0, 0, },
                        { 0.5, 1, 0.5, }, };

    private double[,] filterMatrix6 = //Kernel
   new double[,] { { 1, 0, 1, },
                        { 0,  1, 0, },
                        { 1, 0, 1, }, };

    private double[,] filterKernel0 = //Kernel0
   new double[,] { { -1, -1, 1, },
                        { 0,  1, -1, },
                        { 0, 1, 1, }, };
    private double[,] filterKernel1 = //Kernel1
   new double[,] { { 1, 0, 0, },
                        { 1,  -1, -1, },
                        { 1, 0, -1, }, };
    private double[,] filterKernel2 = //Kernel2
           new double[,] { { 0, 1, 1, },
                        { 0,  1, 0, },
                        {1, -1, 1, }, };

    private double[,] filterMatrix5x =
    new double[,] { { 0, 0, 0, 0, 0, },
                        { 0,  0,  5,  0, 0, },
                        { 0,  -1,  5,  -1,  0, },
                        { 0,  0,  1,  0, 0, },
                        { 0, 0, 0, 0, 0, }, };


}

/*
 * List<double> dataLearnList = new List<double>(output5);
        TeachingData learningData = new TeachingData()
        {
            Id = Microphones.ID_Device,
            publickey = Microphones.StringPublickey,
            Data = dataLearnList,
            Teach = 0,
            First_Shot = fshot
        };

        string jsonDatalearning = JsonUtility.ToJson(learningData);
        //byte[] myData = Encoding.UTF8.GetBytes(jsonDatalearning);

        float[] SimilarPercent = new float[fshot];
        FloatArrayWrapper wrapper2;
        StartCoroutine(SendLearningData(jsonDatalearning));
        //Coroutine tamamlanana kadar bekleyin
        while (!isCoroutineFinished)
        {
            yield return null;
            //Thread.Sleep(150);
            if (Data_Event_text.text != "Data")
            {
                wrapper2 = JsonUtility.FromJson<FloatArrayWrapper>(Data_Event_text.text);
                SimilarPercent = wrapper2.array;
                isCoroutineFinished = true;
                Data_Event_text.text = "Data";
            }
        }
        isCoroutineFinished = false;
*/
/*
 * List<double> dataList = new List<double>(output5);

        //Metacone_lib.Training_Fonk(output5, Train_Out - 1);
        TeachingData trainingData = new TeachingData()
        {
            Id = Microphones.ID_Device,
            publickey = Microphones.StringPublickey,
            Data = dataList,//new List<double>() { 5, 6.0, 3.8, 4.2, 5.1, 2.0, 3.8, 4.2, 5.1, 2.0, 3.8, 4.2, 5.1, 2.0, 3.8, 4.2, 5.1, 2.0, 3.8, 4.2, 5.1, 2.0, 3.8, 4.2, 5.1 },
            Teach = 1,
            Outp = Microphones.CurrentPercentNo,//Train_Out - 1,
            First_Shot = fshot
        };

        string jsonData = JsonUtility.ToJson(trainingData);
        StartCoroutine(SendTrainingData(jsonData));
*/
/*
 IEnumerator CoroutineWrapper(string jsonData)
 {
     yield return StartCoroutine(SendLearningData(jsonData));
     isCoroutineFinished = true; //Coroutine tamamlandı, bayrağı güncelle
 }
 */
/*
private IEnumerator SendTrainingData(string jsonData)
{
    var apiUrl = "http://metcam.c8d3b8dufwgghne0.westeurope.azurecontainer.io/Teaching";
    //var apiUrl = "http://localhost:5264/Teaching";//98.64.10.166
    //var apiUrl = "http://192.168.1.2:5264/Teaching";//98.64.10.166
    using (var request = new UnityWebRequest(apiUrl, "POST"))
    {
        request.SetRequestHeader("Accept-Encoding", "gzip, deflate");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        // İstek gönder
        yield return request.SendWebRequest();

    }
}

IEnumerator SendLearningData(string jsonData)
{
    //var apiUrl = "http://metcam.c8d3b8dufwgghne0.westeurope.azurecontainer.io/Teaching";
    //var apiUrl = "http://localhost:5264/Teaching";
    var apiUrl = "http://192.168.1.2:5264/Teaching";
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
        }
        else
        {
            //UnityEngine.Debug.Log("Form upload complete!");

            string responseJson = www.downloadHandler.text;
            //UnityEngine.Debug.Log("Response JSON: " + responseJson);

            responseJson = "{\"array\":" + responseJson + "}";

            // Convert the JSON response to a float[] array using JsonUtility
            FloatArrayWrapper wrapper = JsonUtility.FromJson<FloatArrayWrapper>(responseJson);

            Data_Event_text.SetText(responseJson);

        }
    } // using bloğunun sonun
}*/

/*private static unsafe byte[] Color32ArrayToByteArrayunsafe(Color32[] colors)
{
    if (colors == null || colors.Length == 0)
        return null;

    int length = 4 * colors.Length;
    byte[] bytes = new byte[length];

    fixed (Color32* srcPtr = colors)
    fixed (byte* dstPtr = bytes)
    {
        Buffer.MemoryCopy(srcPtr, dstPtr, length, length);
    }

    return bytes;
}*/