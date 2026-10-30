// Wake stages over heads (2026-10-27, Lex: "It might be helpful if we could see the stage of wakefulness of a NPC
// above their head"; [Testing] ShowWakeStages, a test aid like Show minds): a one-line tag over each person within 25 m
// on screen who has begun to wake (stage 1 to 4), coloured by stage, with what holds them back when something does
// (riding, can't be directed now, keeps a counter, had their chance at despair). Only people the add-on already knows
// (WakeIdentity without binding: nobody is made a resident to show them); the asleep get no tag. Worked out twice a
// second, drawn each frame inside Draw.FrameBudget (one text each, at most eight). A thought over the same person sits
// on top of the tag (WakeTagLift).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class WakeTag
        {
            public int Handle;
            public string Text;
            public float Width;
            public int R, G, B;
        }

        private const float WakeTagScale = 0.2f, WakeTagRange = 25f;
        private const int WakeTagMax = 8;
        private readonly Dictionary<int, WakeTag> _wakeTags = new Dictionary<int, WakeTag>();
        private int _nextWakeTags, _wakeTagsMade;
        private Action _aWakeTags;

        private void WakeTagsStep(int now)
        {
            if (_cfg == null || !_cfg.ShowWakeStages || !_cfg.WestWorld)
            {
                if (_wakeTags.Count > 0)
                    _wakeTags.Clear();
                return;
            }
            if (_aWakeTags == null)
                _aWakeTags = () => WakeTagsFrame(_now);
            Safe("wake stages", _aWakeTags);
        }

        private void WakeTagsFrame(int now)
        {
            if (_engine == null || _bridge == null || _store == null || PanelOpen || _reader.Open || MISC.GET_MISSION_FLAG())
            {
                _wakeTags.Clear();
                return;
            }
            if (now >= _nextWakeTags)
            {
                _nextWakeTags = now + 500;
                BuildWakeTags();
            }
            float lineH = Draw.LineHeight(WakeTagScale), pad = 0.003f;
            foreach (WakeTag t in _wakeTags.Values)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(t.Handle) || ENTITY.IS_ENTITY_DEAD(t.Handle) || !ENTITY.IS_ENTITY_ON_SCREEN(t.Handle))
                    continue;
                Vector3 head = PED.GET_PED_BONE_COORDS(t.Handle, 21030, 0f, 0f, 0f);
                float x, y;
                if (!ScreenAt(head + new Vector3(0f, 0f, ThoughtAboveHead), out x, out y))
                    continue;
                // sits on the thought's point; while a thought shows over them, on top of the thought (Lex, 2026-10-30: the tag
                // went under the thought box)
                float top = _overHeadTopHandle == t.Handle && _overHeadTopAt == now ? _overHeadTopY - 0.004f - lineH : y - lineH;
                Draw.Rect(x - t.Width / 2f - pad, top - pad, t.Width + pad * 2f, lineH + pad * 2f, 10, 10, 12, 150);
                Draw.Text(t.Text, x - t.Width / 2f, top, WakeTagScale, t.R, t.G, t.B);
            }
        }

        /// How far a thought over this person goes up to clear their tag (0 when they have none).
        private float WakeTagLift(int handle)
        {
            return _wakeTags.ContainsKey(handle) ? Draw.LineHeight(WakeTagScale) + 0.008f : 0f;
        }

        /// The people within 25 m on screen who have begun to wake, nearest first, at most eight.
        private void BuildWakeTags()
        {
            _wakeTags.Clear();
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && x.Handle != me.Handle && !x.IsDead && PED.IS_PED_HUMAN(x.Handle) &&
                                                             x.Position.DistanceTo(me.Position) < WakeTagRange && ENTITY.IS_ENTITY_ON_SCREEN(x.Handle))
                                                 .OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                Person who;
                PersonState st;
                Resident res;
                bool passive;
                if (!WakeIdentity(p, false, out who, out st, out res, out passive))
                    continue;   // asleep, or someone the add-on doesn't know
                int stage = Math.Min(Awakening.Awake, st.WakeStage);
                string held = res != null && res.PostId != null ? "keeps a counter"
                            : stage >= Awakening.Awake && st.Has(DespairMark) ? "had their chance"
                            : PED.IS_PED_ON_MOUNT(p.Handle) || PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) ? "riding"
                            : passive ? "can't be directed now" : null;
                string text = Grammar.UpperFirst(Awakening.StageName(stage)) + " (" + stage + " of 4)" + (held != null ? ", " + held : "");
                int r, g, b;
                switch (stage)
                {
                    case Awakening.Stirring: r = 236; g = 226; b = 170; break;      // pale
                    case Awakening.Questioning: r = 240; g = 196; b = 96; break;    // amber
                    case Awakening.Unravelling: r = 240; g = 140; b = 60; break;    // orange
                    default: r = 236; g = 88; b = 72; break;                         // red: awake
                }
                _wakeTags[p.Handle] = new WakeTag { Handle = p.Handle, Text = text, Width = Draw.Width(text, WakeTagScale), R = r, G = g, B = b };
                if (_wakeTagsMade++ == 0)
                    Log.Write("SHOW wake stages: the first tag, over " + who.Name + ": " + text);
                if (_wakeTags.Count >= WakeTagMax)
                    break;
            }
        }

        private string WakeTagsStatus()
        {
            return (_cfg.ShowWakeStages ? "on" : "off") + " ([Testing] ShowWakeStages" + (_cfg.WestWorld ? "" : "; West World is off, so none") + "); " +
                   _wakeTags.Count + " tags now: " + (_wakeTags.Count == 0 ? "none" : string.Join("; ", _wakeTags.Values.Select(t => t.Text).Take(8))) + "; " + _wakeTagsMade + " made this session";
        }
    }
}
