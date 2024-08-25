using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using EFT;
using EFT.Console.Core;
using Comfort.Common;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT.UI;
using EFT.Communications;
using SPT.Reflection.Utils;


namespace InvisibilityCloak
{ 
    [BepInPlugin("com.Invisibility.Cloak", "InvisibilityCloak", "1.0.0")]
    class Plugin : BaseUnityPlugin
    {
        public static bool InvisibilityCloak_enabled = true;

        private void Awake()
        {
            Logger.LogInfo($"InvisibilityCloak has loaded, use console commands 'invisible_on' or 'invisible_off'");

            ConsoleScreen.Processor.RegisterCommand("invisible_on", new Action(invisible_on));
            ConsoleScreen.Processor.RegisterCommand("invisible_off", new Action(invisible_off));
            new BotMemoryClass_AddEnemy_Patch().Enable();
        }

        private void invisible_on()
        {
            InvisibilityCloak_enabled = true;
            Notifier.DisplayWarningNotification($"InvisibilityCloak is ON", ENotificationDurationType.Long);
        }

        private void invisible_off()
        {
            InvisibilityCloak_enabled = false;
            Notifier.DisplayWarningNotification($"InvisibilityCloak is OFF", ENotificationDurationType.Long);
        }
    }

    static class Notifier
    {
        private static readonly MethodInfo notifierMessageMethod;
        private static readonly MethodInfo notifierWarningMessageMethod;

        static Notifier()
        {
            var notifierType = PatchConstants.EftTypes.Single(x => x.GetMethod("DisplayMessageNotification") != null);
            notifierMessageMethod = notifierType.GetMethod("DisplayMessageNotification");
            notifierWarningMessageMethod = notifierType.GetMethod("DisplayWarningNotification");
        }

        public static void DisplayMessageNotification(string message, ENotificationDurationType duration = ENotificationDurationType.Default, ENotificationIconType iconType = ENotificationIconType.Default, Color? textColor = null)
        {
            notifierMessageMethod.Invoke(null, new object[] { message, duration, iconType, textColor });
        }

        public static void DisplayWarningNotification(string message, ENotificationDurationType duration = ENotificationDurationType.Default)
        {
            notifierWarningMessageMethod.Invoke(null, new object[] { message, duration });
        }
    }

    internal class BotMemoryClass_AddEnemy_Patch : ModulePatch
    {
        [HarmonyPriority(int.MaxValue)] // Load patch with absolute highest priority to override all other patches
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), nameof(BotMemoryClass.AddEnemy));
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotMemoryClass __instance, BotOwner ___botOwner_0, BotsGroup ___botsGroup_0, Action<IPlayer> ___action_0, IPlayer enemy, BotSettingsClass groupInfo, bool onActivation)
        {
            if (!Plugin.InvisibilityCloak_enabled)
            {
                return true; // Skip our function and execute original function instead of ours
            }

            BotMemoryClass.Class896 class896 = new BotMemoryClass.Class896();
            class896.botMemoryClass = __instance;
            class896.enemy = enemy;

            if (class896.enemy.IsYourPlayer) // Do not add enemy if it is our player ;)
            {
                return false; // Skip original
            }
            if (class896.enemy.Id == ___botOwner_0.GetPlayer.Id)
            {
                return false; // Skip original
            }
            for (int i = 0; i < ___botOwner_0.BotsGroup.MembersCount; i++)
            {
                if (___botOwner_0.BotsGroup.Member(i).GetPlayer.Id == class896.enemy.Id)
                {
                    return false; // Skip original
                }
            }
            if (___botOwner_0.EnemiesController.EnemyInfos.ContainsKey(class896.enemy))
            {
                return false; // Skip original
            }
            if (class896.enemy.Transform == null || !class896.enemy.HealthController.IsAlive)
            {
                return false; // Skip original
            }
            global::EnemyInfo enemyInfo = ___botOwner_0.EnemiesController.AddNew(___botsGroup_0, class896.enemy, groupInfo);
            ___botOwner_0.EnemiesController.SetInfo(class896.enemy, enemyInfo);
            ___botOwner_0.BotRequestController.RemoveAllRequestByRequester(global::Comfort.Common.Singleton<global::EFT.GameWorld>.Instance.GetAlivePlayerByProfileID(class896.enemy.ProfileId));
            class896.enemy.HealthController.DiedEvent += class896.method_0;
            float sqrMagnitude = (___botOwner_0.Position - class896.enemy.Position).sqrMagnitude;
            if (!onActivation && sqrMagnitude < 625f && !___botOwner_0.Memory.HaveEnemy && global::GClass301.CanShoot(___botOwner_0, enemyInfo))
            {
                enemyInfo.SetVisible(true);
                ___botOwner_0.Memory.GoalEnemy = enemyInfo;
            }
            global::System.Action<global::EFT.IPlayer> action = ___action_0;
            if (action == null)
            {
                return false; // Skip original
            }
            action(class896.enemy);

            return false; // Skip original
        }
    }
}
