// Ped model names of our own (2026-10-29, roadmap 34 phase A: standing alone). Until now the add-on read them through
// enRAGE's Models class; the names come from the API's PedHash enum (the game's own model names, lower case), cached
// by hash, and a woman's model has "_f_" in it. Adapted from enRAGE's Models (its author permits reuse; credited here).
using System;
using System.Collections.Generic;
using RDR2;

namespace EnrageWestworld.InGame
{
    public static class Models
    {
        private static readonly Dictionary<uint, string> _raw = new Dictionary<uint, string>();
        private static readonly Dictionary<uint, string> _lower = new Dictionary<uint, string>();

        /// The model's name as the enum spells it, "" for one the enum doesn't know.
        public static string Raw(uint model)
        {
            string value;
            if (_raw.TryGetValue(model, out value))
                return value;
            value = Enum.IsDefined(typeof(PedHash), model) ? ((PedHash)model).ToString() : string.Empty;
            _raw[model] = value;
            return value;
        }

        /// The model's name in lower case ("a_m_m_valtownfolk_01"), "" when unknown.
        public static string Name(uint model)
        {
            string value;
            if (_lower.TryGetValue(model, out value))
                return value;
            value = Raw(model).ToLowerInvariant();
            _lower[model] = value;
            return value;
        }

        public static string Name(Ped p)
        {
            return p == null ? string.Empty : Name((uint)p.Model.Hash);
        }

        /// A woman's model ("_f_" in the name); false for 0 or an unknown model.
        public static bool Female(uint model)
        {
            return model != 0 && Name(model).Contains("_f_");
        }
    }
}
