using UnityEngine;

namespace NutHeist.Audio
{
    /// <summary>Singleton façade for locomotion-centric audio cues.</summary>
    public sealed class SoundManager : MonoBehaviour
    {
        static SoundManager _instance;

        [SerializeField] AudioSource sfx;
        [SerializeField] AudioSource ambience;
        [SerializeField] AudioSource music;

        public AudioClip[] footstepDirtPool;
        public AudioClip landingSoft;
        public AudioClip landingHard;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            sfx ??= gameObject.AddComponent<AudioSource>();
            sfx.loop = false;
            sfx.playOnAwake = false;
        }

        public static SoundManager Resolve()
        {
            if (!_instance)
            {
                SoundManager bootstrap = FindFirstObjectByType<SoundManager>();
                if (bootstrap)
                {
                    _instance = bootstrap;
                }
                else
                {
                    GameObject nm = new GameObject("NutHeist_SoundSystem");
                    _instance = nm.AddComponent<SoundManager>();
                    DontDestroyOnLoad(nm);
                }
            }

            return _instance;
        }

        public void PlayLandingHeavy(float volume = 1f)
        {
            PlayOneShot(landingHard ?? landingSoft, volume);
        }

        void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (!clip || !sfx)
            {
                return;
            }

            sfx.pitch = 1f + Random.Range(-0.05f, 0.05f);
            sfx.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        /// <summary>Stub surface router — extend with physic material lookups.</summary>
        public void ReportLandSurface(Collider surface, Vector3 _)
        {
            if (!landingSoft)
            {
                return;
            }

            PlayOneShot(landingSoft, 0.35f);
        }
    }
}
