using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using ARFps.Core.Services;
using ARFps.Core.Events;
using ARFps.Features.PlayerInput.Events;

namespace ARFps.Features.PlayerInput
{
    public class MicrophoneInputService : IService, ITickable
    {
        private AudioClip _micClip;
        private string _device;
        private bool _isInitialized = false;
        private bool _warningLogged = false;
        
        private const int SampleWindow = 128;
        private const float BlowThreshold = 0.1f; // The volume required to trigger a blow

        public void OnInit()
        {
#if UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                Debug.Log("[MicrophoneInputService] Requesting Android Microphone Permission...");
            }
#endif
            TryInitializeMicrophone();
        }
 
        private void TryInitializeMicrophone()
        {
            // Microphone.devices will be empty until the user clicks "Allow" on the Android popup!

            
            if (Microphone.devices.Length > 0)
            {
                _device = Microphone.devices[0];
                _micClip = Microphone.Start(_device, true, 1, 44100);
                _isInitialized = true;
                Debug.Log($"[MicrophoneInputService] Started listening on {_device}");
            }
            else
            {
                if (!_warningLogged)
                {
                    Debug.LogWarning("[MicrophoneInputService] No microphone detected! Waiting for permission or device...");
                    _warningLogged = true;
                }
            }
        }

        public void OnTick()
        {
            // If we don't have permission yet, keep checking until the user grants it!
            if (!_isInitialized)
            {
                TryInitializeMicrophone();
                return;
            }

            if (_micClip == null) return;

            int micPosition = Microphone.GetPosition(_device) - (SampleWindow + 1);
            if (micPosition < 0) return;

            float[] waveData = new float[SampleWindow];
            _micClip.GetData(waveData, micPosition);

            float levelMax = 0;
            for (int i = 0; i < SampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak) levelMax = wavePeak;
            }
            
            float volume = Mathf.Sqrt(Mathf.Sqrt(levelMax)); // Calculate RMS volume

            if (volume > BlowThreshold)
            {
                EventBus<BlowDetectedEvent>.Publish(new BlowDetectedEvent(volume));
            }
        }

        public void OnDispose()
        {
            if (_isInitialized) Microphone.End(_device);
        }
    }
}