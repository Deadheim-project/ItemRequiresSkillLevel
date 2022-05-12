using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemRequiresSkillLevel
{
    [HarmonyPatch]
    class Patches
    {
        static readonly List<string> ValheimLevelSystemList = new List<string> { "Intelligence", "Strength", "Focus", "Constitution", "Agility", "Level" };

        [HarmonyPatch]
        class ItemDropItemData
        {

            [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new Type[] { })]
            [HarmonyPostfix]
            private static void GetToolTip(ItemDrop.ItemData __instance, ref string __result)
            {
                string name = __instance.m_dropPrefab.name;
                SkillRequirement requirement = Requirements.list.FirstOrDefault(x => name.Contains(x.PrefabName));
                if (requirement is null) return;
                __result += GetText(requirement);
            }

            [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
            [HarmonyPostfix]
            private static void IsEquipable(ItemDrop.ItemData __instance, ref bool __result)
            {
                SkillRequirement requirement = Requirements.list.FirstOrDefault(x => __instance.m_dropPrefab.name.Contains(x.PrefabName));
                if (requirement is null) return;

                __result = IsAble(requirement);  
            }
        }

        public static int GetSkillLevelVLS(string skill)
        {
            if (Player.m_localPlayer.m_knownTexts.ContainsKey("player" + skill))
            {
                return Convert.ToInt32(Player.m_localPlayer.m_knownTexts["player" + skill]);
            }

            return 0;
        }

        [HarmonyPatch]
        class UpdateRecipeText
        {
            [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
            [HarmonyPostfix]
            internal static void UpdateRecipe_Post(ref InventoryGui __instance, Player player)
            {
                string name = __instance.m_selectedRecipe.Key.m_item.gameObject.name;
                SkillRequirement requirement = Requirements.list.FirstOrDefault(x => name.Contains(x.PrefabName));
                if (requirement is null) return;

                __instance.m_recipeDecription.text += GetText(requirement);

                if (!ItemRequiresSkillLevel.BlockCraft.Value) return;

                if (!IsAble(requirement))
                {
                    __instance.m_craftButton.enabled = false;
                    __instance.m_craftButton.interactable= false;
                }
                else
                {
                    __instance.m_craftButton.enabled = true;
                    __instance.m_craftButton.interactable = true;
                }
            }
        }

        [HarmonyPatch]
        class Spawn
        {
            static bool hasSpawned = false;             
            [HarmonyPatch(typeof(Game), nameof(Game.RequestRespawn))]
            [HarmonyPostfix]
            internal static void RequestRespawn()
            {
                if (hasSpawned) return;

                if (ItemRequiresSkillLevel.GenerateListWithAllEquipableItems.Value)
                {
                    Requirements.GenerateListWithAllEquipments();
                }
                hasSpawned = true;
            }
        }

        public static bool IsAble(SkillRequirement requirement)
        {
            if (ValheimLevelSystemList.Contains(requirement.Skill))
            {
                int level = GetSkillLevelVLS(requirement.Skill);
                if (level == 0) return true;

                if (level < requirement.Level) return false;
            }

            else if (Player.m_localPlayer.GetSkills().GetSkill((Skills.SkillType)Enum.Parse(typeof(Skills.SkillType), requirement.Skill)).m_level < requirement.Level) return false;

            return true;
        }

        public static string GetText(SkillRequirement requirement)
        {
            return Localization.instance.Localize($"\n\nRequires <color=Red>{requirement.Skill} {requirement.Level}</color>");
        }
    }
}
