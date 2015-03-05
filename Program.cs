using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Authentication.ExtendedProtection;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using SharpDX.Serialization;
using Color = System.Drawing.Color;

namespace ScientificLux
{
    internal class Program
    {
        public const string ChampName = "Lux";
        public static Menu Config;

        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;

        public static HpBarIndicator Hpi = new HpBarIndicator();
        private static SpellSlot Ignite;
        private static SpellSlot Flash;
        private static int LastCast;
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {

            if (player.ChampionName != ChampName)
                return;

            Notifications.AddNotification("Scientific Lux - [V.2.2.4.1]", 8000);

            Q = new Spell(SpellSlot.Q, 1175);
            Q.SetSkillshot(0.4f, 68f, 1200f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 1075);
            W.SetSkillshot(0.5f, 150f, 1200f, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 1100f);
            E.SetSkillshot(0.4f, 275f, 1300f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 3340f);
            R.SetSkillshot(1.50f, 190f, float.MaxValue, false, SkillshotType.SkillshotLine);

            Config = new Menu("Scientific Lux", "STLux", true);
            Orbwalker = new Orbwalking.Orbwalker(Config.AddSubMenu(new Menu("[SL]: Orbwalker", "Orbwalker")));
            TargetSelector.AddToMenu(Config.AddSubMenu(new Menu("[SL]: Target Selector", "Target Selector")));

            var combo = Config.AddSubMenu(new Menu("[SL]: Combo Settings", "Combo Settings"));
            var harass = Config.AddSubMenu(new Menu("[SL]: Harass Settings", "Harass Settings"));
            var killsteal = Config.AddSubMenu(new Menu("[SL]: Killsteal Settings", "Killsteal Settings"));
            var laneclear = Config.AddSubMenu(new Menu("[SL]: Laneclear Settings", "Laneclear Settings"));
            var jungleclear = Config.AddSubMenu(new Menu("[SL]: Jungle Settings", "Jungle Settings"));
            var misc = Config.AddSubMenu(new Menu("[SL]: Misc Settings", "Misc Settings"));
            var drawing = Config.AddSubMenu(new Menu("[SL]: Draw Settings", "Draw Settings"));


            combo.SubMenu("[SBTW]ManaManager").AddItem(new MenuItem("qmana", "[Q] Mana %").SetValue(new Slider(10, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager").AddItem(new MenuItem("wmana", "[W] Mana %").SetValue(new Slider(20, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager").AddItem(new MenuItem("emana", "[E] Mana %").SetValue(new Slider(5, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager").AddItem(new MenuItem("rmana", "[R] Mana %").SetValue(new Slider(10, 100, 0)));

            combo.SubMenu("[Q] Settings").AddItem(new MenuItem("UseQ", "Use Q - Light Binding").SetValue(true));

            combo.SubMenu("[W] Settings").AddItem(new MenuItem("UseW", "Use W - Prismatic Barrier").SetValue(true));
            combo.SubMenu("[W] Settings").AddItem(new MenuItem("UseWP", "W on HP Percentage").SetValue(true));
            combo.SubMenu("[W] Settings").AddItem(new MenuItem("UseWHP", "Player HP%").SetValue(new Slider(80, 100, 1)));
            combo.SubMenu("[W] Settings").AddItem(new MenuItem("UseWA", "Use W on Allies").SetValue(true));
            combo.SubMenu("[W] Settings").AddItem(new MenuItem("UseWAHP", "Ally Hp %").SetValue(new Slider(30, 100, 1)));

            combo.SubMenu("[E] Settings").AddItem(new MenuItem("UseE", "Use E - Lucent Singularity").SetValue(true));

            combo.SubMenu("[R] Settings").AddItem(new MenuItem("UseR", "Use R - Finales Funkeln").SetValue(true));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("UseRA", "Use R [AOE]").SetValue(true));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("RA", "Enemy Hit Count").SetValue(new Slider(3, 5, 1)));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("UseRQ", "Auto R targets hit by Q").SetValue(true));

            combo.SubMenu("Summoner Settings").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));

            killsteal.AddItem(new MenuItem("SmartKS", "Use SmartKS").SetValue(true));
            killsteal.AddItem(new MenuItem("KSI", "Use Ignite").SetValue(true));
            killsteal.AddItem(new MenuItem("KSQ", "Use Q").SetValue(true));
            killsteal.AddItem(new MenuItem("KSE", "Use E").SetValue(true));
            killsteal.AddItem(new MenuItem("KSR", "Use R").SetValue(true));

            drawing.AddItem(new MenuItem("Draw_Disabled", "Disable All Drawings").SetValue(false));
            drawing.AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(new Circle(true, Color.Orange)));
            drawing.AddItem(new MenuItem("Wdraw", "Draw W Range").SetValue(new Circle(true, Color.DarkOrange)));
            drawing.AddItem(new MenuItem("Edraw", "Draw E Range").SetValue(new Circle(true, Color.AntiqueWhite)));
            drawing.AddItem(new MenuItem("Rdraw", "Draw R Range").SetValue(new Circle(true, Color.CornflowerBlue)));
            drawing.AddItem(new MenuItem("RLine", "Draw [R] Prediction").SetValue(new Circle(true, Color.SkyBlue)));

