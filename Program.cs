using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Printing;
using System.Linq;
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

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static SpellSlot Ignite;
        private static SpellSlot Flash;
        private static int LastCast;
        private static readonly Obj_AI_Hero player = ObjectManager.Player;

        private static void Main(string[] args)
        {
            //Welcome Message upon loading assembly.
            Game.PrintChat(
                "<font color=\"#00BFFF\">Scientific Lux -<font color=\"#FFFFFF\"> GODLIKE VERSION Successfully Loaded.</font>");
            CustomEvents.Game.OnGameLoad += OnLoad;

        }

        private static void OnLoad(EventArgs args)
        {
            if (player.ChampionName != ChampName)
                return;

            Q = new Spell(SpellSlot.Q, 1175);
            Q.SetSkillshot(0.4f, 68f, 1200f, false, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 1075);
            W.SetSkillshot(0.5f, 150f, 1200f, false, SkillshotType.SkillshotLine);

            E = new Spell(SpellSlot.E, 1100f);
            E.SetSkillshot(0.4f, 275f, 1300f, false, SkillshotType.SkillshotCircle);

            R = new Spell(SpellSlot.R, 3340f);
            R.SetSkillshot(1.45f, 190f, float.MaxValue, false, SkillshotType.SkillshotLine);

            Config = new Menu("Scientific Lux", "STLux", true);
            Orbwalker = new Orbwalking.Orbwalker(Config.AddSubMenu(new Menu("[SL]: Orbwalker", "Orbwalker")));
            TargetSelector.AddToMenu(Config.AddSubMenu(new Menu("[SL]: Target Selector", "Target Selector")));

            var combo = Config.AddSubMenu(new Menu("[SL]: Combo Settings", "Combo Settings"));
            var harass = Config.AddSubMenu(new Menu("[SL]: Harass Settings", "Harass Settings"));
            var killsteal = Config.AddSubMenu(new Menu("[SL]: Killsteal Settings", "Killsteal Settings"));
            var laneclear = Config.AddSubMenu(new Menu("[SL]: Laneclear Settings", "Laneclear Settings"));
            var jungleclear = Config.AddSubMenu(new Menu("[SL]: Jungle Settings", "Jungle Settings"));
            var drawing = Config.AddSubMenu(new Menu("[SL]: Draw Settings", "Draw Settings"));


            combo.SubMenu("[SBTW]ManaManager").AddItem(new MenuItem("qmana", "[Q] Mana %").SetValue(new Slider(15, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager").AddItem(new MenuItem("wmana", "[W] Mana %").SetValue(new Slider(25, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager").AddItem(new MenuItem("emana", "[E] Mana %").SetValue(new Slider(15, 100, 0)));
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
            killsteal.AddItem(new MenuItem("SmartI", "Use Ignite").SetValue(true));
            killsteal.AddItem(new MenuItem("SmartQ", "Use Q").SetValue(true));
            killsteal.AddItem(new MenuItem("SmartE", "Use E").SetValue(true));
            killsteal.AddItem(new MenuItem("SmartR", "Use R").SetValue(true));

            drawing.AddItem(new MenuItem("Draw_Disabled", "Disable All Spell Drawings").SetValue(false));
            drawing.AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(new Circle(true, Color.Orange)));
            drawing.AddItem(new MenuItem("Wdraw", "Draw W Range").SetValue(new Circle(true, Color.DarkOrange)));
            drawing.AddItem(new MenuItem("Edraw", "Draw E Range").SetValue(new Circle(true, Color.AntiqueWhite)));
            drawing.AddItem(new MenuItem("Rdraw", "Draw R Range").SetValue(new Circle(true, Color.CornflowerBlue)));
            drawing.AddItem(new MenuItem("RLine", "Draw [R] Prediction").SetValue(new Circle(true, Color.SkyBlue)));

            harass.AddItem(new MenuItem("harassQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("harassE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            laneclear
                .AddItem(new MenuItem("laneQ", "Use Q").SetValue(true));
            laneclear
                .AddItem(new MenuItem("laneE", "Use E").SetValue(true));
            laneclear
                .AddItem(new MenuItem("lanemana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            //JUNGLEFARMMENU

            jungleclear
                .AddItem(new MenuItem("jungleQ", "Use Q").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("jungleE", "Use E").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("junglemana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            GameObject.OnDelete += LuxEgone;
            GameObject.OnCreate += GameObject_OnCreate;
            Drawing.OnDraw += OnDraw;
            Program.killsteal();
        }

        private static void LuxEgone(GameObject sender, EventArgs args)
        {
            if (sender.Name.Contains("LuxLightstrike_tar_green") || sender.Name.Contains("LuxLightstrike_tar_red"))

                LuxE = null;
                Ecasted = false;
        }
        public static bool Ecasted { get; set; }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args) //I found this on pornhub trust me, no copy pasterino. 
        {
                if (sender.Name.Contains("LuxLightstrike_tar_green") || sender.Name.Contains("LuxLightstrike_tar_red"))
                
                    LuxE = sender;
                    Ecasted = true;
        }
        private static GameObject LuxE { get; set; }

        private static void OnDraw(EventArgs args)
        {

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
                    var rpredl = R.GetPrediction(target).CastPosition;
                    float predictedHealth = HealthPrediction.GetHealthPrediction(target,
                        (int) (R.Delay + (player.Distance(target.ServerPosition)/R.Speed*1000)));
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
                    Render.Circle.DrawCircle(orbwalkert.Position, 30, Color.DeepSkyBlue, 15);
                    Render.Circle.DrawCircle(target.Position, 30, Color.DeepSkyBlue, 15);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

            if (target.IsInvulnerable)
                return;
            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;
            var qpred = Q.GetPrediction(target, true);
            var qcollision = Q.GetCollision(player.ServerPosition.To2D(), new List<Vector2> { qpred.CastPosition.To2D() });
            var minioncol = qcollision.Where(x => !(x is Obj_AI_Hero)).Count(x => x.IsMinion);

            if (target.IsValidTarget(Q.Range)
                    && minioncol <= 1
                    && Config.Item("qHarass").GetValue<bool>()
                    && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercentage() >= harassmana)
                    Q.Cast(qpred.CastPosition);

            if (E.IsReady() && target.IsValidTarget(E.Range) && Config.Item("eHarass").GetValue<bool>() && player.ManaPercentage() >= harassmana)
                elogic();
        }
        private static void killsteal()
        {
            {
                foreach (
                 Obj_AI_Hero hero in
                 ObjectManager.Get<Obj_AI_Hero>()
                 .Where(
                 hero =>
                 ObjectManager.Player.Distance(hero.ServerPosition) <= R.Range && !hero.IsMe &&
                 hero.IsValidTarget() && hero.IsEnemy && !hero.IsInvulnerable))
                {

                    var qdmg = Q.GetDamage(hero);
                    var edmg = W.GetDamage(hero);
                    var rdmg = E.GetDamage(hero);
                    var rpred = R.GetPrediction(hero);
                    var qpred = R.GetPrediction(hero);
                    var epred = R.GetPrediction(hero);

                    if (hero.Health < edmg && rpred.Hitchance >= HitChance.High && R.IsReady())
                        E.Cast(epred.CastPosition);
                    if (hero.Health < qdmg && rpred.Hitchance >= HitChance.High && R.IsReady())
                        Q.Cast(qpred.CastPosition);

                    if (hero.Health < qdmg && hero.IsValidTarget(Q.Range) && Q.IsReady() &&
                        qpred.Hitchance >= HitChance.High ||
                        hero.Health < edmg && hero.IsValidTarget(E.Range) && E.IsReady() &&
                        epred.Hitchance >= HitChance.High)
                        return;

                    if (hero.Health < rdmg && rpred.Hitchance >= HitChance.High && R.IsReady())
                        R.Cast(rpred.CastPosition);
                }
            }
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
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (player.Distance(target.ServerPosition) / R.Speed)));
            var rdmg = R.GetDamage(target);
            var rpdmg = R.GetDamage(target) + 10 + (8*player.Level) + player.FlatMagicDamageMod*0.2;
            var rpred = R.GetPrediction(target);

            if (target.IsInvulnerable)
                return;

           if (target.IsValidTarget(R.Range)
             && R.IsReady()
             && Config.Item("UseRQ").GetValue<bool>()
             && rpred.Hitchance >= HitChance.VeryHigh
             && target.HasBuff("LuxLightBindingMis"))
             R.Cast(rpred.CastPosition);


          if (target.IsValidTarget(R.Range)
             && R.IsReady()
             && Config.Item("UseRA").GetValue<bool>()
             && rpred.Hitchance >= HitChance.High)
             R.CastIfWillHit(target, Config.Item("RA").GetValue<Slider>().Value);


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
        }

        private static void autospells(Obj_AI_Base target)
        {
            var qpred = Q.GetPrediction(target);
            if (target.IsStunned || target.IsRooted || 
                target.HasBuffOfType(BuffType.Charm) ||
                target.HasBuffOfType(BuffType.Suppression))

                Q.Cast(qpred.CastPosition);
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


            if (Config.Item("UseWP").GetValue<bool>() 
             && (player.HealthPercentage() <= Config.Item("UseWHP").GetValue<Slider>().Value
             && W.IsReady() && player.Position.CountEnemiesInRange(W.Range) >= 1))

             W.Cast(player.Position);
            
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
            {
                var wpred = W.GetPrediction(hero);
                if (Config.Item("UseWA").GetValue<bool>() &&
                    (hero.HealthPercentage() <= Config.Item("UseWAHP").GetValue<Slider>().Value && W.IsReady() &&
                    hero.Distance(player.ServerPosition) <= W.Range && wpred.Hitchance >= HitChance.High) && hero.Position.CountEnemiesInRange(W.Range) >= 1)
                   
                    W.Cast(wpred.CastPosition);

                if (hero.HasBuff("zedulttargetmark")
                    || hero.HasBuff("soulshackles")
                    || hero.HasBuff("vladimirhemoplage")
                    || hero.HasBuff("fallenonetarget")
                    || hero.HasBuff("caitlynaceinthehole")
                    || hero.HasBuff("fizzmarinerdoombomb")
                    || hero.HasBuff("leblancsoulshackle")
                    || hero.HasBuff("mordekaiserchildrenofthegrave"))

                    W.Cast(player.Position);
            }
        }
    
        private static void elogic()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var epred = E.GetPrediction(target);
            var emana = Config.Item("emana").GetValue<Slider>().Value;

            if (E.IsReady() && LuxE == null && target.IsValidTarget(E.Range)
            && epred.Hitchance >= HitChance.High
            && E.IsReady() && player.ManaPercentage() >= emana )
                E.Cast(epred.CastPosition);

            if (target.IsInvulnerable)
                return;

            if (E.IsReady()
                && target.IsMoving
                && LuxE.Position.CountEnemiesInRange(E.Width) >= 1)
                E.Cast();

            if (LuxE.Position.CountEnemiesInRange(E.Width) >= 2)
                E.Cast();

            if (target.HasBuff("luxilluminatingfraulein") && player.Distance(target) < Q.Range)
                return;

            if (LuxE.Position.CountEnemiesInRange(E.Width) >= 1)
                E.Cast();
        }
        private static void Combo()
        {
            var qmana = Config.Item("qmana").GetValue<Slider>().Value;
            var rmana = Config.Item("rmana").GetValue<Slider>().Value;
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var rtarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

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

           if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();
           if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
               rlogic();
           if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
               Harass();
           if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
               Laneclear();
           if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
               Jungleclear();


            autospells(target);

            if (LuxE != null
            && target.IsMoving
            && LuxE.Position.CountEnemiesInRange(E.Width) >= 1 || player.Distance(target) > Orbwalking.GetRealAutoAttackRange(player) && LuxE.Position.CountEnemiesInRange(E.Width) >= 1)
                E.Cast();

            if (Config.Item("UseW").GetValue<bool>() && player.ManaPercentage() >= wmana)
                wlogic();
        }
    }
}