using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideosPlay : MonoBehaviour
{
    public VideoPlayer[] videoPlayer; // 引用VideoPlayer组件
    public AudioSource audioSource; // 添加对AudioSource的引用
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
   
        if (Input.GetMouseButtonDown(0)) // 鼠标左键按下
        {

            for (int i=0; i<videoPlayer.Length; i++)
            {
                if (videoPlayer[i].isPlaying)
                {
                    videoPlayer[i].Pause();
                    audioSource.Pause();
                }
               
                else
                { videoPlayer[i].Play();
                    audioSource.UnPause();
                }
                
            }
          

        }
        else if(Input.GetMouseButtonDown(1)) // 鼠标右键按下
        {
            for (int i = 0; i < videoPlayer.Length; i++)
            {
                videoPlayer[i].time = 0f; // 重置播放时间到开头
                audioSource.time = 0f;
                if (!videoPlayer[i].isPlaying)
                {
                    videoPlayer[i].Play(); // 如果视频暂停，则开始播放
                    audioSource.Play();
                }

            }
             
        }
    }
}
