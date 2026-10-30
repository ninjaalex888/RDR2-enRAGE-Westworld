// The bars under people near you for how they feel about you (fear, fondness, anger), and the meter in the top corner
// for the strangers whose grudge against you is building toward the line where they become a nemesis ([Display] in
// the ini). The choosing and the words are in Core/Feelings.cs; the numbers come from enRAGE (Bridge.Feelings.cs).
// Who is near and what they feel is looked at a few times a second; the bars are drawn every frame from that.
// Nothing is drawn while enRAGE's own debug overlay is up (it has bars of its own), under a menu, or in a cutscene.
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
        private const float BarsAboveHead = 0.30f;   // metres above the head bone: just under where enRAGE puts its name card
        private const int BarPeople = 3, MeterRows = 4;
        private const float MeterRange = 80f;        // strangers this close count for the meter
        private const float MeterX = 0.80f, MeterY = 0.06f, MeterW = 0.18f;

        private sealed class Felt
        {
            public Ped Ped;
            public float Metres;
            public List<FeelingBar> Bars;
        }

        private List<Felt> _felt = new List<Felt>();
        private List<GrudgeRow> _meter = new List<GrudgeRow>();
        private int _nextFeelLook, _grudgeLine;
        private Action _aFeelings;

        private void FeelingsStep(int now)
        {
            _meterBottom = 0f;   // the news box goes under the meter only when it's drawn this frame
            if (_aFeelings == null)
                _aFeelings = () => FeelingsFrame(_now);
            Safe("feelings", _aFeelings);
        }

        private void FeelingsFrame(int now)
        {
            if ((!_cfg.FeelingBars && !_cfg.GrudgeMeter) || _bridge == null || _engine == null || PanelOpen || _reader.Open || !TickerVisible(now))
                return;
            if (!_bridge.FeelingsKnown || _bridge.EnrageOverlayOn())
                return;   // the Bridge has said in the log what's missing; enRAGE's overlay has bars of its own
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            if (now >= _nextFeelLook)
            {
                _nextFeelLook = now + 300;
                LookAtFeelings(me);
            }
            if (_cfg.FeelingBars)
                foreach (Felt f in _felt)
                    DrawFeelingBars(f);
            if (_cfg.GrudgeMeter && _meter.Count > 0)
                DrawGrudgeMeter();
        }

        /// Who near you feels something about you, nearest first and in plain view; and who is sore at you.
        private void LookAtFeelings(Ped me)
        {
            Vector3 at = me.Position;
            List<Felt> felt = new List<Felt>();
            if (_cfg.FeelingBars)
            {
                foreach (Ped p in World.GetAllPeds())
                {
                    if (p == null || p.Handle == me.Handle || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                        continue;
                    float d = p.Position.DistanceTo(at);
                    if (d > _cfg.FeelingBarsMetres || !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle))
                        continue;
                    float fear, like, anger;
                    if (!_bridge.FeelingsOf(p.Handle, out fear, out like, out anger))
                        continue;
                    List<FeelingBar> bars = Feelings.Bars(fear, like, anger);
                    if (bars.Count > 0)
                        felt.Add(new Felt { Ped = p, Metres = d, Bars = bars });
                }
                felt = felt.OrderBy(f => f.Metres).Where(f => ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, f.Ped.Handle, 17)).Take(BarPeople).ToList();
            }
            _felt = felt;
            if (_cfg.GrudgeMeter)
            {
                _grudgeLine = _bridge.GrudgeLine();
                List<GrudgeRow> rows = Feelings.Meter(_bridge.GrudgesAgainstYou(at, MeterRange), MeterRows);
                foreach (GrudgeRow r in rows)
                    if (r.Who == null)
                        r.Who = NameToShow(r.Handle);
                _meter = rows;
            }
        }

        /// The name to show for someone: enRAGE's for them, else theirs as a resident, else what they look like.
        private string NameToShow(int handle)
        {
            string enrages = _bridge.EnrageNameAt(handle);
            if (enrages != null && enrages.Trim().Length > 0)
                return enrages;
            ResBind b;
            if (_resBind.TryGetValue(handle, out b) && b.R != null && !string.IsNullOrEmpty(b.R.Name))
                return b.R.Name;
            return ENTITY.DOES_ENTITY_EXIST(handle) ? Grammar.UpperFirst(Director.Kind(new Ped(handle), !PED.IS_PED_MALE(handle))) : "Someone";
        }

        private void DrawFeelingBars(Felt f)
        {
            Ped p = f.Ped;
            if (!p.Exists() || p.IsDead)
                return;
            Vector3 head = PED.GET_PED_BONE_COORDS(p.Handle, 21030, 0f, 0f, 0f);   // the head bone
            float x, y;
            if (!ScreenAt(head + new Vector3(0f, 0f, BarsAboveHead), out x, out y))
                return;
            float size = Feelings.SizeAt(f.Metres), w = 0.05f * size, h = 0.0055f * size, gap = 0.004f * size, text = 0.2f * size;
            float left = x - w / 2f;
            for (int i = 0; i < f.Bars.Count; i++)
            {
                FeelingBar b = f.Bars[i];
                float top = y + i * (h + gap);
                Draw.Rect(left - 0.001f, top - 0.001f, w + 0.002f, h + 0.002f, 10, 10, 10, 160);
                if (_cfg.FeelingBarsShowEmpty)
                    Draw.Rect(left, top, w, h, 72, 68, 62, 170);   // the whole length, so a little reads as a little
                Draw.Rect(left, top, w * b.Share, h, b.R, b.G, b.B, 230);
                Draw.Text(b.Label, left - 0.004f - Draw.Width(b.Label, text), top + h / 2f - Draw.LineHeight(text) / 2f, text, b.R, b.G, b.B, 230);
            }
        }

        private void DrawGrudgeMeter()
        {
            const float title = 0.27f, name = 0.23f, barH = 0.006f, pad = 0.006f;
            float rowH = Draw.LineHeight(name) + 0.002f + barH + 0.005f;
            float boxH = pad * 2f + Draw.LineHeight(title) + 0.004f + _meter.Count * rowH;
            Draw.Rect(MeterX, MeterY, MeterW, boxH, 12, 10, 8, 150);
            _meterBottom = MeterY + boxH;
            float y = MeterY + pad, barW = MeterW - pad * 2f, line = Feelings.LineAt(_grudgeLine);
            Draw.Text(Feelings.Title, MeterX + pad, y, title, 236, 214, 160);
            y += Draw.LineHeight(title) + 0.004f;
            foreach (GrudgeRow r in _meter)
            {
                Draw.Text(Draw.Fit(Feelings.RowText(r), name, barW), MeterX + pad, y, name, 225, 220, 210);
                y += Draw.LineHeight(name) + 0.002f;
                Draw.Rect(MeterX + pad, y, barW, barH, 40, 36, 32, 200);
                Draw.Rect(MeterX + pad, y, barW * Math.Min(1f, r.Score / 100f), barH, r.Nemesis ? 170 : 215, r.Nemesis ? 40 : 120, r.Nemesis ? 40 : 60, 230);
                if (!r.Nemesis && line > 0f)
                    Draw.Rect(MeterX + pad + barW * line - 0.0008f, y - 0.002f, 0.0016f, barH + 0.004f, 240, 230, 210, 230);   // where he'd become a nemesis
                y += barH + 0.005f;
            }
        }

        private static unsafe bool ScreenAt(Vector3 w, out float x, out float y)
        {
            float sx = 0f, sy = 0f;
            bool ok = GRAPHICS.GET_SCREEN_COORD_FROM_WORLD_COORD(w.X, w.Y, w.Z, &sx, &sy);
            x = sx;
            y = sy;
            return ok;
        }
    }
}
