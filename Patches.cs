using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                if (__instance.m_dropPrefab is null) return;

                SkillRequirement requirement = RequirementService.list.FirstOrDefault(x => __instance.m_dropPrefab.name.GetStableHashCode() == x.StableHashCode);
                if (requirement is null) return;
                __result += GetTextEquip(requirement);
            }

            [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.IsEquipable))]
            [HarmonyPostfix]
            private static void IsEquipable(ItemDrop.ItemData __instance, ref bool __result)
            {
                if (__instance.m_dropPrefab is null) return;

                SkillRequirement requirement = RequirementService.list.FirstOrDefault(x => __instance.m_dropPrefab.name.GetStableHashCode() == x.StableHashCode);
                if (requirement is null) return;

                bool result = requirement.Requirements.Where(x => x.BlockEquip).ToList().Any(x => !IsAble(x)) ? false : true;

                __result = result;
            }
        }

        [HarmonyPatch]
        class StartDrawPatch
        {
            [HarmonyPatch(typeof(Attack), nameof(Attack.StartDraw))]
            [HarmonyPrefix]
            internal static bool StartDraw(Humanoid character, ItemDrop.ItemData weapon)
            {
                if (!character.IsPlayer()) return true;
                if (string.IsNullOrEmpty(weapon.m_shared.m_ammoType)) return true;

                if (character.m_ammoItem is not null && character.m_ammoItem.IsEquipable() && character.GetInventory().GetItem(character.m_ammoItem.m_shared.m_name) is not null)
                {
                    return true;
                }

                foreach (ItemDrop.ItemData item in character.GetInventory().m_inventory)
                {
                    if (!item.IsEquipable()) continue;
                    if (!(item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)) continue;
                    if (item.m_shared.m_ammoType != weapon.m_shared.m_ammoType) continue;
                    character.m_ammoItem = item;
                    return true;
                }

                return false;
            }
        }

        [HarmonyPatch]
        class HumanoidPickUp
        {
            [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
            [HarmonyPrefix]
            internal static bool EquipItem(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (!__instance.IsPlayer()) return true;

                if (item.IsEquipable()) return true;

                return false;
            }
        }

        [HarmonyPatch]
        class UpdateRecipeText
        {
            [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
            [HarmonyPostfix]
            internal static void UpdateRecipe_Post(ref InventoryGui __instance, Player player)
            {
                if (__instance is null) return;
                if (__instance.m_selectedRecipe.Key is null) return;

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
                else level = EpicMMOSystem_API.GetAttribute(requirement.Skill);

                if (level < requirement.Level) return false;

                return true;
            }

            if (ValheimLevelSystemList.Contains(requirement.Skill))
            {
                if (!Player.m_localPlayer.m_knownTexts.TryGetValue("player" + requirement.Skill, out string txt))
                {
                    return true;
                }

                if (Convert.ToInt32(txt) < requirement.Level) return false;

                return true;
            }

            var skill = Player.m_localPlayer.GetSkills().m_skillData.FirstOrDefault(x => x.Key == FromName(requirement.Skill));
            if (skill.Value is null)
            {
                Skills.SkillType type;
                if (Enum.TryParse(requirement.Skill, out type))
                {
                    skill = Player.m_localPlayer.GetSkills().m_skillData.FirstOrDefault(x => x.Key == type);
                } else
                {
                     return false;
                }
            }

            Skills.SkillType enumValue;

            if (Enum.TryParse(requirement.Skill, out enumValue))
            {
                if (skill.Value is null) return false;
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
                str += String.Format(ItemRequiresSkillLevel.RequiresText.Value, colorToUse, req.ExhibitionName, req.Level);
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
                str += String.Format(ItemRequiresSkillLevel.RequiresText.Value, colorToUse, req.ExhibitionName, req.Level);
            }

            return Localization.instance.Localize($"{str}");
        }
    }
}