// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using UnityEngine;

namespace GazeLab
{
    /// <summary>
    /// Plays the synthesised sound effects (see <see cref="SoundSynth"/>) for game events. A small
    /// pool of 2D audio sources lets sounds overlap, each with a slightly varied pitch.
    /// Set up by the GameController.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameSounds : MonoBehaviour
    {
        const int Voices = 8;
        // The same target is announced again only after the gaze stayed off it this long.
        const float LockRepeatSeconds = 0.5f;

        GameSettings settings;
        AudioSource[] voices;
        int nextVoice;
        AudioClip laser, explosion, metalCrash, penalty, shipHit, lockOn, blocked, start, menu, gameOver;
        Object lastAimed;
        float lastAimedTime = float.NegativeInfinity;

        void Awake()
        {
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
            laser = SoundSynth.Laser();
            explosion = SoundSynth.Explosion();
            metalCrash = SoundSynth.MetalCrash();
            penalty = SoundSynth.Penalty();
            shipHit = SoundSynth.ShipHit();
            lockOn = SoundSynth.Lock();
            blocked = SoundSynth.Blocked();
            start = SoundSynth.Start();
            menu = SoundSynth.Menu();
            gameOver = SoundSynth.GameOver();
        }

        /// <summary>The volume is read from <see cref="GameSettings.soundVolume"/> on every sound.</summary>
        public void Init(GameSettings gameSettings) { settings = gameSettings; }

        /// <summary>Laser, then the explosion of a rock or the crash of a satellite with the penalty jingle.</summary>
        public void Shot(TargetKind kind)
        {
            Play(laser, 0.55f, 0.06f);
            if (kind == TargetKind.Rock) Play(explosion, 0.8f, 0.1f, 0.04f);
            else
            {
                Play(metalCrash, 0.7f, 0.06f, 0.04f);
                Play(penalty, 0.4f, 0f, 0.2f);
            }
        }

        public void ShotBlocked() { Play(blocked, 0.4f, 0f); }

        public void ShipHit()
        {
            Play(shipHit, 0.9f, 0.05f);
            Play(explosion, 0.5f, 0.1f, 0.02f);
        }

        public void RoundStart() { Play(start, 0.75f, 0f); }
        public void BackToMenu() { Play(menu, 0.75f, 0f); }
        public void GameOver() { Play(gameOver, 0.9f, 0f, 0.35f); }

        /// <summary>Quiet blip when the gaze settles on a new target (no repeats while the gaze jitters).</summary>
        public void TargetAimed(Object target, float now)
        {
            if (target == null) return;
            bool repeat = target == lastAimed && now - lastAimedTime < LockRepeatSeconds;
            if (!repeat) Play(lockOn, 0.3f, 0.03f);
            lastAimed = target;
            lastAimedTime = now;
        }

        /// <summary>Called every frame while the gaze stays on the target, so short jitter is no new lock.</summary>
        public void TargetStillAimed(Object target, float now)
        {
            if (target != null && target == lastAimed) lastAimedTime = now;
        }

        void Play(AudioClip clip, float volume, float pitchJitter, float delay = 0f)
        {
            float master = settings != null ? settings.soundVolume : 1f;
            if (clip == null || voices == null || master <= 0f) return;
            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.Stop();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume * master);
            voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            if (delay > 0f) voice.PlayDelayed(delay);
            else voice.Play();
        }
    }
}
