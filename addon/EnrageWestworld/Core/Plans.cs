// Plans that run over days (the NPC-life run, step 6; Lex, 2026-10-30). What someone means to do about another person
// over the next few game days, born from what already lies between them: a wrong done to them lately (get even), a
// kindness (repay it), a debt to a real creditor (pay it), a sweetheart (court). A plan sends them to find that person
// when both are near the player, leans the street's reasons toward the scene it wants, and is done when that scene
// plays between them, or let go when its days run out. Kept in the save. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Plan
    {
        [DataMember(EmitDefaultValue = false)] public string HolderId, HolderName;
        [DataMember(EmitDefaultValue = false)] public string Kind;          // geteven, repay, paydebt, court
        [DataMember(EmitDefaultValue = false)] public string TargetId, TargetName;
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public long Made, Due;      // game minutes
        [DataMember(EmitDefaultValue = false)] public bool Done;
        [DataMember(EmitDefaultValue = false)] public string Outcome;      // the scene that settled it, or "let go"
        [DataMember(EmitDefaultValue = false)] public bool Gift;           // something picked out at the store to give (court, repay)
    }

    public static class Plans
    {
        public const int MostEach = 2;
        private static readonly HashSet<string> Wrongs = new HashSet<string> { "rough", "brawl", "mugging", "pickpocket", "drunk", "scorn" };
        private static readonly HashSet<string> Kindnesses = new HashSet<string> { "comfort", "help", "defend", "tend", "carried", "charity", "round" };

        /// What a plan wants played between the two: the scene kinds that settle it.
        public static bool Settles(string plan, string scene)
        {
            switch (plan)
            {
                case "geteven": return scene == "argue" || scene == "brawl" || scene == "standoff" || scene == "x:quarrel" || scene == "apology" || scene == "snap";
                case "repay": return scene == "thanks" || scene == "round" || scene == "comfort";
                case "paydebt": return scene == "debt" || scene == "x:debt";
                case "court": return scene == "flirt" || scene == "court" || scene == "sweet" || scene == "propose" || scene == "x:sweet" || scene == "x:courting" || scene == "mend" || scene == "tiff";
                default: return false;
            }
        }

        /// The scene a plan starts when the two are together and nothing else between them calls for one.
        public static string SceneFor(string plan)
        {
            switch (plan)
            {
                case "geteven": return "argue";
                case "repay": return "thanks";
                case "paydebt": return "debt";
                default: return "flirt";
            }
        }

        public static string Words(Plan p)
        {
            string t = Person.FirstOf(p.TargetName ?? "someone");
            switch (p.Kind)
            {
                case "geteven": return "get even with " + t;
                case "repay": return "thank " + t;
                case "paydebt": return "pay " + t + " back";
                default: return "court " + t;
            }
        }

        public static Plan Active(List<Plan> plans, string a, string b)
        {
            if (plans == null || a == null || b == null)
                return null;
            return plans.FirstOrDefault(p => !p.Done && ((p.HolderId == a && p.TargetId == b) || (p.HolderId == b && p.TargetId == a)));
        }

        /// New plans from the ties and the debt links (one a holder, target and kind; two at most a person); old ones let go.
        /// known: whether an id is someone kept (a plan for someone made on the spot and never kept would go nowhere).
        /// Returns the plans made and let go, for the log.
        public static List<string> Make(List<Plan> plans, List<SceneTie> ties, List<WorryLink> links, long now, Func<string, bool> known)
        {
            var said = new List<string>();
            foreach (Plan p in plans.Where(p => !p.Done && now > p.Due))
            {
                p.Done = true;
                p.Outcome = "let go";
                said.Add("let go: " + p.HolderName + " meant to " + Words(p));
            }
            plans.RemoveAll(p => p.Done && now - p.Due > 7 * 1440L);   // the record of a week back is enough
            Action<string, string, string, string, string, string, long> add = (holder, hName, kind, target, tName, town, days) =>
            {
                if (holder == null || target == null || holder == target || !known(holder) || !known(target))
                    return;
                if (plans.Any(p => p.HolderId == holder && p.TargetId == target && p.Kind == kind && (!p.Done || now - p.Made < 3 * 1440L)))
                    return;   // one of these at a time, and not again straight after
                if (plans.Count(p => p.HolderId == holder && !p.Done) >= MostEach)
                    return;
                var np = new Plan { HolderId = holder, HolderName = hName, Kind = kind, TargetId = target, TargetName = tName, Town = town, Made = now, Due = now + days * 1440L };
                plans.Add(np);
                said.Add(hName + " means to " + Words(np) + " (" + days + " days)");
            };
            if (ties != null)
                foreach (SceneTie t in ties)
                {
                    if (t.Kind == null || now - t.At < 0 || now - t.At > 3 * 1440L)
                        continue;
                    if (Wrongs.Contains(t.Kind) && t.Warmth <= -2 && t.Doer != null)
                    {
                        string done = t.Doer == t.A ? t.B : t.A, doneName = t.Doer == t.A ? t.BName : t.AName, doerName = t.Doer == t.A ? t.AName : t.BName;
                        add(done, doneName, "geteven", t.Doer, doerName, t.Town, 4);
                    }
                    else if (Kindnesses.Contains(t.Kind) && t.Warmth >= 2)
                    {
                        // the one helped: B in a tending or a carrying home, else whoever didn't do it
                        string helper = t.Doer ?? t.A;
                        string helped = helper == t.A ? t.B : t.A, helpedName = helper == t.A ? t.BName : t.AName, helperName = helper == t.A ? t.AName : t.BName;
                        add(helped, helpedName, "repay", helper, helperName, t.Town, 5);
                    }
                    else if (Ties.Sweethearts(t) && t.Warmth > 0)
                    {
                        add(t.A, t.AName, "court", t.B, t.BName, t.Town, 3);
                    }
                }
            if (links != null)
                foreach (WorryLink l in links)
                    if (l.Kind == "debt" && !l.Ended && now - l.At >= 0 && now - l.At < 8 * 1440L)
                        add(l.Id, l.Name, "paydebt", l.OtherId, l.OtherName, l.Town, 4);
            return said;
        }

        /// A thought of the plan's, as they go about the day.
        public static string Thought(Plan p, string seed)
        {
            string t = Person.FirstOf(p.TargetName ?? "him");
            switch (p.Kind)
            {
                case "geteven":
                    return Voice.Pick(seed, t + " thinks it's over. It isn't.", "I'll find " + t + ". Today, or tomorrow.", "Every time I close my eyes I see " + t + "'s face.",
                                      "Not letting " + t + " walk away from what he did.", "One of these days, " + t + ". One of these days.");
                case "repay":
                    return Voice.Pick(seed, "I owe " + t + ". Ought to say so.", "Never thanked " + t + " proper. I will.", t + " didn't have to help. I won't forget it.",
                                      "Must find " + t + " and do something for them.");
                case "paydebt":
                    return Voice.Pick(seed, "I'll have " + t + "'s money. Somehow.", "If I see " + t + ", I'll pay what I can.", t + " has been patient. Patience runs out.",
                                      "Owe " + t + ". Owe the whole town, near enough.");
                default:
                    return Voice.Pick(seed, "Maybe I'll see " + t + " today.", "Put on the good shirt. In case " + t + " is about.", "Can't stop thinking of " + t + ".",
                                      "If " + t + " is in town, I'll find a reason to pass by.");
            }
        }

        /// At the store, picking out something to give.
        public static string GiftThought(Plan p, string seed)
        {
            string t = Person.FirstOf(p.TargetName ?? "them");
            if (p.Kind == "court")
                return Voice.Pick(seed, "Ribbon, or the lemon drops? " + t + " likes a sweet.", "Too dear, the good one. I'll take it anyway.",
                                  "Something for " + t + ". Nothing that says too much.", "Peppermints. And a card, if I can find the words.");
            return Voice.Pick(seed, "A tin of tobacco for " + t + ". Small, but it's something.", "Coffee, the good kind. " + t + " won't refuse that.",
                              "Can't repay it in full. This'll have to start it.", "A jar of preserves for " + t + "'s table.");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string k in new[] { "geteven", "repay", "paydebt", "court" })
                for (int i = 0; i < 12; i++)
                    yield return Thought(new Plan { Kind = k, TargetName = "Wade Hollis" }, k + i);
            foreach (string k in new[] { "repay", "court" })
                for (int i = 0; i < 8; i++)
                    yield return GiftThought(new Plan { Kind = k, TargetName = "Wade Hollis" }, k + "g" + i);
        }
    }
}
