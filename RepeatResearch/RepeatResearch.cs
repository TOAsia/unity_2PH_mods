using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using UnityEngine;
using UnityModManagerNet;
using TH20;

namespace RepeatResearch
{
    internal static class Main
    {
        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            HarmonyInstance.Create(modEntry.Info.Id).PatchAll(Assembly.GetExecutingAssembly());
            // Main.settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            Main.Logger = modEntry.Logger;
            modEntry.OnToggle = new Func<UnityModManager.ModEntry, bool, bool>(Main.OnToggle);
            // modEntry.OnGUI = new Action<UnityModManager.ModEntry>(Main.OnGUI);
            // modEntry.OnSaveGUI = new Action<UnityModManager.ModEntry>(Main.OnSaveGUI);
            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            Main.enabled = value;
            return true;
        }

        /*
        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            /*
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("Research Rate Increase: Increase Each Tick By Amount ", new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(false)
            });
            string text = Main.settings.ResearchTickRate.ToString();
            string text2 = GUILayout.TextField(text, 3, new GUILayoutOption[]
            {
                GUILayout.Width(50f)
            });
            int value;
            if (text2 != text && int.TryParse(text2, out value))
            {
                Main.settings.ResearchTickRate = Mathf.Clamp(value, 0, 500) * 1f;
            }
            GUILayout.EndHorizontal();
            

            Main.settings.RepeatProjectsFlag = GUILayout.Toggle(Main.settings.RepeatProjectsFlag, " Repeat Research Projects", new GUILayoutOption[0]);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            Main.settings.Save(modEntry);
        }
        */

        public static bool enabled;

        // public static Settings settings;

        public static UnityModManager.ModEntry.ModLogger Logger;
    }

    [HarmonyPatch(typeof(ResearchManager), "CompleteResearchProject")]
    internal static class ResearchManager_CompleteResearchProject_Patch
    {
        private static bool Prefix(ResearchManager __instance, ResearchProject project, Level ___level)
        {
            //if (!Main.enabled || !Main.settings.RepeatProjectsFlag || !project.Definition.Repeatable)
            if (!Main.enabled || !project.Definition.Repeatable)
            {
                return true;
            }
            RewardUtils.GiveAllRewards(null, project.Definition.Rewards, ___level.Metagame, null);
            ___level.Notifications.Send(new NotificationResearchComplete(___level.Notifications.MessageDefinitions._researchCompleteMessage, project.Definition, ___level));
            __instance.OnResearchProjectComplete.InvokeSafe(project);
            ___level.ObjectiveEvents.OnGameEvent.InvokeSafe(ObjectiveGameEvent.ResearchProjectCompleted);
            return false;
        }


    }

    /*
    [HarmonyPatch(typeof(RoomLogicResearch), "Tick")]
    internal static class RoomLogicResearch_Tick_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();
            for (var i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];
                yield return instruction;
                if (instruction.opcode == OpCodes.Stloc_S
                    && instructionsList[i - 1].operand == typeof(Staff).GetMethod("GetResearchRate", new Type[] { typeof(float) }))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_S);
                    yield return new CodeInstruction(OpCodes.Call, typeof(RoomLogicResearch_Tick_Patch).GetMethod("AddTickFloat"));
                }
            }
        }
        public static bool AddTickFloat(float num)
        {
            num += Main.settings.ResearchTickRate;
            return true;
        }
    }
    

    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save<Settings>(this, modEntry);
        }

        // public bool RepeatProjectsFlag;

        // public float ResearchTickRate = 0f;
    }
    */
}