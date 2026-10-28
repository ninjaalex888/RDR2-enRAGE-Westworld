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
        }

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
            for (int i = 0; i < _mindCards.Count; i++)
            {
                MindCardShown c = _mindCards[i];
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
                float top = y - lineH * c.Lines.Length;
                Draw.Rect(x - c.Width / 2f - pad, top - pad, c.Width + pad * 2f, lineH * c.Lines.Length + pad * 2f, 8, 12, 18, 170);
                Draw.Rect(x - c.Width / 2f - pad, top - pad, c.Width + pad * 2f, 0.0015f, 120, 170, 210, 200);   // blue, not the thought's gold
                for (int k = 0; k < c.Lines.Length; k++)
                    Draw.Text(c.Lines[k], x - c.Width / 2f, top + k * lineH, MindCardScale, k == 0 ? 236 : 200, k == 0 ? 236 : 214, k == 0 ? 236 : 226);
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
                _mindCards.Add(new MindCardShown { Handle = p.Handle, Lines = lines, Width = lines.Max(l => Draw.Width(l, MindCardScale)) });
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
