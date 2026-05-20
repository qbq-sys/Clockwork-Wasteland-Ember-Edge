using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClockworkWasteland.Combat
{
    public sealed class CombatAudio : MonoBehaviour
    {
        private static CombatAudio instance;

        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private AudioSource sfxSource;
        private AudioSource musicSource;

        public static CombatAudio Instance => instance != null ? instance : Ensure();

        public static CombatAudio Ensure()
        {
            if (instance != null)
            {
                return instance;
            }

            var audioObject = new GameObject("Combat Audio");
            DontDestroyOnLoad(audioObject);
            instance = audioObject.AddComponent<CombatAudio>();
            instance.Build();
            return instance;
        }

        public void PlayUiClick()
        {
            PlayOneShot("UI\u70b9\u51fb\u97f3\u65481", 0.55f);
        }

        public void PlayStartExpedition()
        {
            PlayOneShot("\u5f00\u59cb\u4efb\u52a1\u70b9\u51fb\u97f3\u6548", 0.7f);
        }

        public void PlayAttack(SkillData skill)
        {
            if (skill != null && IsGunLikeSkill(skill))
            {
                PlayOneShot("\u67aa\u51fb\u97f3\u65481", 0.75f);
                return;
            }

            var slashClips = new[] { "\u5200\u780d\u97f3\u65481", "\u5200\u780d\u97f3\u65482", "\u5200\u780d\u97f3\u65483" }
                .Where(HasClip)
                .ToArray();
            if (slashClips.Length > 0)
            {
                PlayOneShot(slashClips[Random.Range(0, slashClips.Length)], 0.72f);
                return;
            }

            PlayOneShot("\u6280\u80fd\u6253\u51fb\u97f3\u65481", 0.72f);
        }

        public void PlayImpact()
        {
            var impactClips = new[] { "\u6280\u80fd\u6253\u51fb\u97f3\u65481", "\u901a\u7528\u6253\u51fb\u97f3\u65481" }
                .Where(HasClip)
                .ToArray();
            if (impactClips.Length > 0)
            {
                PlayOneShot(impactClips[Random.Range(0, impactClips.Length)], 0.7f);
            }
        }

        public void PlayHeal()
        {
            PlayOneShot("\u56de\u8840\u6cbb\u7597\u97f3\u65481", 0.75f);
        }

        public void PlayBossMusic()
        {
            var clip = GetClip("boss\u6218\u80cc\u666f\u97f3\u4e501");
            if (clip == null || musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = 0.38f;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }

        private void Build()
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.9f;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicSource.volume = 0.38f;

            LoadClips();
        }

        private void PlayOneShot(string clipName, float volume)
        {
            var clip = GetClip(clipName);
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        private bool HasClip(string clipName)
        {
            return GetClip(clipName) != null;
        }

        private AudioClip GetClip(string clipName)
        {
            if (clips.TryGetValue(clipName, out var clip))
            {
                return clip;
            }

            return null;
        }

        private static bool IsGunLikeSkill(SkillData skill)
        {
            var id = (skill.skillId ?? string.Empty).ToLowerInvariant();
            return id.Contains("volley") || id.Contains("storm") || id.Contains("shot") || id.Contains("gun");
        }

        private void LoadClips()
        {
#if UNITY_EDITOR
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip != null && !clips.ContainsKey(clip.name))
                {
                    clips.Add(clip.name, clip);
                }
            }
#endif
        }
    }
}