            harass.AddItem(new MenuItem("Qharass", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("Qharassslowed", "Only use Q if target is slowed/stunned/rooted").SetValue(true));
            harass.AddItem(new MenuItem("Eharass", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            misc
                .AddItem(new MenuItem("DrawD", "Damage Indicator").SetValue(true));
            misc
                .AddItem(new MenuItem("AntiGap", "AntiGapCloser [Q]").SetValue(true));


            laneclear
                .AddItem(new MenuItem("laneQ", "Use Q").SetValue(true));
            laneclear
                .AddItem(new MenuItem("laneE", "Use E").SetValue(true));
            laneclear
                .AddItem(new MenuItem("lanemana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));
            

            jungleclear
                .AddItem(new MenuItem("jungleQ", "Use Q").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("jungleE", "Use E").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("junglemana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));
            jungleclear
                .AddItem(new MenuItem("blank", "                                        "));

            jungleclear.AddItem(new MenuItem("jungleks", "Junglesteal [PRESS] ").SetValue(new KeyBind('K', KeyBindType.Press)));

            jungleclear
                .AddItem(new MenuItem("Blue", "Steal Bluebuff").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("Red", "Steal Redbuff").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("Dragon", "Steal Dragon").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("Baron", "Steal Baron").SetValue(true));

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnDelete += LuxEgone;
            GameObject.OnCreate += GameObject_OnCreate;
            Drawing.OnDraw += OnDraw;
            Drawing.OnEndScene += OnEndScene;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private static void OnEndScene(EventArgs args)
        {
            if (Config.Item("DrawD").GetValue<bool>())
            {
                foreach (var enemy in
                    ObjectManager.Get<Obj_AI_Hero>().Where(ene => !ene.IsDead && ene.IsEnemy && ene.IsVisible))
                {
                    Hpi.unit = enemy;
                    Hpi.drawDmg(CalcDamage(enemy), Color.Green);
                }
            }
        }
        private static int CalcDamage(Obj_AI_Base target)
        {
            //Calculate Combo Damage
            var aa = player.GetAutoAttackDamage(target, true); 
            var damage = aa;
            Ignite = player.GetSpellSlot("summonerdot");

            if (Ignite.IsReady())
                damage += player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            if (Config.Item("UseE").GetValue<bool>()) // edamage
            {
                if (E.IsReady())
                {
                    damage += E.GetDamage(target);
                }
            }
            if (target.HasBuff("luxilluminatingfraulein"))
            {
                damage += aa +10 + (8 * player.Level) + player.FlatMagicDamageMod * 0.2;
            }

            if (R.IsReady() && Config.Item("UseR").GetValue<bool>()) // rdamage
            {

                damage += R.GetDamage(target);
            }

            if (Q.IsReady() && Config.Item("UseQ").GetValue<bool>())
            {
                damage += Q.GetDamage(target);
            }
            return (int)damage;


        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (player.IsDead || gapcloser.Sender.IsInvulnerable)
                return;

            var targetpos = Drawing.WorldToScreen(gapcloser.Sender.Position);
            if (gapcloser.Sender.IsValidTarget(Q.Range) && Config.Item("AntiGap").GetValue<bool>())
            {
                Render.Circle.DrawCircle(gapcloser.Sender.Position, gapcloser.Sender.BoundingRadius, Color.DeepPink);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.MediumPurple, "GAPCLOSER!");
            }
            if (Q.IsReady() && gapcloser.Sender.IsValidTarget(Orbwalking.GetRealAutoAttackRange(player)) && Config.Item("AntiGap").GetValue<bool>())
                Q.Cast(gapcloser.Sender);
        }

        private static void LuxEgone(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("LuxLightstrike_tar_green") || sender.Name.Contains("LuxLightstrike_tar_red"))

                LuxE = null;
                Ecasted = false;
        }
        public static bool Ecasted { get; set; }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args) //I found this on pornhub trust me, no copy pasterino. //Credits to Chewymoon
        {
                if (sender.Name.Contains("LuxLightstrike_tar_green") || sender.Name.Contains("LuxLightstrike_tar_red"))
                
                    LuxE = sender;
                    Ecasted = true;
        }
        private static GameObject LuxE { get; set; }

        private static void OnDraw(EventArgs args)
        {
            var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            if (Config.Item("jungleks").GetValue<KeyBind>().Active)
                Drawing.DrawText(pos.X - 50, pos.Y + 50, Color.HotPink, "[R] Junglesteal Enabled");

            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;
            {
                if (Config.Item("Qdraw").GetValue<Circle>().Active)
                    if (Q.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range,
                            Q.IsReady() ? Config.Item("Qdraw").GetValue<Circle>().Color : Color.Red);

                if (Config.Item("Wdraw").GetValue<Circle>().Active)
                    if (W.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range,
                            W.IsReady() ? Config.Item("Wdraw").GetValue<Circle>().Color : Color.Red);

                if (Config.Item("Edraw").GetValue<Circle>().Active)
                    if (E.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range - 1,
                            E.IsReady() ? Config.Item("Edraw").GetValue<Circle>().Color : Color.Red);

                if (Config.Item("Rdraw").GetValue<Circle>().Active)
                    if (R.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range - 2,
                            R.IsReady() ? Config.Item("Rdraw").GetValue<Circle>().Color : Color.Red);

                {
                    var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                    if (target == null || !target.IsValidTarget())
                        return;
                    
                    var rpredl = R.GetPrediction(target).CastPosition;
                    float predictedHealth = HealthPrediction.GetHealthPrediction(target,
                        (int) (R.Delay + (player.Distance(target.ServerPosition)/R.Speed*500)));
                    var rdmg = R.GetDamage(target);
                    var rpdmg = R.GetDamage(target) + 10 + (8*player.Level) + player.FlatMagicDamageMod*0.2;
                    var rpred = R.GetPrediction(target);
                    var rdraw = new Geometry.Polygon.Line(player.Position, rpredl, R.Range);

                    if (Config.Item("RLine").GetValue<Circle>().Active
                        && target.IsValidTarget(R.Range)
                        && R.IsReady()
                        && rpred.Hitchance >= HitChance.VeryHigh
                        && Config.Item("UseR").GetValue<bool>()
                        && target.HasBuff("luxilluminatingfraulein")
                        && predictedHealth <= rpdmg
                        && target.Path.Count() < 2 ||
                        target.IsValidTarget(R.Range)
                        && Config.Item("UseR").GetValue<bool>()
                        && R.IsReady()
                        && rpred.Hitchance >= HitChance.VeryHigh
                        && predictedHealth <= rdmg
                        && target.Path.Count() < 2)

                        rdraw.Draw(Config.Item("RLine").GetValue<Circle>().Color, 4);

                    var orbwalkert = Orbwalker.GetTarget();
                    Render.Circle.DrawCircle(orbwalkert.Position, 80, Color.DeepSkyBlue, 7);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target.IsInvulnerable)
                return;
            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;
            var qpred = Q.GetPrediction(target, true);
            var qcollision = Q.GetCollision(player.ServerPosition.To2D(), new List<Vector2> { qpred.CastPosition.To2D() });
            var minioncol = qcollision.Where(x => !(x is Obj_AI_Hero)).Count(x => x.IsMinion);

            if (E.IsReady() && target.IsValidTarget(E.Range) && Config.Item("Eharass").GetValue<bool>() && player.ManaPercentage() >= harassmana)
                elogic();

            if (target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Config.Item("Qharass").GetValue<bool>()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercentage() >= harassmana
                && target.HasBuffOfType(BuffType.Slow) || target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Config.Item("Qharass").GetValue<bool>()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercentage() >= harassmana
                && target.HasBuffOfType(BuffType.Stun) || target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Config.Item("Qharass").GetValue<bool>()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercentage() >= harassmana
                && target.HasBuffOfType(BuffType.Snare))

                Q.Cast(qpred.CastPosition);

            if (Config.Item("Qharassslowed").GetValue<bool>())
                return;

            if (target.IsValidTarget(Q.Range)
                    && minioncol <= 1
                    && Config.Item("Qharass").GetValue<bool>()
                    && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercentage() >= harassmana)
                    Q.Cast(qpred.CastPosition);


        }
        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (Ignite == SpellSlot.Unknown || player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
                return 0f;
            return (float)player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private static void killsteal()
        {
            {
            if (!R.IsReady())
            {
                return;
            }

            foreach (var enemy in
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(x => x.IsValidTarget())
                    .Where(x => !x.IsZombie)
                    .Where(x => !x.IsDead))
                {
                    Ignite = player.GetSpellSlot("summonerdot");
                    var qdmg = Q.GetDamage(enemy);
                    var edmg = W.GetDamage(enemy);
                    var rdmg = E.GetDamage(enemy);
                    var rpred = R.GetPrediction(enemy);
                    var qpred = Q.GetPrediction(enemy);
                    var epred = E.GetPrediction(enemy);
                    float predictedHealth = HealthPrediction.GetHealthPrediction(enemy, (int)(R.Delay + (player.Distance(enemy.ServerPosition) / R.Speed*1000)));
                    if (enemy.Health < edmg && epred.Hitchance >= HitChance.High && E.IsReady() && Config.Item("KSE").GetValue<bool>())
                        E.Cast(enemy);
                    if (enemy.Health < qdmg && qpred.Hitchance >= HitChance.High && Q.IsReady() && Config.Item("KSQ").GetValue<bool>())
                        Q.Cast(enemy);
                    if (player.Distance(enemy) <= 600 && IgniteDamage(enemy) >= enemy.Health &&
                         Config.Item("KSI").GetValue<bool>())
                        player.Spellbook.CastSpell(Ignite, enemy);

                    if (enemy.Health < qdmg && enemy.IsValidTarget(Q.Range) && Q.IsReady() &&
                        qpred.Hitchance >= HitChance.High ||
                        enemy.Health < edmg && enemy.IsValidTarget(E.Range) && E.IsReady() &&
                        epred.Hitchance >= HitChance.High)
                        return;

                    if (predictedHealth < rdmg && rpred.Hitchance >= HitChance.High && R.IsReady() && Config.Item("KSR").GetValue<bool>())
                        R.Cast(rpred.CastPosition);
                }
            }
        }

        private static void Junglesteal()
        {
            Orbwalking.Orbwalk(null, Game.CursorPos);
           if (!R.IsReady())           
                return;
            
            var blueBuff =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.BaseSkinName == "SRU_Blue")
                    .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                    .FirstOrDefault(x => ( x.IsAlly) || (x.IsEnemy));
            if (blueBuff != null && Config.Item("Blue").GetValue<bool>())
                R.Cast(blueBuff);

                        var redBuff =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.BaseSkinName == "SRU_Red")
                    .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                    .FirstOrDefault(x => ( x.IsAlly) || (x.IsEnemy));
            if (redBuff != null && Config.Item("Red").GetValue<bool>())
                R.Cast(redBuff);

                        var Baron =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.BaseSkinName == "SRU_Baron")
                    .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                    .FirstOrDefault(x => ( x.IsAlly) || (x.IsEnemy));
                        if (Baron != null && Config.Item("Baron").GetValue<bool>())
                R.Cast(Baron);

                        var Dragon =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.BaseSkinName == "SRU_Dragon")
                    .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                    .FirstOrDefault(x => ( x.IsAlly) || (x.IsEnemy));
                        if (Dragon != null && Config.Item("Dragon").GetValue<bool>())
                R.Cast(Dragon);
            
        }
        

        private static void Jungleclear()
        {
            var junglem = Config.Item("junglemana").GetValue<Slider>().Value;
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            var Qfarmpos = W.GetLineFarmLocation(allMinionsQ, Q.Width);
            var Efarmpos = E.GetCircularFarmLocation(allMinionsE, E.Width);
            if (Qfarmpos.MinionsHit >= 1 && Config.Item("jungleQ").GetValue<bool>() &&
                player.ManaPercentage() >= junglem)
            {
                Q.Cast(Qfarmpos.Position);
            }
            if (Efarmpos.MinionsHit >= 1 && allMinionsE.Count >= 1 && Config.Item("jungleE").GetValue<bool>() &&
                player.ManaPercentage() >= junglem)          
                E.Cast(Efarmpos.Position);

                if (LuxE != null)
                    E.Cast();
            
        }

        private static void Laneclear()
        {
            var lanem = Config.Item("lanemana").GetValue<Slider>().Value;
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width);

            var Qfarmpos = W.GetLineFarmLocation(allMinionsQ, Q.Width);
            var Efarmpos = E.GetCircularFarmLocation(allMinionsE, E.Width);
            if (Qfarmpos.MinionsHit >= 1 && Config.Item("laneQ").GetValue<bool>() && player.ManaPercentage() >= lanem)
            {
                Q.Cast(Qfarmpos.Position);
            }
            if (Efarmpos.MinionsHit >= 3 && allMinionsE.Count >= 2 && Config.Item("laneE").GetValue<bool>() &&
                player.ManaPercentage() >= lanem)
                E.Cast(Efarmpos.Position);

                if (LuxE != null)
                    E.Cast();

            
        }

        private static void rlogic()
        {
            Ignite = player.GetSpellSlot("summonerdot");
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (player.Distance(target.ServerPosition) / R.Speed*500)));
            var rdmg = R.GetDamage(target);
            var rpdmg = R.GetDamage(target) + 10 + (8*player.Level) + player.FlatMagicDamageMod*0.2;
            var rpred = R.GetPrediction(target);
            var ripdmg = R.GetDamage(target) + 10 + (8*player.Level) + player.FlatMagicDamageMod*0.2 +
                         IgniteDamage(target);

