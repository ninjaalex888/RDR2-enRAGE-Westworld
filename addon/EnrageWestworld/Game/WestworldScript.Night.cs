// The night's own scenes near the player (the NPC-life run, step 5; Core/Scenes.Night.cs). A mugging: in a town at night
// out of doors, with no lawman within 50 m, a man whose mind leans to it (little honesty, an appetite for risk, and money
// short, or a known pickpocket: roadmap 28) stops someone on their own for their purse; whether they hand it over is
// their nature's (the anxious and the meek do, the bold won't, and are knocked down). The robber is marked for it. A drunk
// carried home: late, someone in their cups goes down in the street and a friend near gets them up and walks them home.
// Each at most once a town a night, a roll of the mind's when one fits. Behind [Scenes] NightScenes and MindLive.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _nightNextAt, _muggings, _carried;
        private readonly HashSet<string> _nightDone = new HashSet<string>();   // kind|town|game day
        private const string MuggerMark = "mugger";
        private const float NightMetres = 21f;   // the night's scenes start this near you (16 until 2026-10-30; Lex: 30% farther)

        /// From DramaTick when the street is free: tonight's mugging or drunk taken home, if one fits.
        private bool NightScene(Ped me, int now)
        {
            if (!_cfg.NightScenes || !_cfg.MindLive || now < _nightNextAt || _placeTown == null || _interiorType != 0)
                return false;
            _nightNextAt = now + 20000;
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (clock >= 5 && clock < 21)
                return false;
            long day = _store.LastGameMin / 1440;
            try
            {
                if (!_nightDone.Contains("mug|" + _placeTown + "|" + day) && Mugging(me, now, day))
                    return true;
                if ((clock >= 22 || clock < 4) && !_nightDone.Contains("drunk|" + _placeTown + "|" + day) && DrunkHome(me, now, day))
                    return true;
            }
            catch (Exception ex)
            {
                Log.Detail("NIGHT failed: " + ex.Message);
            }
            return false;
        }

        private Traits TraitsAt(Ped p, int now)
        {
            MindWho w = MindKnown(p.Handle, now);
            return w != null && w.Id != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
        }

        private bool Mugging(Ped me, int now, long day)
        {
            if (LawmanNear(me, 50f))
                return false;
            List<Candidate> people = ReasonCandidates(me, now).Where(c => !c.Ped.IsDead && c.Ped.Position.DistanceTo(me.Position) < NightMetres).ToList();
            Candidate robber = null;
            string why = null;
            foreach (Candidate c in people)
            {
                if (c.Who.Woman || PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle))
                    continue;
                PersonState st = _store.Find(c.Who.Id);
                bool known = st != null && (st.Has(PickpocketMark) || st.Has(MuggerMark));
                MindWho w = MindKnown(c.Ped.Handle, now);
                Traits t = TraitsAt(c.Ped, now);
                bool leans = t != null && t.Z[Traits.Honesty] < -0.6f && t.Z[Traits.Risk] > 0.3f && w != null && w.Needs != null && w.Needs[NeedState.Money] >= 0.5f;
                if (known || leans)
                {
                    robber = c;
                    why = known ? "known for it" : "short of money, and honest with nobody";
                    break;
                }
            }
            if (robber == null)
                return false;
            // someone on their own: nobody else within 8 m of them, 4 to 25 m from the robber
            Candidate mark = people.Where(c => c != robber && c.Ped.Position.DistanceTo(robber.Ped.Position) > 4f && c.Ped.Position.DistanceTo(robber.Ped.Position) < 25f &&
                                               !people.Any(o => o != c && o != robber && o.Ped.Position.DistanceTo(c.Ped.Position) < 8f))
                                   .OrderBy(c => c.Ped.Position.DistanceTo(robber.Ped.Position)).FirstOrDefault();
            if (mark == null)
                return false;
            string seed = robber.Ped.Handle + "|" + mark.Ped.Handle + "|" + day;
            _nightDone.Add("mug|" + _placeTown + "|" + day);   // the roll is the night's, whatever it says
            if (!Voice.Chance(seed + "|mug", 40))
            {
                Log.Detail("NIGHT " + robber.Who.Name + " eyes " + mark.Who.Name + ", and lets it go tonight");
                return false;
            }
            Traits mt = TraitsAt(mark.Ped, now);
            bool hands = mt == null ? Voice.Chance(seed + "|hands", 60) : mt.Z[Traits.Anxiety] > 0.2f || mt.Z[Traits.Assertiveness] < 0f;
            SceneCast cast = BuildCast(robber.Ped, mark.Ped, null, false, me, now, seed);
            Scene s = Scenes.Mugging(cast, seed, hands, cast.Armed0);
            if (!StartScene(s, new List<Ped> { robber.Ped, mark.Ped }, cast, seed, now))
                return false;
            _muggings++;
            PersonState rs = _store.Find(robber.Who.Id);
            if (rs != null)
                rs.Mark(MuggerMark);
            Log.Write("NIGHT a mugging: " + robber.Who.Name + " (" + why + ") stops " + mark.Who.Name + ", who " + (hands ? "hands it over" : "won't") + " (" + (int)robber.Ped.Position.DistanceTo(me.Position) + " m from you)");
            return true;
        }

        private bool DrunkHome(Ped me, int now, long day)
        {
            List<Candidate> people = ReasonCandidates(me, now).Where(c => !c.Ped.IsDead && c.Ped.Position.DistanceTo(me.Position) < NightMetres).ToList();
            foreach (Candidate drunk in people)
            {
                if (!InHisCups(drunk.Ped, drunk.Who.P) || PED.IS_PED_USING_ANY_SCENARIO(drunk.Ped.Handle))
                    continue;
                Candidate friend = people.Where(c => c != drunk && c.Ped.Position.DistanceTo(drunk.Ped.Position) < 20f && LadderWarmth(c.Who.Id, drunk.Who.Id) > 0 && !InHisCups(c.Ped, c.Who.P))
                                         .OrderByDescending(c => LadderWarmth(c.Who.Id, drunk.Who.Id)).FirstOrDefault();
                if (friend == null)
                    continue;
                string seed = drunk.Ped.Handle + "|" + friend.Ped.Handle + "|" + day;
                _nightDone.Add("drunk|" + _placeTown + "|" + day);
                SceneCast cast = BuildCast(drunk.Ped, friend.Ped, null, false, me, now, seed);
                bool carry = Voice.Chance(seed + "|carry", 50) && PED.IS_PED_MALE(friend.Ped.Handle);   // out cold half the time: carried, the game's own way
                if (!StartScene(Scenes.CarriedHome(cast, seed, carry), new List<Ped> { drunk.Ped, friend.Ped }, cast, seed, now))
                    return false;
                _carried++;
                Log.Write("NIGHT " + friend.Who.Name + " takes " + drunk.Who.Name + " home, drunk (" + (int)drunk.Ped.Position.DistanceTo(me.Position) + " m from you)");
                return true;
            }
            return false;
        }

        private string NightStatus()
        {
            return !_cfg.NightScenes ? "off ([Scenes] NightScenes)" : _muggings + " muggings, " + _carried + " drunks taken home this session";
        }
    }
}
