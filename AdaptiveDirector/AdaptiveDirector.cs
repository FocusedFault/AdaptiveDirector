using BepInEx;
using HarmonyLib;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdaptiveDirector
{
  [BepInPlugin("com.Nuxlar.AdaptiveDirector", "AdaptiveDirector", "1.0.0")]

  public class AdaptiveDirector : BaseUnityPlugin
  {
    private CreditAdaptation creditAdaptationInstance;

    public void Awake()
    {
      // Adaptive Director
      Harmony.CreateAndPatchAll(this.GetType(), null);
      On.RoR2.CharacterMaster.OnBodyDeath += EnemyDeathCounter;
      On.RoR2.CharacterMaster.OnBodyStart += GiveAdaptiveArmor;
      On.RoR2.CombatDirector.Awake += TrackMultiplier;
      On.RoR2.SceneExitController.Begin += ResetDirector;
      IL.RoR2.HealthComponent.TakeDamage += TweakAdaptiveArmor;
      IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += IncreaseInitialDifficulty;
    }

    public void EnemyDeathCounter(
      On.RoR2.CharacterMaster.orig_OnBodyDeath orig,
      CharacterMaster self,
      CharacterBody body)
    {
      orig(self, body);
      if ((double)self.currentLifeStopwatch >= 5.0 || (body.teamComponent.teamIndex != TeamIndex.Monster && body.teamComponent.teamIndex != TeamIndex.Void && body.teamComponent.teamIndex != TeamIndex.Lunar))
        return;
      if (SceneManager.GetActiveScene().name == "moon2" && body.hullClassification == HullClassification.Golem && (double)self.currentLifeStopwatch < 5.0)
      {
        this.creditAdaptationInstance.enemyDeathCounter += 6f + (float)(1 * (Run.instance.stageClearCount + 1.0));
      }
      else
      {
        float num = 0.0f;
        switch (body.hullClassification)
        {
          case HullClassification.Human:
            num = 0.5f;
            break;
          case HullClassification.Golem:
            num = 2f;
            break;
          case HullClassification.BeetleQueen:
            num = 6f + (float)(1 * (Run.instance.stageClearCount + 1.0));
            break;
        }
        if (body.isElite)
          num *= 2f;
        this.creditAdaptationInstance.enemyDeathCounter += num;
      }
    }

    public void GiveAdaptiveArmor(
  On.RoR2.CharacterMaster.orig_OnBodyStart orig,
  CharacterMaster self,
  CharacterBody body)
    {
      orig(self, body);
      if (body.name.Contains("Brother") || body.name.Contains("VoidRaid") || body.teamComponent.teamIndex != TeamIndex.Monster && body.teamComponent.teamIndex != TeamIndex.Void && body.teamComponent.teamIndex != TeamIndex.Lunar)
        return;
      body.inventory.GiveItem(RoR2Content.Items.AdaptiveArmor);
    }

    public void ResetDirector(On.RoR2.SceneExitController.orig_Begin orig, SceneExitController self)
    {
      orig(self);
      this.creditAdaptationInstance = null;
    }

    public void TrackMultiplier(On.RoR2.CombatDirector.orig_Awake orig, CombatDirector self)
    {
      orig(self);
      if (this.creditAdaptationInstance == null)
      {
        Debug.LogWarning((object)"Added Credit Adapter");
        this.creditAdaptationInstance = new GameObject("CreditAdaptation").AddComponent<CreditAdaptation>();
      }
      this.creditAdaptationInstance.combatDirectors.Add(self);
    }

    private void TweakAdaptiveArmor(ILContext il)
    {
      ILCursor ilCursor = new ILCursor(il);
      if (ilCursor.TryGotoNext((MoveType)2, new Func<Instruction, bool>[1]
      {
        (Func<Instruction, bool>) (x => ILPatternMatchingExt.MatchLdcR4(x, 400f))
      }))
      {
        ilCursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        ilCursor.EmitDelegate<Func<float, HealthComponent, float>>((Func<float, HealthComponent, float>)((armorCap, self) =>
        {
          if (self.body.name.Contains("Brother") || self.body.name.Contains("VoidRaid"))
            return armorCap;
          return (bool)(UnityEngine.Object)this.creditAdaptationInstance && (double)this.creditAdaptationInstance.creditMultiplier > 1.0 ? (float)(100.0 * (double)(this.creditAdaptationInstance.creditMultiplier - 1f) * 4.0) : 0.0f;
        }));
      }
      else
        this.Logger.LogError((object)"EclipseAugments: ReduceAdaptiveArmor IL Hook failed");
    }

    [HarmonyPatch(typeof(CombatDirector), "Awake")]
    [HarmonyPrefix]
    private static void AddComponent(CombatDirector __instance)
    {
      if (__instance.shouldSpawnOneWave || !__instance.skipSpawnIfTooCheap)
        return;
      __instance.gameObject.AddComponent<Limiter>().director = __instance;
    }

    [HarmonyPatch(typeof(CombatDirector), "AttemptSpawnOnTarget")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AllowLimit(IEnumerable<CodeInstruction> IL)
    {
      CodeInstruction previous = null;
      FieldInfo info = typeof(CombatDirector).GetField("maximumNumberToSpawnBeforeSkipping");
      foreach (CodeInstruction instruction in IL)
      {
        if (instruction.opcode == System.Reflection.Emit.OpCodes.Mul && CodeInstructionExtensions.LoadsField(previous, info, false))
        {
          yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldc_I4_1, null);
          yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Add, null);
        }
        yield return instruction;
        previous = instruction;
      }
    }

    [HarmonyPatch(typeof(CombatDirector), "AttemptSpawnOnTarget")]
    [HarmonyPostfix]
    private static void TooExpensive(CombatDirector __instance, bool __result)
    {
      if (__result || __instance.spawnCountInCurrentWave > 0)
        return;
      double monsterCredit = (double)__instance.monsterCredit;
      int? cost = __instance.currentMonsterCard?.cost;
      float? nullable = cost.HasValue ? new float?((float)cost.GetValueOrDefault()) : new float?();
      double valueOrDefault = (double)nullable.GetValueOrDefault();
      if (!(monsterCredit < valueOrDefault & nullable.HasValue))
        return;
      ++__instance.consecutiveCheapSkips;
    }
    public static void IncreaseInitialDifficulty(ILContext il)
    {
      ILCursor ilCursor = new ILCursor(il);
      ilCursor.GotoNext(
        x => x.MatchLdcR4(0.7f)
      );
      ilCursor.Index += 1;
      ilCursor.Next.Operand = 1.5f;

      ilCursor.GotoNext(
        x => x.MatchLdcR4(0.7f)
      );
      ilCursor.Index += 1;
      ilCursor.Next.Operand = 1.5f;
    }
  }
}