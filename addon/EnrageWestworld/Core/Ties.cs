// What a street scene leaves between two people: a tie, kept once per pair, with the latest kind of scene, how warm
// or hostile it has been, how many times, and when. Kept only between people who can be met again (town regulars,
// residents already saved for something else, enRAGE's townsfolk), so a tie never saves anyone by itself. The two
// think about each other by name for a few days (InnerLife), and the rest of their town talks about it.
// All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class SceneTie
    {
        [DataMember] public string A, B;           // ids: res:N for a resident, folk:... for one of enRAGE's townsfolk
        [DataMember] public string AName, BName;   // their full names, so a thought never has to look them up
        [DataMember] public string Town;
        [DataMember] public string Kind;           // the latest scene between them: argue, brawl, flirt, smalltalk...
        [DataMember] public int Warmth;            // the scenes added up: warm above 0, hostile below (-3..3)
        [DataMember] public int Count;             // scenes between them
        [DataMember] public long At;               // game minute of the latest
        [DataMember] public int Stage;             // how far a story between them has gone (follow-up scenes)
        [DataMember] public string Doer;           // for trouble one did to the other (rough, pickpocket, lift, drunk): who did it

        /// This one did it to the other (the attacker, the thief, the drunk who called him out).
        public bool Did(string id)
        {
            return Doer != null && Doer == id;
        }

        public bool Has(string id)
        {
            return A == id || B == id;
        }

        public string Other(string id)
        {
            return A == id ? BName : AName;
        }

        public string OtherId(string id)
        {
            return A == id ? B : A;
        }
    }

    public static class Ties
    {
        public const int Max = 300;

        /// What a scene does to the two in it: warmer above 0, colder below, 0 they know each other a little better;
        /// null for a scene that leaves nothing between them (a song, a tale, a pitch to a crowd, the player's
        /// business). The exchanges (Exchanges.cs) end as comfort, brushoff, snap, cheer, envy, makeup or sweet.
        public static int? Effect(string kind)
        {
            switch (kind)
            {
                case "argue": case "brawl": case "cheat": case "debt": case "standoff": case "snap": case "envy": case "drunk":
                    return -1;
                case "rough": case "pickpocket":
                    return -2;   // a face into the bar, or a hand caught in a coat, isn't soon forgotten
                case "lift":
                    return 0;    // the mark never saw who: nothing between them that he knows of (the thief knows)
                case "flirt": case "propose": case "sweet": case "comfort": case "cheer": case "charity": case "help": case "defend": case "round": case "apology":
                    return 1;
                case "scorn": case "passby": case "faced":
                    return -1;   // asked for a coin and sent off with a flea in the ear; left lying in the street
                case "makeup":
                    return 2;   // a quarrel mended counts for more than one more kind word
                case "smalltalk": case "gossip": case "haggle": case "hosts": case "brushoff": case "avoid":
                    return 0;
                default:
                    return null;
            }
        }

        /// Courting, as opposed to a friendly word.
        public static bool Romantic(string kind)
        {
            return kind == "flirt" || kind == "propose" || kind == "sweet";
        }

        /// Notes a scene between two people: the tie between them, made or carried on. Null if the scene leaves none.
        public static SceneTie Note(List<SceneTie> all, string aId, string aName, string bId, string bName, string town, string kind, long now)
        {
            return Note(all, aId, aName, bId, bName, town, kind, now, Trouble(kind) ? aId : null);
        }

        /// A scene where one did something to the other: A (the first in the scene) is the one who did it.
        public static bool Trouble(string kind)
        {
            return kind == "rough" || kind == "pickpocket" || kind == "lift" || kind == "drunk" || kind == "charity" || kind == "scorn" ||   // (the asking is A's doing too)
                   kind == "help" || kind == "passby" ||   // A helped B up, or walked past B
                   kind == "defend" || kind == "faced" ||  // A stood up for B; A faced B down
                   kind == "round";                        // A stood B a drink
        }

        public static SceneTie Note(List<SceneTie> all, string aId, string aName, string bId, string bName, string town, string kind, long now, string doer)
        {
            int? effect = Effect(kind);
            if (effect == null || aId == null || bId == null || aId == bId)
                return null;
            SceneTie t = all.FirstOrDefault(x => x.Has(aId) && x.Has(bId));
            if (t == null)
            {
                t = new SceneTie { A = aId, B = bId };
                all.Add(t);
            }
            bool keepRomance = Romantic(t.Kind) && !Romantic(kind) && effect.Value > 0;   // a kindness between sweethearts: still sweethearts
            if (t.Count > 0 && !keepRomance)
                t.Stage++;   // they meet again: their story moves on (Reasons)
            t.AName = t.A == aId ? aName : bName;
            t.BName = t.A == aId ? bName : aName;
            t.Town = town ?? t.Town;
            if (!keepRomance)
                t.Kind = kind;
            if (doer != null || kind != "avoid")
                t.Doer = doer;   // keeping clear of him doesn't change who did what to whom
            t.Warmth = Math.Max(-3, Math.Min(3, t.Warmth + effect.Value));
            t.Count++;
            t.At = now;
            while (all.Count > Max)
                all.Remove(all.OrderBy(x => x.At).First());
            return t;
        }

        /// This person's ties from the last few days, newest first.
        public static List<SceneTie> Of(IEnumerable<SceneTie> all, string id, long now, int days)
        {
            return all.Where(t => t.Has(id) && now - t.At >= 0 && now - t.At < days * 1440L).OrderByDescending(t => t.At).ToList();
        }

        /// The town's talk: ties in this town from the last two days worth talking about (warm or hostile), not
        /// the thinker's own, newest first.
        public static List<SceneTie> TalkOf(IEnumerable<SceneTie> all, string town, string notId, long now)
        {
            return all.Where(t => t.Town == town && t.Kind != "lift" && (t.Warmth < 0 || Romantic(t.Kind) || t.Kind == "makeup" || t.Kind == "charity" || t.Kind == "help" || t.Kind == "round") && !t.Has(notId) && now - t.At >= 0 && now - t.At < 2 * 1440L)
                      .OrderByDescending(t => t.At).ToList();
        }

        // ---- what they think of each other --------------------------------------------------------------

        private static readonly string[] Quarrel =
        {
            "Shouldn't have said that to {other}. Or should have said worse. Can't decide which.",
            "Words with {other}. Keep having the argument again in my head, and winning it this time.",
            "{other} looked at me like I was dirt on a boot. I'll remember that face."
        };

        private static readonly string[] Blows =
        {
            "Jaw still aches. {other} hits like a mule kicks.",
            "Knuckles split from {other}'s teeth. Worth it. Mostly.",
            "Came to blows with {other}. In the street, like boys. Can't look anybody in the eye."
        };

        private static readonly string[] Money =
        {
            "{other} and the money. Always the money between us now.",
            "Saw {other} buy a round like a rich man. With whose money, I wonder."
        };

        private static readonly string[] Sweet =
        {
            "{other} laughed at what I said. Twice. I've been counting.",
            "Thinking about {other} again. Burned the coffee doing it.",
            "Wonder if {other} thinks of me at all. Probably not. Maybe."
        };

        private static readonly string[] Known =
        {
            "Talked with {other} a good while. Easy company. Rare, that.",
            "{other} said a thing I keep turning over. Clever, for a talker."
        };

        private static readonly string[] Kindness =
        {
            "{other} asked after my trouble. Meant it, too. Didn't expect that.",
            "{other} was glad for me. Truly glad. There's few would be."
        };

        private static readonly string[] MadeUp =
        {
            "Made it up with {other}. Like setting down a sack of feed I'd carried a month.",
            "{other} said sorry first. I'd have said it second. Probably."
        };

        private static readonly string[] Shrugged =
        {
            "Told {other} my news. Got a shrug for it. I'll remember the shrug.",
            "{other} couldn't get away from me fast enough. Fine. Fine."
        };

        // trouble one did to the other: the one who did it, and the one it was done to
        private static readonly string[] RoughDid =
        {
            "Put {other}'s face into the bar. Felt good for about a minute.",
            "Hand still smells of {other}'s hair oil. Should've washed it.",
            "Folks give me room since that business with {other}. I like the room."
        };
        private static readonly string[] RoughDone =
        {
            "Can't chew on the left side since {other} put my face in the bar. Every bite, I think of {other}.",
            "Everybody saw. Everybody saw {other} do it, and nobody said a word.",
            "I'll be standing with my back to the wall from now on. {other} taught me that much."
        };
        private static readonly string[] LiftDid =
        {
            "{other} never felt a thing. Some folks walk around asking to be robbed.",
            "Three dollars and a lucky rabbit's foot off {other}. Wasn't lucky for {other}.",
            "Saw {other} patting their coat. Didn't look back. You never look back."
        };
        private static readonly string[] LiftDone =
        {
            "Had money this morning. Somewhere between here and the store it walked off.",
            "Somebody had a hand in my coat. I keep going over every face I passed.",
            "Lost my money. Or it was took. Either way I'm a fool, and I'll eat beans till Saturday."
        };
        private static readonly string[] CaughtDid =
        {
            "{other} saw my face. Has to have. Best stay off that street a while.",
            "Should've let {other} be. Should've picked an easier mark."
        };
        private static readonly string[] CaughtDone =
        {
            "I'd know {other}'s face anywhere now. Had a hand right in my pocket.",
            "{other}. Bold as brass, in broad day. I'll count my change twice around {other}."
        };
        private static readonly string[] AvoidDid =
        {
            "{other} saw me coming and found somewhere else to be. Good.",
            "{other} won't look me in the eye now. Suits me."
        };
        private static readonly string[] AvoidDone =
        {
            "Went the long way round so as not to pass {other}. Coward's walk. Still walking it.",
            "Saw {other} and my hands started shaking. Hate that they do that."
        };
        // {other}: the one who fell (for the one who helped or walked past), or the one who helped or walked past
        private static readonly string[] HelpDid = { "{other} took a fall and I helped them up. Glad I was there.", "Hope {other}'s mending. Went down hard." };
        private static readonly string[] HelpDone = { "{other} picked me up when I fell. I owe {other} for that.", "{other} stopped. Plenty didn't." };
        private static readonly string[] PassDid = { "{other} took a tumble. Not my affair.", "{other} looked at me like I'd done something. I hadn't." };
        private static readonly string[] PassDone = { "{other} stepped right past me. Me lying in the street.", "I won't forget {other} walking on by." };
        // standing up to a bully: {other} the one picked on (for the one who stepped in), or the one who stepped in; faced: the bully, or the one who faced him
        private static readonly string[] DefendDid = { "Stood up for {other}. Hands shook after. Worth it.", "Couldn't stand by and watch it done to {other}." };
        private static readonly string[] DefendDone = { "{other} stepped in for me. Didn't have to.", "Owe {other}. Plain as that." };
        private static readonly string[] FacedDid = { "Told {other} to his face. About time somebody did.", "{other} looked at me like murder. Let him." };
        private static readonly string[] RoundDid = { "{other} drank my health tonight. Worth every cent.", "Stood {other} a drink. Felt like a rich man, for once." };
        private static readonly string[] RoundDone = { "{other} stood me a drink. I'll remember that.", "Drank to {other}'s health. Meant it, too." };
        private static readonly string[] TownRound = { "{a} stood the whole saloon a round. Folks'll be friends with {a} till Sunday.", "{a} bought for the house, {b} says. Wish I'd been there." };
        private static readonly string[] FacedDone = { "{other} put their nose in my business. I won't forget it.", "{other}, playing the hero. In front of half the street." };

        private static readonly string[] CharityDid =
        {
            "{other} gave me money. Didn't even make me ask twice. There's good in folks yet.",
            "Bread tonight, thanks to {other}. Swore I'd never ask. Swear a lot of things."
        };
        private static readonly string[] CharityDone =
        {
            "Gave {other} a dollar. Felt good, then foolish, then good again.",
            "{other} looked so ashamed to ask. I know that look. Wore it myself, once."
        };
        private static readonly string[] ScornDid =
        {
            "Asked {other} for a nickel. Got told to work. I'd work, if there was work.",
            "Won't ask {other} for anything again. Won't ask anybody. Rather go hungry."
        };
        private static readonly string[] ScornDone =
        {
            "{other} asked me for money, in the street. The nerve. ...Still thinking about it, though.",
            "Should've given {other} something. A nickel's nothing. Too late now."
        };
        private static readonly string[] DrunkDid =
        {
            "Don't remember much past the third glass. Woke up with sawdust in my mouth. Did I call {other} out?",
            "Folks keep grinning at me. Something about {other}, and the floor. I'll not ask."
        };
        private static readonly string[] DrunkDone =
        {
            "{other} wanted to fight me. Couldn't find his own feet. Almost felt sorry for him.",
            "Poor {other}. Drink'll do that. Somebody ought to take that bottle away."
        };

        // the other one has died since
        private static readonly string[] DeadFoe =
        {
            "Last thing I ever gave {other} was trouble. Now {other}'s dead, and I can't take it back.",
            "{other}'s in the ground, and I'm still angry. Angry at the dead. What does that make me?"
        };
        private static readonly string[] DeadFriend =
        {
            "{other}'s dead. I keep turning round to tell {other} things.",
            "Can't walk past where {other} used to stand. Go the long way now."
        };
        private static readonly string[] DeadKnown =
        {
            "Talked with {other} only the other day. Now {other}'s gone. That's how quick it is."
        };

        /// A thought about someone they had a scene with lately, by first name; about their death if they've died since.
        public static string Thought(SceneTie t, string id, string seed, bool otherDead = false)
        {
            if (otherDead)
                return Voice.Pick(seed + "tdead", t.Warmth < 0 ? DeadFoe : t.Warmth > 0 ? DeadFriend : DeadKnown).Replace("{other}", Person.FirstOf(t.Other(id)));
            if (t.Kind == "avoid" && t.Doer != null)
                return Voice.Pick(seed + "avoid", t.Did(id) ? AvoidDid : AvoidDone).Replace("{other}", Person.FirstOf(t.Other(id)));
            if (Trouble(t.Kind) && t.Doer != null)
            {
                bool did = t.Did(id);
                string[] trouble = t.Kind == "rough" ? (did ? RoughDid : RoughDone) : t.Kind == "lift" ? (did ? LiftDid : LiftDone)
                                 : t.Kind == "pickpocket" ? (did ? CaughtDid : CaughtDone) : t.Kind == "charity" ? (did ? CharityDid : CharityDone)
                                 : t.Kind == "scorn" ? (did ? ScornDid : ScornDone) : t.Kind == "help" ? (did ? HelpDid : HelpDone)
                                 : t.Kind == "passby" ? (did ? PassDid : PassDone) : t.Kind == "defend" ? (did ? DefendDid : DefendDone)
                                 : t.Kind == "faced" ? (did ? FacedDid : FacedDone) : t.Kind == "round" ? (did ? RoundDid : RoundDone) : (did ? DrunkDid : DrunkDone);
                return Voice.Pick(seed + "trouble", trouble).Replace("{other}", Person.FirstOf(t.Other(id)));
            }
            string[] pool = t.Kind == "brawl" ? Blows : t.Kind == "debt" ? Money : t.Kind == "makeup" ? MadeUp : t.Kind == "brushoff" ? Shrugged
                          : t.Kind == "comfort" || t.Kind == "cheer" || t.Kind == "apology" ? Kindness : t.Warmth < 0 ? Quarrel : Romantic(t.Kind) ? Sweet : Known;
            return Voice.Pick(seed + "tie", pool).Replace("{other}", Person.FirstOf(t.Other(id)));
        }

        // ---- what the town says ---------------------------------------------------------------------------

        private static readonly string[] TownHostile =
        {
            "{a} and {b} were shouting at each other in the street. Half of {town} heard it.",
            "{a} won't stand in the same room as {b} now. Ask either one why, you get a different story."
        };

        private static readonly string[] TownBlows =
        {
            "{a} and {b} came to blows. Over nothing, they say. It's never nothing.",
            "Somebody pulled {a} off {b} before it got worse. Blood on the boardwalk."
        };

        // {a} did it, {b} had it done to him
        private static readonly string[] TownRough =
        {
            "{a} grabbed {b} by the collar and put {b}'s face through the bar top. Barkeep's still picking up teeth.",
            "Did you see what {a} did to {b}? Never seen a man move that fast with his hands full."
        };
        private static readonly string[] TownCaught =
        {
            "{b} caught {a} with a hand in a coat pocket. In the open street. Some nerve.",
            "Watch your pockets round {a}. {b} found out the hard way."
        };
        private static readonly string[] TownDrunk =
        {
            "{a} called {b} out, drunk as a lord, and went down like a felled tree before a fist was raised.",
            "{a} wanted to fight {b}. Fought the floor instead. The floor won."
        };

        private static readonly string[] TownCharity =
        {
            "{b} gave {a} money in the street, plain as day. Folks will be lining up now.",
            "Saw {b} press a coin on {a}. Didn't think {b} had it in them."
        };
        private static readonly string[] TownScorn =
        {
            "{a} asked {b} for a nickel and got sent off like a stray dog.",
            "{b} wouldn't spare {a} a penny. And {b} with a gold watch chain."
        };

        private static readonly string[] TownHelp =
        {
            "{b} took a fall in the street and {a} was there before anybody. Helped {b} right up.",
            "Saw {a} pick {b} up off the boards. Dusted {b} off and all."
        };
        private static readonly string[] TownPassby =
        {
            "{b} fell flat in the street and {a} walked right past. Folks noticed.",
            "{a} stepped round {b} like a dropped hat. With half the street watching."
        };

        private static readonly string[] TownWarm =
        {
            "{a} has been seen walking with {b}. Folks talk. Folks always talk.",
            "{a} and {b}, stood close as two spoons. Give it a month."
        };

        private static readonly string[] TownMadeUp =
        {
            "{a} and {b} made it up, would you credit it. Shook on it outside the store."
        };

        // one of the two has died since: {a} is the one left, {b} the dead ({af}, {bf}: first names, said again)
        private static readonly string[] TownDeadFoe =
        {
            "Everybody knew {a} couldn't stand {b}. Now {bf}'s dead, and folks are careful what they say about it.",
            "They say {b} never got over that business with {a}. Now {bf}'s buried, and {af} won't speak of it."
        };
        private static readonly string[] TownDeadWarm =
        {
            "{a} was sweet on {b}, and now {bf}'s gone. Somebody ought to look in on {af}."
        };
        private static readonly string[] TownDeadMended =
        {
            "{a} and {b} only just made it up, and now {bf}'s gone. Glad they got to, I suppose."
        };

        /// The town's talk about a tie between two others, with both their full names; deadA/deadB: that one has died since.
        public static string TownThought(SceneTie t, string town, string seed, bool deadA = false, bool deadB = false)
        {
            if (deadA || deadB)
            {
                string left = deadB ? t.AName : t.BName, gone = deadB ? t.BName : t.AName;
                string[] dead = t.Kind == "makeup" ? TownDeadMended : Romantic(t.Kind) ? TownDeadWarm : TownDeadFoe;
                return Voice.Pick(seed + "ttd", dead).Replace("{af}", Person.FirstOf(left)).Replace("{bf}", Person.FirstOf(gone)).Replace("{a}", left).Replace("{b}", gone);
            }
            string[] pool = t.Kind == "brawl" ? TownBlows : t.Kind == "makeup" ? TownMadeUp : Romantic(t.Kind) ? TownWarm
                          : t.Kind == "rough" ? TownRough : t.Kind == "pickpocket" ? TownCaught : t.Kind == "drunk" ? TownDrunk
                          : t.Kind == "charity" ? TownCharity : t.Kind == "scorn" ? TownScorn : t.Kind == "help" ? TownHelp : t.Kind == "passby" ? TownPassby : t.Kind == "round" ? TownRound : TownHostile;
            // in the trouble lines {a} did it: the one who did, whichever way round the tie was first made
            bool swap = Trouble(t.Kind) && t.Doer != null && t.Doer == t.B;
            string an = swap ? t.BName : t.AName, bn = swap ? t.AName : t.BName;
            return Voice.Pick(seed + "tt", pool).Replace("{a}", an).Replace("{b}", bn).Replace("{town}", town ?? "town");
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return Quarrel.Concat(Blows).Concat(Money).Concat(Sweet).Concat(Known).Concat(Kindness).Concat(MadeUp).Concat(Shrugged)
                .Concat(RoughDid).Concat(RoughDone).Concat(LiftDid).Concat(LiftDone).Concat(CaughtDid).Concat(CaughtDone).Concat(DrunkDid).Concat(DrunkDone).Concat(AvoidDid).Concat(AvoidDone)
                .Concat(CharityDid).Concat(CharityDone).Concat(ScornDid).Concat(ScornDone).Concat(TownCharity).Concat(TownScorn)
                .Concat(TownRough).Concat(TownCaught).Concat(TownDrunk).Concat(HelpDid).Concat(HelpDone).Concat(PassDid).Concat(PassDone).Concat(TownHelp).Concat(TownPassby).Concat(DefendDid).Concat(DefendDone).Concat(FacedDid).Concat(FacedDone).Concat(RoundDid).Concat(RoundDone).Concat(TownRound)
                .Concat(DeadFoe).Concat(DeadFriend).Concat(DeadKnown)
                .Concat(TownHostile).Concat(TownBlows).Concat(TownWarm).Concat(TownMadeUp)
                .Concat(TownDeadFoe).Concat(TownDeadWarm).Concat(TownDeadMended);
        }
    }
}
