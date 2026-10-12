using AllinDefense.Data;
using UnityEngine;

namespace AllinDefense.Presentation
{
    public readonly struct PresentationSignal
    {
        public readonly PresentationCue cue;
        public readonly Vector3 position;
        public readonly GameObject source;
        public readonly PresentationActorKind actor;
        public readonly OutpostKind outpost;
        public readonly int value;

        public PresentationSignal(PresentationCue cue, Vector3 position, GameObject source = null,
            PresentationActorKind actor = PresentationActorKind.Raider,
            OutpostKind outpost = OutpostKind.Archer, int value = 0)
        {
            this.cue = cue;
            this.position = position;
            this.source = source;
            this.actor = actor;
            this.outpost = outpost;
            this.value = value;
        }
    }
}
