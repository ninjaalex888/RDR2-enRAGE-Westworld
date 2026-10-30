// The game's crime types by name, for the log (Lex, 2026-10-30: "is this from legitimate action?"; the LAW lines showed
// only a number). Names from femga/rdr3_discoveries AI/EVENTS/crime_types.lua, each the joaat of its CRIME_ name.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Crimes
    {
        public const uint Trample = 0x45DB39D8;

        private static readonly Dictionary<uint, string> Names = new Dictionary<uint, string>
        {
            { 0x68134DC7, "arson" }, { 0x0BADC882, "assault" }, { 0x18DA55EE, "assaulting an animal" }, { 0x4E5F23F2, "assaulting a corpse" },
            { 0xC4736181, "assaulting a horse" }, { 0xD7466D7C, "assaulting the law" }, { 0xCCE1CCBD, "assaulting livestock" }, { 0x6A1ADE3D, "bank robbery" },
            { 0xA54C77E0, "burglary" }, { 0xA2FF1145, "cheating" }, { 0x5011F613, "disturbing the peace" }, { 0x3EBA7A37, "an explosion" },
            { 0x971EA5AF, "grave robbing" }, { 0x58488776, "hassling" }, { 0xFF0A3CC4, "hit and run" }, { 0x064814AF, "hit and run on the law" },
            { 0x8319FBAB, "intimidation" }, { 0x82F7E4A2, "taking a horse from its rider" }, { 0x6B981F4C, "taking a wagon from its driver" }, { 0x12C1D589, "a jail break" },
            { 0x98F908DB, "kidnapping" }, { 0xFD72A7EA, "kidnapping the law" }, { 0x56EE5D5A, "a lasso assault" }, { 0x1CB91DF0, "threatening the law" },
            { 0x6629D2F4, "loitering" }, { 0x55AD2BEB, "looting" }, { 0xE28ECE7E, "murder" }, { 0x48F59A66, "killing an animal" },
            { 0xC7261D79, "killing a horse" }, { 0x7797FCE7, "murdering a lawman" }, { 0x9569C546, "killing livestock" }, { 0x533B003D, "destroying property" },
            { 0xDF577BA5, "resisting arrest" }, { 0xA3BEDE4C, "robbery" }, { 0xFC738E61, "stagecoach robbery" }, { 0x9A949C79, "stolen goods" },
            { 0x72ADE410, "theft" }, { 0xBE3A5838, "horse theft" }, { 0x85BA08FD, "livestock theft" }, { 0x43A9ECA1, "wagon theft" },
            { 0x941C985A, "threatening" }, { 0x7F908566, "threatening the law" }, { 0x647D2A5A, "train robbery" }, { Trample, "trampling" },
            { 0xF00F266B, "trampling a lawman" }, { 0xAEDE8E35, "trespassing" }, { 0x5098CC5A, "unarmed assault" }, { 0x80FDC759, "vandalism" },
            { 0xF9E7ECE4, "vandalising a wagon" }, { 0x54A85DDC, "wrecking a wagon" }, { 0xAF074F6D, "being an accomplice" }, { 0x91D0A0E1, "a poison explosion" },
            { 0xF5ABD6C9, "murdering a player" }, { 0xD55C6A79, "killing a player's horse" }, { 0xBD6A0AA3, "self-defence" }, { 0x75970C15, "trampling a player" }
        };

        /// "trampling (0x45DB39D8)": the name where it's known, the number always (for a report).
        public static string Name(uint crime)
        {
            string n;
            return (Names.TryGetValue(crime, out n) ? n + " " : "") + "(0x" + crime.ToString("X8") + ")";
        }
    }
}
