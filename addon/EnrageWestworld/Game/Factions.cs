// The game's gangs and companies by their model names (2026-10-29, Lex: "how the enRAGE mod was able to identify the
// game's existing faction NPCs (such as Lemoyne Raiders or O'Driscoll Gang) to show that ... under their nameplate").
// enRAGE reads a token in the model's name (g_m_m_uniexconfeds_01 is a Lemoyne Raider, g_m_m_unibanditos_01 a Del Lobo);
// this is the same idea with tokens adapted from its gang table (its author permits reuse; credited here). Read only:
// a word for the name cards today, and the seed of roadmap 35 (the gangs as ours).
using System;

namespace EnrageWestworld.InGame
{
    internal static class Factions
    {
        // token in the lower-case model name -> what one member is called
        private static readonly string[][] Tokens =
        {
            new[] { "odriscoll", "O'Driscoll" }, new[] { "duster", "O'Driscoll" },
            new[] { "uniexconfeds", "Lemoyne Raider" }, new[] { "exconfed", "Lemoyne Raider" },
            new[] { "uniinbred", "Murfree" },
            new[] { "unimountainmen", "Skinner Brother" }, new[] { "skinnerbrother", "Skinner Brother" },
            new[] { "laramie", "Laramie" }, new[] { "uniranchers", "Laramie" },
            new[] { "unibanditos", "Del Lobo" },
            new[] { "uniswamp", "Night Folk" },
            new[] { "brontegoon", "Bronte man" }, new[] { "nbxbronte", "Bronte man" },
            new[] { "unigrays", "Gray" }, new[] { "unibraithwaites", "Braithwaite man" },
            new[] { "wapwarriors", "Wapiti" },
            new[] { "army", "Soldier" },
            new[] { "pinkerton", "Pinkerton" }
        };

        /// What a member of the gang this model belongs to is called ("Lemoyne Raider"), or null for anyone else.
        public static string MemberOf(uint model)
        {
            string m = Models.Name(model);
            if (string.IsNullOrEmpty(m))
                return null;
            foreach (string[] t in Tokens)
                if (m.Contains(t[0]))
                    return t[1];
            return null;
        }

        // the gangs' relationship groups, as seen in game (2026-10-29: the Raiders in the Valentine saloon, 0x211A2F38), for a
        // member whose model carries no token
        private static readonly System.Collections.Generic.Dictionary<uint, string> Groups = new System.Collections.Generic.Dictionary<uint, string>
        {
            { 0x211A2F38u, "Lemoyne Raider" }
        };

        /// By the model, else by the relationship group the game put him in.
        public static string MemberOf(uint model, uint relGroup)
        {
            string g = MemberOf(model);
            if (g != null)
                return g;
            string byGroup;
            return Groups.TryGetValue(relGroup, out byGroup) ? byGroup : null;
        }

        /// "an O'Driscoll", "a Lemoyne Raider".
        public static string AOne(string member)
        {
            if (string.IsNullOrEmpty(member))
                return null;
            return ("AEIOU".IndexOf(char.ToUpperInvariant(member[0])) >= 0 ? "An " : "A ") + member;
        }
    }
}
