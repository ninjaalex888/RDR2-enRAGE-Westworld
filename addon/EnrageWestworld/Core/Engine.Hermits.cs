// The hermits who woke (see Core/Encounters.cs): an awake resident who ran, in the wake acts' escape ending, has
// left their town for good. They are never recast on a passer-by again; the first time the player is out in the
// wilds and the roll comes up, they get a camp there, saved, and live at it from then on. When nobody has fled yet,
// someone who woke and walked out before the player ever met them is made instead.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        public const int MaxCamps = 8;

        /// An awake resident ran from their town: they live rough from now on.
        public bool Flee(Resident r, long now)
        {
            if (r == null || r.Dead || r.Fled || r.PostId != null)
                return false;
            r.Fled = true;
            r.FledAt = now;
            Store.KeepResident(r, now);
            Log("HERMIT " + r.Name + " walked out of " + (string.IsNullOrEmpty(r.Town) ? "town" : r.Town) + " for the wilds");
            return true;
        }

        /// Who gets the next camp: whoever fled longest ago and has none yet; null when nobody is waiting.
        public Resident Homeless()
        {
            return Store.Residents.Where(x => x.Fled && !x.Dead && !x.HasCamp).OrderBy(x => x.FledAt).FirstOrDefault();
        }

        /// Living hermits with a camp.
        public List<Resident> Camps()
        {
            return Store.Residents.Where(x => x.Fled && !x.Dead && x.HasCamp).ToList();
        }

        /// Someone who woke and walked out of their town some days before the player came by.
        public Resident MakeHermit(uint model, bool woman, string town, string trade, string seed, long now)
        {
            Resident r = MeetResident(model, woman, town, trade, seed, now);
            Person p = PersonOfResident(r);
            PersonState st = Store.Get(p);
            while (st.WakeStage < Awakening.Awake)
                Advance(p, st, now);
            r.Fled = true;
            r.FledAt = now - 1440L * (3 + _rng.Next(40));
            Store.KeepResident(r, now);
            Log("HERMIT " + r.Name + " made: woke and walked out of " + (string.IsNullOrEmpty(town) ? "town" : town) + " before you came by");
            return r;
        }

        public void SetCamp(Resident r, float x, float y, float z, long now)
        {
            r.HasCamp = true;
            r.CampX = x;
            r.CampY = y;
            r.CampZ = z;
            Store.KeepResident(r, now);
        }
    }
}
