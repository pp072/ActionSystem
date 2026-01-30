using System;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Playables;

namespace ActionSystem
{
    public enum TimelineControlType
    {
        Play,
        Pause,
        Stop,
        Resume
    }

    [Serializable, ActionMenuPath("Animation/Timeline Control")]
    public class ActionTimeline : ActionItemBase
    {
        [SerializeField] private ComponentRef<PlayableDirector> _playableDirector;
        [SerializeField] private TimelineControlType _controlType;
        [SerializeField] private bool _setStartTime;
        [SerializeField, ShowIf(nameof(_setStartTime)), AllowNesting]
        private FloatRef _startTime;
        [SerializeField] private bool _waitForFinish;

        public override async UniTask<bool> Run()
        {
            var director = _playableDirector.GetValue(Context);
            if (director == null) return true;

            switch (_controlType)
            {
                case TimelineControlType.Play:
                    if (_setStartTime)
                        director.time = _startTime.GetValue(Context);
                    director.Play();
                    if (_waitForFinish)
                        await UniTask.WaitUntil(() => director.state != PlayState.Playing);
                    break;
                case TimelineControlType.Pause:
                    director.Pause();
                    break;
                case TimelineControlType.Stop:
                    director.Stop();
                    break;
                case TimelineControlType.Resume:
                    director.Resume();
                    if (_waitForFinish)
                        await UniTask.WaitUntil(() => director.state != PlayState.Playing);
                    break;
            }
            return true;
        }
    }
}
