// Names over heads (Lex, 2026-10-29, with enRAGE gone: "it did a really good job of placing NPC names over the NPC head
// that looked really good and our mod wasn't doing this"). Adapted from enRAGE's DrawNameCards (its author permits reuse;
// credited here): the candidates re-read every 250 ms, the head bone cached and looked up for six bodies a frame at most,
// the screen point steadied by a one-euro filter and led by its velocity, an occlusion ray for three bodies a frame every
// 150 ms, a fade by distance to the edge of the range and in and out, the name in the game's title font over a second
// line in smaller type, both centred by the font's own width table; no box. The names are ours: one of enRAGE's records
// (coloured by his hate), one of its townsfolk, or a resident bound to the body (a regular, someone who thought, spoke or
// took part in a scene); in "everyone" mode anyone else gets the kind of person they look ("A townsman") in grey, since
// nobody is given an identity just to draw a card (names over every passer-by wait for identity by look, roadmap 34).
// With enRAGE running and its own cards on, ours stay off unless [Display] NameCardsBesideEnrage says otherwise.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private sealed class NameCard
        {
            public int Handle;
            public uint Model;
            public string Name, Line2;
            public int R, G, B, R2, G2, B2;
            public bool Known;
            public int MadeAt, FirstSeen, SeenFrame;
            public float Metres, Range;
            public Vector3 HeadOff;
            public int HeadFrame = -1000;
            public float HeadZ;
            public bool HasHeadZ;
            public float PrevX, PrevY, VelX, VelY;
            public int PrevFrame = -1000;
            public bool Blocked;
            public int BlockedAt;
            public float Alpha;
            // the text as drawn and its width in the title font's units, kept until the text changes (night 2026-10-29,
            // NIGHT.md checklist 4-5: built anew for every card every frame, the costliest feature in a Saint Denis crowd)
            public string FormedOf, Formed, Formed2Of, Formed2;
            public float Units, Units2;
        }

        private readonly Dictionary<int, NameCard> _nameCards = new Dictionary<int, NameCard>();
        private readonly List<NameCard> _nameCardsNow = new List<NameCard>();
        private int _nameCardsCandAt = -100000, _nameCardsFrame, _nameCardsHoldUntil = -100000, _nameCardsLastMs, _nameCardsUp, _nameCardsMade;
        private int? _enrageCards;
        private int _enrageCardsAt = -100000;
        private Action _aNameCards;
        private int _nameCardsProbedIn = -1, _nameCardsProbeAt = -100000;
        private const int NameCardsMost = 10, NameCardsMostAlways = 16, NameCardsRefreshMs = 3000;
        private const float NameCardAbove = 0.32f, NameCardSteady = 2.5f, NameCardLead = 1f;
        private static readonly int[] ColourTitle = { 245, 244, 240 }, ColourStranger = { 170, 168, 160 }, ColourDim = { 146, 144, 138 };

        private void NameCardsStep(int now)
        {
            if (_cfg == null || _cfg.NameCards == "off")
            {
                if (_nameCards.Count > 0)
                    _nameCards.Clear();
                return;
            }
            if (_aNameCards == null)
                _aNameCards = () => NameCardsFrame(_now);
            Safe("name cards", _aNameCards);
        }

        /// enRAGE is running and drawing its own cards (its [Menu] NameCards 1 or 2), and we weren't asked to draw beside them.
        private bool NameCardsLeftToEnrage(int now)
        {
            if (_bridge == null || !_bridge.Present || _cfg.NameCardsBesideEnrage)
                return false;
            if (now - _enrageCardsAt > 5000)
            {
                _enrageCardsAt = now;
                int? was = _enrageCards;
                _enrageCards = _bridge.EnrageNameCards();
                bool on = _enrageCards.HasValue && _enrageCards.Value > 0, wasOn = was.HasValue && was.Value > 0;
                if (on != wasOn)
                    Log.Write("NAME cards: enRAGE's own cards are " + (on ? "on (its NameCards " + _enrageCards.Value + "): " + (_cfg.NameCardsAlways ? "ours card everyone it doesn't" : "ours stand aside") : "off: ours card everyone"));
            }
            _enrageCardsOn = _enrageCards.HasValue && _enrageCards.Value > 0;
            // always on, ours never stand aside: enRAGE's cards are only over its own people, and a city of strangers had
            // none at all (Lex, 2026-10-30, Saint Denis); ours leave those to it (NameCardFillInner)
            return _enrageCardsOn && !_cfg.NameCardsAlways;
        }

        private bool _enrageCardsOn;

        private void NameCardsFrame(int now)
        {
            if (_engine == null || _bridge == null || PanelOpen || _reader.Open || now < _nameCardsHoldUntil || NameCardsLeftToEnrage(now))
            {
                if (_nameCards.Count > 0)
                    _nameCards.Clear();
                _nameCardsNow.Clear();
                _nameCardsUp = 0;
                return;
            }
            Ped me = Game.Player.Character;
            Vector3 mePos = me.Position;
            if (now - _nameCardsCandAt >= 250)
            {
                _nameCardsCandAt = now;
                NameCardsCandidates(me, mePos, now);
            }
            _nameCardsFrame++;
            float dt = Math.Max(0.004f, Math.Min(0.1f, (now - _nameCardsLastMs) / 1000f));
            _nameCardsLastMs = now;
            int bones = 0, rays = 0, up = 0;
            int myInterior = INTERIOR.GET_INTERIOR_FROM_ENTITY(me.Handle);
            // always on, the cards are drawn before the Testing overlays: they leave those their room (Lex, 2026-10-30: a wake
            // stage tag went the moment her Fondness bar came, the frame's text all spent on names)
            int keep = !_cfg.NameCardsAlways ? 0 : (_cfg.ShowWakeStages && _cfg.WestWorld ? WakeTagMax : 0) + (_cfg.ShowMinds ? 12 : 0) + 2;
            foreach (NameCard c in _nameCardsNow)
            {
                int h = c.Handle;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || !PED.IS_PED_READY_TO_RENDER(h))
                    continue;
                if (now - c.FirstSeen < 1000)
                    continue;   // a body just come into range settles first
                Vector3 pos = ENTITY.GET_ENTITY_COORDS(h, true, true);
                bool hasHead = c.HeadFrame > -1000;
                Vector3 head;
                if ((!hasHead || _nameCardsFrame - c.HeadFrame >= 3) && bones < 6)
                {
                    bones++;
                    head = PED.GET_PED_BONE_COORDS(h, 21030, 0f, 0f, 0f);
                    if (head.X != 0f || head.Y != 0f)
                    {
                        c.HeadOff = head - pos;
                        c.HeadFrame = _nameCardsFrame;
                    }
                }
                else
                {
                    if (!hasHead)
                        continue;
                    head = pos + c.HeadOff;
                }
                bool noHead = head.X == 0f && head.Y == 0f;
                float z = noHead ? pos.Z + 0.75f : head.Z;
                if (c.HasHeadZ && Math.Abs(c.HeadZ - z) < 1.5f)
                    z = c.HeadZ + (z - c.HeadZ) * Math.Min(1f, dt * 6f);
                c.HeadZ = z;
                c.HasHeadZ = true;
                float wx = noHead ? pos.X : pos.X + (head.X - pos.X) * 0.6f;
                float wy = noHead ? pos.Y : pos.Y + (head.Y - pos.Y) * 0.6f;
                float x, y;
                if (!ScreenAt(new Vector3(wx, wy, z + NameCardAbove), out x, out y))
                {
                    c.SeenFrame = -1000;
                    continue;
                }
                // the one-euro filter: still cards are smoothed hard, moving ones follow, and a little lead hides the lag
                if (_nameCardsFrame - c.PrevFrame == 1)
                {
                    float vx = (x - c.PrevX) / dt, vy = (y - c.PrevY) / dt;
                    if (Math.Abs(x - c.PrevX) > 0.08f || Math.Abs(y - c.PrevY) > 0.08f)
                    {
                        vx = vy = 0f;
                        c.VelX = c.VelY = 0f;
                        c.PrevX = x;
                        c.PrevY = y;
                    }
                    float a1 = OneEuroAlpha(1f, dt);
                    c.VelX += (vx - c.VelX) * a1;
                    c.VelY += (vy - c.VelY) * a1;
                    float speed = (float)Math.Sqrt(c.VelX * c.VelX + c.VelY * c.VelY);
                    float a2 = OneEuroAlpha(NameCardSteady + 18f * speed, dt);
                    float sx = c.PrevX + (x - c.PrevX) * a2, sy = c.PrevY + (y - c.PrevY) * a2;
                    c.PrevX = sx;
                    c.PrevY = sy;
                    x = sx + c.VelX * dt * NameCardLead;
                    y = sy + c.VelY * dt * NameCardLead;
                }
                else
                {
                    c.PrevX = x;
                    c.PrevY = y;
                    c.VelX = c.VelY = 0f;
                }
                c.PrevFrame = _nameCardsFrame;
                if ((c.BlockedAt == 0 || now - c.BlockedAt > 150) && rays < 3)
                {
                    rays++;
                    // always on: only a wall hides it (the map's own shapes), not a table, a post or someone in the way (Lex, 2026-10-30)
                    c.Blocked = Natives.Blocked(CAM.GET_FINAL_RENDERED_CAM_COORD(), new Vector3(pos.X, pos.Y, z + 0.05f), me.Handle, _cfg.NameCardsAlways ? 1 : 1 | 16);
                    c.BlockedAt = now;
                }
                bool hidden = c.BlockedAt == 0 || c.Blocked;
                if (!hidden && !_cfg.NameCardsAlways)   // always on, the walls' ray alone decides: a big building is several interiors (Lex, 2026-10-30: none in the Bastille)
                {
                    int interior = INTERIOR.GET_INTERIOR_FROM_ENTITY(h);
                    if (interior != 0 && interior != myInterior)
                        hidden = true;
                }
                float edge = Math.Max(0f, Math.Min(1f, (c.Range - c.Metres) / (_cfg.NameCardsAlways ? 4f : 12f)));   // always on: full until the last few metres
                float alpha = _nameCardsFrame - c.SeenFrame <= 3 ? c.Alpha : 0f;
                alpha = hidden ? Math.Max(0f, alpha - dt * 6f) : Math.Min(edge, alpha + dt * 4f);
                c.Alpha = alpha;
                c.SeenFrame = _nameCardsFrame;
                int a = (int)(255f * alpha);
                if (a < 8)
                    continue;
                if (keep > 0 && Draw.Left - 2 < keep)
                    break;   // the rest of the frame's text is the overlays'
                // smaller with distance: the title 0.25 down to 0.19, the line under it 0.175 down to 0.145
                float far = Math.Max(0f, Math.Min(1f, (c.Metres - 6f) / Math.Max(1f, c.Range - 6f)));
                float s1 = 0.25f - 0.06f * far, s2 = 0.175f - 0.03f * far;
                float h1 = 0.1f * s1, h2 = 0.094f * s2;
                if (!ReferenceEquals(c.FormedOf, c.Name))
                {
                    c.FormedOf = c.Name;
                    c.Formed = Draw.Formed(c.Name, "$title");
                    c.Units = TitleUnits(c.Name);
                }
                Draw.TextFormed(c.Formed, x - c.Units * s1 * 0.0205f / 2f, y - h1 - h2 + 0.002f, s1, c.R, c.G, c.B, a);
                if (_cfg.NameCardsSecondLine && !string.IsNullOrEmpty(c.Line2))   // off: the name alone (players, 2026-10-30)
                {
                    if (!ReferenceEquals(c.Formed2Of, c.Line2))
                    {
                        c.Formed2Of = c.Line2;
                        c.Formed2 = Draw.Formed(c.Line2, "$title");
                        c.Units2 = TitleUnits(c.Line2);
                    }
                    Draw.TextFormed(c.Formed2, x - c.Units2 * s2 * 0.0205f / 2f, y - h2 - 0.0018518518f, s2, c.R2, c.G2, c.B2, (int)(a * 0.88f));
                }
                up++;
                if (_nameCardsMade++ == 0)
                    Log.Write("NAME cards: the first, over " + c.Name + (c.Line2 != null && c.Line2.Length > 0 ? " (" + c.Line2 + ")" : "") + ", " + (int)c.Metres + " m off");
            }
            _nameCardsUp = up;
            if (_nameCardsFrame % 120 == 0)
                foreach (int gone in _nameCards.Where(kv => _nameCardsFrame - kv.Value.SeenFrame > 60 && now - kv.Value.FirstSeen > 5000 && !_nameCardsNow.Contains(kv.Value)).Select(kv => kv.Key).ToList())
                    _nameCards.Remove(gone);
        }

        /// The living humans in range with a name of ours to show, nearest first, at most NameCardsMost; each keeps its card
        /// (and its smoothing) between looks, refreshed every three seconds or when the game reuses the handle.
        private void NameCardsCandidates(Ped me, Vector3 mePos, int now)
        {
            _nameCardsNow.Clear();
            bool everyone = _cfg.NameCards == "everyone";
            int inside = INTERIOR.GET_INTERIOR_FROM_ENTITY(me.Handle);
            bool probe = inside != _nameCardsProbedIn && now - _nameCardsProbeAt > 3000;
            int seen = 0, carded = 0;
            var why = probe ? new Dictionary<string, int>() : null;
            var models = probe ? new List<string>() : null;
            float knownRange = _cfg.NameCardsKnownMetres, strangerRange = _cfg.NameCardsMetres, most = Math.Max(knownRange, strangerRange) + 20f;
            List<NameCard> found = new List<NameCard>();
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle)
                    continue;
                float d = p.Position.DistanceTo(mePos);
                if (d > most || p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                    continue;   // the distance first: a far body needs neither native (the same people pass)
                int h = p.Handle;
                uint model = ENTITY.GET_ENTITY_MODEL(h);
                NameCard c;
                if (!_nameCards.TryGetValue(h, out c) || c.Model != model)
                {
                    c = new NameCard { Handle = h, Model = model, FirstSeen = now, MadeAt = -100000 };
                    _nameCards[h] = c;
                }
                if (probe && d <= strangerRange)
                    seen++;
                if (now - c.MadeAt >= NameCardsRefreshMs || probe)
                {
                    c.MadeAt = now;
                    if (!NameCardFill(p, c, everyone))
                    {
                        c.Name = null;
                        if (probe && d <= strangerRange)
                        {
                            string r = Natives.StoryModel(p) ? "the game's own" : !IsGrownUp(p) ? "not grown" : !everyone ? "not known (NameCards known)" : "nothing to say";
                            int n0;
                            why.TryGetValue(r, out n0);
                            why[r] = n0 + 1;
                            if (models.Count < 6)
                                models.Add(Natives.ModelName(p));
                        }
                        continue;
                    }
                }
                if (c.Name == null)
                    continue;
                c.Range = c.Known ? knownRange : strangerRange;
                c.Metres = d;
                if (d <= c.Range)
                    found.Add(c);
            }
            foreach (NameCard c in found.OrderBy(x => x.Metres).Take(_cfg.NameCardsAlways ? NameCardsMostAlways : NameCardsMost))
                _nameCardsNow.Add(c);
            if (probe)
            {
                carded = _nameCardsNow.Count;
                _nameCardsProbedIn = inside;
                _nameCardsProbeAt = now;
                Log.Write("NAME cards " + (inside != 0 ? "indoors (" + (_placeKind ?? "a building") + (_placeTown != null ? ", " + _placeTown : "") + ")" : "out of doors") + ": " + seen + " people within " + (int)strangerRange + " m, " + carded + " carded" +
                          (why.Count > 0 ? "; none for " + string.Join(", ", why.Select(kv => kv.Value + " " + kv.Key)) + " (" + string.Join(", ", models) + ")" : ""));
            }
        }

        /// What the card says for this body: false when there's nothing of ours to say (and the mode doesn't show everyone).
        /// The card, with the gang a member belongs to before the rest of the second line ("Lemoyne Raider - Hothead"), and
        /// an unnamed gang member carded by his gang ("A Lemoyne Raider"), as enRAGE showed them (Lex, 2026-10-29).
        private bool NameCardFill(Ped p, NameCard c, bool everyone)
        {
            string gang = Factions.MemberOf(c.Model, PED.GET_PED_RELATIONSHIP_GROUP_HASH(p.Handle));
            bool got = NameCardFillInner(p, c, everyone && gang == null);
            // what they are, before the rest of the line: "Lawman - Proud", "Barkeep" (Lex, 2026-10-30: enRAGE showed a
            // lawman's and a shopkeeper's trade on the card); a stranger's "Stranger" gives way to it, the badge or the apron
            // being plain to see
            string job = got && gang == null ? JobOf(p) : null;
            if (job != null && c.Name != null)
            {
                if (string.IsNullOrEmpty(c.Line2) || c.Line2 == "Stranger")
                    c.Line2 = job;
                else if (c.Line2.IndexOf(job, StringComparison.OrdinalIgnoreCase) < 0)
                    c.Line2 = job + " - " + c.Line2;
            }
            if (gang == null)
                return got;
            if (!got)
            {
                Set(c, Factions.AOne(gang), "", ColourStranger, ColourDim, false);
                return true;
            }
            if (c.Name != null && !c.Name.EndsWith(gang))
                c.Line2 = gang + (string.IsNullOrEmpty(c.Line2) ? "" : " - " + c.Line2);
            return true;
        }

        /// The trade a card shows first: a lawman (the sheriff, a deputy), or a town's counter keeper (Posts); null else.
        private static string JobOf(Ped p)
        {
            if (Natives.IsLaw(p))
            {
                string m = Natives.ModelName(p);
                return m.Contains("sheriff") ? "Sheriff" : m.Contains("deputy") ? "Deputy" : "Lawman";
            }
            PostKind k = PostKindOf(p);
            if (k == null || string.IsNullOrEmpty(k.Role))
                return null;
            string role = k.Role.StartsWith("the ") ? k.Role.Substring(4) : k.Role;
            return Grammar.UpperFirst(role.Replace("general store keeper", "store keeper"));
        }

        // A stranger greeted keeps the name over their head (Lex, 2026-10-30: "when I greet them, they get a different name than
        // they had before the greet"): one of our townsfolk, named on the card, whom enRAGE takes up as one of its own (at the
        // greet, or a moment after) under a name of its making. The name we showed is noted each time the card shows it (enRAGE
        // had no one in that body then) and at the greet; when enRAGE has someone there within the minute, and its someone is
        // new (seen once, or first seen these few minutes), they're given ours. Someone enRAGE knew before, met again in a new
        // body, keeps the name enRAGE gave them.
        private readonly Dictionary<int, KeyValuePair<string, int>> _nameAtGreet = new Dictionary<int, KeyValuePair<string, int>>();

        private void NoteNameAtGreet(int h, int now)
        {
            ResBind b;
            if (h == 0 || _bridge.RecordAt(h) != null || !_resBind.TryGetValue(h, out b) || b == null || b.R == null || string.IsNullOrEmpty(b.R.Name))
                return;
            NoteOurName(h, b.R.Name, now);
        }

        private void NoteOurName(int h, string name, int now)
        {
            _nameAtGreet[h] = new KeyValuePair<string, int>(name, now);
            if (_nameAtGreet.Count > 40)
                _nameAtGreet.Remove(_nameAtGreet.OrderBy(kv => kv.Value.Value).First().Key);
        }

        private void KeepNameAtGreet(int h)
        {
            KeyValuePair<string, int> was;
            if (_nameAtGreet.Count == 0 || !_nameAtGreet.TryGetValue(h, out was))
                return;
            int now = Game.GameTime;
            if (now - was.Value > 60000 || now < was.Value)
            {
                _nameAtGreet.Remove(h);   // a while since we named them: whatever enRAGE has there isn't new to this
                return;
            }
            FolkRef ac = _bridge.AcquaintanceAt(h);
            if (ac == null)
                return;
            _nameAtGreet.Remove(h);
            string theirs = ac.Name;
            if (theirs == was.Key)
                return;
            try
            {
                if (!ac.IsNew)
                {
                    // someone enRAGE knew before: ours goes by enRAGE's name from now on, everywhere we keep it (Lex, 2026-10-30:
                    // "our regulars could keep enrage names if enrage installed"), so the next time they're ours alone it's the same
                    ResBind rb;
                    int changed = 0;
                    if (!string.IsNullOrEmpty(theirs) && _resBind.TryGetValue(h, out rb) && rb != null && rb.R != null && rb.R.Name == was.Key)
                    {
                        rb.R.Name = theirs;
                        changed = 1 + _store.Rename(rb.R.Id, theirs);
                    }
                    Log.Write("NAME " + was.Key + " is someone enRAGE knew before, " + (theirs ?? "nobody") + " (seen " + ac.TimesSeen + " times): " +
                              (changed > 0 ? "ours goes by " + theirs + " now (" + changed + " records)" : "its name kept"));
                    return;
                }
                ac.SetName(was.Key);
                Log.Write("NAME enRAGE took up " + was.Key + " as " + (theirs ?? "nobody") + ": kept as " + was.Key);
            }
            catch (Exception ex)
            {
                Log.Write("NAME couldn't keep " + was.Key + "'s name when enRAGE took them up: " + ex.Message);
            }
        }

        private bool NameCardFillInner(Ped p, NameCard c, bool everyone)
        {
            int h = p.Handle;
            bool woman = !PED.IS_PED_MALE(h);
            RecordRef rec = _bridge.RecordAt(h);
            if (_enrageCardsOn && !_cfg.NameCardsBesideEnrage && (rec != null || _bridge.AcquaintanceAt(h) != null))
                return false;   // enRAGE draws its own over its people: one card a head
            if (rec != null && rec.IsAlive)
            {
                Person who = PersonOf(rec);
                int hate = who.Hate;
                int[] colour = hate >= 70 ? new[] { 205, 55, 45 } : hate >= 40 ? new[] { 215, 135, 50 } : new[] { 205, 185, 100 };
                string tier = hate >= 70 ? "Murderous" : hate >= 40 ? "Vengeful" : hate >= 15 ? "Sore" : "Wary";
                Set(c, who.Name, (who.Personality != null ? who.Personality + " - " : "") + tier, colour, colour, true);
                return true;
            }
            KeepNameAtGreet(h);
            FolkRef ac = _bridge.AcquaintanceAt(h);
            if (ac != null && !string.IsNullOrEmpty(ac.Name))
            {
                bool met = ac.Met;
                string temper = ac.Temper != null && ac.Temper.Length > 0 ? Grammar.UpperFirst(ac.Temper) : null;
                // no name until you've met: "Stranger" alone (Lex, 2026-10-30: "have their name show as Stranger until they are
                // greeted or interacted with")
                if (met)
                    Set(c, ac.Name, temper ?? "", ColourTitle, ColourDim, true);
                else if (_cfg.NameCardsStrangerNames)
                    Set(c, ac.Name, "Stranger", ColourStranger, ColourDim, true);   // the name with Stranger under it, if asked for
                else
                    Set(c, "Stranger", "", ColourStranger, ColourDim, true);
                return true;
            }
            ResBind b;
            if (!_resBind.TryGetValue(h, out b) && IsHermit(h))
                b = new ResBind { R = _hermit.R, Model = _hermit.Model };   // the hermit at their camp is one of ours, never bound to the town (Lex, 2026-10-30: no name over her)
            if (b != null && b.R != null && !b.R.Dead && !string.IsNullOrEmpty(b.R.Name))
            {
                // met: talked, greeted, flirted, any contact, or they saw you do something; else a stranger, whatever their temper
                // (Lex, 2026-10-29: "it should show Stranger until the main character has greeted or interacted with the NPC")
                PersonState st = _store != null ? _store.Find(b.R.Id) : null;
                int greetedAt;
                // met: you greeted, talked, flirted or had any contact, or they spoke up to you; only seeing you do something isn't
                // meeting (Lex, 2026-10-30)
                bool met = (st != null && (st.Talks > 0 || st.KnowsName || st.Flirts > 0 || st.ContactAt > 0 || st.LastRemarkAt > 0)) || _greetedAt.TryGetValue(h, out greetedAt);
                string temper = b.R.Personality != null && b.R.Personality.Length > 0 ? b.R.Personality : null;
                string trade = b.R.Trade != null && b.R.Trade.Length > 0 ? Grammar.UpperFirst(b.R.Trade.StartsWith("a ") ? b.R.Trade.Substring(2) : b.R.Trade.StartsWith("an ") ? b.R.Trade.Substring(3) : b.R.Trade) : null;
                string line2 = temper != null && trade != null ? temper + " - " + trade : temper ?? trade ?? "";
                NoteOurName(h, b.R.Name, Game.GameTime);   // shown (or meant) as ours while enRAGE has no one in this body
                if (st != null && st.Has(PickpocketMark))
                    line2 = "Pickpocket" + (temper != null ? " - " + temper : "");   // the trade he keeps to (Lex, 2026-10-30)
                if (met)
                    Set(c, b.R.Name, line2, ColourTitle, ColourDim, true);
                else if (_cfg.NameCardsStrangerNames)
                    Set(c, b.R.Name, "Stranger", ColourStranger, ColourDim, true);
                else
                    Set(c, "Stranger", "", ColourStranger, ColourDim, true);   // no name until you've met (Lex, 2026-10-30)
                return true;
            }
            // the game's own named people: the story characters and the uniques the game names (StoryCast.cs; Lex, 2026-10-29: the
            // sheriff of Valentine had his name under enRAGE and not under us)
            string ml = Models.Name(c.Model);
            StoryCast.Person sp;
            if (StoryCast.TryGet(ml, out sp))
            {
                Set(c, sp.Name, StoryCast.JobFor(ml) ?? sp.Temper ?? "", ColourTitle, ColourDim, true);   // "Valentine sheriff", else the temper
                return true;
            }
            string gameName = StoryCast.NameFor(ml);
            if (gameName != null)
            {
                Set(c, gameName, ml.Contains("_bht_") || ml.Contains("bounty") ? "Wanted" : "", ColourTitle, ColourDim, true);
                return true;
            }
            if (!everyone || Natives.StoryModel(p) || !IsGrownUp(p))
                return false;
            Set(c, Grammar.UpperFirst(Director.Kind(p, woman)), "Stranger", ColourStranger, ColourDim, false);
            return true;
        }

        private static void Set(NameCard c, string name, string line2, int[] colour, int[] colour2, bool known)
        {
            c.Name = name;
            c.Line2 = line2;
            c.R = colour[0]; c.G = colour[1]; c.B = colour[2];
            c.R2 = colour2[0]; c.G2 = colour2[1]; c.B2 = colour2[2];
            c.Known = known;
        }

        private static float OneEuroAlpha(float cutoff, float dt)
        {
            float tau = 1f / ((float)Math.PI * 2f * Math.Max(0.01f, cutoff));
            return 1f / (1f + tau / dt);
        }

        /// The width of text in the game's title font in its own units (enRAGE's table, with lower case at 0.8): x scale x 0.0205
        /// gives screen fractions; kept on a card while its text stands.
        private static float TitleUnits(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0f;
            float w = 0f;
            foreach (char c in s)
            {
                float u;
                switch (char.ToUpperInvariant(c))
                {
                    case 'I': u = 0.55f; break;
                    case 'J': u = 0.6f; break;
                    case 'L': u = 0.8f; break;
                    case 'F': case 'E': case 'S': case 'T': case 'Z': u = 0.85f; break;
                    case 'B': case 'P': case 'R': u = 0.92f; break;
                    case 'C': case 'K': case 'Y': u = 0.97f; break;
                    case 'D': case 'G': u = 1.07f; break;
                    case 'O': case 'Q': u = 1.06f; break;
                    case 'M': u = 1.3f; break;
                    case 'W': u = 1.45f; break;
                    case ' ': u = 0.45f; break;
                    case '-': u = 0.55f; break;
                    case ',': case '.': case ':': u = 0.35f; break;
                    case '\'': u = 0.18f; break;
                    default: u = char.IsDigit(c) ? 0.9f : 1f; break;
                }
                w += char.IsLower(c) ? u * 0.8f : u;
            }
            return w;
        }

        private string NameCardsStatus()
        {
            // who is near, by model and relationship group: which token or group marks a gang member (Lex's Lemoyne Raiders, 2026-10-29)
            Ped meP = Game.Player.Character;
            string near = string.Join(", ", World.GetAllPeds().Where(x => x != null && x.Exists() && x.Handle != meP.Handle && PED.IS_PED_HUMAN(x.Handle) && x.Position.DistanceTo(meP.Position) < 12f)
                .OrderBy(x => x.Position.DistanceTo(meP.Position)).Take(6)
                .Select(x => (_nameCards.ContainsKey(x.Handle) && _nameCards[x.Handle].Name != null ? _nameCards[x.Handle].Name : "?") + " " + Natives.ModelName(x) + " rel 0x" + PED.GET_PED_RELATIONSHIP_GROUP_HASH(x.Handle).ToString("X8") + (Factions.MemberOf(ENTITY.GET_ENTITY_MODEL(x.Handle)) is string g ? " (" + g + ")" : "")));
            Log.Write("TEST near models: " + near);
            if (_cfg.NameCards == "off")
                return "off ([Display] NameCards)";
            string left = _bridge != null && _bridge.Present && NameCardsLeftToEnrage(_now) ? "; left to enRAGE's own cards (NameCardsBesideEnrage off)" : "";
            return _cfg.NameCards + (_cfg.NameCardsSecondLine ? "" : ", names alone") + " within " + (int)_cfg.NameCardsMetres + "/" + (int)_cfg.NameCardsKnownMetres + " m; " + _nameCardsUp + " up now, " + _nameCardsNow.Count +
                   " in range, " + _nameCards.Count + " kept; " + _nameCardsMade + " drawn this session" + left;
        }
    }
}
