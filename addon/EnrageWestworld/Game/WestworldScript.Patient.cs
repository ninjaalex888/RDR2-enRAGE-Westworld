// The doctor's patient ([Scenes] DoctorsPatient; the words and the choosing are Core/Patient.cs): by day in a town whose
// doctor is about (Valentine's and Rhodes's at their posts, Saint Denis's by his story model), someone within 16 m of you
// walks to the doctor for a complaint of their own (Lex, 2026-10-29: "instead of bringing their child in, NPCs can go to
// the doctor and try to interact"): a child's fever or a tooth when that's their worry, ribs after a beating, else
// something by their age, trade and sex. At the doctor they face him and say what's wrong, he looks at them and gives his
// word (both aloud: the box, their own recorded voices, and the ticker within ScenesOnTickerMetres), and they walk back
// thinking it over. They count as busy for the other features while they walk (IsAcknowledging). The doctor is only
// looked at and asked to look back (TASK_LOOK_AT_ENTITY, head only): he keeps his post. A look every 60 s from the drama
// tick, Pacing "patient" (35%, four game hours between, a day a person); without a test, someone goes only when it's
// pressing (Patient.Pressing). Never two together; a mission, a load, the wipe and the add-on stopping end a walk
// (PatientAbort). A test takes the nearest named person and gives them their complaint, so it can be seen on demand.
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
        private sealed class PatientRun
        {
            public Ped Walker, Doctor;
            public string Name, DoctorName, Child, Complaint, Seed, Phase = "going", Pending;   // going, talk, after, back; Pending: a thought kept until they're on screen
            public int Stage, StartedAt, PhaseAt, StillSince, Said;
            public uint Model;
            public Vector3 From, DoctorAt;
            public bool Test;
            public bool Retried;   // stopped short once and sent on again, right up to the doctor
        }

        private PatientRun _patient;
        private int _patientLookAt, _patients, _patientsArrived, _patientsTalked;
        private string _patientLast;

        /// A doctor in this body: a doctor's post (Valentine, Rhodes) or Saint Denis's doctor by his story model.
        private static bool IsDoctor(Ped p)
        {
            PostKind k = PostKindOf(p);
            return (k != null && k.Kind == "doctor") || Natives.ModelName(p) == "cs_sddoctor_01";
        }

        /// What to call the doctor: the post's person by name, else "The doctor".
        private string DoctorLabel(Ped p)
        {
            PostKind k = PostKindOf(p);
            Resident r = k != null ? HolderAt(p, k) : null;
            return r != null && !string.IsNullOrEmpty(r.Name) ? r.Name : "The doctor";
        }

        /// From the drama tick: a look every 60 s by day in a town; the patient's walk if one is live.
        private bool PatientScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.DoctorsPatient)
            {
                why = "off ([Scenes] DoctorsPatient)";
                return false;
            }
            if (!_cfg.Drama)
            {
                why = "[World] Drama is off";
                return false;
            }
            if (_patient != null)
            {
                why = "one is walking already (" + _patient.Name + ", " + _patient.Phase + ")";
                return false;
            }
            if (!test && now - _patientLookAt < 60000 && now >= _patientLookAt)
                return false;
            _patientLookAt = now;
            if (_placeTown == null)
            {
                why = "outside the towns";
                return false;
            }
            if (!test && !Patient.Hour(CLOCK.GET_CLOCK_HOURS()))
            {
                why = "the doctor's hours (eight to six)";
                return false;
            }
            long g = _store.LastGameMin;
            string skipped;
            string pre = Pacing.WhyNot(_store.Pace, "patient", _placeTown, null, g, _rng.Next(100), test, out skipped);
            if (pre != null)
            {
                why = pre;
                return false;
            }
            // a doctor about, within 90 m
            Ped doctor = World.GetAllPeds().Where(p => p != null && p.Exists() && !ENTITY.IS_ENTITY_DEAD(p.Handle) && p.Position.DistanceTo(me.Position) <= 90f && IsDoctor(p))
                                            .OrderBy(p => p.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (doctor == null)
            {
                why = "no doctor within 90 m (Valentine's, Rhodes's or Saint Denis's)";
                return false;
            }
            long day = g / 1440;
            int year = Voice.NowYear > 0 ? Voice.NowYear : 1899;
            List<Candidate> people = ReasonCandidates(me, now, test);
            Candidate pick = null;
            string complaint = null;
            int stage = 0;
            var notToday = new List<string>();
            foreach (Candidate c in people)
            {
                if (c.Who.P == null || c.Who.Name == null)
                    continue;
                int h = c.Ped.Handle;
                if (h == doctor.Handle || IsDoctor(c.Ped) || NotFree(h) || PED.IS_PED_USING_ANY_SCENARIO(h) || !NodInView(me, c.Ped) || MayDirectWhyNot(c.Ped) != null)
                    continue;
                string kind = null; int st = 0; bool good = false;
                InnerLife.WorryNow(c.Who.P, day, out kind, out st, out good);
                string seed = c.Who.Id + "|" + day;
                string what = Patient.ComplaintOf(c.Who.P.AgeIn(year), c.Who.P.Trade, c.Who.Woman, c.Who.Hurt != null || FoughtLately(c.Who.Id, g), kind, st, seed);
                if (!test)
                {
                    string ignored;
                    if (!Patient.Pressing(what, seed) || Pacing.WhyNot(_store.Pace, "patient", null, c.Who.Id, g, 0, false, out ignored) != null)
                    {
                        if (notToday.Count < 6)
                            notToday.Add(c.Who.Name + " (" + Patient.Why(what, "a child", st) + ", not today)");
                        continue;
                    }
                }
                pick = c;
                complaint = what;
                stage = Patient.Takes(kind, st) ? st : 1;
                break;
            }
            if (pick == null)
            {
                why = "nobody within 16 m free, in view and ailing enough to go today" + (notToday.Count > 0 ? " (" + string.Join(", ", notToday) + ")" : "");
                return false;
            }
            string child = Residents.FirstName(Voice.Hash(pick.Who.P.Id + "|cw") % 2 == 0, pick.Who.P.Id + "|child");   // the child the fever worry names (InnerLife's own naming)
            var run = new PatientRun
            {
                Walker = pick.Ped, Doctor = doctor, Name = pick.Who.Name, DoctorName = DoctorLabel(doctor), Child = child, Complaint = complaint, Seed = "pt" + now,
                Stage = stage, StartedAt = now, PhaseAt = now, From = pick.Ped.Position, DoctorAt = doctor.Position, Test = test, Model = ENTITY.GET_ENTITY_MODEL(pick.Ped.Handle)
            };
            _patient = run;
            _patients++;
            Pacing.Note(_store.Pace, "patient", _placeTown, pick.Who.Id, g);   // a test too, so a test doesn't leave the world free for another at once
            NoteInScene(new[] { pick.Ped }, "patient", now);
            PatientWalk(run, run.Doctor.Position);
            _ticker.Push(run.Name + ", thinking", Fill(Patient.Going(complaint, stage, run.Seed), run), "near", pick.Ped.Handle);
            Log.Write("PATIENT " + run.Name + " walks to " + run.DoctorName.ToLowerInvariant().Replace("the doctor", "the doctor") + " for " + Patient.Why(complaint, child, stage) + " (" + (int)run.From.DistanceTo(run.DoctorAt) + " m)" +
                      (test ? " (a test; skipped: " + skipped + ")" : ""));
            return true;
        }

        private static string Fill(string line, PatientRun r)
        {
            return line == null ? null : line.Replace("{who}", r.Child);
        }

        /// A beating lately: a blows memory of their own (done to them or by them) within the last game day.
        private bool FoughtLately(string id, long g)
        {
            PersonState st = id != null ? _store.Find(id) : null;
            return st != null && st.Mind != null && st.Mind.Any(m => m != null && m.Kind == "blows" && m.How == "did" && g - m.At >= 0 && g - m.At < 1440);
        }

        /// Busy for the other features: the one walking to the doctor's.
        private bool PatientWalking(int h)
        {
            return _patient != null && _patient.Walker != null && _patient.Walker.Handle == h;
        }

        /// A mission, a save load, the wipe, the add-on stopping: the walk ends with no more orders.
        private void PatientAbort(string why)
        {
            PatientRun r = _patient;
            _patient = null;
            if (r == null)
                return;
            if (r.Walker != null && r.Walker.Exists() && !r.Walker.IsDead && ENTITY.GET_ENTITY_MODEL(r.Walker.Handle) == r.Model)
                TASK.CLEAR_PED_TASKS(r.Walker.Handle, true, false);
            Log.Write("PATIENT " + r.Name + ": " + why);
        }

        /// Briskly there (an errand), at an ordinary walk home again (Lex, 2026-10-29: "once he is done talking to the doctor, they
        /// should return back to normal speed").
        private static void PatientWalk(PatientRun r, Vector3 to, bool home = false)
        {
            int h = r.Walker.Handle;
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, to.X, to.Y, to.Z, home ? 1f : Natives.Brisk, 120000, 1.5f, 0, 0f);
        }

        /// A line said aloud in the visit: the box with their recorded voice, and the ticker while it's within earshot.
        private void PatientSay(string label, string line, Ped speaker, Ped listener)
        {
            if (string.IsNullOrEmpty(line) || speaker == null || !speaker.Exists())
                return;
            // the voice now, as it is said (through the box's onShow it waited on the box, which stays off while the game's own subtitles are on:
            // Lex heard nothing, 2026-10-29)
            Func<string> voice = Speech.For(speaker.Handle, line, listener != null && listener.Exists() ? listener.Handle : 0, fallback: "chat");
            bool played = false;
            try
            {
                Speech.LastPlayed = false;
                if (voice != null)
                    voice();
                played = Speech.LastPlayed;
            }
            catch { }
            Feed.Say(label, line, null, speaker.Handle);
            if (_cfg.ScenesOnTicker && _cfg.Ticker && speaker.Position.DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                _ticker.Say(label, line, speaker.Handle, Math.Min(7000, 1600 + line.Length * 55));
            Log.Write("PATIENT   " + label + ": " + line + (voice == null ? " (no voice for it)" : played ? " (voiced)" : " (the game turned the voice down)"));
        }

        /// Every tick while one is walking (from the drama tick's top).
        private void PatientStep(int now)
        {
            PatientRun r = _patient;
            if (r == null)
                return;
            Ped me = Game.Player.Character;
            if (r.Walker == null || !r.Walker.Exists() || r.Walker.IsDead || ENTITY.GET_ENTITY_MODEL(r.Walker.Handle) != r.Model || me == null || !me.Exists() || r.Walker.Position.DistanceTo(me.Position) > 150f)
            {
                EndPatient(r.Walker != null && r.Walker.Exists() && !r.Walker.IsDead && ENTITY.GET_ENTITY_MODEL(r.Walker.Handle) == r.Model ? "left behind" : "gone");
                return;
            }
            int h = r.Walker.Handle;
            if (r.Pending != null && NodInView(me, r.Walker))
            {
                _ticker.Push(r.Name + ", thinking", r.Pending, "near", h);   // the thought once they're on screen
                r.Pending = null;
            }
            bool doctorHere = r.Doctor != null && r.Doctor.Exists() && !r.Doctor.IsDead;
            Vector3 target = r.Phase == "back" ? r.From : doctorHere ? r.Doctor.Position : r.DoctorAt;
            float d = r.Walker.Position.DistanceTo(target);
            // stopped moving a while (at a counter, or at the door if the nav mesh won't take them in), after a first few seconds of walking
            bool still = now - r.PhaseAt > 5000 && ENTITY.GET_ENTITY_SPEED(h) < 0.3f;
            if (still && r.StillSince == 0)
                r.StillSince = now;
            else if (!still)
                r.StillSince = 0;
            bool stopped = r.StillSince != 0 && now - r.StillSince > 3000;
            switch (r.Phase)
            {
                case "going":
                    if (stopped && d >= 2.6f && !r.Retried && doctorHere)
                    {
                        // stopped short (at the door): on again, right up to him, once (Lex, 2026-10-29: they stopped at the door)
                        r.Retried = true;
                        r.StillSince = 0;
                        r.PhaseAt = now;
                        TASK.TASK_GO_TO_ENTITY(h, r.Doctor.Handle, 30000, Natives.Brisk, 1.0f, 0f, 0);
                        return;
                    }
                    if (d < 2.6f || stopped || now - r.PhaseAt > 120000)
                    {
                        r.Phase = "talk";
                        r.PhaseAt = now;
                        r.StillSince = 0;
                        r.Said = 0;
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        bool close = doctorHere && d < 8f;   // near enough to talk to him (over a counter, too)
                        if (close)
                        {
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, r.Doctor.Handle, 4000, 0f, 0f, 0f);
                            TASK.TASK_LOOK_AT_ENTITY(h, r.Doctor.Handle, 12000, 0, 51, 0);
                            TASK.TASK_LOOK_AT_ENTITY(r.Doctor.Handle, h, 12000, 0, 51, 0);   // head only: the doctor keeps his post
                            _patientsArrived++;
                        }
                        else
                            r.Said = 3;   // nobody to talk to: the thought alone, then home
                        Log.Write("PATIENT " + r.Name + (close ? " has reached " + r.DoctorName.Replace("The doctor", "the doctor") + " (" + d.ToString("0.0") + " m)" :
                                                       stopped ? " stopped " + (int)d + " m from the doctor (the door, likely): no word with him" : " gave up short of the doctor (" + (int)d + " m)"));
                    }
                    return;
                case "talk":
                    // what's wrong, said to him; his word; a moment, then home
                    if (r.Said == 0 && now - r.PhaseAt > 800)
                    {
                        PatientSay(r.Name, Fill(Patient.Ask(r.Complaint, r.Stage, r.Seed), r), r.Walker, r.Doctor);
                        r.Said = 1;
                        r.PhaseAt = now;
                    }
                    else if (r.Said == 1 && now - r.PhaseAt > 3800)
                    {
                        if (doctorHere)
                        {
                            PatientSay(r.DoctorName, Fill(Patient.Answer(r.Complaint, r.Stage, r.Seed), r), r.Doctor, r.Walker);
                            _patientsTalked++;
                        }
                        r.Said = 2;
                        r.PhaseAt = now;
                        TASK.TASK_STAND_STILL(h, 8000);
                    }
                    else if ((r.Said == 2 && now - r.PhaseAt > 5500) || (r.Said == 3 && now - r.PhaseAt > 8000))
                    {
                        r.Phase = "back";
                        r.PhaseAt = now;
                        r.StillSince = 0;
                        PatientWalk(r, r.From, true);
                        r.Pending = Fill(Patient.Back(r.Complaint, r.Stage, r.Seed), r);
                        Log.Write("PATIENT " + r.Name + " heads home");
                    }
                    return;
                case "back":
                    if (d < 3f || stopped || now - r.PhaseAt > 120000)
                        EndPatient("home again");
                    return;
            }
        }

        private void EndPatient(string why)
        {
            PatientRun r = _patient;
            _patient = null;
            if (r == null)
                return;
            if (r.Walker != null && r.Walker.Exists() && !r.Walker.IsDead)
                WanderLater(r.Walker.Handle, Game.GameTime);   // let go to the street, as the Sabbath's kneeler is
            _patientLast = r.Name + " (" + Patient.Why(r.Complaint, r.Child, r.Stage) + "): " + why;
            Log.Write("PATIENT " + r.Name + " " + why);
        }

        private string PatientStatus()
        {
            if (!_cfg.DoctorsPatient)
                return "off ([Scenes] DoctorsPatient)";
            return "on (the doctor's hours, a town with its doctor within 90 m, someone ailing enough to go); " + _patients + " walks, " + _patientsArrived + " reached the doctor, " + _patientsTalked + " heard his word" +
                   (_patient != null ? "; now: " + _patient.Name + " (" + _patient.Complaint + ", " + _patient.Phase + ")" : "") + (_patientLast != null ? "; last: " + _patientLast : "");
        }
    }
}
