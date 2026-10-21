using System;
using UnityEngine;

namespace RogueLikeEngine.Audio
{
    /// <summary>
    /// Start, loop and end parts, each optional, such as a gun's spin-up, firing loop and tail. The start part flows
    /// gaplessly into the loop (or the end when there is no loop). Releasing the handle fades out whatever plays and
    /// starts the end part.
    /// </summary>
    [Serializable]
    public class SoundSequence : SoundBase<SoundSequencePlayback>
    {
        [SerializeField] private SoundClips m_start = new();
        [SerializeField] private SoundClips m_loop = new();
        [SerializeField] private SoundClips m_end = new();

        internal SoundClips StartPart => m_start;
        internal SoundClips LoopPart => m_loop;
        internal SoundClips EndPart => m_end;

        protected override bool HasClips => m_start.HasClips || m_loop.HasClips || m_end.HasClips;

        protected override SoundSequencePlayback CreateNewPlayback() => new(this);
    }
}
