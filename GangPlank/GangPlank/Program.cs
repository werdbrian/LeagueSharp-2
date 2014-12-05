using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace GangPlank
{
    internal class Program
    {

        //      {
        private const string ChampName = "Gangplank"; 
        public static Orbwalking.Orbwalker Orbwalker;
        public static List<Spell> Spells = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static SpellSlot IgniteSlot;
        private static Obj_AI_Hero Player = ObjectManager.Player;

        public static Menu Config;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Game.PrintChat("test ba");
        }


        private static void Game_OnGameLoad(EventArgs args)
        {

            if (Player.BaseSkinName != ChampName) return; 
            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 1300);

            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");

            Spells.Add(Q);
            Spells.Add(W);
            Spells.Add(E);


            Config = new Menu(ChampName, ChampName, true);
            Config.AddSubMenu(new Menu("Combo Settings", "combo"));
            Config.SubMenu("combo").AddItem(new MenuItem("comboKey", "Full Combo Key").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("combo").AddItem(new MenuItem("autoIgnite", "Use Ignite in combo").SetValue(true));


            Config.AddSubMenu(new Menu("Harass Settings", "harass"));
            Config.SubMenu("harass").AddItem(new MenuItem("autoQ", "Auto-Q when Target in Range").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("harass").AddItem(new MenuItem("harassKey", "Harass Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("harass").AddItem(new MenuItem("harassMana", "Min. Mana Percent: ").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Farming Settings", "farm"));
            Config.SubMenu("farm").AddItem(new MenuItem("farmKey", "Farming Key").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("farm").AddItem(new MenuItem("qFarm", "Farm with (Q)").SetValue(true));
            Config.SubMenu("farm").AddItem(new MenuItem("farmMana", "Min. Mana Percent: ").SetValue(new Slider(50)));

            Config.AddSubMenu(new Menu("Draw Settings", "drawing"));
            Config.SubMenu("drawing").AddItem(new MenuItem("mDraw", "Disable All Range Draws").SetValue(false));
            Config.SubMenu("drawing").AddItem(new MenuItem("Target", "Draw Circle on Target").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 255, 0, 0))));
            Config.SubMenu("drawing").AddItem(new MenuItem("QDraw", "Draw (Q) Range").SetValue(new Circle(true, System.Drawing.Color.FromArgb(255, 178, 0, 0))));

            Config.AddSubMenu(new Menu("Misc Settings", "misc"));
            Config.SubMenu("misc").AddItem(new MenuItem("usePackets", "Use Packets to Cast Spells").SetValue(false));
            Config.SubMenu("misc").AddItem(new MenuItem("anticc", "Use Scurvy when stunned/rooted").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            var tsMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(tsMenu);
            Config.AddSubMenu(tsMenu);
            Config.AddToMainMenu();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;


            //            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            //            GameObject.OnDelete += Game_OnObjectDelete;
            Game.PrintChat("<font color=\"#00BFFF\">GankPlank by Taerarenai -</font> <font color=\"#FFFFFF\">Loaded</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            var comboKey = Config.Item("comboKey").GetValue<KeyBind>().Active;
            var harassKey = Config.Item("harassKey").GetValue<KeyBind>().Active;
            var farmKey = Config.Item("farmKey").GetValue<KeyBind>().Active;
            var movement = ObjectManager.Player.CanMove;

            if (comboKey && target != null)
                Combo(target);
            else
            {
                if (harassKey && target != null)
                    Harass(target);

                if (farmKey)
                    Farm();

                if (Config.Item("autoQ").GetValue<KeyBind>().Active && target != null && ObjectManager.Player.Distance(target) <= Q.Range && Q.IsReady())
                        Q.CastOnUnit(target, Config.Item("usePackets").GetValue<bool>());

                if (Config.Item("anticc").GetValue<KeyBind>().Active && W.IsReady() &&
                    (Player.HasBuffOfType(BuffType.Stun) || Player.HasBuffOfType(BuffType.Fear) ||
                    Player.HasBuffOfType(BuffType.Slow) || Player.HasBuffOfType(BuffType.Snare) ||
                    Player.HasBuffOfType(BuffType.Taunt)))
                    W.Cast();

            }
        }


        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config.Item("mDraw").GetValue<bool>())
                return;
            foreach (var spell in Spells.Where(spell => Config.Item(spell.Slot + "Draw").GetValue<Circle>().Active))
            {
                Utility.DrawCircle(ObjectManager.Player.Position, spell.Range, Config.Item(spell.Slot + "Draw").GetValue<Circle>().Color);
            }
            var target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Physical);
            if (Config.Item("Target").GetValue<Circle>().Active && target != null)
                Utility.DrawCircle(target.Position, 50, Config.Item("Target").GetValue<Circle>().Color, 1, 50);
        }



        private static void Combo(Obj_AI_Base target)
        {
            if (target == null) return;
            if (Q.IsReady())
                Q.CastOnUnit(target, Config.Item("usePackets").GetValue<bool>());
            if (E.IsReady())
                E.Cast(target);


            if (!Config.Item("autoIgnite").GetValue<bool>()) return;
            if (IgniteSlot == SpellSlot.Unknown || ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) != SpellState.Ready) return;
            if (!(ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) >= target.Health)) return;
            ObjectManager.Player.SummonerSpellbook.CastSpell(IgniteSlot, target);
        }



        private static void Harass(Obj_AI_Base target)
        {
            if (target == null) return;
            var mana = ObjectManager.Player.MaxMana * (Config.Item("harassMana").GetValue<Slider>().Value / 100.0);
            if (!(ObjectManager.Player.Mana > mana)) return;
            if (Q.IsReady())
                Q.CastOnUnit(target, Config.Item("usePackets").GetValue<bool>());
        }

        private static void Farm()
        {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var mana = ObjectManager.Player.MaxMana * (Config.Item("farmMana").GetValue<Slider>().Value / 100.0);
            if (!(ObjectManager.Player.Mana > mana)) return;
            if (Config.Item("qFarm").GetValue<bool>() && Q.IsReady())
            {
                foreach (var minion in from minion in minions
                                       let actualHp = (HealthPrediction.GetHealthPrediction(minion, (int)(ObjectManager.Player.Distance(minion) * 1000 / 1500)) <= minion.MaxHealth * 0.15) ? ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) * 2 : ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q)
                                       where minion.IsValidTarget() && HealthPrediction.GetHealthPrediction(minion,
                                       (int)(ObjectManager.Player.Distance(minion) * 1000 / 1500)) <= actualHp
                                       select minion)
                {
                    Q.CastOnUnit(minion, Config.Item("usePackets").GetValue<bool>());
                    return;
                }
            }
        }


        private static float ComboDamage(Obj_AI_Base target)
        {
            var dmg = 0d;
            if (Q.IsReady())
                dmg += (target.Health <= target.MaxHealth * 0.15) ? (ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) * 2) : ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q);
            if (W.IsReady())
                dmg += ObjectManager.Player.GetSpellDamage(target, SpellSlot.W);
            if (E.IsReady())
                dmg += ObjectManager.Player.GetSpellDamage(target, SpellSlot.E);
            if (IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                dmg += ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            return (float)dmg;
        }

    }
}
