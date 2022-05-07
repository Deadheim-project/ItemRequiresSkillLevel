using System.Collections.Generic;
using YamlDotNet.Serialization;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine;
using YamlDotNet.Serialization.NamingConventions;

namespace ItemRequiresSkillLevel
{
    public class SkillRequirement
    {
        public string PrefabName { get; set; }
        public string Skill { get; set; }
        public int Level { get; set; }

        public static List<SkillRequirement> Parse(string yaml) => new DeserializerBuilder().IgnoreFields().Build().Deserialize<List<SkillRequirement>>(yaml);
    }

    public class Requirements
    {
        public static List<SkillRequirement> list = new();

        public static void Init()
        {
            if (!File.Exists(ItemRequiresSkillLevel.ConfigPath))
            {
                List<SkillRequirement> initials = new();
                initials.Add(new SkillRequirement
                {
                    PrefabName = "ArmorBronzeLegs",
                    Skill = "Blocking",
                    Level = 10
                });
                initials.Add(new SkillRequirement
                {
                    PrefabName = "ArmorBronzeChest",
                    Skill = "Blocking",
                    Level = 10
                });
                initials.Add(new SkillRequirement
                {
                    PrefabName = "HelmetBronze",
                    Skill = "Blocking",
                    Level = 10
                });

                var serializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

                var yaml = serializer.Serialize(initials);

                using StreamWriter streamWriter = File.CreateText(ItemRequiresSkillLevel.ConfigPath);
                streamWriter.Write(new StringBuilder()
                        .AppendLine(yaml));
                streamWriter.Close();
            }
        }

        public static void Load()
        {
            list.Clear();

            foreach (KeyValuePair<string, string> yamlFile in ItemRequiresSkillLevel.YamlData.Value)
            {
                list.AddRange(SkillRequirement.Parse(yamlFile.Value));
            }
            Debug.Log("ItemRequiresSkillLevel Loaded: " + list.Count());
        }
    }
}
