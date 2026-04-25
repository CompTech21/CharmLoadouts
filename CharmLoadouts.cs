using System;
using System.Collections.Generic;
using Modding;
using Modding.Menu;
using UnityEngine;
using Satchel.BetterMenus;
using Object = UnityEngine.Object;

namespace CharmLoadouts
{
    public class GlobalSettings
    {
        public List<int> Loadout1 = new List<int>();
        public List<int> Loadout2 = new List<int>();
        public List<int> Loadout3 = new List<int>();
    }

    public class CharmLoadouts : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
    {
        public override string GetVersion() => "1.5.0";
        public static GlobalSettings Settings { get; set; } = new GlobalSettings();
        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;

        public bool ToggleButtonInsideMenu => false;
        private Menu MenuRef;

        private string GetStatus(List<int> loadout)
        {
            return (loadout != null && loadout.Count > 0) ? "True" : "False";
        }

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            if (MenuRef == null)
            {
                MenuRef = new Menu("Charm Loadouts", new Element[]
                {
                    new TextPanel("SAVED BUILDS:"),
                    new TextPanel("1. " + GetStatus(Settings.Loadout1)),
                    new TextPanel("2. " + GetStatus(Settings.Loadout2)),
                    new TextPanel("3. " + GetStatus(Settings.Loadout3)),
                    new StaticPanel("Spacer", (GameObject go) => {}),
                    new TextPanel("Controls:"),
                    new TextPanel("SHIFT + 1/2/3: Save Build"),
                    new TextPanel("1/2/3: Load Build (At Bench)")
                });
            }

            return MenuRef.GetMenuScreen(modListMenu);
        }

        public override void Initialize()
        {
            GameObject go = new GameObject("CharmLoadoutHandler");
            go.AddComponent<LoadoutManager>();
            Object.DontDestroyOnLoad(go);
            Log("Charm Loadouts Simplified Ready!");
        }
    }

    public class LoadoutManager : MonoBehaviour
    {
        private string _statusMessage = "";
        private float _messageTimer = 0f;

        private void Update()
        {
            PlayerData pd = PlayerData.instance;
            if (pd == null) return;
            if (_messageTimer > 0) _messageTimer -= Time.deltaTime;

            for (int i = 1; i <= 3; i++)
            {
                KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + (i - 1));
                if (Input.GetKeyDown(key))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        SaveLoadout(i);
                    else
                        TryLoad(i, pd);
                }
            }
        }

        private void TryLoad(int slot, PlayerData pd)
        {
            if (pd.GetBool("atBench")) LoadLoadout(slot);
            else ShowMessage("Need a Bench!");
        }

        private void SaveLoadout(int slot)
        {
            PlayerData pd = PlayerData.instance;
            if (pd == null || pd.equippedCharms == null) return;

            List<int> current = new List<int>(pd.equippedCharms);
            if (slot == 1) CharmLoadouts.Settings.Loadout1 = current;
            else if (slot == 2) CharmLoadouts.Settings.Loadout2 = current;
            else if (slot == 3) CharmLoadouts.Settings.Loadout3 = current;

            ShowMessage("Slot " + slot + " SAVED");
        }

        private void LoadLoadout(int slot)
        {
            List<int> target;
            switch (slot)
            {
                case 1: target = CharmLoadouts.Settings.Loadout1; break;
                case 2: target = CharmLoadouts.Settings.Loadout2; break;
                default: target = CharmLoadouts.Settings.Loadout3; break;
            }

            if (target == null || target.Count == 0)
            {
                ShowMessage("Slot " + slot + " is EMPTY");
                return;
            }

            PlayerData pd = PlayerData.instance;
            bool hasVoidHeart = pd.GetBool("gotCharm_36");

            for (int i = 1; i <= 45; i++)
            {
                if (i == 36 && hasVoidHeart) continue;
                pd.SetBool("equippedCharm_" + i, false);
            }
            pd.equippedCharms.Clear();
            if (hasVoidHeart) pd.equippedCharms.Add(36);

            int totalCost = 0;
            foreach (int id in target)
            {
                if (id == 36) continue;
                if (pd.GetBool("gotCharm_" + id))
                {
                    pd.SetBool("equippedCharm_" + id, true);
                    pd.equippedCharms.Add(id);
                    totalCost += pd.GetInt("charmCost_" + id);
                }
            }

            pd.SetInt("charmSlotsFilled", totalCost);
            pd.SetBool("overcharmed", totalCost > pd.GetInt("charmSlots"));

            if (HeroController.instance != null)
            {
                ReflectionHelper.CallMethod(HeroController.instance, "RefreshOvercharmed");
            }

            ShowMessage("Slot " + slot + " LOADED");
        }

        private void ShowMessage(string msg) { _statusMessage = msg; _messageTimer = 2.5f; }

        private void OnGUI()
        {
            if (_messageTimer > 0)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 24,
                    fontStyle = FontStyle.Bold
                };
                GUI.color = Color.black;
                GUI.Label(new Rect(Screen.width / 2 - 199, Screen.height - 149, 400, 50), _statusMessage, style);
                GUI.color = Color.white;
                GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height - 150, 400, 50), _statusMessage, style);
            }
        }
    }
}