            if (target.IsInvulnerable)
                return;
            if (target.IsValidTarget(R.Range)
            && R.IsReady()
            && Config.Item("UseRA").GetValue<bool>()
            && rpred.Hitchance >= HitChance.High)
                R.CastIfWillHit(target, Config.Item("RA").GetValue<Slider>().Value);

           if (target.IsValidTarget(R.Range)
             && R.IsReady()
             && Config.Item("UseRQ").GetValue<bool>()
             && rpred.Hitchance >= HitChance.VeryHigh
             && target.HasBuff("LuxLightBindingMis"))
             R.Cast(rpred.CastPosition);

            if (E.IsReady() && player.Distance(target) < Orbwalking.GetRealAutoAttackRange(player) &&
                LuxE.Position.CountEnemiesInRange(E.Width) >= 1 && target.Health <= E.GetDamage(target)
                || target.Health < player.GetAutoAttackDamage(target) * 2
                && player.Distance(target) <= Orbwalking.GetRealAutoAttackRange(player))
                return;

           if (target.IsValidTarget(R.Range)
            && rpred.Hitchance >= HitChance.VeryHigh
            && Config.Item("UseR").GetValue<bool>()
            && target.HasBuff("luxilluminatingfraulein")
            && predictedHealth <= rpdmg 
            && target.Path.Count() <= 1)
            R.Cast(rpred.CastPosition);                    
            
