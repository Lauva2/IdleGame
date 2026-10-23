using System.Collections.Generic;
using ForestShop.Core;
using UnityEngine;

namespace ForestShop
{
    public sealed class ForestAudio : MonoBehaviour
    {
        AudioSource music, ambience, effects;
        bool soundEnabled = true;
        readonly Dictionary<string, float> last = new Dictionary<string, float>();
        void Awake()
        {
            music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = .33f; music.playOnAwake = false;
            music.clip = Resources.Load<AudioClip>("Audio/woodland");
            ambience = gameObject.AddComponent<AudioSource>(); ambience.loop = true; ambience.volume = .6f; ambience.playOnAwake = false;
            ambience.clip = Resources.Load<AudioClip>("Audio/ambience");
            effects = gameObject.AddComponent<AudioSource>(); effects.volume = .45f; effects.playOnAwake = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            StartCoroutine(MeasureOutput());
#endif
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        System.Collections.IEnumerator MeasureOutput()
        {
            var samples = new float[1024];
            for (int pass = 0; pass < 5; pass++)
            {
                yield return new WaitForSecondsRealtime(2);
                AudioListener.GetOutputData(samples, 0);
                double sum = 0; float peak = 0;
                foreach (float sample in samples) { sum += sample * sample; peak = Mathf.Max(peak, Mathf.Abs(sample)); }
                Debug.Log("FOREST_AUDIO_OUTPUT rms=" + System.Math.Sqrt(sum / samples.Length).ToString("F8", System.Globalization.CultureInfo.InvariantCulture)
                    + " peak=" + peak.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)
                    + " listener=" + AudioListener.volume + " pause=" + AudioListener.pause
                    + " music=" + music.isPlaying + " sourceVolume=" + music.volume + " spatial=" + music.spatialBlend
                    + " clip=" + (music.clip == null ? "missing" : music.clip.loadState + "/" + music.clip.samples + "/" + music.clip.channels));
            }
        }
#endif
        public void Apply(GameState state)
        {
            soundEnabled = state.sound;
            if (!state.sound) effects.Stop();
            if (state.sound && ambience.clip != null) { if (!ambience.isPlaying) ambience.Play(); }
            else ambience.Stop();
            if (state.music && music.clip != null) { if (!music.isPlaying) music.Play(); }
            else music.Stop();
        }
        public void Cue(string key)
        {
            if (!soundEnabled) return;
            float interval = key == "sale" || key == "harvest" ? 2 : .12f;
            if (last.TryGetValue(key, out float time) && Time.unscaledTime - time < interval) return;
            last[key] = Time.unscaledTime;
            var clip = Resources.Load<AudioClip>("Audio/" + key); if (clip != null) effects.PlayOneShot(clip);
        }
    }
}
