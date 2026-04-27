using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ActionSystem
{
    public enum AudioControlType
    {
        Play,
        Pause,
        Stop,
        Resume,
        Volume,
        Restart
    }

    [Serializable, ActionMenuPath("Audio/Audio Source")]
    public class ActionAudioSource : ActionItemBase
    {
        [SerializeField] private ComponentRef<AudioSource> _audioSource;
        [SerializeField] private AudioControlType _controlType;

        [SerializeField, ShowIf(nameof(IsVolumeMode)), Range(0f, 1f)]
        private float _targetVolume = 1f;

        [SerializeField, ShowIf(nameof(IsVolumeMode)), Min(0f)]
        private float _fadeDuration = 0.25f;

        [SerializeField, ShowIf(nameof(CanWaitForFinish))]
        private bool _waitForFinish;

        private bool IsVolumeMode => _controlType == AudioControlType.Volume;
        private bool CanWaitForFinish => _controlType == AudioControlType.Play || _controlType == AudioControlType.Restart || _controlType == AudioControlType.Volume;

        public override async UniTask<bool> Run()
        {
            var audioSource = _audioSource.GetValue(Context);
            if (audioSource == null) return true;

            switch (_controlType)
            {
                case AudioControlType.Play:
                    audioSource.Play();
                    if (_waitForFinish)
                        await UniTask.WaitUntil(() => !audioSource.isPlaying);
                    break;

                case AudioControlType.Pause:
                    audioSource.Pause();
                    break;

                case AudioControlType.Stop:
                    audioSource.Stop();
                    break;

                case AudioControlType.Resume:
                    audioSource.UnPause();
                    break;

                case AudioControlType.Volume:
                    var fadeTask = FadeVolume(audioSource, _targetVolume, _fadeDuration);
                    if (_waitForFinish)
                        await fadeTask;
                    break;

                case AudioControlType.Restart:
                    audioSource.time = 0f;
                    audioSource.Play();
                    if (_waitForFinish)
                        await UniTask.WaitUntil(() => !audioSource.isPlaying);
                    break;
            }

            return true;
        }

        private static async UniTask FadeVolume(AudioSource source, float target, float duration)
        {
            float start = source.volume;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield();
            }
            source.volume = target;
        }
    }
}
