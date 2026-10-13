using System;
using AllinDefense.Presentation;

namespace AllinDefense.UI
{
    public static class UiSounds
    {
        public static event Action<PresentationCue> Requested;
        public static Action<PresentationCue> Playback { get; set; }
        public static void Play(PresentationCue cue)
        {
            Requested?.Invoke(cue);
            if (Playback != null) Playback(cue);
            else UnityEngine.Object.FindFirstObjectByType<DefenderAudioController>()?.Play(cue);
        }
        public static void Click() => Play(PresentationCue.UiClick);
    }
}
