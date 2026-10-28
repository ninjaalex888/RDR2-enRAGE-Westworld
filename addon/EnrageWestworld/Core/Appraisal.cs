// Appraisal and emotion: the NPC mind's fast layer (the mind design, step 4; daytime run 2026-10-27, shadow mode).
// Each person involved in something, or who saw it, judges it for themselves (appraisal theory: Lazarus; the OCC model
// of Ortony, Clore and Collins; Scherer's checks): does it matter to me, is it good or bad for me, did it happen or
// might it, did I see it coming, who caused it and did they mean it, can I cope with it, is it right by my values, who
// saw it, and whom it happened to and how I feel about them. Their traits weigh each answer (anxiety sees more threat,
// low trust more intent, assertiveness more power to cope). The answers name a feeling, its strength and the urge it
// brings (anger to confront, fear to avoid or freeze, shame to hide, guilt to make amends, grief to withdraw, pity to
// help, contempt to dismiss, gratitude to repay, envy to disparage, pride to display, joy to share, relief to rest).
// Whether it shows is the display rules' business (step 5). Pure: the same event and person give the same feeling.
// The vignettes (daytime\harness\mindtest\vignettes.txt, Lex to review) are what it's held to. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// What happened, as the game can tell it (the vignettes' facts line).
    public sealed class AppraisalEvent
    {
        public string Kind;              // shove theft death debtcall kindness brawl slight goodnews lawgrab challenge pratfall
                                         // rivalluck lift revived insult robbed rescued killing refused praised beg
        public string Role;              // self (it happened to them), doer, kin, friend, onlooker
        public string Doer;              // player stranger known lawman kin self nobody
        public int WarmDoer, WarmTarget; // -3..3: toward whoever did it, toward whomever it happened to
        public string Place;             // saloon street church store road home
        public bool Drunk, DoerDrunk, Woman, WomenPresent, Law, Armed;
        public string Company;           // friends neighbours strangers alone
        public int Witnesses, Doers = 1, WakeStage;
        public string DoerName, TargetName;   // for the log: whom the feeling is about
        public string DoerId, TargetId;       // the same by id ("player" for him; null unknown), for the thought's words
    }

    /// The feeling named, how strong, and the urge.
    public sealed class Feeling
    {
        public static readonly string[] Kinds = { "fear", "anger", "shame", "guilt", "grief", "pride", "gratitude", "envy", "pity", "contempt", "relief", "joy" };
        public string Kind;          // one of Kinds, or "none"
        public float Intensity;      // 0..1
        public string Urge;
        public string About;         // whom it's about (a name, "you" for the player), or null
        public string AboutId;       // the same by id ("player"), or null; DoerId: whoever did it, for a feeling about oneself
        public string DoerId;
        public string Why;           // the event kind
        public string Shows;         // what the face showed of it, when the game side worked it out (Display.Shows: shown, softened, masked, turned), else null
        public float[] All;          // every feeling's intensity, in Kinds order (for the log and the tests)

        /// mild, moderate, strong, or "-" for none.
        public string Strength
        {
            get { return Kind == "none" ? "-" : Appraisal.StrengthOf(Intensity); }
        }
    }

    public static class Appraisal
    {
        public const float Felt = 0.12f, Moderate = 0.35f, Strong = 0.65f;

        public static string StrengthOf(float x)
        {
            return x < Felt ? "-" : x < Moderate ? "mild" : x < Strong ? "moderate" : "strong";
        }

        // what each kind of event is, seen from the one it happened to: how bad (harm), how it lowers them before others
        // (status), a loss of someone (loss), how good (gain), the danger to those near (threat), how wrong the doer's
        // act is (wrong), whether it was meant (intent), whether it ends something feared (feared), whether it's of
        // their own doing (self), how much the one it happened to needs help (need), how much the one who did it does
        // (doerNeed: a beggar); `Own`: how far a loss of face reflects on them (their own failing, which is shame)
        // rather than on whoever did it (which is anger); `Money`: harm to what they have, which the anxious and the
        // grasping fear losing
        private sealed class Meaning
        {
            public float Harm, Status, Loss, Gain, Threat, Wrong, Intent, Feared, Need, DoerNeed, Uncanny, Own, Money;
            public bool Self;
        }

        private static readonly Dictionary<string, Meaning> Meanings = new Dictionary<string, Meaning>
        {
            { "shove", new Meaning { Harm = 0.35f, Status = 0.55f, Threat = 0.3f, Wrong = 0.6f, Intent = 1f, Need = 0.4f, Own = 0.2f } },
            { "theft", new Meaning { Harm = 0.5f, Status = 0.1f, Threat = 0.3f, Wrong = 0.75f, Intent = 1f, Need = 0.3f, Money = 0.5f } },
            { "death", new Meaning { Loss = 1f } },
            { "debtcall", new Meaning { Harm = 0.5f, Status = 0.75f, Threat = 0.1f, Wrong = 0.2f, Intent = 1f, Own = 0.65f, Money = 0.6f } },
            { "kindness", new Meaning { Gain = 0.45f, Status = 0.2f, Intent = 1f, Own = 1f } },
            { "brawl", new Meaning { Harm = 0.5f, Threat = 0.3f, Wrong = 0.3f, Intent = 1f, Need = 0.4f } },   // two men at it: neither all to blame
            { "slight", new Meaning { Status = 0.55f, Wrong = 0.5f, Intent = 1f, Own = 0.2f } },
            { "goodnews", new Meaning { Gain = 0.9f, Feared = 0.85f } },
            { "lawgrab", new Meaning { Harm = 0.3f, Status = 0.45f, Threat = 0.4f, Wrong = 0.45f, Intent = 1f, Own = 0.25f } },
            { "challenge", new Meaning { Status = 0.5f, Threat = 0.45f, Wrong = 0.5f, Intent = 1f, Own = 0.2f } },
            { "pratfall", new Meaning { Harm = 0.1f, Status = 0.55f, Self = true, Own = 1f } },
            { "rivalluck", new Meaning { Gain = 0.6f } },
            { "lift", new Meaning { Gain = 0.4f, Wrong = 0.7f, Self = true } },
            { "revived", new Meaning { Threat = 0.5f, Uncanny = 1f } },
            { "insult", new Meaning { Status = 0.55f, Wrong = 0.6f, Intent = 1f, Threat = 0.2f, Own = 0.2f } },
            { "robbed", new Meaning { Harm = 0.7f, Threat = 1f, Wrong = 0.9f, Intent = 1f, Money = 0.7f } },
            { "rescued", new Meaning { Gain = 1f, Feared = 1f, Intent = 1f } },
            { "killing", new Meaning { Harm = 1f, Loss = 1f, Threat = 1f, Wrong = 1f, Intent = 1f, Need = 0.8f } },
            { "refused", new Meaning { Harm = 0.3f, Status = 0.6f, Intent = 1f, Own = 0.85f } },
            { "praised", new Meaning { Gain = 0.6f, Self = true } },
            { "beg", new Meaning { DoerNeed = 0.55f, Harm = 0.05f } },
            { "heard", new Meaning { Threat = 0.55f } },   // a shot or a shout nearby, nothing seen: fright only
            { "uncanny", new Meaning { Threat = 0.45f, Uncanny = 1f } },   // the night visitor seen: a pale figure over a body, gone when looked at (night 2026-10-28)
        };

        /// The kinds of event it knows.
        public static IEnumerable<string> EventKinds
        {
            get { return Meanings.Keys; }
        }


        // scene kinds where one did it to the other (the tie's Doer says which: a scene's cast is by who speaks first,
        // not by who did what); the rest are mutual (a quarrel, a brawl, a standoff: each did it to the other)
        private static readonly string[] OneSided = { "rough", "pickpocket", "scorn", "charity", "help", "passby", "faced", "defend", "round", "settled", "apology", "comfort", "cheer", "debt", "x:debt", "brushoff", "drunk" };
        private static readonly string[] Mutual = { "brawl", "argue", "x:quarrel", "snap", "x:snap", "standoff" };

        /// What a recorded happening (WorldEvents) is, as an event this person appraises, by their part in it: the one it
        /// was done to, one with a warm tie to them, or an onlooker. Null when it's nothing the appraisal knows, nothing
        /// to them, or nothing they could know. `doerId`: who really did it (the pair's tie's Doer, from the game side);
        /// for a one-sided scene with no known doer, nothing (it can't tell who did what). `heardOnly`: they only heard it
        /// (a shot, a shout): no face to be angry at, nobody to grieve yet (word takes time), only fright. Killings are
        /// felt from the death the ledger records, not again from the deed. Nobody knows who lifted a purse. `warmth(a, b)`:
        /// their tie, -3..3. `sawCount`: how many saw it (not those who only heard, not the player).
        public static AppraisalEvent From(Happening h, string id, bool woman, string home, Func<string, string, int> warmth, string doerId = null, bool heardOnly = false, int sawCount = -1)
        {
            if (h == null || h.Kind == null || id == null)
                return null;
            string k = h.Kind.StartsWith("scene:") ? h.Kind.Substring(6) : h.Kind, kind;
            bool scene = h.Kind.StartsWith("scene:"), mutual = scene && Array.IndexOf(Mutual, k) >= 0, oneSided = scene && Array.IndexOf(OneSided, k) >= 0;
            string doer = h.DoerId, target = h.TargetId;
            if (oneSided)
            {
                if (doerId == null || doerId != h.DoerId && doerId != h.TargetId)
                    return null;   // who did it to whom unknown: nothing rather than the wrong way round
                doer = doerId;
                target = doerId == h.DoerId ? h.TargetId : h.DoerId;
            }
            else if (mutual && (id == h.DoerId || id == h.TargetId))
            {
                doer = id == h.DoerId ? h.TargetId : h.DoerId;   // each did it to the other: the other is "the doer"
                target = id;
            }
            else if (mutual && warmth != null && h.DoerId != null && h.TargetId != null && warmth(id, h.DoerId) > warmth(id, h.TargetId))
            {
                doer = h.TargetId;   // watching a friend in a mutual quarrel: the friend is the one it's done to
                target = h.DoerId;
            }
            if (id == doer && !mutual)
                return null;   // the doer's own side is the scenes' business
            if (h.Kind == "death" && id == target)
                return null;   // the dead feel nothing
            switch (k)
            {
                case "rough": case "deed:assault": kind = "shove"; break;
                case "brawl": case "standoff": case "drunk": kind = id == target ? "challenge" : "brawl"; break;
                case "pickpocket": case "deed:theft": case "deed:robbery": case "deed:looting": case "deed:horsetheft": kind = "theft"; break;
                case "argue": case "x:quarrel": case "snap": case "x:snap": case "brushoff": case "passby": case "faced": kind = "slight"; break;
                case "scorn": kind = id == target ? "refused" : "slight"; break;
                case "charity": case "help": case "comfort": case "cheer": case "round": case "settled": case "apology": case "defend": kind = "kindness"; break;
                case "debt": case "x:debt": kind = "debtcall"; break;
                case "death": kind = h.DoerId != null && h.DoerId != h.TargetId ? "killing" : "death"; break;
                case "encounter:execution":
                    // the men who hanged a man on the road: a killing seen when the captive died, nothing otherwise (saved
                    // by the player, he feels the rescue; the record's outcome says which: night 2026-10-28)
                    kind = ExecutionKind(h.Outcome, id == target);
                    break;
                case "encounter:visitor":
                    kind = id != "player" ? "uncanny" : null;   // whoever saw the figure over the body (the visitor's record names the player as its target)
                    break;
                default:
                    if (h.Kind.StartsWith("scene:court:") && h.Kind.Contains(":no"))
                        kind = id == target ? "refused" : null;
                    else
                        kind = null;   // lift (nobody knows who), deed killings (the death covers them), the rest
                    break;
            }
            if (kind == null)
                return null;
            if (heardOnly)
            {
                // only heard: a shot or a shout somewhere near, nothing seen
                if (kind != "killing" && kind != "brawl" && kind != "challenge" && kind != "shove")
                    return null;
                return new AppraisalEvent { Kind = "heard", Role = "onlooker", Doer = "nobody", Place = h.Place ?? "street", Woman = woman, Company = "strangers", Witnesses = Math.Max(0, sawCount), Armed = kind == "killing" || k == "standoff" };
            }
            if (kind == "uncanny")
            {
                doer = null;
                target = null;   // nobody did it to anybody: every witness is an onlooker of it
            }
            int toTarget = id != target && target != null && warmth != null ? warmth(id, target) : 0, toDoer = id != doer && doer != null && warmth != null ? warmth(id, doer) : 0;
            string role = id == target ? "self" : toTarget > 0 ? "friend" : "onlooker";
            if (kind == "kindness" && role != "self" || kind == "refused" && role != "self" || kind == "debtcall" && role != "self")
                return null;   // someone else's kindness or trouble: nothing the appraisal names for an onlooker yet
            return new AppraisalEvent
            {
                Kind = kind, Role = role, Doer = doer == "player" ? "player" : toDoer != 0 ? "known" : doer == null ? "nobody" : "stranger",
                WarmDoer = toDoer, WarmTarget = toTarget, Place = h.Place ?? "street", Woman = woman, WomenPresent = true,
                Company = home != null && home == h.Town ? "neighbours" : "strangers", Witnesses = sawCount >= 0 ? sawCount : h.Witnesses != null ? h.Witnesses.Count : 0,
                Armed = kind == "killing" || k == "standoff" || doer == "player", DoerDrunk = k == "drunk" && id == target, Doers = 1,
                DoerName = doer == h.DoerId ? h.DoerName : h.TargetName, TargetName = target == h.TargetId ? h.TargetName : h.DoerName,
                DoerId = doer, TargetId = target
            };
        }

        /// What an execution on the road is to someone by how it ended: "killing" when the captive died (the outcome
        /// names the death), "rescued" for the captive himself when the player saved him, else nothing. The dead feel nothing.
        public static string ExecutionKind(string outcome, bool captive)
        {
            if (outcome == null)
                return null;
            string o = outcome.ToLowerInvariant();
            bool died = o.Contains("died") || o.Contains("dead") || o.Contains("hanged") || o.Contains("shot") || o.Contains("killed") || o.Contains("drop");
            if (died)
                return captive ? null : "killing";
            bool saved = o.Contains("saved") || o.Contains("freed") || o.Contains("cut") || o.Contains("rescued") || o.Contains("drove");
            return saved && captive ? "rescued" : null;
        }

        /// How this person takes what happened. `t` may be null (a middling person).
        public static Feeling Of(AppraisalEvent e, Traits t)
        {
            var f = new Feeling { All = new float[Feeling.Kinds.Length], Why = e != null ? e.Kind : null };
            Meaning m;
            if (e == null || e.Kind == null || !Meanings.TryGetValue(e.Kind, out m))
            {
                f.Kind = "none";
                f.Urge = "none";
                return f;
            }
            float Z(int i) => t != null ? t.Z[i] : 0f;
            float anxiety = Z(Traits.Anxiety), angerT = Z(Traits.Anger), assert = Z(Traits.Assertiveness), compassion = Z(Traits.Compassion), trust = Z(Traits.Trust), order = Z(Traits.Order);
            float honesty = Z(Traits.Honesty), soc = Z(Traits.Sociability), piety = Z(Traits.Piety), defer = Z(Traits.Deference), risk = Z(Traits.Risk), control = Z(Traits.SelfControl);
            string role = e.Role ?? "onlooker";
            bool self = role == "self", doer = role == "doer", near = role == "kin" || role == "friend";
            // does it matter to me: all of it when it's mine; through how much I care for whoever it happened to (the
            // warm and the sociable care more for their own; the cold less)
            float bond = 0.08f * compassion + 0.06f * soc;
            float care = Clamp(role == "kin" ? 0.75f + 0.08f * e.WarmTarget + bond : role == "friend" ? 0.45f + 0.12f * e.WarmTarget + bond : 0f);
            float kinBond = e.Doer == "kin" ? Clamp(0.6f + 0.1f * e.WarmDoer + 0.1f * compassion + 0.08f * soc) : 1f;   // good or bad news of one's own
            float myHarm = self ? m.Harm : near ? m.Harm * care : 0f;
            float myStatus = self ? m.Status : near ? m.Status * care * 0.35f : 0f;
            float myLoss = self ? m.Loss : near ? m.Loss * care : 0f;
            float myGain = (self ? m.Gain : near ? m.Gain * care : 0f) * kinBond;
            float otherHarm = self || doer ? 0f : m.Harm + 0.5f * m.Status;   // what was done to someone else, in front of me
            // the danger to me: those present share it, the one it's aimed at most (a friend beside the man shot almost as
            // much); a drunk is less of one
            float threat = m.Threat * (self ? 1f : near ? (m.Loss > 0 ? 0.9f : 0.75f) : 0.55f) * (e.Armed ? 1f : 0.75f) * (e.Doers > 1 ? 1.15f : 1f) * (e.DoerDrunk ? 0.55f : 1f);
            // can I cope with it: my nerve and my standing against theirs
            float cope = 0.5f + 0.13f * assert - 0.13f * anxiety + 0.06f * risk + (e.Woman ? -0.06f : 0.04f)
                         + (e.Company == "friends" ? 0.1f : e.Company == "alone" ? -0.08f : 0f) + (e.Drunk ? 0.22f : 0f)
                         - (e.Armed ? 0.15f : 0f) - (e.Doer == "lawman" ? 0.12f : 0f) - (e.Doers > 1 ? 0.15f : 0f) + (e.DoerDrunk ? 0.18f : 0f);
            cope = Clamp(cope, 0.05f, 0.95f);
            // who caused it and did they mean it: blame, by how wrong it is by my values; low trust sees more intent; the
            // law's hand is less wrong to the law-abiding
            float wrong = m.Wrong * (1f + 0.1f * honesty) * (e.Doer == "lawman" ? 1f - 0.25f * defer : 1f) * (e.Place == "church" ? 1.15f : 1f)
                          * (e.Kind == "insult" && e.Woman && self ? 1.25f : 1f);   // a coarse word to a woman in the street: a graver wrong in 1899
            float blame = m.Intent * wrong * (1f - 0.12f * trust) * (e.WarmDoer > 0 ? 0.7f : e.WarmDoer < 0 ? 1.2f : 1f);
            // who saw it: before people who know them it weighs most (in church, before the whole congregation, more);
            // how much they care how they're seen
            float seen = Clamp(e.Witnesses / 6f) * (e.Company == "friends" || e.Company == "neighbours" ? 1f : e.Company == "strangers" ? 0.75f : 0.3f) * (e.Place == "church" ? 1.15f : 1f);
            float standing = 0.75f + 0.18f * anxiety + 0.12f * assert - 0.1f * honesty + 0.2f * soc + 0.1f * compassion + 0.2f * defer;   // the sociable, the kind and the conventional care most how they are seen
            float[] a = f.All;
            // anger: bad for me (or mine), someone to blame, and the power to stand up to it (anger needs it, fear is what
            // comes without it); the hot-tempered flare at any provocation, the steady less (Berkowitz)
            float provoked = m.Intent * (m.Status + m.Wrong) * 0.5f;
            float hot = 1f + 0.45f * angerT - 0.12f * control + 0.08f * assert;
            float power = 0.15f + 1.1f * cope;
            float angerSelf = (myHarm + myStatus * standing * (1f - 0.5f * m.Own)) * blame * power * hot * 1.55f
                              + (self ? provoked * 0.45f * Math.Max(0f, angerT) : 0f);
            float hotFor = 1f + 0.35f * angerT - 0.1f * control + 0.15f * trust;   // on a friend's behalf the loyal flare too
            float angerFor = near ? 0.9f * care * otherHarm * blame * power * hotFor * 1.6f : (0.5f + 0.15f * compassion + 0.1f * honesty + 0.1f * angerT) * otherHarm * blame * power * hot * 1.5f;
            a[1] = Math.Max(angerSelf, angerFor);
            // fear: danger to me I can't easily cope with; losing what little I have; the uncanny frightens everyone; a
            // wrong of my own that might come out
            float discovery = doer && m.Wrong > 0 ? m.Wrong * (e.Witnesses > 0 ? 0.6f : 0.25f) * (1f + 0.6f * anxiety - 0.15f * risk) : 0f;
            a[0] = threat * (1.2f - cope) * (1f + 0.5f * anxiety)
                   + (self ? m.Money * (0.5f + 0.3f * anxiety - 0.35f * honesty) * (1.2f - cope) : 0f)
                   + m.Uncanny * (0.55f + 0.08f * anxiety + 0.06f * Math.Min(4, e.WakeStage))
                   + discovery;
            // shame: lowered before others; by my own doing it's shame, by another's mostly anger, except for the anxious
            // and the meek (the more I care how I'm seen, the more)
            float ownPart = m.Own * standing, theirPart = (1f - m.Own) * Math.Max(0f, 0.2f + 0.12f * anxiety - 0.12f * assert);
            a[2] = myStatus * (0.35f + 0.65f * seen) * (ownPart + theirPart) * (1.25f - 0.5f * cope) * 1.2f;
            // guilt: a wrong of my own doing, by my conscience (kindness, honesty, faith, a respect for the rules); a debt
            // I can't pay too
            float conscience = 0.5f + 0.25f * honesty + 0.35f * compassion + 0.1f * piety + 0.1f * defer;
            a[3] = (doer && m.Wrong > 0 ? m.Wrong * Math.Max(0f, conscience) : 0f) + (self && e.Kind == "debtcall" ? 0.45f * Math.Max(0f, conscience + 0.2f * order) : 0f);
            // grief: losing someone I care for; seen done in front of me, sharper
            a[4] = myLoss * (0.75f + 0.15f * compassion + 0.1f * soc + (role == "kin" ? 0.08f : 0f)) * (m.Threat > 0 && m.Loss > 0 ? 1.25f : 1f);
            // pride: something good of my own doing, before others (the forward and the vain the more)
            a[5] = m.Self && m.Gain > 0 && m.Wrong == 0 ? m.Gain * (0.35f + 0.65f * seen) * (0.75f + 0.25f * assert - 0.15f * honesty + 0.1f * soc) : 0f;
            // gratitude: good to me from someone who meant it
            a[6] = !m.Self && m.Intent > 0 && myGain > 0 ? myGain * (0.75f + 0.12f * trust + 0.12f * compassion + 0.08f * honesty) : 0f;
            // envy: a good thing to someone I don't care for, that I want (the grasping and the orderly strivers the more)
            a[7] = e.Kind == "rivalluck" ? m.Gain * 1.25f * (e.WarmDoer < 0 ? 1f : 0.5f) * (0.45f - 0.2f * honesty + 0.06f * angerT + 0.05f * anxiety + 0.08f * assert + 0.15f * order) : 0f;   // low honesty-humility and a fretful, angry nature (envy's company in the studies)
            // pity: someone else's trouble they didn't deserve, the kinder the more; a beggar asking; a drunk making a fool of himself
            float need = m.Need * (self ? 0f : near ? 1f : 0.7f) + m.DoerNeed + (e.DoerDrunk ? 0.3f : 0f);
            a[8] = need * (0.35f + 0.25f * compassion + (near ? 0.25f * care : 0f));
            // contempt: someone in the wrong, or weak, whom I don't fear and don't care for; the cold and the proud the
            // more, the kind less; the lawless have it for the law; the orderly for a beggar
            float weak = (e.DoerDrunk ? 0.45f : 0f) + (e.Kind == "beg" ? 0.35f : 0f) + blame * (self ? 0.7f : 0.6f);
            a[9] = weak * (1f - threat * 0.7f) * (role == "onlooker" ? 0.85f + 0.1f * soc + 0.05f * assert : 1f) * Math.Max(0f, 0.3f + 0.18f * assert - 0.15f * compassion - 0.08f * soc - 0.08f * honesty - (e.Doer == "lawman" ? 0.15f * defer : 0f)
                   + (e.Kind == "beg" ? 0.1f * order - 0.05f * risk : 0f)) * 1.6f;
            // relief: something feared, ended (the anxious the more)
            a[10] = m.Feared * myGain * (1f + 0.25f * anxiety);
            // joy: good to me or mine (a kindness done me is mostly gratitude); a thief's take, for the grasping and the reckless
            a[11] = myGain * (0.6f + 0.12f * soc - 0.08f * anxiety) * (m.Feared > 0 ? 0.95f : 1f) * (m.Intent > 0 && !m.Self ? 0.6f : 1f)
                    + (doer && m.Gain > 0 ? m.Gain * Math.Max(0f, 0.3f - 0.25f * honesty + 0.1f * risk - 0.15f * compassion) : 0f);
            for (int i = 0; i < a.Length; i++)
                a[i] = Clamp(a[i]);
            int best = -1;
            float top = Felt;
            for (int i = 0; i < a.Length; i++)
                if (a[i] >= top)
                {
                    top = a[i];
                    best = i;
                }
            f.Kind = best < 0 ? "none" : Feeling.Kinds[best];
            f.Intensity = best < 0 ? 0f : a[best];
            f.About = f.Kind == "grief" || f.Kind == "pity" || f.Kind == "relief" ? e.TargetName : f.Kind == "shame" || f.Kind == "guilt" || f.Kind == "pride" ? null : e.DoerName;
            f.AboutId = f.Kind == "grief" || f.Kind == "pity" || f.Kind == "relief" ? e.TargetId : f.Kind == "shame" || f.Kind == "guilt" || f.Kind == "pride" ? null : e.DoerId;
            f.DoerId = e.DoerId;
            f.Urge = UrgeOf(f, e, m, role, standing, t);
            return f;
        }

        /// How strongly a stranger must feel it to step into someone else's fight before this many others: the bar rises
        /// with the crowd, so each one's own chance falls while the chance that somebody steps in rises (Philpot et al. 2020,
        /// 219 public fights on CCTV: at least one stepped in 90.9% of the time, 3.76 on average, the odds of help up 1.1 for
        /// each bystander; the authors read the individual's chance as falling). Friends and kin are not held by it.
        public static float CrowdBar(int witnesses)
        {
            return CrowdBase + CrowdRise * (float)Math.Log(1 + Math.Max(0, witnesses));
        }

        public const float CrowdBase = 0.10f, CrowdRise = 0.025f;   // set against the study in mindtest's town: 20 watching, somebody steps in about 9 times in 10

        /// The urge a feeling brings, here. A mild feeling moves people less: a glare, a turned shoulder.
        private static string UrgeOf(Feeling f, AppraisalEvent e, Meaning m, string role, float standing, Traits t)
        {
            float Z(int i) => t != null ? t.Z[i] : 0f;
            bool other = role != "self" && role != "doer", mild = f.Intensity < Moderate, strong = f.Intensity >= Strong;
            bool cold = Z(Traits.Compassion) < -0.6f && Z(Traits.Sociability) < -0.6f;   // the detached
            float fear = f.All[0];
            switch (f.Kind)
            {
                case "anger":
                    if (e.Kind == "refused")
                        return "disparage";   // running her down to his friends
                    if (role == "onlooker" && e.Armed && fear >= 0.7f * f.Intensity)
                        return "avoid";   // angry at an armed man, and afraid of him too: away (the CCTV study was of unarmed fights)
                    if (other)
                        return mild || role == "onlooker" && f.Intensity < CrowdBar(e.Witnesses) ? "watch" : e.Kind == "killing" ? "confront" : "intervene";   // on someone else's behalf, stepping in (at a killer, going for him); mildly, only watching hard
                    if (e.Woman && !strong && Z(Traits.Anger) < 0.5f)
                        return e.Doer == "known" ? "disparage" : "dismiss";   // a woman's anger in 1899: from a stranger she turns away; a neighbour she cuts and talks of to the others
                    if (!strong && (cold || e.DoerDrunk))
                        return "dismiss";   // a cold man's anger, or anger at a drunk: a turned back
                    return "confront";
                case "fear":
                    if (role == "doer")
                        return "hide";   // a wrong of their own that might come out
                    return strong && (e.Armed && (e.Kind == "robbed" || e.Kind == "killing" || e.Kind == "lawgrab") || e.Kind == "revived" || e.Kind == "uncanny") || e.Kind == "robbed" ? "freeze" : "avoid";
                case "shame":
                    if (m.Money > 0 && Z(Traits.Order) < -0.5f && Z(Traits.Deference) < -0.5f)
                        return "avoid";   // real trouble: the rootless leave town
                    if (mild && standing < 0.65f)
                        return "dismiss";   // a shrug from those who don't much care how they're seen
                    if (e.Kind == "refused")
                        return Z(Traits.Assertiveness) > 0.3f || Z(Traits.Honesty) < -0.5f ? "disparage" : "withdraw";   // the proud and the vain run her down; the rest go off
                    if (m.Own < 0.5f && fear >= 0.6f * f.Intensity)
                        return "avoid";   // shamed by someone they fear: away from them
                    return "hide";
                case "guilt":
                    return fear >= 0.8f * f.Intensity || Z(Traits.Honesty) < -0.5f ? "hide" : "amends";   // afraid of being found out, or too proud to own it
                case "grief":
                    return e.Kind == "killing" ? "help" : "withdraw";   // to the friend shot down in front of them
                case "pride":
                    if (mild && Z(Traits.Sociability) < -0.5f)
                        return "dismiss";
                    return Z(Traits.Assertiveness) > 0.3f ? "display" : "share";
                case "gratitude": return "repay";
                case "envy":
                    if (mild && Z(Traits.Compassion) > 0.5f)
                        return "none";   // a pang, let go
                    return mild && Z(Traits.Order) < -0.5f ? "dismiss" : "disparage";
                case "pity":
                    if (e.Kind == "brawl" || e.Kind == "shove")
                        return role == "onlooker" && f.Intensity < CrowdBar(e.Witnesses) ? "watch" : "intervene";   // a stranger's pity must clear the crowd's bar
                    return "help";
                case "contempt":
                    if (e.Woman && e.Doer == "known" && role == "self" && Z(Traits.Sociability) > -0.5f)
                        return "disparage";   // a woman slighted by a neighbour: her tongue, later, to the others
                    return mild && role == "onlooker" ? "watch" : "dismiss";   // someone else's business: a hard look
                case "relief": return m.Threat > 0 || e.Kind == "rescued" || Z(Traits.Anxiety) > 0.9f ? "rest" : "share";
                case "joy": return role == "doer" ? "rest" : "share";
                default:
                    if (role == "onlooker" && (m.Threat > 0 || m.Need > 0 || m.Harm > 0))
                        return "watch";
                    return role == "self" && (m.Status > 0 || m.Harm > 0) ? "dismiss" : "none";
            }
        }

        private static float Clamp(float v, float lo = 0f, float hi = 1f)
        {
            return v < lo ? lo : v > hi ? hi : v;
        }
    }
}
