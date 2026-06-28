using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 31.03.2020 - first
 17.02.2021 - add volumeTouchSlider, musicVolumeTouchSlider,  PSlider musicSlider
 */
namespace Mkey
{
	public class VolumeController : MonoBehaviour
	{
        [SerializeField]
        private PSlider volumeSlider;
        [SerializeField]
        private PSlider musicSlider;

        [SerializeField]
        private Slider volumeTouchSlider;

        [SerializeField]
        private Slider musicVolumeTouchSlider;

        #region temp vars
        private SoundMaster MSound { get { return SoundMaster.Instance; } }
        #endregion temp vars
		
		#region regular
		private IEnumerator Start()
		{
            while (!MSound) yield return new WaitForEndOfFrame();

            if (volumeSlider) volumeSlider.SetFillAmount(MSound.Volume);

            if (volumeTouchSlider) 
            { 
                volumeTouchSlider.value = MSound.Volume; 
            }
            if (musicVolumeTouchSlider)
            {
                musicVolumeTouchSlider.value = MSound.VolumeMusic; 
            }
          //  MSound.ChangeVolumeEvent += ChangeVolumeEventHandler;
		}

        private void OnDestroy()
        {
            if(MSound) MSound.ChangeVolumeEvent -= ChangeVolumeEventHandler;
        }
        #endregion regular

        public void VolumePlusButton_Click()
        {
            MSound.SetVolume(MSound.Volume + 0.1f);
        }

        public void VolumeMinusButton_Click()
        {
            MSound.SetVolume(MSound.Volume - 0.1f);
        }

        public void SetVolume(float volume)
        {
            MSound.SetVolume(volume);
        }

        public void SetMusicVolume(float volume)
        {
            MSound.SetVolumeMusic(volume);
        }

        private void ChangeVolumeEventHandler(float volume)
        {
            if (volumeSlider) volumeSlider.SetFillAmount(volume);
        }
    }
}
