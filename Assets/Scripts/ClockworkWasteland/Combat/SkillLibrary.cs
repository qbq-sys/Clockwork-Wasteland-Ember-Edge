using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ClockworkWasteland.Combat
{
    public sealed class SkillLibrary : MonoBehaviour
    {
        private static SkillLibrary instance;
        private readonly Dictionary<string, SkillData> skillsById = new Dictionary<string, SkillData>();

        public static SkillLibrary Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindObjectOfType<SkillLibrary>();
                    instance = existing != null ? existing : new GameObject("Skill Library").AddComponent<SkillLibrary>();
                }

                return instance;
            }
        }

        public IReadOnlyList<SkillData> AllSkills { get; private set; } = new SkillData[0];

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        public void LoadAll()
        {
            AllSkills = Resources.LoadAll<SkillData>("Skills")
                .Where(skill => skill != null)
                .OrderBy(skill => skill.skillId)
                .ToArray();

            skillsById.Clear();
            foreach (var skill in AllSkills)
            {
                if (!string.IsNullOrWhiteSpace(skill.skillId))
                {
                    skillsById[skill.skillId] = skill;
                }
            }
        }

        public SkillData Get(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return null;
            }

            if (skillsById.Count == 0)
            {
                LoadAll();
            }

            return skillsById.TryGetValue(skillId, out var skill) ? skill : null;
        }
    }
}
