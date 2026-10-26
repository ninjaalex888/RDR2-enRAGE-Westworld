using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
	public static class Romance
	{
		public const int Scorned = -1;

		public const int None = 0;

		public const int Noticed = 1;

		public const int Sweet = 2;

		public const int Smitten = 3;

		public const int Sweethearts = 4;

		public const int Crush = 0;

		public const int Flirting = 1;

		public const int Courting = 2;

		public const int Steady = 3;

		public const int Engaged = 4;

		public const int Married = 5;

		public static readonly string[] HeartNames = new string[6] { "scorned", "nothing", "noticed you", "sweet on you", "smitten", "sweethearts" };

		public static readonly string[] StageNames = new string[6] { "a crush", "flirting", "courting", "sweethearts", "engaged", "married" };

		private static readonly string[][] ManToWoman = new string[6][]
		{
			new string[2] { "Evening, miss. You look lovely tonight.", "Why, thank you, sir." },
			new string[2] { "Care to dance?", "Not with you, I don't." },
			new string[2] { "I'd buy you a drink, if you'd let me.", "One drink. And mind your manners." },
			new string[2] { "Haven't seen you around here before.", "Maybe you weren't looking." },
			new string[2] { "Your eyes are like... like two...", "Go home, you're drunk." },
			new string[2] { "Walk you home, miss?", "It's three doors down. But all right." }
		};

		private static readonly string[][] WomanToMan = new string[4][]
		{
			new string[2] { "You going to stand there all night, or you going to talk to me?", "I... evening, ma'am." },
			new string[2] { "Buy a girl a drink?", "I'd be honored." },
			new string[2] { "That's a fine horse you got out front.", "Thank you kindly. She's the only lady who puts up with me." },
			new string[2] { "You clean up nice for a ranch hand.", "You noticed." }
		};

		private static readonly string[][] Friendly = new string[2][]
		{
			new string[2] { "Evening.", "Evening yourself." },
			new string[2] { "Save me a seat?", "Always." }
		};

		private static readonly string[] Women = new string[14]
		{
			"Nell", "Ada", "Clara", "Hattie", "Ida", "Lottie", "Rose", "Mae", "Cora", "Josephine",
			"Lucy", "Esther", "Florence", "Ruth"
		};

		private static readonly string[] Men = new string[12]
		{
			"Silas", "Eli", "Walter", "Henry", "Josiah", "Owen", "Luther", "Abel", "Samuel", "Frank",
			"Isaac", "Daniel"
		};

		private static readonly string[] Surnames = new string[12]
		{
			"Doyle", "Hale", "Whitaker", "Lambert", "Moreau", "Fischer", "Barlow", "Quinn", "Abbott", "Pruitt",
			"Vance", "Kearney"
		};

		private static readonly string[] WomenLines = new string[8] { "a seamstress", "a schoolteacher", "a saloon girl", "a laundress", "a widow who keeps a boarding house", "a shopkeeper's daughter", "a nurse", "a singer at the theatre" };

		private static readonly string[] MenLines = new string[8] { "a blacksmith", "a bank clerk", "a deputy", "a cowhand", "a widower who keeps a store", "a dock hand", "a fiddler at the saloon", "a telegraph man" };

		public static int PlayerBorn(bool john)
		{
			if (!john)
			{
				return 1863;
			}
			return 1873;
		}

		public static int AgeOf(Person p)
		{
			if (p == null || p.BornYear <= 0)
			{
				return -1;
			}
			return Voice.NowYear - p.BornYear;
		}

		public static bool Adult(Person p)
		{
			return AgeOf(p) >= 18;
		}

		public static int Orientation(string id, bool sameSexOn)
		{
			if (!sameSexOn)
			{
				return 0;
			}
			int num = Voice.Hash(id + "|heart") % 100;
			if (num >= 8)
			{
				if (num >= 14)
				{
					return 0;
				}
				return 2;
			}
			return 1;
		}

		public static bool DrawnTo(string id, bool woman, bool otherIsWoman, bool sameSexOn)
		{
			int num = Orientation(id, sameSexOn);
			bool flag = woman == otherIsWoman;
			if (num != 2 && (num != 0 || flag))
			{
				return num == 1 && flag;
			}
			return true;
		}

		/// Drawn to the player at all: to his sex, and near enough his age (22 years either way), a grown-up.
		public static bool Suits(Person p, bool sameSexOn, bool john)
		{
			return p != null && Adult(p) && DrawnTo(p.Id ?? p.Name, p.Woman, otherIsWoman: false, sameSexOn) && Math.Abs(AgeOf(p) - (Voice.NowYear - PlayerBorn(john))) <= 22;
		}

		public static bool Free(Person p)
		{
			return p.Family != "married";
		}

		public static int ToPlayer(Person p, bool sameSexOn, bool john)
		{
			if (p == null || !Adult(p) || !p.IsAlive || !Free(p) || !DrawnTo(p.Id ?? p.Name, p.Woman, otherIsWoman: false, sameSexOn))
			{
				return 0;
			}
			if (p.Hate >= 30 || p.Fear >= 50 || p.Murderous || p.Vengeful)
			{
				return 0;
			}
			int num = Math.Abs(AgeOf(p) - (Voice.NowYear - PlayerBorn(john)));
			if (num > 22)
			{
				return 0;
			}
			int num2 = 35 + Voice.Hash(p.Id + "|spark") % 30;
			if (p.Outgoing)
			{
				num2 += 10;
			}
			if (p.Friendly)
			{
				num2 += 10;
			}
			if (p.Surly)
			{
				num2 -= 15;
			}
			if (p.Personality == "Cold")
			{
				num2 -= 15;
			}
			if (p.Personality == "Drifter" || p.Personality == "Hothead")
			{
				num2 += 5;
			}
			num2 += (p.Facet("Humour") - 50) / 5;
			if (num > 12)
			{
				num2 -= 15;
			}
			num2 += Math.Min(30, p.Memories.Count((MemoryItem m) => m.AboutYou && m.Emotion == "Joy") * 10);
			num2 -= p.Memories.Count((MemoryItem m) => m.AboutYou && (m.Emotion == "Anger" || m.Emotion == "Grief")) * 25;
			num2 -= p.Hate;
			return Clamp(num2, 0, 100);
		}

		public static int Between(Person a, Person b, bool sameSexOn)
		{
			if (a == null || b == null || a.Id == b.Id || !Adult(a) || !Adult(b) || !Free(a) || !Free(b) || !a.IsAlive || !b.IsAlive)
			{
				return 0;
			}
			if (!DrawnTo(a.Id, a.Woman, b.Woman, sameSexOn))
			{
				return 0;
			}
			int num = Math.Abs(a.BornYear - b.BornYear);
			if (num > 15)
			{
				return 0;
			}
			int num2 = 30 + Voice.Hash(a.Id + "|" + b.Id + "|spark") % 40;
			if (a.Outgoing)
			{
				num2 += 8;
			}
			if (a.Friendly)
			{
				num2 += 5;
			}
			if (a.Surly)
			{
				num2 -= 10;
			}
			if (a.Personality == "Cold")
			{
				num2 -= 10;
			}
			if (!string.IsNullOrEmpty(a.GangId) && a.GangId == b.GangId)
			{
				num2 += 5;
			}
			if (a.Devotion == "Devout" && b.Devotion == "Godless")
			{
				num2 -= 10;
			}
			if (num > 8)
			{
				num2 -= 10;
			}
			return Clamp(num2, 0, 100);
		}

		public static int HeartFor(int affection, int flirts)
		{
			if (affection >= 85 && flirts >= 3)
			{
				return 4;
			}
			if (affection >= 60)
			{
				return 3;
			}
			if (affection >= 35)
			{
				return 2;
			}
			if (affection >= 15)
			{
				return 1;
			}
			return 0;
		}

		public static int Clamp(int v, int lo, int hi)
		{
			if (v >= lo)
			{
				if (v <= hi)
				{
					return v;
				}
				return hi;
			}
			return lo;
		}

		public static string PlayerLine(bool john, bool toWoman, int heart, string seed)
		{
			if (heart >= 3)
			{
				if (!john)
				{
					return Voice.Pick(seed, "Been thinking about you.", "Every time I ride into town, I'm hoping it's you I see.", "You make a man forget his troubles.");
				}
				return Voice.Pick(seed, "I been thinking about you.", "You're a sight for sore eyes, you know that?", "Wish I could stay longer.");
			}
			if (!toWoman)
			{
				return Voice.Pick(seed, "You clean up nice, partner.", "Buy you a drink sometime?", "Ain't you a handsome devil.");
			}
			if (!john)
			{
				return Voice.Pick(seed, "You're a sight for sore eyes.", "I'd remember a face like yours.", "Anybody ever tell you you've got a lovely smile?", "Wouldn't mind running into you again.", "You look mighty fine today.");
			}
			return Voice.Pick(seed, "You, uh... you look real nice today.", "Can't say I've seen a prettier face in this town.", "Maybe I'll see you around.", "That's a fine hat, miss. Suits you.");
		}

		public static string Welcome(Person p, int heart, PlayerRef you, string seed, string why = null)
		{
			string text = Call(p, heart, you, seed);
			if (heart < 3)
			{
				// what drew them, in their own words
				switch (why)
				{
				case "money":
					return Voice.Pick(seed, "A man with money in his pocket. Well, now.", "You dress rough, but you don't spend rough. I noticed.");
				case "danger":
					return Voice.Pick(seed, "Folks say you're trouble. Maybe I like trouble.", "Half this town's scared of you. I ain't.");
				case "decent":
					return Voice.Pick(seed, "I heard you did right by folks here. That counts with me.", "Word is you're decent. I'd like to find out.");
				case "restless":
					return Voice.Pick(seed, "A man who don't stay put. I could get used to that.", "You've got the look of somewhere else about you. I like it.");
				case "lonely":
					return Voice.Pick(seed, "Been a long while since anybody said a thing like that to me.", "Lord. I'd forgotten what that sounds like.");
				case "tipsy":
					return Voice.Pick(seed, "Buy me another and say that again.", "Keep talking. The whiskey likes you.");
				case "saloon":
					return Voice.Pick(seed, "Well, aren't you a sight. Sit a while.", "Buy a girl a drink, and we'll see where the evening goes.");
				case "stolen":
					return Voice.Pick(seed, "Hush. Not so loud. Folks talk in this town.", "I shouldn't. ...Say it again, quieter.");
				}
			}
			if (heart >= 4)
			{
				return Voice.Pick(seed, "There's my " + Fella(p) + ".", "Missed you, " + text + ".", "Took you long enough.");
			}
			if (heart >= 3)
			{
				return Voice.Pick(seed, "You keep saying things like that, I'm liable to believe you.", "I was hoping you'd come by, " + text + ".", "Careful, " + text + ". I might get used to this.");
			}
			if (p.Sociability == "Introvert")
			{
				return Voice.Pick(seed, "Oh... I... thank you.", "I don't... nobody says things like that to me.", "You're kind.");
			}
			if (p.Personality == "Proud")
			{
				return Voice.Pick(seed, "I know. But it's nice to hear.", "Took you long enough to notice.");
			}
			if (p.Facet("Humour") >= 60)
			{
				return Voice.Pick(seed, "Flattery'll get you everywhere. Almost.", "Smooth talker. Keep going.", "Is that the best you got, " + text + "?");
			}
			return Voice.Pick(seed, "Well, aren't you a charmer.", "Careful, " + text + ". A body could get used to that.", "Is that so? Maybe you'll buy me a drink sometime.", "You ain't so bad yourself.");
		}

		public static string Unsure(Person p, string seed, string why = null)
		{
			if (why == "shy")
				return Voice.Pick(seed, "I... oh. I don't rightly know what to say.", "You shouldn't say such things. ...Say it again?");
			if (why == "troubled")
				return Voice.Pick(seed, "That's kind. I've got a lot on my mind just now.", "Maybe another day, when I can think straight.");
			return Voice.Pick(seed, "Is that so?", "Hm. We'll see.", "Smooth talker, ain't you.", "You say that to everybody?");
		}

		public static string Rebuff(Person p, string reason, string seed)
		{
			switch (reason)
			{
			case "married":
				if (!p.Woman)
				{
					return Voice.Pick(seed, "I'm a married man, friend.", "My wife'd skin us both.");
				}
				return Voice.Pick(seed, "I'm a married woman, mister.", "My husband'd have something to say about that.");
			case "hate":
				return Voice.Pick(seed, "After what you did? You've got some gall.", "Don't you sweet-talk me.");
			case "fear":
				return "Please... I'd sooner be alone.";
			case "spam":
				return Voice.Pick(seed, "You said that already.", "Easy, cowboy.", "One at a time.");
			case "scorned":
				return Voice.Pick(seed, "Save it.", "You had your chance.", "Don't.");
			case "frightened":
				return Voice.Pick(seed, "You frighten me a little, mister. I'd sooner you didn't.", "Please, mister. Go sweet-talk somebody braver.");
			case "beneath":
				return Voice.Pick(seed, "You'll need more than a kind word and a dirty coat.", "I'm particular about who I keep company with.");
			case "poor":
				return Voice.Pick(seed, "Sweet talk don't pay the rent, mister.", "Come back when your pockets ain't empty.");
			case "killer":
				return Voice.Pick(seed, "Not with a man like you. Not ever.", "I know what kind of man you are. No.");
			case "pious":
				return Voice.Pick(seed, "I'm a churchgoing " + (p.Woman ? "woman" : "man") + ", mister, and I know what you've done.", "The Lord sees you, mister. So do I.");
			case "troubled":
				return Voice.Pick(seed, "I've got trouble enough without you adding to it.", "Not now. Not with everything as it is.");
			case "wary":
				return Voice.Pick(seed, "Not out here, where the whole street can see. Go on.", "Folks are watching. Always watching. Go on.");
			case "elsewhere":
				return Voice.Pick(seed, "I'm waiting on somebody. Somebody who writes.", "There's someone else. Somewhere.");
			case "shy":
				return Voice.Pick(seed, "I... no. Thank you. No.", "I couldn't. I just couldn't.");
			case "badrep":
				return Voice.Pick(seed, "I know what you did, mister. Don't you smile at me.", "I've heard about you. Every word of it.");
			case "busy":
				return Voice.Pick(seed, "Not today. I've got more on my mind than I can carry.", "Another time, maybe. Not today.");
			case "sore":
				return Voice.Pick(seed, "Not today of all days, mister.", "I'm in no mood for sweet talk today.");
			case "notdrawn":
				if (!p.Woman)
				{
					if (!(p.Personality == "Hothead"))
					{
						return Voice.Pick(seed, "You drunk, mister?", "Watch yourself.", "I ain't that kind of fella.");
					}
					return Voice.Pick(seed, "Say that again and I'll knock your teeth in.", "You drunk, mister?");
				}
				return Voice.Pick(seed, "Keep your sweet talk to yourself, mister.", "I ain't interested.", "You've got some nerve.");
			default:
				return Voice.Pick(seed, "Not interested.", "Go on, now.", "I don't think so.");
			}
		}

		public static string FlirtLine(Person p, int heart, PlayerRef you, string seed)
		{
			string text = Call(p, heart, you, seed);
			switch (heart)
			{
			case 1:
				return Voice.Pick(seed, "You again. Can't say I mind.", "Didn't expect to see you today. Nice surprise.", "You clean up all right, for a " + (you.Scarred ? "scarred-up" : "rough-looking") + " cowboy.", "Back so soon, " + text + "?");
			case 2:
				return Voice.Pick(seed, "I was hoping I'd run into you.", "You ever going to ask me to dance, or do I have to ask you?", "Folks are going to start talking, you keep coming around.", "You smell like a campfire. I don't mind it.");
			case 3:
				return Voice.Pick(seed, "I think about you more than I ought to, " + text + ".", "Promise me you'll be careful out there.", "Take me riding sometime? Just us.", "Every time you ride off, I wonder if you're coming back.");
			case 4:
				return Voice.Pick(seed, "There's my " + Fella(p) + ".", "Don't you go getting yourself killed, you hear?", "I told my folks about you. Don't make a liar of me.", "Stay a while. The world can wait.");
			case -1:
				return Voice.Pick(seed, "There's nothing left to say between us.", "You made your choice.", "Don't you smile at me.");
			default:
				return null;
			}
		}

		public static string JealousLine(string rival, string seed)
		{
			return Voice.Pick(seed, "Saw you sweet-talking " + rival + ". Don't think I didn't.", "I hear you've been calling on " + rival + ".", "So it's " + rival + " now, is it?");
		}

		public static string WifeLine(string seed)
		{
			return Voice.Pick(seed, "Heard you've got a woman and a boy back home. You might've said.", "You've got a family. You might've told me.");
		}

		public static Remark RemarkFor(Person p, int heart, PlayerRef you, string seed)
		{
			string text = Call(p, heart, you, seed);
			switch (heart)
			{
			case 1:
			{
				Remark remark;
				if (p.Sociability == "Introvert")
				{
					remark = new Remark();
					remark.Muttered = true;
					remark.Key = "heart1";
					remark.Text = Voice.Pick(seed, "Oh my. It's him.", "Don't stare. Don't stare.");
					return remark;
				}
				remark = new Remark();
				remark.Key = "heart1";
				remark.Text = Voice.Pick(seed, "Well, look who it is.", "Evening, " + text + ".", "Don't be a stranger now.");
				return remark;
			}
			case 2:
			{
				Remark remark = new Remark();
				remark.Key = "heart2";
				remark.Text = Voice.Pick(seed, "There you are, " + text + ".", "You coming by later?", "Hello, handsome.");
				return remark;
			}
			case 3:
			case 4:
			{
				Remark remark = new Remark();
				remark.Key = "heart3";
				remark.Text = Voice.Pick(seed, "Hey, " + text + ". Missed you.", "You look tired. Come sit with me a while.", "There's my " + Fella(p) + ".");
				return remark;
			}
			case -1:
			{
				Remark remark = new Remark();
				remark.Muttered = true;
				remark.Key = "scorned";
				remark.Text = Voice.Pick(seed, "Him. Of all people.", "Don't look. Just keep walking.");
				return remark;
			}
			default:
				return null;
			}
		}

		public static string Thought(Person p, int heart, PlayerRef you, string seed)
		{
			string text = Voice.YouPrivate(p, you, firstMention: true);
			switch (heart)
			{
			case 1:
				if (!you.KnownByName)
				{
					return Voice.Pick(seed, "That tall stranger. I keep looking for him in every crowd.", "There's a stranger in town I can't stop thinking about. Foolish.");
				}
				return Voice.Pick(seed, you.FullName + ". Can't get him out of my head.", "Why do I keep thinking about " + you.FullName + "?");
			case 2:
				return Voice.Pick(seed, "I hope " + text + " comes by again.", "Caught myself smiling for no reason today. I know the reason.");
			case 3:
				return Voice.Pick(seed, "I think I'm falling for " + text + ". God help me.", "I told myself I wouldn't. Here I am anyway.");
			case 4:
				return Voice.Pick(seed, "My " + Fella(p) + ". Who'd have thought it?", "Whatever he is out there, he's good to me.");
			case -1:
				return Voice.Pick(seed, "Fool. I was a fool to think he cared.", "Never again. Not for him, not for anybody.");
			default:
				return null;
			}
		}

		public static string Diary(Person p, int heart, PlayerRef you, string date, string seed)
		{
			string text = Voice.YouPrivate(p, you, firstMention: true);
			string text2;
			switch (heart)
			{
			case 1:
				text2 = (you.KnownByName ? ("Saw " + text + " again today. I can't stop smiling and I don't know why.") : "Met a man today. Rough around the edges. I can't stop smiling and I don't know why.");
				break;
			case 2:
				text2 = "I think I'm sweet on " + text + ". There, I wrote it down.";
				break;
			case 3:
				text2 = "I told myself I wouldn't. I'm falling for " + text + ", and I don't care who knows it.";
				break;
			case 4:
				text2 = ((text.StartsWith("the ") || text.StartsWith("that ")) ? ("He's my " + Fella(p) + " now. Lord help me.") : (Grammar.UpperFirst(text) + " is my " + Fella(p) + " now. Lord help me."));
				break;
			case -1:
				text2 = "He made a fool of me today. Never again.";
				break;
			default:
				return null;
			}
			return date + " - " + text2;
		}

		public static string IntelLine(string gangName, string agenda, string job, string seed)
		{
			string text = Voice.Pick(seed, "Listen... don't tell nobody I said this.", "I shouldn't be telling you this.", "If they knew I told you, they'd kill me.");
			string text2 = string.Empty;
			if (!string.IsNullOrEmpty(agenda))
			{
				text2 = text2 + " " + Grammar.UpperFirst(gangName ?? "The boys") + " mean to " + agenda + ".";
			}
			if (!string.IsNullOrEmpty(job))
			{
				text2 = text2 + " They're planning to " + job + ".";
			}
			if (agenda == "see you dead")
			{
				text2 += " Watch the roads.";
			}
			if (text2.Length != 0)
			{
				return text + text2;
			}
			return null;
		}

		public static List<string> CoupleExchange(Affair a, string seed)
		{
			string pick = Voice.Pick(seed, "1", "2", "3", "4");
			switch (a.Stage)
			{
			case 0:
			case 1:
				return Pair(pick, "You look awful pretty today.", "Oh, hush.", "Save me a dance tonight?", "Maybe I will.", "I made you something.", "You didn't have to.", "Walk with me a spell?", "Just a spell.");
			case 2:
			case 3:
				return Pair(pick, "Missed you.", "Missed you too.", "You're late.", "Worth the wait, ain't I?", "Folks are talking about us.", "Let 'em talk.", "Where were you last night?", "Never you mind.");
			case 4:
				return Pair(pick, "Not long now.", "I can't wait.", "Mother wants a church wedding.", "Then a church wedding it is.", "Still sure about me?", "Never been surer.", "I wrote my brother about you.", "What'd he say?");
			case 5:
				return Pair(pick, "Did you eat?", "I'll eat later.", "Come home early tonight.", "I'll try.", "You're tracking mud in.", "I love you too.", "We need flour.", "Then we'll get flour.");
			default:
				return null;
			}
		}

		private static List<string> Pair(string pick, params string[] lines)
		{
			int num = (int.Parse(pick) - 1) * 2;
			if (num + 1 >= lines.Length)
			{
				num = 0;
			}
			return new List<string>
			{
				lines[num],
				lines[num + 1]
			};
		}

		public static List<string> Strangers(bool firstIsMan, bool secondIsMan, string seed)
		{
			string[][] array = ((firstIsMan && !secondIsMan) ? ManToWoman : ((!firstIsMan && secondIsMan) ? WomanToMan : Friendly));
			string[] array2 = array[Voice.Hash(seed) % array.Length];
			return new List<string>
			{
				array2[0],
				array2[1]
			};
		}

		public static string StrangerReply(bool woman, bool drawn, string seed)
		{
			if (!drawn)
			{
				if (!woman)
				{
					return Voice.Pick(seed, "You drunk, mister?", "Watch yourself.", "Huh?");
				}
				return Voice.Pick(seed, "I don't think so, mister.", "Keep walking.", "Excuse me?");
			}
			switch (Voice.Hash(seed + "roll") % 3)
			{
			case 0:
				return Voice.Pick(seed, "Well, aren't you sweet.", "My, my.", "You're a charmer, ain't you?");
			case 1:
				return Voice.Pick(seed, "Is that so?", "Hm.", "Maybe.");
			default:
				if (!woman)
				{
					return Voice.Pick(seed, "Not today, friend.", "I'm spoken for.");
				}
				return Voice.Pick(seed, "Not today, cowboy.", "I'm spoken for.");
			}
		}

		public static string News(Affair a, int stage, string ended)
		{
			string text = (a.Offstage ? (a.BName + ", " + a.BLine + (string.IsNullOrEmpty(a.Town) ? "" : (" in " + a.Town))) : a.BName);
			string text2 = (a.AWoman ? "her" : "him");
			switch (ended)
			{
			case "jilted":
				return a.BName + " turned " + a.AName + " down flat. " + Grammar.UpperFirst(a.AWoman ? "she" : "he") + "'s taking it hard.";
			case "quarrel":
				return a.AName + " and " + a.BName + " had a falling out. It's over between them.";
			case "rival":
				return a.RivalName + " and " + a.AName + " came to blows over " + a.BName + ". " + a.AName + " came off worse.";
			case "died":
				return a.AName + " is mourning " + a.BName + ".";
			default:
				return null;
			case null:
				switch (stage)
				{
				case 2:
					return a.AName + " has been courting " + text + ".";
				case 3:
					return a.AName + " and " + a.BName + " are sweethearts now. Folks give it a month.";
				case 4:
					return a.AName + " asked " + a.BName + " to marry " + text2 + ". " + Person.FirstOf(a.BName) + " said yes.";
				case 5:
					return a.AName + " and " + a.BName + " were married" + (string.IsNullOrEmpty(a.Town) ? "" : (" in " + a.Town)) + ".";
				default:
					return null;
				}
			}
		}

		public static string Title(Affair a)
		{
			switch (a.Stage)
			{
			case 2:
				return a.AName + " has been courting " + a.BName;
			case 3:
				return a.AName + " has been stepping out with " + a.BName;
			case 4:
				return a.AName + " is to marry " + a.BName;
			case 5:
				return a.AName + " married " + a.BName;
			default:
				return null;
			}
		}

		private static string Call(Person p, int heart, PlayerRef you, string seed)
		{
			if (you.KnownByName)
			{
				if (heart < 2)
				{
					return "Mr. " + you.Surname;
				}
				return you.FirstName;
			}
			return Voice.Pick(seed + "call", "cowboy", "stranger", "handsome");
		}

		private static string Fella(Person p)
		{
			return "fella";
		}

		public static void Offstage(Affair a, Person p, bool woman, string seed)
		{
			int num = Voice.Hash(seed);
			a.BWoman = woman;
			a.BName = (woman ? Women[num % Women.Length] : Men[num % Men.Length]) + " " + Surnames[num / 7 % Surnames.Length];
			a.BLine = (woman ? WomenLines[num / 3 % WomenLines.Length] : MenLines[num / 3 % MenLines.Length]);
			a.BLikes = 25 + num / 11 % 65;
		}
	}
}
