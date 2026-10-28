// The reactions rebuilt on the five-step mapping (the mind design's step 5, second half; night run 2026-10-28, item 13;
// behind [Mind] MindLive, off: exactly Reactions.Listener as before). With the mind on, the one spoken to in a street
// scene takes the line as an event (a threat is a challenge, a jeer an insult, a cheer a kindness, a plea a beggar's
// need), appraises it by their traits (Appraisal.Of), the rules of 1899 decide what shows of it (Display.Shows), and the
// catalogue of what the game can show (Catalogue.For) picks a gesture, read back as one of today's reaction kinds, so the
// same clips play. Masked: nothing shows (the thought alone). Plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public static partial class Reactions
    {
        /// What the one spoken to does, from the mind: the feeling the line gives them and what shows of it (out), and
        /// the reaction kind to play (null: nothing to see, or nothing the mind names: the caller falls back to the old
        /// table then). `place` as the engine reads it (saloon, church...; null the street), `witnesses` the people
        /// about, `neighbours` whether this is their own town, `law` a lawman standing by.
        public static string Mapped(string tone, Traits t, bool woman, string place, int witnesses, bool neighbours, bool law, bool drunk, int wakeStage, string seed,
                                    out Feeling feeling, out string shows)
        {
            return Mapped(tone, t, woman, place, witnesses, neighbours, law, drunk, wakeStage, false, 0, seed, out feeling, out shows);
        }

        /// The same, with the pair's tie: `known` whether they have one, `warm` its warmth (a neighbour's jeer is taken
        /// differently from a stranger's: a woman cuts a stranger and talks of a neighbour; blame falls lighter on a friend).
        public static string Mapped(string tone, Traits t, bool woman, string place, int witnesses, bool neighbours, bool law, bool drunk, int wakeStage, bool known, int warm, string seed,
                                    out Feeling feeling, out string shows)
        {
            feeling = null;
            shows = null;
            if (tone == null)
                return null;
            if (tone == HurtTone)
                return "hurt";   // a face into the bar: the body answers before the mind does
            AppraisalEvent e = EventOf(tone, woman, place, witnesses, neighbours, law, drunk, wakeStage);
            if (e != null && known)
            {
                e.Doer = "known";
                e.WarmDoer = warm;
            }
            if (e == null)
                return null;
            feeling = Appraisal.Of(e, t);
            if (feeling.Kind == "none")
                return null;
            shows = Display.Shows(feeling, e, t);
            if (shows == "masked")
                return null;
            if (shows == "turned")
                return "bristle";   // fear shown as bluster (a timid man in a saloon): he squares up
            string kind = KindOf(feeling, woman, seed);
            return shows == "softened" ? Softened(kind, feeling.Kind) : kind;
        }

        /// A line's tone as an event the appraisal knows: the speaker is a stranger of the scene unless the caller says
        /// they're known (Mapped's `known`); whoever is spoken to is the one it's done to.
        public static AppraisalEvent EventOf(string tone, bool woman, string place, int witnesses, bool neighbours, bool law, bool drunk, int wakeStage)
        {
            string kind;
            bool doerDrunk = false;
            switch (tone)
            {
                case "mock": case "shame":
                    kind = "insult"; break;                   // laughed at, shamed: a coarse word before others
                case "won":
                    kind = "pratfall"; break;                 // beaten, and the winner crowing: the loss is his own doing
                case "angry": case "threat": case "storm": case "thief": case "fight":
                    kind = "challenge"; break;                // squared up to
                case "no": case "tut": case "replybad":
                    kind = "slight"; break;                   // a refusal, a tut, a sharp answer: a slight, no threat in it
                case "drunk":
                    kind = "challenge"; doerDrunk = true; break;
                case "cheer": case "thanks": case "yes": case "welcome": case "flatter": case "familiar": case "greet": case "again": case "sorry":
                    kind = "kindness"; break;                 // a good word, a cheer, a thank-you, an apology
                case "plead": case "help":
                    kind = "beg"; break;                      // the other's need put to them (a beggar's ask is the nearest the appraisal has)
                case "scared": case "chatbad":
                    kind = "beg"; break;                      // the other's fright: pity from the kind, contempt from the cold (the appraisal has no "someone else's fright" of its own yet)
                default:
                    return null;                              // the everyday, the odd: the old table's shrug, nod or puzzled look
            }
            return new AppraisalEvent
            {
                Kind = kind, Role = "self", Doer = "stranger", Place = place ?? "street", Woman = woman, WomenPresent = true, Law = law, Drunk = drunk, DoerDrunk = doerDrunk,
                Company = witnesses <= 0 ? "alone" : neighbours ? "neighbours" : "strangers", Witnesses = Math.Max(0, witnesses), WakeStage = wakeStage, Armed = false
            };
        }

        /// The reaction kind for a feeling that shows: the catalogue must hold something proven for the feeling (or its
        /// urge) at that strength for this sex; then the kind is read from the urge first (what they mean to do), else
        /// from the feeling. The clips are today's eight kinds, so the same clips play.
        public static string KindOf(Feeling f, bool woman, string seed)
        {
            if (f == null || f.Kind == "none")
                return null;
            bool any = false;
            foreach (Expression x in Catalogue.For(f.Kind, f.Urge, f.Strength, woman, true))
                if (x.Channel == "gesture")
                {
                    any = true;
                    break;
                }
            if (!any)
                return null;
            if ((f.Kind == "shame" || f.Kind == "guilt") && f.Urge != "dismiss")
                return "shamed";   // shame's getting away is the eyes down and away, not a flinch (the review)
            return KindOfUrge(f.Urge) ?? KindOfFeeling(f.Kind);
        }

        /// What they mean to do, as a kind: squaring up bristles, getting away flinches, a turned back dismisses...
        public static string KindOfUrge(string urge)
        {
            switch (urge)
            {
                case "confront": case "intervene": return "bristle";
                case "avoid": case "freeze": return "flinch";
                case "dismiss": case "disparage": return "dismiss";
                case "hide": case "withdraw": case "amends": return "shamed";
                case "repay": case "share": case "display": return "nod";
                case "watch": case "help": return "puzzled";
                case "rest": return "shrug";
                default: return null;
            }
        }

        public static string KindOfFeeling(string feeling)
        {
            switch (feeling)
            {
                case "anger": return "bristle";
                case "fear": return "flinch";
                case "shame": case "guilt": case "grief": return "shamed";
                case "contempt": case "envy": return "dismiss";
                case "gratitude": case "joy": case "relief": case "pride": return "nod";
                case "pity": return "puzzled";
                default: return null;
            }
        }

        /// A feeling shown softened plays one step down: anger a head-shake, fear a puzzled look, the rest as they are.
        public static string Softened(string kind, string feeling)
        {
            if (kind == "bristle")
                return "dismiss";
            if (kind == "flinch" && feeling == "fear")
                return "puzzled";
            return kind;
        }

        /// A catalogue gesture read back as a reaction kind by its clip's name (kept for the tests and the log).
        public static string KindOfGesture(string name)
        {
            if (name == null)
                return null;
            if (name.Contains("look_down")) return "shamed";
            if (name.Contains("might_flee") || name.Contains("surprised_gasp")) return "flinch";
            if (name.Contains("aggressive_taunt") || name.Contains("spit") || name.Contains("neutral_point") || name.Contains("angry_victimised") || name.Contains("negative_dismiss")) return "bristle";
            if (name.Contains("head shake") || name.Contains("headshake")) return "dismiss";
            if (name.Contains("headnod") || name.Contains("headonod") || name.Contains("hat_tip") || name.Contains("wave") || name.Contains("happy_cheer") || name.Contains("happy_laugh")) return "nod";
            if (name.Contains("disbelief") || name.Contains("confused")) return "puzzled";
            if (name.Contains("shrug")) return "shrug";
            if (name.Contains("wince")) return "hurt";
            return null;
        }
    }
}
