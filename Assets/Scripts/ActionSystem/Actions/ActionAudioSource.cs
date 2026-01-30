using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
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

        [SerializeField, ShowIf(nameof(IsVolumeMode)), AllowNesting, Range(0f, 1f)]
        private float _targetVolume = 1f;

        [SerializeField, ShowIf(nameof(IsVolumeMode)), AllowNesting, Min(0f)]
        private float _fadeDuration = 0.25f;

        [SerializeField, ShowIf(nameof(CanWaitForFinish)), AllowNesting]
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
                    var tween = audioSource.DOFade(_targetVolume, _fadeDuration);
                    if (_waitForFinish)
                        await tween.AsyncWaitForCompletion();
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
    }
}
