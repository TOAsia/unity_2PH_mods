using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityModManagerNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TH20;
using FullInspector;

namespace RepeatResearch
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool ToggleRepeatResearch = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    static class Main
    {
        public static bool enabled;
        public static Settings settings;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = Settings.Load<Settings>(modEntry);

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
            settings.ToggleRepeatResearch = GUILayout.Toggle(settings.ToggleRepeatResearch, " Repeat Research Projects.");
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }

    [HarmonyPatch(typeof(ResearchManager), "CompleteResearchProject")]
    static class ResearchManager_CompleteResearchProject_Patch
    {
        static bool Prefix(ResearchManager __instance, ResearchProject project, Level ___level)
        {
            if (!Main.enabled || !Main.settings.ToggleRepeatResearch || !project.Definition.Repeatable)
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
}
