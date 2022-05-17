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
                SkillRequirement requirement = RequirementService.list.FirstOrDefault(x => __instance.m_dropPrefab.name.GetStableHashCode() == x.StableHashCode);
                if (requirement is null) return;
                __result += GetTextEquip(requirement);
            }

            [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
            [HarmonyPostfix]
            private static void IsEquipable(ItemDrop.ItemData __instance, ref bool __result)
            {
                SkillRequirement requirement = RequirementService.list.FirstOrDefault(x => __instance.m_dropPrefab.name.GetStableHashCode() == x.StableHashCode);
                if (requirement is null) return;

                bool result = requirement.Requirements.Where(x => x.BlockEquip).ToList().Any(x => !IsAble(x)) ? false : true;
                __result = result;
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
                SkillRequirement requirement = RequirementService.list.FirstOrDefault(x => name.GetStableHashCode() == x.StableHashCode);
                if (requirement is null) return;

                string craftText = GetTextCraft(requirement);
                __instance.m_recipeDecription.text += craftText;

                bool result = requirement.Requirements.Where(x => x.BlockCraft).ToList().Any(x => !IsAble(x)) ? true : false;
                if (result)
                {
                    __instance.m_craftButton.interactable = false;
                }
             }
        }

        [HarmonyPatch]
        class PlayerShit
        {
            [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem))]
            [HarmonyPostfix]
            internal static void CanConsumeItem(ItemDrop.ItemData item, ref bool __result)
            {
                SkillRequirement requirement = RequirementService.list.FirstOrDefault(x => item.m_dropPrefab.name.GetStableHashCode() == x.StableHashCode);
                if (requirement is null) return;

                __result = requirement.Requirements.Where(x => x.BlockEquip).ToList().Any(x => !IsAble(x)) ? false : true;
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
                    RequirementService.GenerateListWithAllEquipments();
                }
                hasSpawned = true;
            }
        }

        public static bool IsAble(Requirement requirement)
        {

            if (requirement.EpicMMO)
            {
                int level = 0;

                if (requirement.Skill == "Level") level = EpicMMOSystem_API.GetLevel();
                else level = EpicMMOSystem_API.GetAttribute((EpicMMOSystem_API.Attribut)Enum.Parse(typeof(EpicMMOSystem_API.Attribut), requirement.Skill));


                if (level == 0) return true;

                if (level < requirement.Level) return false;
            }

            if (ValheimLevelSystemList.Contains(requirement.Skill))
            {
                int level = GetSkillLevelVLS(requirement.Skill);
                if (level == 0) return true;

                if (level < requirement.Level) return false;
            }

            var skill = Player.m_localPlayer.GetSkills().m_skillData.FirstOrDefault(x => x.Key == FromName(requirement.Skill));
            if (skill.Value is null)
            {
                Skills.SkillType type = (Skills.SkillType)Enum.Parse(typeof(Skills.SkillType), requirement.Skill);
                skill = Player.m_localPlayer.GetSkills().m_skillData.FirstOrDefault(x => x.Key == type);
            }
            if (skill.Value is null) return true;


            if (skill.Value.m_level < requirement.Level) return false;

            return true;
        }

        public static Skills.SkillType FromName(string englishName) => (Skills.SkillType)Math.Abs(englishName.GetStableHashCode());


        public static string GetTextCraft(SkillRequirement requirement)
        {
            List<Requirement> requirements = requirement.Requirements.Where(x => x.BlockCraft).ToList();

            string cantEquipColor = ItemRequiresSkillLevel.cantEquipColor.Value;
            string canEquipColor = ItemRequiresSkillLevel.canEquipColor.Value;

            string str = "";

            foreach (Requirement req in requirements)
            {
                string colorToUse = cantEquipColor;
                if (IsAble(req)) colorToUse = canEquipColor;
                str += String.Format(ItemRequiresSkillLevel.RequiresText.Value, colorToUse, req.Skill, req.Level);
            }

            return Localization.instance.Localize($"{str}");
        }

        public static string GetTextEquip(SkillRequirement requirement)
        {
            List<Requirement> requirements = requirement.Requirements.Where(x => x.BlockEquip).ToList();

            string cantEquipColor = ItemRequiresSkillLevel.cantEquipColor.Value;
            string canEquipColor = ItemRequiresSkillLevel.canEquipColor.Value;

            string str = "";

            foreach (Requirement req in requirements)
            {
                string colorToUse = cantEquipColor;
                if (IsAble(req)) colorToUse = canEquipColor;
                str += String.Format(ItemRequiresSkillLevel.RequiresText.Value, colorToUse, req.Skill, req.Level);
            }

            return Localization.instance.Localize($"{str}");
        }
    }
}