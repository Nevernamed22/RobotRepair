using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BetterRobotHearts
{
    public class Module : ETGModule
    {
        public override void Start()
        {
            try
            {
                itemPickupHook = new Hook(
                    typeof(PassiveItem).GetMethod("Pickup"),
                    typeof(Module).GetMethod("AcquireItemHook"),
                    typeof(PassiveItem)
                );
                gunPickupHook = new Hook(
                    typeof(GunInventory).GetMethod("AddGunToInventory"),
                    typeof(Module).GetMethod("AcquireGunHook")
                );
                ShrineUseHook = new Hook(
                typeof(AdvancedShrineController).GetMethod("DoShrineEffect", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Module).GetMethod("OnShrineUsed", BindingFlags.Static | BindingFlags.Public)
            );
                ShrineBenefitHook = new Hook(
                typeof(ShrineBenefit).GetMethod("ApplyBenefit", BindingFlags.Instance | BindingFlags.Public),
                typeof(Module).GetMethod("OnShrineBenefit", BindingFlags.Static | BindingFlags.Public)
            );
                ShrineCostHook = new Hook(
                typeof(ShrineCost).GetMethod("ApplyCost", BindingFlags.Instance | BindingFlags.Public),
                typeof(Module).GetMethod("OnShrineCost", BindingFlags.Static | BindingFlags.Public)
            );
                miserlyDropHook = new Hook(
                    typeof(MiserlyProtectionItem).GetMethod("Drop"),
                    typeof(Module).GetMethod("MiserlyDropHook"),
                    typeof(MiserlyProtectionItem)
                );

                ETGModConsole.Log("<color=#3b86ff>Robot Repair installed successfully.</color>");

            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
            }
        }
        public static void OnShrineCost(Action<ShrineCost, PlayerController> orig, ShrineCost self, PlayerController playa)
        {
            //ETGModConsole.Log("'" + self.rngString + "'");
            //Turns out I don't actually think the Robot can get the Enfeebled effect, so this hook is useless :)
            orig(self, playa);
        }
        public static void OnShrineBenefit(Action<ShrineBenefit, PlayerController> orig, ShrineBenefit self, PlayerController playa)
        {
            if (!string.IsNullOrEmpty(self.rngString) && playa.characterIdentity == PlayableCharacters.Robot)
            {
                if (self.rngString == "#SHRINE_DICE_GOOD_HEALTHSLOT" || self.rngString == "#SHRINE_DICE_GOOD_HEALTH") //Bolstered
                {
                    int amt = UnityEngine.Random.Range(1, 3);
                    for (int i = 0; i < amt; i++)
                    {
                        LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, playa);
                    }
                }
            }
            orig(self, playa);
        }
        public static void OnShrineUsed(Action<AdvancedShrineController, PlayerController> orig, AdvancedShrineController self, PlayerController playa)
        {
            if (self.displayTextKey == "#SHRINE_HEALTH_DISPLAY")
            {
                if (playa.characterIdentity == PlayableCharacters.Robot && !playa.CharacterUsesRandomGuns)
                {
                    LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, playa);
                }
            }
            orig(self, playa);
        }
        public DebrisObject MiserlyDropHook(Func<MiserlyProtectionItem, PlayerController, DebrisObject> orig, MiserlyProtectionItem self, PlayerController dropper)
        {
            try
            {
                if (dropper.characterIdentity == PlayableCharacters.Robot)
                {
                    if (!dropper.HasActiveBonusSynergy(CustomSynergyType.MISERLY_PIGTECTION, false))
                    {
                        if (dropper.healthHaver.Armor > 2)
                        {
                            dropper.healthHaver.Armor -= 2;
                        }
                        else if (dropper.healthHaver.Armor > 1)
                        {
                            dropper.healthHaver.Armor -= 1;
                        }
                    }
                }
                return orig(self, dropper);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
                return null;
            }
        }
        public static Gun AcquireGunHook(Func<GunInventory, Gun, bool, Gun> orig, GunInventory self, Gun item, bool active = false)
        {
            try
            {
                if (healthUpgrades.ContainsKey(item.PickupObjectId))
                {
                    if (self.Owner && self.Owner is PlayerController && (self.Owner as PlayerController).characterIdentity == PlayableCharacters.Robot && (self.Owner as PlayerController).CharacterUsesRandomGuns == false)
                    {
                        if (!item.HasBeenPickedUp)
                        {
                            int amt = healthUpgrades[item.PickupObjectId];
                            for (int i = 0; i < amt; i++)
                            {
                                LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, self.Owner as PlayerController);
                            }
                        }
                    }
                }
                return orig(self, item, active);
            }
            catch (Exception e)
            {
                ETGModConsole.Log(e.Message);
                ETGModConsole.Log(e.StackTrace);
                return null;
            }
        }
        public void AcquireItemHook(Action<PassiveItem, PlayerController> orig, PassiveItem self, PlayerController player)
        {
            //ETGModConsole.Log("picked up an item");

            if (player.characterIdentity == PlayableCharacters.Robot)
            {
                if (healthUpgrades.ContainsKey(self.PickupObjectId))
                {
                    //ETGModConsole.Log("item was in the thing");

                    bool pickedUp = ReflectionHelpers.ReflectGetField<bool>(typeof(PassiveItem), "m_pickedUpThisRun", self);
                    if (!pickedUp)
                    {
                        //ETGModConsole.Log("item not picked up this run");

                        int amt = healthUpgrades[self.PickupObjectId];
                        for (int i = 0; i < amt; i++)
                        {
                            LootEngine.GivePrefabToPlayer(PickupObjectDatabase.GetById(120).gameObject, player);
                        }
                    }
                }
            }
            orig(self, player);
        }

        public static Dictionary<int, int> healthUpgrades = new Dictionary<int, int>()
        {
            {110, 1}, //Magic Sweet
            {132, 2}, //Ring of Miserly Protection
            {260, 1}, //Pink Guon Stone
            {313, 1}, //Monster Blood
            {421, 1}, //Heart Holster
            {422, 1}, //Heart Lunchbox
            {423, 1}, //Heart Locket
            {424, 1}, //Heart Bottle
            {425, 1}, //Heart Purse
            {364, 1}, //Heart of Ice
            {489, 1}, //Gun Soul
            {271, 1}, //Riddle of Lead
            {570, 2}, //Yellow Chamber
            {479, 1}, //Super Meat Gun
        };

        public static Hook itemPickupHook;
        public static Hook gunPickupHook;
        public static Hook ShrineUseHook;
        public static Hook ShrineCostHook;
        public static Hook ShrineBenefitHook;
        public static Hook miserlyDropHook;
        public override void Exit() { }
        public override void Init() { }
    }
    static class ReflectionHelpers
    {
        public static T GetTypedValue<T>(this FieldInfo This, object instance) { return (T)This.GetValue(instance); }
        public static T ReflectGetField<T>(Type classType, string fieldName, object o = null)
        {
            FieldInfo field = classType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | ((o != null) ? BindingFlags.Instance : BindingFlags.Static));
            return (T)field.GetValue(o);
        }
    }
}
