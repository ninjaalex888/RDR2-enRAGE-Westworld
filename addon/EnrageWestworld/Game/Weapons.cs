// Weapon hash -> how a dead man would put it: "a revolver", "a knife", "bare hands".
using System.Collections.Generic;

namespace EnrageWestworld.InGame
{
    internal static class Weapons
    {
        private static Dictionary<uint, string> _phrases;

        private static readonly string[] Names =
        {
            "weapon_unarmed", "weapon_bow", "weapon_bow_improved", "weapon_lasso", "weapon_lasso_reinforced",
            "weapon_melee_knife", "weapon_melee_knife_jawbone", "weapon_melee_knife_civil_war", "weapon_melee_knife_horror", "weapon_melee_knife_miner",
            "weapon_melee_knife_rustic", "weapon_melee_knife_trader", "weapon_melee_knife_vampire", "weapon_melee_knife_bear", "weapon_melee_knife_john",
            "weapon_melee_machete", "weapon_melee_machete_collector", "weapon_melee_machete_horror", "weapon_melee_cleaver", "weapon_melee_torch",
            "weapon_melee_hatchet", "weapon_melee_hatchet_double_bit", "weapon_melee_hatchet_double_bit_rusted", "weapon_melee_hatchet_hewing",
            "weapon_melee_hatchet_hunter", "weapon_melee_hatchet_hunter_rusted", "weapon_melee_hatchet_viking", "weapon_melee_ancient_hatchet",
            "weapon_melee_broken_sword", "weapon_melee_lantern", "weapon_melee_davy_lantern",
            "weapon_pistol_m1899", "weapon_pistol_mauser", "weapon_pistol_semiauto", "weapon_pistol_volcanic",
            "weapon_repeater_carbine", "weapon_repeater_evans", "weapon_repeater_henry", "weapon_repeater_winchester", "weapon_repeater_winchester_john",
            "weapon_revolver_cattleman", "weapon_revolver_cattleman_john", "weapon_revolver_cattleman_mexican", "weapon_revolver_doubleaction",
            "weapon_revolver_doubleaction_exotic", "weapon_revolver_doubleaction_gambler", "weapon_revolver_lemat", "weapon_revolver_navy",
            "weapon_revolver_navy_crossover", "weapon_revolver_schofield", "weapon_revolver_schofield_golden", "weapon_revolver_schofield_calloway",
            "weapon_rifle_boltaction", "weapon_rifle_elephant", "weapon_rifle_springfield", "weapon_rifle_varmint",
            "weapon_shotgun_doublebarrel", "weapon_shotgun_doublebarrel_exotic", "weapon_shotgun_pump", "weapon_shotgun_repeating",
            "weapon_shotgun_sawedoff", "weapon_shotgun_semiauto",
            "weapon_sniperrifle_carcano", "weapon_sniperrifle_rollingblock", "weapon_sniperrifle_rollingblock_exotic",
            "weapon_thrown_bolas", "weapon_thrown_dynamite", "weapon_thrown_molotov", "weapon_thrown_poisonbottle", "weapon_thrown_throwing_knives",
            "weapon_thrown_tomahawk", "weapon_thrown_tomahawk_ancient",
            "weapon_fire", "weapon_explosion", "weapon_fall", "weapon_drowning", "weapon_horse", "weapon_run_over_by_car", "weapon_rammed_by_car", "weapon_bleeding"
        };

        private static string PhraseOf(string name)
        {
            if (name == "weapon_unarmed") return "bare hands";
            if (name.StartsWith("weapon_revolver")) return "a revolver";
            if (name.StartsWith("weapon_pistol")) return "a pistol";
            if (name.StartsWith("weapon_repeater")) return "a repeater";
            if (name.StartsWith("weapon_sniperrifle")) return "a rifle, from far off";
            if (name.StartsWith("weapon_rifle")) return "a rifle";
            if (name.StartsWith("weapon_shotgun")) return "a shotgun";
            if (name.StartsWith("weapon_bow")) return "an arrow";
            if (name.StartsWith("weapon_lasso")) return "a rope";
            if (name.Contains("knife") || name.Contains("broken_sword")) return "a knife";
            if (name.Contains("machete")) return "a machete";
            if (name.Contains("cleaver")) return "a cleaver";
            if (name.Contains("hatchet") || name.Contains("tomahawk")) return "a hatchet";
            if (name.Contains("lantern")) return "a lantern";
            if (name.Contains("torch") || name.Contains("molotov") || name == "weapon_fire") return "fire";
            if (name.Contains("dynamite") || name == "weapon_explosion") return "dynamite";
            if (name.Contains("poison")) return "poison";
            if (name.Contains("bolas")) return "bolas";
            if (name == "weapon_fall") return "a fall";
            if (name == "weapon_drowning") return "the river";
            if (name == "weapon_bleeding") return "bleeding out";
            if (name == "weapon_horse" || name.Contains("car")) return "a horse's hooves";
            return null;
        }

        public static string Phrase(uint hash)
        {
            if (_phrases == null)
            {
                _phrases = new Dictionary<uint, string>();
                foreach (string n in Names)
                {
                    string p = PhraseOf(n);
                    if (p != null)
                        _phrases[Natives.Hash(n)] = p;
                }
            }
            string s;
            return _phrases.TryGetValue(hash, out s) ? s : null;
        }

        public static bool IsGun(string phrase)
        {
            return phrase != null && (phrase.Contains("revolver") || phrase.Contains("pistol") || phrase.Contains("repeater") || phrase.Contains("rifle") || phrase.Contains("shotgun"));
        }
    }
}
