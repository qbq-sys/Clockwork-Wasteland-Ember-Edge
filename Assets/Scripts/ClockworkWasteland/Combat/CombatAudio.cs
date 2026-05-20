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

        private readonly List<ClipEntry> clips = new List<ClipEntry>();
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
            PlayOneShot(FindClip("\u70b9\u51fb", "UI"), 0.55f);
        }

        public void PlayStartExpedition()
        {
            PlayOneShot(FindClip("\u5f00\u59cb\u4efb\u52a1"), 0.8f);
        }

        public void PlayAttack(SkillData skill)
        {
            if (skill != null && IsGunLikeSkill(skill))
            {
                PlayOneShot(FindClip("\u67aa\u51fb") ?? FindAnyCombatClip(), 1f);
                return;
            }

            var slashClip = FindRandomClip("\u5200\u780d");
            if (slashClip != null)
            {
                PlayOneShot(slashClip, 1f);
                return;
            }

            PlayOneShot(FindClip("\u6280\u80fd\u6253\u51fb") ?? FindAnyCombatClip(), 1f);
        }

        public void PlayImpact()
        {
            PlayOneShot(FindRandomClip("\u6253\u51fb") ?? FindAnyCombatClip(), 1f);
        }

        public void PlayHeal()
        {
            PlayOneShot(FindClip("\u56de\u8840", "\u6cbb\u7597") ?? FindAnyCombatClip(), 1f);
        }

        public void PlayBossMusic()
        {
            var clip = FindClip("boss", "\u80cc\u666f\u97f3\u4e50");
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

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        private AudioClip FindClip(params string[] keywords)
        {
            return clips.FirstOrDefault(clip => clip.MatchesAny(keywords))?.Clip;
        }

        private AudioClip FindRandomClip(params string[] keywords)
        {
            var matches = clips
                .Where(clip => clip.MatchesAny(keywords))
                .ToArray();
            return matches.Length > 0 ? matches[Random.Range(0, matches.Length)].Clip : null;
        }

        private AudioClip FindAnyCombatClip()
        {
            return FindRandomClip("\u5200\u780d", "\u67aa\u51fb", "\u6253\u51fb", "\u6280\u80fd", "\u901a\u7528");
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
                if (clip != null && clips.All(entry => entry.Clip != clip))
                {
                    clips.Add(new ClipEntry(clip, assetPath));
                }
            }
#endif
        }

        private sealed class ClipEntry
        {
            public ClipEntry(AudioClip clip, string assetPath)
            {
                Clip = clip;
                SearchText = $"{clip.name}|{assetPath}";
            }

            public AudioClip Clip { get; }
            private string SearchText { get; }

            public bool MatchesAny(params string[] keywords)
            {
                return keywords != null && keywords.Any(keyword => !string.IsNullOrWhiteSpace(keyword) && SearchText.Contains(keyword));
            }
        }
    }
}
