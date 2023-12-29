﻿using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static GamblersMod.config.GambleConstants;

namespace GamblersMod.Patches
{
    internal class GamblingMachine : NetworkBehaviour
    {
        // Cooldown
        int gamblingMachineMaxCooldown = 4;
        public int gamblingMachineCurrentCooldown = 0;

        // Multipliers for winning or losing
        int jackpotMultiplier;
        int tripleMultiplier;
        int doubleMultiplier;
        float halvedMultiplier;

        // Percentages for the outcome of gambling
        int jackpotPercentage;
        int triplePercentage;
        int doublePercentage;
        int halvedPercentage;
        int removedPercentage;
        int zeroMultiplier;

        // Dice roll range (inclusive)
        int rollMinValue;
        int rollMaxValue;
        int currentRoll = 1;

        // Current state
        public float currentGamblingOutcomeMultiplier = 1;
        public string currentGamblingOutcome = GamblingOutcome.DEFAULT;

        void Awake()
        {
            Plugin.mls.LogInfo("GamblingMachine has Awoken");

            jackpotMultiplier = Plugin.UserConfig.configJackpotMultiplier;
            tripleMultiplier = Plugin.UserConfig.configTripleMultiplier;
            doubleMultiplier = Plugin.UserConfig.configDoubleMultiplier;
            halvedMultiplier = Plugin.UserConfig.configHalveMultiplier;
            zeroMultiplier = Plugin.UserConfig.configZeroMultiplier;

            jackpotPercentage = Plugin.UserConfig.configJackpotChance;
            triplePercentage = Plugin.UserConfig.configTripleChance;
            doublePercentage = Plugin.UserConfig.configDoubleChance;
            halvedPercentage = Plugin.UserConfig.configHalveChance;
            removedPercentage = Plugin.UserConfig.configZeroChance;

            Plugin.mls.LogInfo($"jackpotMultiplier loaded from config: {jackpotMultiplier}");
            Plugin.mls.LogInfo($"tripleMultiplier loaded from config: {tripleMultiplier}");
            Plugin.mls.LogInfo($"doubleMultiplier loaded from config: {doubleMultiplier}");
            Plugin.mls.LogInfo($"halvedMultiplier loaded from config: {halvedMultiplier}");
            Plugin.mls.LogInfo($"zeroMultiplier loaded from config: {zeroMultiplier}");

            Plugin.mls.LogInfo($"jackpotPercentage loaded from config: {jackpotPercentage}");
            Plugin.mls.LogInfo($"triplePercentage loaded from config: {triplePercentage}");
            Plugin.mls.LogInfo($"doublePercentage loaded from config: {doublePercentage}");
            Plugin.mls.LogInfo($"halvedPercentage loaded from config: {halvedPercentage}");
            Plugin.mls.LogInfo($"removedPercentage loaded from config: {removedPercentage}");

            // Rolls
            rollMinValue = 1;
            rollMaxValue = jackpotPercentage + triplePercentage + doublePercentage + halvedPercentage + removedPercentage;
        }

        void Start()
        {
            Plugin.mls.LogInfo("GamblingMachine has Started");
        }

