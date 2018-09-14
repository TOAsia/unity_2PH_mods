using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using TH20;

namespace RepeatResearch
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool RepeatProjectsFlag = true;
        public int ResearchTickRate = 0;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    static class Main
    {
        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }

        
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Research Rate Increase: Increase Each Tick By Amount ", GUILayout.ExpandWidth(false));
            var text = settings.ResearchTickRate.ToString();
            var text2 = GUILayout.TextField(text, 3, GUILayout.Width(50));
            if (text2 != text && int.TryParse(text2, out var value))
            {
                settings.ResearchTickRate = Mathf.Clamp(value, 0, 500);
            }
            GUILayout.EndHorizontal();

            settings.RepeatProjectsFlag = GUILayout.Toggle(Main.settings.RepeatProjectsFlag, " Repeat Research Projects");
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    [HarmonyPatch(typeof(ResearchManager), "CompleteResearchProject")]
    static class ResearchManager_CompleteResearchProject_Patch
    {
        static bool Prefix(ResearchManager __instance, ResearchProject project, ref Level ___level)
        {
            if (!Main.enabled || !Main.settings.RepeatProjectsFlag || !project.Definition.Repeatable)
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
    
    [HarmonyPatch(typeof(RoomLogicResearch), "Tick")]
    static class RoomLogicResearch_Tick_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();
            for (var i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];
                yield return instruction;
                if (instruction.opcode == OpCodes.Stloc_S
                    && (MethodInfo) instructionsList[i - 1].operand == typeof(Staff).GetMethod("GetResearchRate", new Type[] { typeof(float) }))
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
}