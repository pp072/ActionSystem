using System;
using Cysharp.Threading.Tasks;
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
    [Serializable, ActionMenuPathAttribute("Timeline"), ActionName("TimelineControl")]
    public class ActionTimeline : IActionItem
    {
        [HideInInspector]public string Name { get; set; } = "TimelineControl";
        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private TimelineControlType _controlType;
        public void Validate(int index) { }
        public void Init(){}

        public async UniTask<bool> Run()
        {
            switch (_controlType)
            {
                case TimelineControlType.Play:
                    _playableDirector.Play();
                    break;
                case TimelineControlType.Pause:
                    _playableDirector.Pause();
                    break;
                case TimelineControlType.Stop:
                    _playableDirector.Stop();
                    break;
                case TimelineControlType.Resume:
                    _playableDirector.Resume();
                    break;
            }

            return true;
        }
    }
}