            if (target.IsValidTarget(R.Range)
            && Config.Item("UseR").GetValue<bool>()
            && rpred.Hitchance >= HitChance.VeryHigh 
            && predictedHealth <= rdmg 
            && target.Path.Count() <= 1)
            R.Cast(rpred.CastPosition);

            if (player.Distance(target) <= 600 && ripdmg >= target.Health && 
            Config.Item("UseIgnite").GetValue<bool>() && R.IsReady() && Ignite.IsReady())
                player.Spellbook.CastSpell(Ignite, target);       
        }

        private static void wlogic()
        {
            if (player.HasBuff("zedulttargetmark")
                || player.HasBuff("soulshackles")
                || player.HasBuff("vladimirhemoplage")
                || player.HasBuff("fallenonetarget")
                || player.HasBuff("caitlynaceinthehole")
                || player.HasBuff("fizzmarinerdoombomb")
                || player.HasBuff("leblancsoulshackle")
                || player.HasBuff("mordekaiserchildrenofthegrave"))

                W.Cast(player.Position);

            if (player.HasBuff("Recall") || Utility.InFountain(player) || player.IsDead)
                return;

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && h.IsMe))
        
            if (Config.Item("UseWP").GetValue<bool>() 
             && (player.HealthPercentage() <= Config.Item("UseWHP").GetValue<Slider>().Value
             && W.IsReady() && player.Position.CountEnemiesInRange(W.Range) >= 1) 
             || player.HasBuffOfType(BuffType.Slow) && player.Position.CountEnemiesInRange(W.Range) >= 1  || player.HasBuffOfType(BuffType.Poison) && player.Position.CountEnemiesInRange(W.Range) >= 1 
             || player.HasBuffOfType(BuffType.Snare)  && player.Position.CountEnemiesInRange(W.Range) >= 1)

             W.Cast(hero.Position);
            
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
            {
                var wpred = W.GetPrediction(hero);
                if (player.Distance(hero) > W.Range) return;
                if (Config.Item("UseWA").GetValue<bool>() &&
                    (hero.HealthPercentage() <= Config.Item("UseWAHP").GetValue<Slider>().Value && W.IsReady() &&
                    hero.Distance(player.ServerPosition) <= W.Range && wpred.Hitchance >= HitChance.High) && hero.Position.CountEnemiesInRange(W.Range) >= 1
                    || hero.HasBuffOfType(BuffType.Slow) && hero.Position.CountEnemiesInRange(W.Range) >= 1
                    || hero.HasBuffOfType(BuffType.Poison) && hero.Position.CountEnemiesInRange(W.Range) >= 1
                    || hero.HasBuffOfType(BuffType.Snare) && hero.Position.CountEnemiesInRange(W.Range) >= 1)
                   
                    W.Cast(wpred.CastPosition);

                if (hero.HasBuff("zedulttargetmark")
                    || hero.HasBuff("soulshackles")
                    || hero.HasBuff("vladimirhemoplage")
                    || hero.HasBuff("fallenonetarget")
                    || hero.HasBuff("caitlynaceinthehole")
                    || hero.HasBuff("fizzmarinerdoombomb")
                    || hero.HasBuff("leblancsoulshackle")
                    || hero.HasBuff("mordekaiserchildrenofthegrave"))

                    W.Cast(wpred.CastPosition);
            }
        }
    
        private static void elogic()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var epred = E.GetPrediction(target);
            var emana = Config.Item("emana").GetValue<Slider>().Value;

            if (E.IsReady() && LuxE == null && target.IsValidTarget(E.Range)
            && epred.Hitchance >= HitChance.VeryHigh
            && E.IsReady() && player.ManaPercentage() >= emana )
                E.Cast(epred.CastPosition);

            if (E.IsReady() && LuxE.Position.CountEnemiesInRange(E.Width) < 1 && LuxE != null)
                Utility.DelayAction.Add(1000, () => E.Cast());

            if (target.IsInvulnerable)
                return;

            if (E.IsReady()
                && player.Distance(target) >= Orbwalking.GetRealAutoAttackRange(player)
                && LuxE.Position.CountEnemiesInRange(E.Width) >= 1)
                E.Cast();

            if (LuxE.Position.CountEnemiesInRange(E.Width) >= 2)
                E.Cast();

            if (target.HasBuff("luxilluminatingfraulein") && target.HasBuff("LuxLightBindingMis") && player.Distance(target) < Q.Range)
                return;

            if (LuxE.Position.CountEnemiesInRange(E.Width) >= 1)
                E.Cast();
        }
        private static void Combo()
        {
            Ignite = player.GetSpellSlot("summonerdot");
            var qmana = Config.Item("qmana").GetValue<Slider>().Value;
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
             if (target == null || !target.IsValidTarget())
                        return;

            if (target.IsInvulnerable)
                return;

            var qpred = Q.GetPrediction(target, true);
            var qcollision = Q.GetCollision(player.ServerPosition.To2D(), new List<Vector2> { qpred.CastPosition.To2D() });
            var minioncol = qcollision.Where(x => !(x is Obj_AI_Hero)).Count(x => x.IsMinion);

            if (Q.IsReady() && qpred.Hitchance >= HitChance.High && target.IsValidTarget(Q.Range))          
            {
                if (target.IsDashing()
                    && qpred.Hitchance >= HitChance.Dashing
                    && minioncol <= 1 && player.ManaPercentage() >= qmana && Config.Item("UseQ").GetValue<bool>())
                    Q.Cast(qpred.CastPosition);

                else if (target.IsValidTarget(Q.Range)
                    && minioncol <= 1
                    && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercentage() >= qmana && Config.Item("UseQ").GetValue<bool>())
                    Q.Cast(qpred.CastPosition);
            }

            else if (E.IsReady() && target.IsValidTarget(E.Range) && Config.Item("UseE").GetValue<bool>())
                elogic();

            if (player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health &&
            Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);  
        }


        private static void RFlogic()
        {
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

            var trollR = (player.Position.Extend(target.Position, - 300));

            if (R.IsReady() && Flash.IsReady()) ;
            R.Cast(trollR);

            LastCast = Environment.TickCount;
            player.Spellbook.CastSpell(Flash, target.ServerPosition);
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
           var wmana = Config.Item("wmana").GetValue<Slider>().Value;
           var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
           
           switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    rlogic();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Laneclear();
                    Jungleclear();
                    break;
            }
               
           if (Config.Item("jungleks").GetValue<KeyBind>().Active)
               Junglesteal();

            if (Config.Item("SmartKS").GetValue<bool>())
            {
                killsteal();
            }
            if (Config.Item("UseW").GetValue<bool>() && player.ManaPercentage() >= wmana)
            {
                wlogic();
            }

            if (LuxE != null &&
                target.IsMoving
                && player.Distance(target) > Orbwalking.GetRealAutoAttackRange(player) &&
                LuxE.Position.CountEnemiesInRange(E.Width) >= 1)
            {
                E.Cast();
            }
        }
    }
}
