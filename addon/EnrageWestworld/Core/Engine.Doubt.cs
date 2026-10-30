// The Awakening read from the beliefs (the mind doc's step 10, second half: "the Awakening's doubts sit here"; night
// 2026-10-29, item 4; [Mind] MindWakes, on, gated by MindLive). The daily tick (Engine.Awakening.cs) chose who wakes and
// moved them on by a flat roll; now, for people the mind knows, what they have lived through weighs in: the shadow's
// moved delta on "things are as they seem" (the uncanny chips it, the revived dead break it, a good turn steadies it)
// leans the choice of who first stirs and the daily roll to wake a stage further, and the moment that belief gives way
// they stir at once ("slowly, then suddenly", the doc's beliefs layer). Read only from the shadow, whose state is in
// memory and goes with a rewind; what changes is the Awakening's own saved stage, written the way it always was
// (Advance). The mind never makes anyone's state: someone with none kept yet doesn't stir from it (the mind reads only
// people who already have an identity, and Store.Get is not the mind's to call). Pure; plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// The daily roll to wake a stage further, in percent, as it always was.
        public const int WakeChanceAsBefore = 60;

        private bool DoubtOn { get { return Cfg != null && Cfg.MindLive && Cfg.MindWakes && Cfg.WestWorld; } }

        /// How far what they've lived through has shaken "things are as they seem": the shadow's moved delta on that
        /// belief, turned round (above 0 shaken, below 0 steadied, -1..1); 0 when the mind isn't live, MindWakes is off, or
        /// the shadow holds nothing for them.
        public float Shaken(string id)
        {
            if (!DoubtOn || id == null || Shadow == null)
                return 0f;
            BeliefState b = Shadow.BeliefsPeek(id);   // never makes one
            return b == null ? 0f : -b.Moved[Beliefs.World];
        }

        /// The daily roll to wake a stage further, in percent: 60 as before, and with the mind's doubts from 40 (steadied)
        /// to 78 (shaken hard: the revived dead seen moves the belief 0.5, its cap). Half the first night's lean (the review
        /// of item 4: one sighting at 95% a day walked a man to awake in about four days on nothing more).
        public int WakeChance(string id)
        {
            float s = Shaken(id);
            if (s == 0f)
                return WakeChanceAsBefore;
            return Math.Max(40, Math.Min(78, WakeChanceAsBefore + (int)Math.Round(35f * s)));
        }

        /// The lean toward who first stirs, on top of their awareness (Hosts.Awareness, 0-100, and a roll of 25): up to 40
        /// for what shook them.
        public int WakeLean(string id)
        {
            return (int)Math.Round(40f * Math.Max(0f, Shaken(id)));
        }

        /// Whether they saw this person die, by their own memories (a death seen: Engine.Minds' "death" memory, how "saw",
        /// naming them). Read only.
        public static bool SawThemDie(PersonState st, string deadId)
        {
            if (st == null || st.Mind == null || deadId == null)
                return false;
            foreach (MindMemory m in st.Mind)
                if (m.Kind == "death" && m.How == "saw" && m.Who != null && m.Who.Contains(deadId))
                    return true;
            return false;
        }

        /// The appraisal of a buried man walking past: the revived dead seen by one who saw them die.
        public static AppraisalEvent RevivedSeen(Person who, string place, string town, int witnesses, int wakeStage, string deadId, string deadName)
        {
            return new AppraisalEvent
            {
                Kind = "revived", Role = "onlooker", Doer = "nobody", Place = place ?? "street", Woman = who != null && who.Woman, WomenPresent = true,
                Company = who != null && who.Home != null && who.Home == town ? "neighbours" : "strangers", Witnesses = Math.Max(0, witnesses), WakeStage = wakeStage,
                TargetName = deadName, TargetId = deadId
            };
        }

        /// "Things are as they seem" has just given way in them: someone asleep stirs at once; someone already waking goes
        /// a stage further if a game day has passed since their last; the fully awake, the dead, story characters and anyone
        /// with no state kept yet, nothing. Returns the new stage, or -1 for nothing (`why` says why).
        public int StirFromDoubt(Person p, long now, out string why)
        {
            why = null;
            if (!DoubtOn || Cfg.AwakeningShare <= 0)
            {
                why = Cfg != null && Cfg.AwakeningShare <= 0 ? "the Awakening is off (AwakeningShare 0)" : "off";
                return -1;
            }
            if (p == null || p.Id == null || p.Lore || !p.IsAlive || HasDied(p.Id))
            {
                why = "not one the Awakening reaches";
                return -1;
            }
            PersonState st = Store.Find(p.Id);
            if (st == null)
            {
                why = "no state kept for them yet";
                return -1;
            }
            if (st.WakeStage >= Awakening.Awake)
            {
                why = "awake already";
                return -1;
            }
            if (st.WakeStage > 0 && now - st.WakeStageAt >= 0 && now - st.WakeStageAt < 1440)
            {
                why = "moved a stage within the day";
                return -1;
            }
            Resident r = Resident.IsResident(p.Id) ? Store.FindResident(p.Id) : null;
            int stage = Advance(p, st, now);
            if (r != null)
                Store.KeepResident(r, now);   // a resident's stage is kept from now on, as one woken by the daily roll is
            return stage;
        }
    }
}
