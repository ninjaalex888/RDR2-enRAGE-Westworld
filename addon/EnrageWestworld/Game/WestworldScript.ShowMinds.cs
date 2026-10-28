// Show minds (overnight 2026-10-27, the Testing page's tool): a small card over each named person within 11 m (at
// most four, nearest first): who they are and their temper, their mood and the worry they're living through and its
// stage, their ties, and what is on their mind now (the strongest pull, Engine.MindCard). The cards are worked out
// twice a second and only drawn each frame, inside Draw.FrameBudget (four lines each). Only people the add-on already
// knows (enRAGE's records and townsfolk, residents it has bound): it never makes anyone up to show them.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class MindCardShown
        {
            public int Handle;
            public string[] Lines;
            public float Width;
            public string Short;      // the one line the rest get (name, temper, mood) while the nearest has the full card
            public float ShortWidth;
        }
        private readonly List<float[]> _cardRects = new List<float[]>();   // this frame's cards, so none overlaps another (Lex, 2026-10-28)

        private const float MindCardScale = 0.2f, MindCardAbove = 1.0f, MindCardWidth = 0.24f;
        private const float MindCardMetres = 11f;   // Lex, 2026-10-27 (it was 15)
        private readonly List<MindCardShown> _mindCards = new List<MindCardShown>();
        private int _nextMindCards, _mindCardsShown;
        private Action _aShowMinds;

        private void ShowMindsStep(int now)
        {
            if (_cfg == null || !_cfg.ShowMinds)
            {
                if (_mindCards.Count > 0)
                    _mindCards.Clear();
                return;
            }
            if (_aShowMinds == null)
                _aShowMinds = () => ShowMindsFrame(_now);
            Safe("show minds", _aShowMinds);
        }

        private void ShowMindsFrame(int now)
        {
            if (_engine == null || _bridge == null || PanelOpen || _reader.Open || MISC.GET_MISSION_FLAG())
                return;
            if (now >= _nextMindCards)
            {
                _nextMindCards = now + 500;
                BuildMindCards();
            }
            float lineH = Draw.LineHeight(MindCardScale), pad = 0.004f;
            _cardRects.Clear();
            for (int i = 0; i < _mindCards.Count; i++)
            {
                MindCardShown c = _mindCards[i];
                // the nearest gets the full card, the rest one line each; a card that would cross one already placed
                // (nearer people first) lifts itself above it (Lex, 2026-10-28: four full cards piled up over the bar)
                string[] lines = i == 0 ? c.Lines : new[] { c.Short };
                float width = i == 0 ? c.Width : c.ShortWidth;
                if (!ENTITY.DOES_ENTITY_EXIST(c.Handle) || ENTITY.IS_ENTITY_DEAD(c.Handle) || !ENTITY.IS_ENTITY_ON_SCREEN(c.Handle))
                    continue;
                Vector3 head = PED.GET_PED_BONE_COORDS(c.Handle, 21030, 0f, 0f, 0f);
                float x, y, tx, ty;
                if (!ScreenAt(head + new Vector3(0f, 0f, MindCardAbove), out x, out y))
                    continue;
                // clear above their thought (Lex, 2026-10-27: the card's last line sat on a two-line thought): room for
                // two of its lines over the wake tag always, so the card holds still as thoughts come and go, and higher
                // still while a longer one is showing over this head
                if (_cfg.ThoughtsOverHeads && ScreenAt(head + new Vector3(0f, 0f, ThoughtAboveHead), out tx, out ty))
                {
                    float ts = _cfg.ThoughtsOverHeadScale * Math.Max(0.75f, Feelings.SizeAt(head.DistanceTo(Game.Player.Character.Position)));
                    float thoughtTop = ty - WakeTagLift(c.Handle) - Draw.LineHeight(ts) * 2f - 0.003f;
                    if (_overHeadTopHandle == c.Handle && _overHeadTopAt == now)
                        thoughtTop = Math.Min(thoughtTop, _overHeadTopY);
                    y = Math.Min(y, thoughtTop - 0.008f - pad);
                }
                float h = lineH * lines.Length, top = y - h;
                float x0 = x - width / 2f - pad, x1 = x + width / 2f + pad;
                for (int tries = 0; tries < 8; tries++)
                {
                    float[] hit = _cardRects.FirstOrDefault(r => x0 < r[2] && x1 > r[0] && top - pad < r[3] && top + h + pad > r[1]);
                    if (hit == null)
                        break;
                    top = hit[1] - 0.003f - pad - h;   // above the one it crossed
                }
                _cardRects.Add(new[] { x0, top - pad, x1, top + h + pad });
                Draw.Rect(x0, top - pad, width + pad * 2f, h + pad * 2f, 8, 12, 18, 170);
                Draw.Rect(x0, top - pad, width + pad * 2f, 0.0015f, 120, 170, 210, 200);   // blue, not the thought's gold
                for (int k = 0; k < lines.Length; k++)
                    Draw.Text(lines[k], x - width / 2f, top + k * lineH, MindCardScale, k == 0 ? 236 : 200, k == 0 ? 236 : 214, k == 0 ? 236 : 226);
            }
        }

        /// The named people within MindCardMetres (11 m), nearest first (at most four), and what to show for each.
        private void BuildMindCards()
        {
            _mindCards.Clear();
            Ped me = Game.Player.Character;
            int clockHour = CLOCK.GET_CLOCK_HOURS();
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_HUMAN(x.Handle) &&
                                                             x.Position.DistanceTo(me.Position) < MindCardMetres).OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                Person who = KnownPerson(p);
                if (who == null)
                    continue;
                string mind = MindCardLine(p.Handle);   // the mind in shadow mode: their traits in words (WestworldScript.Mind.cs)
                IEnumerable<string> card = _engine.MindCard(who, clockHour);
                if (mind != null)
                    card = card.Concat(new[] { mind });
                string[] lines = card.Select(l => Draw.Fit(l, MindCardScale, MindCardWidth)).ToArray();
                // the one line: the name line, and the mood (the first clause of the second line) when there is one
                string mood = lines.Length > 1 ? lines[1].Split(';')[0].Trim() : null;
                string one = Draw.Fit(lines[0] + (mood != null && mood.Length > 0 ? ", " + mood : ""), MindCardScale, MindCardWidth);
                _mindCards.Add(new MindCardShown { Handle = p.Handle, Lines = lines, Width = lines.Max(l => Draw.Width(l, MindCardScale)), Short = one, ShortWidth = Draw.Width(one, MindCardScale) });
                if (_mindCardsShown++ == 0)
                    Log.Write("SHOW minds: the first card, over " + who.Name + ": " + string.Join(" | ", lines));
                if (_mindCards.Count >= 4)
                    break;
            }
        }

        /// Someone the add-on already knows, as their Person: one of enRAGE's records, one of its townsfolk, or a resident
        /// bound to this body. Null for anyone else (nobody is made up to show them).
        private Person KnownPerson(Ped p)
        {
            int h = p.Handle;
            NemesisRecord rec = _bridge.RecordAt(h);
            if (rec != null && rec.IsAlive)
                return PersonOf(rec);
            Acquaintance ac = _bridge.AcquaintanceAt(h);
            if (ac != null && !string.IsNullOrEmpty(ac.Name))
            {
                Folk f = Snapshot.Of(ac);
                Person fp = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, Bridge.Now());
                if (fp.Personality == null)
                    fp.Personality = f.Temper;
                return fp;
            }
            ResBind b;
            return _resBind.TryGetValue(h, out b) && !b.R.Dead ? _engine.PersonOfResident(b.R) : null;
        }

        private string ShowMindsStatus()
        {
            return (_cfg.ShowMinds ? "on" : "off") + " ([Testing] ShowMinds); " + _mindCards.Count + " cards now, " + _mindCardsShown + " made this session";
        }
    }
}
