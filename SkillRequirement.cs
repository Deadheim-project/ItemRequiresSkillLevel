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

        public static void GenerateListWithAllEquipments()
        {
            if (!File.Exists(ItemRequiresSkillLevel.AllItemsConfigPath))
            {
                List<SkillRequirement> initials = new();

                foreach (var item in ObjectDB.instance.m_items)
                {
                    ItemDrop itemDrop = item.GetComponent<ItemDrop>();
                    if (!itemDrop) continue;

                    if (itemDrop.m_itemData is null) continue;

                    if (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool || itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon || (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon || itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow) || (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield || itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet || (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest || itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)) || (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder || itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch) || itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
                    {
                        initials.Add(new SkillRequirement
                        {
                            PrefabName = item.name,
                            Skill = "Level",
                            Level = 10
                        });
                    }
                }    

                var serializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

                var yaml = serializer.Serialize(initials);

                using StreamWriter streamWriter = File.CreateText(ItemRequiresSkillLevel.AllItemsConfigPath);
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
