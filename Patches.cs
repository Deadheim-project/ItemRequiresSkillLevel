using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemRequiresSkillLevel
{
    [HarmonyPatch]
    class Patches
    {
        [HarmonyPatch]
        class ItemDropItemData
        {
            static List<string> ValheimLevelSystemList = new List<string> { "Intelligence", "Strength", "Focus", "Constitution", "Agility", "Level" };

            [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip), new Type[] { })]
            [HarmonyPostfix]
            private static void GetToolTip(ItemDrop.ItemData __instance, ref string __result)
            {
                __result += GetText(__instance.m_dropPrefab.name);
            }

            [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
            [HarmonyPostfix]
            private static void IsEquipable(ItemDrop.ItemData __instance, ref bool __result)
            {
                SkillRequirement requirement = Requirements.list.FirstOrDefault(x => __instance.m_dropPrefab.name.Contains(x.PrefabName));
                if (requirement is null) return;

                if (ValheimLevelSystemList.Contains(requirement.Skill))
                {
                    int level = GetSkillLevelVLS(requirement.Skill);
                    if (level == 0) return;

                    if (level < requirement.Level) __result = false;
                }

                else if (Player.m_localPlayer.GetSkills().GetSkill((Skills.SkillType)Enum.Parse(typeof(Skills.SkillType), requirement.Skill)).m_level < requirement.Level) __result = false;
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

                __instance.m_recipeDecription.text += GetText(__instance.m_selectedRecipe.Key.m_item.gameObject.name);
            }
        }

        public static string GetText(string prefabName)
        {
            SkillRequirement requirement = Requirements.list.FirstOrDefault(x => prefabName.Contains(x.PrefabName));
            if (requirement is null) return "";

            return Localization.instance.Localize($"\n\nRequires <color=Red>{requirement.Skill} {requirement.Level}</color>");
        }
    }
}
