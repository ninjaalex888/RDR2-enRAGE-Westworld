// Display rules for 1899 (the mind design, step 5; daytime run 2026-10-27, Core only, shadow mode). A feeling is felt
// whole (the thought always says what's really felt: that gap between face and mind is what makes people read as
// human); what shows is filtered by what this person, here, before these people, may let show: shown, softened, masked
// (kept off the face) or turned (shown as something else: fear as bluster). The rules, from the mind doc: men hide fear,
// above all before other men and in saloons, while anger may show; women of standing hide anger in public, while
// distress may show; everyone keeps quiet and still in church, at a graveside and before the law; anger is swallowed
// upward and let out downward; drink loosens every rule, and so does the Awakening, more with each stage. The detached
// keep nearly everything off their faces; a hot temper or a timid nature can't quite hold it; the proud keep their chin
// up. Pure; plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public static class Display
    {
        public static readonly string[] Ways = { "shown", "softened", "masked", "turned" };

        /// How much of this feeling shows on their face and in what they do.
        public static string Shows(Feeling f, AppraisalEvent e, Traits t)
        {
            if (f == null || e == null)
                return "shown";
            if (f.Kind == "none")
                return "masked";   // nothing to show
            float Z(int i) => t != null ? t.Z[i] : 0f;
            float control = Z(Traits.SelfControl), anger = Z(Traits.Anger), soc = Z(Traits.Sociability), compassion = Z(Traits.Compassion), anxiety = Z(Traits.Anxiety);
            float assert = Z(Traits.Assertiveness), honesty = Z(Traits.Honesty), defer = Z(Traits.Deference);
            bool loose = e.Drunk || e.WakeStage >= 3;                                   // drink and waking loosen every rule
            bool hushed = (e.Place == "church" || e.Place == "graveside") && !loose;     // still and quiet
            bool lawBy = e.Law && !loose;   // a lawman standing by hushes anger and contempt too (the doc: quiet before the law; the night review, 2026-10-28: the flag was set but never read)
            bool publicPlace = e.Witnesses > 0 && e.Company != "alone";
            bool cold = compassion < -0.6f && soc < -0.6f;                              // the detached
            bool hot = anger > 1f;                                                      // can't quite hold it
            bool proud = assert > 0.8f, vain = honesty < -1f, timid = anxiety > 0.8f;
            bool strong = f.Intensity >= Appraisal.Strong, mild = f.Intensity < Appraisal.Moderate;
            switch (f.Kind)
            {
                case "fear":
                    if (f.Urge == "hide")
                        return "masked";   // a wrong of their own: nothing must show
                    if (e.Doer == "lawman")
                        return strong ? "shown" : defer > 0f && honesty > -0.5f || timid ? "softened" : "masked";   // before the law the law-abiding let it show (it's expected); the sly keep it off their face
                    if (f.Urge == "freeze" && strong)
                        return proud || cold ? "masked" : "shown";   // frozen at a gun: only the proud and the cold keep their face
                    if (!e.Woman && publicPlace && !loose)
                        return timid && role(f, e) == "self" ? (e.Place == "saloon" ? "turned" : "softened") : "masked";   // a man before other men hides it; a timid one can't quite, and in a saloon he blusters
                    return strong || loose || timid ? "shown" : "softened";
                case "anger":
                    if (loose)
                        return "shown";
                    if (hushed || lawBy || e.Doer == "lawman")
                        return hot ? "softened" : "masked";   // church and the law: anger swallowed upward (a hothead only half manages)
                    if (role(f, e) == "onlooker" && mild)
                        return hot ? "shown" : cold || defer < -0.8f ? "masked" : "softened";   // someone else's business: a hard look
                    if (e.Woman && publicPlace)
                        return hot ? (strong ? "shown" : "softened") : "masked";   // a woman of standing keeps it off her face; a hot one can't quite
                    if (f.Urge == "disparage")
                        return "masked";   // saved for later, for the others
                    if (f.Urge == "dismiss")
                        return cold || control > 0.3f ? "masked" : "softened";   // a cold man's anger is a turned back
                    if (hot || proud || strong)
                        return "shown";
                    return "softened";
                case "shame":
                    if (hot || timid)
                        return "shown";   // the red face, the eyes down: they can't hide it
                    if (f.Urge == "dismiss")
                        return cold ? "masked" : e.Woman ? "softened" : "shown";
                    if (f.Urge == "avoid")
                        return e.Woman ? "softened" : "masked";   // gone before it can show (a woman's distress may)
                    if (proud || vain || cold || control > 0.6f)
                        return "masked";   // chin up
                    return "softened";
                case "guilt":
                    return f.Urge == "amends" && e.Witnesses > 0 ? "shown" : "masked";   // owning a wrong that's known; a secret one never shows
                case "grief":
                    if (e.Kind == "killing" && strong)
                        return "shown";   // a friend shot down in front of them: nobody holds that
                    if (e.Woman || loose)
                        return "softened";   // distress may show for a woman, quietly
                    return cold || proud || control > 0.6f ? "masked" : "softened";   // a man grieves where nobody sees
                case "pride":
                    if (hushed)
                        return "softened";
                    if (cold)
                        return "masked";
                    return proud || hot || vain ? "shown" : "softened";
                case "joy":
                case "relief":
                    if (role(f, e) == "doer")
                        return "masked";   // a thief's take
                    if (cold)
                        return "masked";
                    if (hushed)
                        return timid || defer < -0.8f ? "shown" : "softened";   // kept quiet in church (the timid and the careless can't)
                    if (proud && f.Kind == "relief")
                        return "softened";
                    return f.Kind == "joy" && timid ? "softened" : "shown";
                case "gratitude":
                    if (cold || proud)
                        return "masked";
                    return hot ? "softened" : "shown";
                case "envy":
                    return hot && !hushed ? "shown" : "masked";   // nobody owns to envy but a hothead
                case "contempt":
                    if (hushed || lawBy)
                        return "masked";
                    if (role(f, e) != "self")
                        return proud ? "shown" : f.Urge == "watch" || cold ? "masked" : "shown";
                    return proud || cold || e.Woman || hot || vain || defer < -0.8f ? "shown" : "softened";   // the cut is meant to be seen
                case "pity":
                    return f.Urge == "intervene" || !mild && compassion > 0.5f || defer < -0.8f ? "shown" : "softened";   // the kind show it once it's more than a pang; the careless always
                default:
                    return "shown";
            }
        }

        private static string role(Feeling f, AppraisalEvent e)
        {
            return e.Role ?? "onlooker";
        }
    }
}
