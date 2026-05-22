using UnityEngine;

namespace XRMultiplayer
{
    // Generates procedural phasing white noise — two amplitude-modulated noise streams
    // with slightly different LFO rates, producing a slow beating/phasing effect.
    // Attach to any GameObject; AudioSource is added automatically.
    [RequireComponent(typeof(AudioSource))]
    public class PhasingWhiteNoise : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float m_Volume = 0.8f;
        [SerializeField] float m_LfoRate1 = 0.20f;  // Hz
        [SerializeField] float m_LfoRate2 = 0.23f;  // Hz — ~33s beat period creates the phasing

        float m_Phase1;
        float m_Phase2;
        int m_SampleRate;

        void Awake()
        {
            m_SampleRate = AudioSettings.outputSampleRate;
            var src = GetComponent<AudioSource>();
            src.spatialBlend = 0f; // 2D — ambient, not positional
            src.Play();
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float delta1 = m_LfoRate1 * 2f * Mathf.PI / m_SampleRate;
            float delta2 = m_LfoRate2 * 2f * Mathf.PI / m_SampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                float mod = ((Mathf.Sin(m_Phase1) + 1f) + (Mathf.Sin(m_Phase2) + 1f)) * 0.25f; // 0..1
                float sample = UnityEngine.Random.Range(-1f, 1f) * m_Volume * mod;

                for (int c = 0; c < channels; c++)
                    data[i + c] = sample;

                m_Phase1 += delta1;
                m_Phase2 += delta2;
            }
        }
    }
}