        public void GenerateGamblingOutcomeFromCurrentRoll()
        {
            bool isJackpotRoll = (currentRoll >= rollMinValue && currentRoll <= jackpotPercentage); // [0 - JACKPOT]

            int tripleStart = jackpotPercentage;
            int tripleEnd = jackpotPercentage + triplePercentage;
            bool isTripleRoll = (currentRoll > tripleStart && currentRoll <= tripleEnd); // [JACKPOT - (JACKPOT + TRIPLE)]

            int doubleStart = tripleEnd;
            int doubleEnd = tripleEnd + doublePercentage;
            bool isDoubleRoll = (currentRoll > doubleStart && currentRoll <= doubleEnd); // [(JACKPOT + TRIPLE) - (JACKPOT + TRIPLE + DOUBLE)]

            int halvedStart = doubleEnd;
            int halvedEnd = doubleEnd + halvedPercentage;
            bool isHalvedRoll = (currentRoll > halvedStart && currentRoll <= halvedEnd); // [(JACKPOT + TRIPLE + DOUBLE) - (JACKPOT + TRIPLE + DOUBLE + HALVED)]

            if (isJackpotRoll)
            {
                Plugin.mls.LogMessage($"Rolled Jackpot");
                currentGamblingOutcomeMultiplier = jackpotMultiplier;
                currentGamblingOutcome = GamblingOutcome.JACKPOT;
            }
            else if (isTripleRoll)
            {
                Plugin.mls.LogMessage($"Rolled Triple");
                currentGamblingOutcomeMultiplier = tripleMultiplier;
                currentGamblingOutcome = GamblingOutcome.TRIPLE;
            }
            else if (isDoubleRoll)
            {
                Plugin.mls.LogMessage($"Rolled Double");
                currentGamblingOutcomeMultiplier = doubleMultiplier;
                currentGamblingOutcome = GamblingOutcome.DOUBLE;
            }
            else if (isHalvedRoll)
            {
                Plugin.mls.LogMessage($"Rolled Halved");
                currentGamblingOutcomeMultiplier = halvedMultiplier;
                currentGamblingOutcome = GamblingOutcome.HALVE;
            }
            else
            {
                Plugin.mls.LogMessage($"Rolled Remove");
                currentGamblingOutcomeMultiplier = zeroMultiplier;
                currentGamblingOutcome = GamblingOutcome.REMOVE;
            }
        }

        public void PlayGambleResultAudio()
        {
            if (currentGamblingOutcome == GamblingOutcome.JACKPOT)
            {
                AudioSource.PlayClipAtPoint(Plugin.GamblingJackpotScrapAudio, transform.position, 0.6f);
            }
            else if (currentGamblingOutcome == GamblingOutcome.TRIPLE)
            {
                AudioSource.PlayClipAtPoint(Plugin.GamblingTripleScrapAudio, transform.position, 0.6f);
            }
            else if (currentGamblingOutcome == GamblingOutcome.DOUBLE)
            {
                AudioSource.PlayClipAtPoint(Plugin.GamblingDoubleScrapAudio, transform.position, 0.6f);
            }
            else if (currentGamblingOutcome == GamblingOutcome.HALVE)
            {
                AudioSource.PlayClipAtPoint(Plugin.GamblingHalveScrapAudio, transform.position, 0.6f);
            }
            else if (currentGamblingOutcome == GamblingOutcome.REMOVE)
            {
                AudioSource.PlayClipAtPoint(Plugin.GamblingRemoveScrapAudio, transform.position, 0.6f);
            }
        }

        public void PlayDrumRoll()
        {
            AudioSource.PlayClipAtPoint(Plugin.GamblingDrumrollScrapAudio, transform.position, 0.6f);
        }

        public void BeginGamblingMachineCooldown(Action onCountdownFinish)
        {
            gamblingMachineCurrentCooldown = gamblingMachineMaxCooldown;
            StartCoroutine(CountdownCooldownCoroutine(onCountdownFinish));
        }

        public bool isInCooldownPhase()
        {
            return gamblingMachineCurrentCooldown > 0;
        }

        IEnumerator CountdownCooldownCoroutine(Action onCountdownFinish)
        {
            Plugin.mls.LogInfo("Start gambling machine cooldown");
            while (gamblingMachineCurrentCooldown > 0)
            {
                yield return new WaitForSeconds(1);
                gamblingMachineCurrentCooldown -= 1;
                Plugin.mls.LogMessage($"Gambling machine cooldown: {gamblingMachineCurrentCooldown}");
            }
            onCountdownFinish();
            Plugin.mls.LogMessage("End gambling machine cooldown");
        }

        public void SetRoll(int newRoll)
        {
            currentRoll = newRoll;
        }

        public int RollDice()
        {
            int roll = UnityEngine.Random.Range(rollMinValue, rollMaxValue);

            Plugin.mls.LogMessage($"rollMinValue: {rollMinValue}");
            Plugin.mls.LogMessage($"rollMaxValue: {rollMaxValue}");
            Plugin.mls.LogMessage($"Roll value: {currentRoll}");

            return roll;
        }

        public int GetScrapValueBasedOnGambledOutcome(GrabbableObject scrap)
        {
            return (int)Mathf.Floor(scrap.scrapValue * currentGamblingOutcomeMultiplier);
        }
    }
}
