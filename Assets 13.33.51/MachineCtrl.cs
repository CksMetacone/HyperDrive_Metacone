using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
//using System.Text;

public class MachineCtrl : MonoBehaviour
{
    //public const string audioName = "1_1.wav";
    [Header("Audio Stuff")]
    public AudioSource audioSource;
    public AudioClip audioClip;
    public string soundPath;

    public static int Learn_Timing = 0;
    public static int Cam_Activate_Mic = 0;
    //public static int Cam_Activate_Train = 0;
    public static int Cam_Activate_Mic_Train_Out = 0;
    public static int Mic_Closed = 0;


    // Start is called before the first frame update
    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    IEnumerator GetAudioClip2(string fullPath)
    {
        //debugSongPath2.text = previewSong;
        using (UnityWebRequest unityWebRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + fullPath, AudioType.WAV))
        {
            yield return unityWebRequest.SendWebRequest();

            //if (www.isNetworkError || www.isHttpError)
            if((unityWebRequest.result == UnityWebRequest.Result.ConnectionError)|| (unityWebRequest.result == UnityWebRequest.Result.ProtocolError))
            {
                Debug.Log(unityWebRequest.error);
            }
            else
            {
                AudioClip myClip = DownloadHandlerAudioClip.GetContent(unityWebRequest);
                audioSource.clip = myClip;
                audioSource.Play();
            }
        }
    }

    public static int Have_a_Sound = 0;
    int Have_a_Sound_count = 0;
    public static int Have_a_Moving = 0;
    int Have_a_Moving_count = 0;
    public static int Have_a_Sound_not_Moving = 0;


    float countdownTo2 = 10.0F;
    float countdownTo3 = 2.0F;
    int Solved = 0;

    void FixedUpdate()
    {
        if ((Have_a_Sound == 1) && (Have_a_Sound_count < 100))
        {
            Have_a_Sound_count++;//20ms *100 = 2ms
        }
        else
        {
            if (Have_a_Sound_count > 99)
                Have_a_Sound_not_Moving = 1;//2 sn içinde hareket olmadığı için learning yap
            Have_a_Sound = 0;
            Have_a_Sound_count = 0;
        }
        if ((Have_a_Moving == 1) && (Have_a_Moving_count < 100))
        {
            Have_a_Moving_count++;//20ms *100 = 2ms
        }
        else
        {
            Have_a_Moving = 0;
            Have_a_Moving_count = 0;

        }


        if (Solved == 1)
            countdownTo2 -= Time.deltaTime;
        if ((countdownTo2 <= 0)) //5sn de bir ancak tekrarlayabilir.
        {
            Solved = 0;
            countdownTo2 = 10.0f;
            CamSc.Moving_start = 0;
        }
        if(Mic_Closed==1)
        {
            countdownTo3 -= Time.deltaTime;
        }
        if ((countdownTo3 <= 0)) //5sn de bir ancak tekrarlayabilir.
        {
            Mic_Closed = 0;
            countdownTo3 = 2.0f;
        }

        if ((CamSc.Percantage_Cam >0.035) && (Solved == 0)&&(CamSc.Moving_start ==1))
        {//Kamerada tanınmış veri var.
            CamSc.Moving_start = 0;
            //CamSc.Out_Cam = 0;
            Solved = 1;
            //if(CamSc.Out_Cam==1)
            soundPath = Path.Combine("/Users/metacone/Desktop/Sesrecord/", CamSc.Out_Cam.ToString()+ ".wav");
            //else if (CamSc.Out_Cam == 2)
            //    soundPath = Path.Combine("/Users/metacone/Desktop/Sesrecord/", "2_2.wav");
            //else if (CamSc.Out_Cam == 3)
            //    soundPath = Path.Combine("/Users/metacone/Desktop/Sesrecord/", "3_3.wav");
            //else if (CamSc.Out_Cam == 4)
            //   soundPath = Path.Combine("/Users/metacone/Desktop/Sesrecord/", "4_4.wav");

#if UNITY_IOS
            if (Application.platform == RuntimePlatform.IPhonePlayer)
                //if (CamSc.Out_Cam == 1)
                   soundPath = Path.Combine(Application.persistentDataPath + CamSc.Out_Cam.ToString(), ".wav");
                //else if (CamSc.Out_Cam == 2)
                //   soundPath = Path.Combine(Application.persistentDataPath, "2_2.wav");
                //else if (CamSc.Out_Cam == 3)
                //    soundPath = Path.Combine(Application.persistentDataPath, "3_3.wav");
                //else if (CamSc.Out_Cam == 4)
                //  soundPath = Path.Combine(Application.persistentDataPath, "4_4.wav");
#endif
#if !UNITY_IOS
            soundPath = Path.Combine("/Users/metacone/Desktop/Sesrecord/", CamSc.Out_Cam.ToString()+ ".wav");

#endif

                UnityEngine.Debug.Log("soundPath " + soundPath);
            Mic_Closed = 1;
            StartCoroutine(GetAudioClip2(soundPath));


            UnityEngine.Debug.Log("Have a Detected " + CamSc.Out_Cam.ToString());

        }

    }

}
