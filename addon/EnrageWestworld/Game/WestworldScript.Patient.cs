// The doctor's patient ([Scenes] DoctorsPatient; the words are Core/Patient.cs): by day in a town whose doctor is at
// his post (Valentine and Rhodes have one), someone within 16 m of you whose child has the fever (their own worry,
// stage 1 or 2) walks to the doctor's, stands inside a while, and walks back to where they started, a thought over their
// head at each turn (the later two once they're on screen). They count as busy for the other features while they walk
// (IsAcknowledging). Nobody else is involved; the doctor keeps his post. A look every 60 s from the drama tick, Pacing
// "patient" (20%, twelve game hours between, a day a person). Never two together; a mission, a load, the wipe and the
// add-on stopping end a walk (PatientAbort). A test takes the nearest named person and supplies the worry when nobody
// carries it, so the walk can be seen on demand. Night run 2026-10-28, item 14 (Tier 4's 16).
using System;
using System.Collections.Generic;
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
            public Ped Walker;
            public string Name, Child, Seed, Phase = "going", Pending;   // going, inside, back, done; Pending: a thought kept until they're on screen
            public int Stage, StartedAt, PhaseAt, StillSince;
            public uint Model;
            public Vector3 From, DoctorAt;
            public bool Test;
        }

        private PatientRun _patient;
        private int _patientLookAt, _patients, _patientsArrived;
        private string _patientLast;

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
            // the doctor at his post, within 90 m
            Ped doctor = null;
            foreach (Ped p in World.GetAllPeds())
            {
                if (!p.Exists() || p.Position.DistanceTo(me.Position) > 90f)
                    continue;
                PostKind k = PostKindOf(p);
                if (k != null && k.Kind == "doctor" && !ENTITY.IS_ENTITY_DEAD(p.Handle))
                {
                    doctor = p;
                    break;
                }
            }
            if (doctor == null)
            {
                why = "the doctor isn't at his post within 90 m";
                return false;
            }
            // someone near with a child down with the fever
            long day = g / 1440;
            List<Candidate> people = ReasonCandidates(me, now, test);
            Candidate pick = null, spare = null;
            int stage = 0;
            var refused = new List<string>();
            foreach (Candidate c in people)
            {
                if (c.Who.P == null)
                    continue;
                int h = c.Ped.Handle;
                if (NotFree(h) || PED.IS_PED_USING_ANY_SCENARIO(h) || !NodInView(me, c.Ped) || MayDirectWhyNot(c.Ped) != null)
                    continue;
                string kind = null; int st = 0; bool good = false;
                if (!InnerLife.WorryNow(c.Who.P, day, out kind, out st, out good) || !Patient.Takes(kind, st))
                {
                    if (refused.Count < 6)
                        refused.Add(c.Who.Name + " (" + (kind ?? "no worry") + ")");
                    if (test && spare == null && c.Who.Name != null)
                        spare = c;   // a test takes the nearest named person and supplies the worry
                    continue;
                }
                string ignored;
                if (!test && Pacing.WhyNot(_store.Pace, "patient", null, c.Who.Id, g, 0, false, out ignored) != null)
                    continue;
                pick = c;
                stage = st;
                break;
            }
            bool supplied = false;
            if (pick == null && test && spare != null)
            {
                pick = spare;
                stage = 1;
                supplied = true;
            }
            if (pick == null)
            {
                why = "nobody within 16 m with a child down with the fever, free and in view" + (refused.Count > 0 ? " (" + string.Join(", ", refused) + ")" : "");
                return false;
            }
            string child = Residents.FirstName(Voice.Hash(pick.Who.P.Id + "|cw") % 2 == 0, pick.Who.P.Id + "|child");   // the child the worry names (InnerLife's own naming)
            var run = new PatientRun { Walker = pick.Ped, Name = pick.Who.Name, Child = child, Seed = "pt" + now, Stage = stage, StartedAt = now, PhaseAt = now, From = pick.Ped.Position, DoctorAt = doctor.Position, Test = test, Model = ENTITY.GET_ENTITY_MODEL(pick.Ped.Handle) };
            _patient = run;
            _patients++;
            Pacing.Note(_store.Pace, "patient", _placeTown, pick.Who.Id, g);   // a test too, so a test doesn't leave the world free for another at once
            NoteInScene(new[] { pick.Ped }, "patient", now);
            PatientWalk(run, run.DoctorAt);
            _ticker.Push(run.Name + ", thinking", Patient.Going(stage, run.Seed).Replace("{who}", child), "near", pick.Ped.Handle);
            Log.Write("PATIENT " + run.Name + " walks to the doctor for " + Patient.Why(child, stage) + " (" + (int)run.From.DistanceTo(run.DoctorAt) + " m)" +
                      (test ? " (a test" + (supplied ? ", the worry supplied" : "") + "; skipped: " + skipped + ")" : ""));
            return true;
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

        private static void PatientWalk(PatientRun r, Vector3 to)
        {
            int h = r.Walker.Handle;
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, to.X, to.Y, to.Z, 1.2f, 120000, 1.5f, 0, 0f);
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
            float d = r.Walker.Position.DistanceTo(r.Phase == "back" ? r.From : r.DoctorAt);
            // stopped moving a while (at the doctor's door, if the nav mesh won't take them inside), after a first few seconds of walking
            bool still = now - r.PhaseAt > 5000 && ENTITY.GET_ENTITY_SPEED(h) < 0.3f;
            if (still && r.StillSince == 0)
                r.StillSince = now;
            else if (!still)
                r.StillSince = 0;
            bool stopped = r.StillSince != 0 && now - r.StillSince > 3000;
            switch (r.Phase)
            {
                case "going":
                    if (d < 6f || stopped || now - r.PhaseAt > 120000)
                    {
                        r.Phase = "inside";
                        r.PhaseAt = now;
                        r.StillSince = 0;
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_STAND_STILL(h, 25000);
                        if (d < 10f)
                            _patientsArrived++;
                        r.Pending = Patient.Inside(r.Stage, r.Seed).Replace("{who}", r.Child);
                        Log.Write("PATIENT " + r.Name + (d < 6f ? " has reached the doctor" : stopped ? " stopped at " + (int)d + " m from the doctor (the door, likely)" : " gave up short of the doctor's door (" + (int)d + " m)"));
                    }
                    return;
                case "inside":
                    if (now - r.PhaseAt > 20000)
                    {
                        r.Phase = "back";
                        r.PhaseAt = now;
                        r.StillSince = 0;
                        PatientWalk(r, r.From);
                        r.Pending = Patient.Back(r.Stage, r.Seed).Replace("{who}", r.Child);
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
            _patientLast = r.Name + ": " + why;
            Log.Write("PATIENT " + r.Name + " " + why);
        }

        private string PatientStatus()
        {
            if (!_cfg.DoctorsPatient)
                return "off ([Scenes] DoctorsPatient)";
            return "on (the doctor's hours, a town with its doctor within 90 m, someone with a child down with the fever); " + _patients + " walks, " + _patientsArrived + " reached the doctor's" +
                   (_patient != null ? "; now: " + _patient.Name + " (" + _patient.Phase + ")" : "") + (_patientLast != null ? "; last: " + _patientLast : "");
        }
    }
}
