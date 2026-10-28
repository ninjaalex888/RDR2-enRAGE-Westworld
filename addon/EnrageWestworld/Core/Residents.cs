// Residents: everyone who isn't one of enRAGE's own people. A resident is a small identity the add-on owns (a
// name, a trade, a town, a temperament) given to an ordinary person the moment something asks who they are.
// They are never added to enRAGE's roster, and nothing is ever written back to enRAGE about them.
// Only residents that something has happened to are saved (woken, talked to, remembered something); the rest
// are made up again if met again. The names are the add-on's own lists of common period names.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Resident
    {
        public const string Prefix = "res:";

        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Name;
        [DataMember(EmitDefaultValue = false)] public bool Woman;
        [DataMember(EmitDefaultValue = false)] public uint Model;            // the ped model they were met in; a returning resident is cast on a matching passer-by
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Trade;          // "a farmhand", "a townswoman"
        [DataMember(EmitDefaultValue = false)] public string Personality;    // Hothead, Coward, Proud, Cold, Greedy, Loyal, Drifter
        [DataMember(EmitDefaultValue = false)] public string Disposition;    // Friendly, Neutral, Surly
        [DataMember(EmitDefaultValue = false)] public string Sociability;    // Introvert, Outgoing
        [DataMember(EmitDefaultValue = false)] public string Devotion;       // Devout, Churchgoer, Lapsed, Godless
        [DataMember(EmitDefaultValue = false)] public string Smarts;         // Sharp, Average, Dim
        [DataMember(EmitDefaultValue = false)] public int BornYear;
        [DataMember(EmitDefaultValue = false)] public long MetAt;            // game minute they were made
        [DataMember(EmitDefaultValue = false)] public long TouchedAt;        // game minute something last happened to them
        [DataMember(EmitDefaultValue = false)] public bool Dead;
        [DataMember(EmitDefaultValue = false)] public string PostId;         // the counter they keep (see Posts.cs), if any
        [DataMember(EmitDefaultValue = false)] public string Kin;            // how they're related to the one who kept it before ("son"), null for a newcomer
        [DataMember(EmitDefaultValue = false)] public string KinOf;          // the one who kept it before, by name
        [DataMember(EmitDefaultValue = false)] public string From;           // a newcomer's old town
        [DataMember(EmitDefaultValue = false)] public long TookOverAt;       // game minute they took the post over, 0 for the first
        [DataMember(EmitDefaultValue = false)] public int Wounds;            // times they were shot down at their counter and the game brought them back
        [DataMember(EmitDefaultValue = false)] public long WoundedAt;
        [DataMember(EmitDefaultValue = false)] public bool WoundedByPlayer;
        [DataMember(EmitDefaultValue = false)] public bool KnowsShooter;     // they saw, or someone told them, that it was the player
        [DataMember(EmitDefaultValue = false)] public bool Fled;             // woke and walked out of their town (the wake acts' escape): lives rough now, never recast on a passer-by
        [DataMember(EmitDefaultValue = false)] public long FledAt;
        [DataMember(EmitDefaultValue = false)] public bool HasCamp;          // where they live in the wilds, chosen once
        [DataMember(EmitDefaultValue = false)] public float CampX, CampY, CampZ;
        [DataMember(EmitDefaultValue = false)] public bool Regular;          // one of their town's regulars (Regulars.cs): met again and again, kept
        [DataMember(EmitDefaultValue = false)] public int Outfit;            // a regular's outfit preset, put on whenever they're cast (read only when Regular or Cast)
        [DataMember(EmitDefaultValue = false)] public string Cast;           // one of the Testing page's test cast, by part (Core/TestCast.cs); null for everyone else

        public static bool IsResident(string id)
        {
            return id != null && id.StartsWith(Prefix);
        }
    }

    public static class Residents
    {
        private static readonly string[] MenFirst =
        {
            "Abel", "Albert", "Alfred", "Amos", "Andrew", "Asa", "August", "Barnaby", "Benjamin", "Calvin", "Carl", "Charles", "Clarence", "Cyrus",
            "Daniel", "David", "Dennis", "Edgar", "Edmund", "Edward", "Eli", "Ephraim", "Ezra", "Frank", "Frederick", "George", "Gideon", "Grover",
            "Harlan", "Harvey", "Henry", "Herman", "Horace", "Hugh", "Isaac", "Jacob", "James", "Jasper", "Jedediah", "Jesse", "John", "Joseph",
            "Josiah", "Lars", "Lemuel", "Levi", "Lewis", "Louis", "Luther", "Martin", "Matthew", "Milo", "Nathan", "Nelson", "Obadiah", "Oliver",
            "Orville", "Oscar", "Owen", "Patrick", "Pierce", "Rufus", "Samuel", "Seth", "Silas", "Simon", "Stephen", "Thaddeus", "Thomas", "Tobias",
            "Virgil", "Walter", "Warren", "Wilbur", "William", "Zachariah"
        };

        private static readonly string[] WomenFirst =
        {
            "Abigail", "Adelaide", "Agnes", "Alice", "Amelia", "Anna", "Beatrice", "Bertha", "Carrie", "Catherine", "Cecilia", "Charlotte", "Clara",
            "Cora", "Dorothy", "Edith", "Eleanor", "Eliza", "Ella", "Emma", "Esther", "Ethel", "Eunice", "Florence", "Frances", "Georgia", "Grace",
            "Hannah", "Harriet", "Hattie", "Helen", "Henrietta", "Ida", "Irene", "Isabel", "Jane", "Josephine", "Judith", "Julia", "Katherine",
            "Lavinia", "Lena", "Lillian", "Lottie", "Louisa", "Lucy", "Mabel", "Margaret", "Martha", "Mary", "Matilda", "Minnie", "Nellie", "Olive",
            "Opal", "Pearl", "Phoebe", "Rachel", "Rebecca", "Rosa", "Ruth", "Sadie", "Sarah", "Susannah", "Sylvia", "Theodora", "Viola", "Winifred"
        };

        private static readonly string[] Surnames =
        {
            "Abbott", "Ackerman", "Adair", "Alder", "Ames", "Archer", "Ashby", "Bailey", "Baxter", "Beckett", "Bellamy", "Bishop", "Blackwood",
            "Boone", "Bowman", "Brannigan", "Briggs", "Brockway", "Calloway", "Carmichael", "Carver", "Chandler", "Clayton", "Coleman", "Cobb",
            "Crandall", "Cutler", "Dalton", "Davenport", "Dawes", "Delaney", "Dillard", "Donovan", "Drummond", "Eastman", "Eldridge", "Ellison",
            "Emerson", "Fairchild", "Farrow", "Fenton", "Fitch", "Fletcher", "Foster", "Gallagher", "Garrity", "Gentry", "Gilbert", "Goodwin",
            "Granger", "Gunderson", "Haggerty", "Halloran", "Hammond", "Hartley", "Hastings", "Hatfield", "Hawkins", "Hendry", "Holloway",
            "Hubbard", "Ingram", "Jennings", "Kearney", "Kimball", "Kirby", "Lambert", "Lacey", "Larkin", "Lathrop", "Lindqvist", "Lowry",
            "Mabry", "Maddox", "Mallory", "Marsh", "Maxwell", "McAllister", "McBride", "McKenna", "Merritt", "Mercer", "Mulligan", "Munro",
            "Nash", "Nolan", "Norwood", "Oakes", "Odell", "Parrish", "Pemberton", "Pickett", "Pruitt", "Quigley", "Radford", "Rafferty",
            "Redmond", "Rhodes", "Ridley", "Rowland", "Rutledge", "Sanderson", "Sawyer", "Schuyler", "Shelby", "Sinclair", "Slater", "Spencer",
            "Stanton", "Stockdale", "Sullivan", "Talbot", "Tanner", "Thackeray", "Thorne", "Tolliver", "Tucker", "Underhill", "Vance", "Vickers",
            "Wadsworth", "Waller", "Walsh", "Wheeler", "Whitaker", "Whitlow", "Wickham", "Winslow", "Wolcott", "Wyatt", "Yardley", "Yates"
        };

        private static readonly string[] Personalities = { "Hothead", "Coward", "Proud", "Cold", "Greedy", "Loyal", "Drifter", null, null, null, null };
        private static readonly string[] Dispositions = { "Friendly", "Neutral", "Neutral", "Surly" };

        public static string NameFor(bool woman, string seed)
        {
            return FirstName(woman, seed) + " " + Surname(seed);
        }

        /// A first name or a surname alone, for the people in someone's life (a child, a creditor, a sweetheart).
        public static string FirstName(bool woman, string seed)
        {
            string[] first = woman ? WomenFirst : MenFirst;
            return first[Voice.Hash(seed + "f") % first.Length];
        }

        /// Whether a word is one of the names residents are given (the language layer's check).
        internal static bool IsName(string w)
        {
            return Array.IndexOf(MenFirst, w) >= 0 || Array.IndexOf(WomenFirst, w) >= 0 || Array.IndexOf(Surnames, w) >= 0;
        }

        public static string Surname(string seed)
        {
            return Surnames[Voice.Hash(seed + "s") % Surnames.Length];
        }

        /// A new resident for an ordinary person. The same seed always gives the same person.
        public static Resident Make(string id, string seed, uint model, bool woman, string town, string trade, long now, int year)
        {
            return new Resident
            {
                Id = id,
                Name = NameFor(woman, seed),
                Woman = woman,
                Model = model,
                Town = town,
                Trade = trade,
                Personality = Personalities[Voice.Hash(seed + "p") % Personalities.Length],
                Disposition = Dispositions[Voice.Hash(seed + "d") % Dispositions.Length],
                Sociability = Voice.Hash(seed + "o") % 2 == 0 ? "Outgoing" : "Introvert",
                Devotion = Voice.Pick(seed + "v", "Devout", "Churchgoer", "Churchgoer", "Lapsed", "Lapsed", "Godless"),
                Smarts = Voice.Pick(seed + "m", "Sharp", "Average", "Average", "Average", "Dim"),
                BornYear = year - 18 - Voice.Hash(seed + "b") % 45,
                MetAt = now,
                TouchedAt = now
            };
        }

        /// The plain Person the voice and awareness code works on. Every collection is filled in, nothing is
        /// known about the player, and it carries no enRAGE data.
        public static Person ToPerson(Resident r)
        {
            return new Person
            {
                Id = r.Id,
                Name = r.Name,
                Woman = r.Woman,
                Home = r.Town,
                Trade = r.Trade,
                Personality = r.Personality,
                Disposition = r.Disposition,
                Sociability = r.Sociability,
                Devotion = r.Devotion,
                Smarts = r.Smarts,
                BornYear = r.BornYear,
                ModelHash = r.Model,
                IsAlive = !r.Dead
            };
        }
    }
}
