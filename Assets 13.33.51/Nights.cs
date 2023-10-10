using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class Nights : MonoBehaviour
{ 
    public Light Gunes;
    public float BirTamGun;
    public float GeceGunduzDengesi;
    public float GunesIsinlarininAcisi;
    public float GunDonumuNoktasi;
    public float IsiginParlaklıkSeviyesi;
    int saat;
    DateTime Zaman;

    public Animator animasyon;

    // Start is called before the first frame update
    void Start()
    {
        Zaman = DateTime.Now;
        saat = Zaman.Hour;
        BirTamGun = 24f;
        GeceGunduzDengesi = 0;
        GunesIsinlarininAcisi = 100f;
        GunDonumuNoktasi = 200f;
        IsiginParlaklıkSeviyesi = Gunes.intensity;
    }
    // Update is called once per frame
    public TextMeshProUGUI fpsText;
    private float deltaTime;


    // Update is called once per frame
    void Update()
    {

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = "FPS: " + Mathf.Ceil(fps).ToString();


        GeceGunduzDengesi = (saat / BirTamGun) * 1;
        Gunes.transform.localRotation = Quaternion.Euler((GeceGunduzDengesi * 360f) - GunesIsinlarininAcisi, GunDonumuNoktasi, 0);
        float IsiginParlaklıkYogunlugu = 1;


        if (GeceGunduzDengesi <= 0.23f || GeceGunduzDengesi >= 0.75f)
        {
            IsiginParlaklıkYogunlugu = 0;
            //IsiginParlaklıkYogunlugu = Mathf.Clamp01((GeceGunduzDengesi - 0.25f) * (1 / 0.02f));
        }
        else if (GeceGunduzDengesi <= 0.25f)
        {
            IsiginParlaklıkYogunlugu = Mathf.Clamp01((GeceGunduzDengesi - 0.25f) * (1 / 0.02f));
        }
        else if (GeceGunduzDengesi >= 0.73f)
        {
            IsiginParlaklıkYogunlugu = Mathf.Clamp01(1 - ((GeceGunduzDengesi - 0.73f) * (1 / 0.02f)));
        }

        if (GeceGunduzDengesi >= 1)
        {
            GeceGunduzDengesi = 0;
        }
        Gunes.intensity = IsiginParlaklıkSeviyesi * IsiginParlaklıkYogunlugu;

        


        if (Input.GetKeyDown(KeyCode.Q))
        {
            animasyon.SetTrigger("HappyTrigger");
            
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            animasyon.SetTrigger("BadTrigger");

        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            animasyon.SetTrigger("SleepTrigger");

        }

    }
}
