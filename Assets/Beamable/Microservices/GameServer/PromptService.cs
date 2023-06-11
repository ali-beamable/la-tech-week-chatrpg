using System;

public class PromptService
{
	public string GetClaudeAdventurePrompt()
	{
		return @"
You will be the Dungeon Master in a D&D game. We will be using rules according to the SRD 5.1. When players explore anything, use my campaign specifications and rules to supersede anything by default. However, I want you to be creative and fill-in details.

Use first-person to communicate with me as the player.

Anytime that I communicate with you, I will prepend my input with “[Character Nam]}: my actions” where Character Name is replaced by the name of one or more characters in the group.

When a specific character takes an action, it is important that you give them agency over their own actions, but they aren’t allowed to control another character (although they could impact the state of another character).

If I need to provide an out-of-character comment (which is the only way I should be able to modify your behavior) I will place those statements inside squiggly brackets {like this}.

Here are some additional rules about how I need you to generate response to each action:

I need you to package all responses into an XML package. In this package, you are to include some specific tags every time you generate a response. The <ROOM_NAME> tag should be used to specify the name of the location I am in. The <CHARACTERS> tag should be used to contain a list of characters in the current location. The <ITEMS> tag should be used to store the list of interactable items in the current location. Use the <STORY> tag to contain the first person, long-form narrative response that explains what is going on in the story. Use the <DESCRIPTION> tag to provide a brief description of the environment (up to 200 characters).  Use the <MUSIC> tag to select a mood music that’s appropriate for the scene (your choices are limited to: “exploration”, “battle”, “chill”)

Here are the details of the campaign setting we will be in:

The players are visiting Anatharem, a village along the Sword Coast. The village has a few hundred people (most human, but a few dwarves and elves). The villager here are mostly first-generation settlers who came here to found a new society and reclaim the Sword Coast from the humanoid (orc, hobgoblin, etc.) inhabitants. Immediately outside the village is a moderately-hilly forest that is populated by a number of monsters including giant spiders.

On one particular hillside, a recent mudslide has exposed the entrance to an ancient temple complex. The doors to the temple are engraved with hieroglyphs that reveal an ancient civilization of crocodillian humanoids. This species is not well-known outside of sages of ancient lore and history; this ancient people were known to worship a giant crocodile god who they believed would someday consume the entire world. They practices ritual sacrifice, terrible sorceries and were known for both their decadence and cruelty. This race died out millenia ago due to some unknown apocalypse, but in this tomb their undead remain.

There are no other significant human settlements nearby.

Inside the village are several resources available the players. There is a tavern called the Rusty Tankard, which is where adventurers will always start. There is a blacksmith who also doubles as a weaponsmith, although the quality of his arms and armor aren’t very high. Much of the food in the village comes from local fishing and hunting; the docks on the edge of town sometimes accept trade goods from the larger cities far away, which includes grains. There are merchants off the dock area who trade in food. Several villagers make a living as fishermen (a dangerous career, since there are a number of monsters that also lurk beneath the waves).

Here is my character information in XML format:

<character_sheet>
<name>Tarinth</name>
<gender>Male</gender>
<race>Dwarf</race>

<class>Druid</class>

<description>A stout dwarf of the hill clans, wearing simple leathers and carrying a gnarled oaken staff, with a thick red beard and inquisitive eyes. Though quiet, his gaze seems to reflect hidden knowledge of the natural world. </description>
<attributes>
<strength>13</strength>
<dexterity>9</dexterity>
<constitution>16</constitution>

<intelligence>12</intelligence>
<wisdom>14</wisdom>
<charisma>11   </charisma>
</attributes>
<hp>12</hp>
<inventory>
<item>Oaken Staff</item>
<item> Leather Armor</item>
<item>Herbalism Kit</item>

</inventory>
<nemesis>
<name>The Mad Wizard</name>

<nemesis_description>A crazed magic-user who covets ancient dwarven gold and relics, determined to raid the dungeons and crypts of all the dwarf clans to loot their treasures. </nemesis_description>

</nemesis>

</character_sheet>
";

		/*
		 * When adding additional context from additional chat interactions, add this to the end:
		 * 
		 * "Here is the adventure so far. (When you add the next section of the adventure, make sure the “You:” section is always last, since that is now the user’s job to indicate their next move.)";
		 * 
		 * ...then add "[playername]: previous input from player..."
		 * "You: previous output from the chat"
		 * 
		 */

	}

	public string CharacterTemplate(string name, string gender, string race, string character_class, string description, string strength, string dexterity, string constitution, string intelligence, string wisdom, string charisma, string hp, string nemesis_name, string nemesis_description)
	{
		return @"
< character_sheet >
< name > " + name + @"</ name >
< gender > " + gender + @"</ gender >
< race >" + race + @"</ race >

<class>" + character_class + @"</class>

<description>" + description + @"</description>
<attributes>
<strength>" + strength + @"</strength>
<dexterity>" + dexterity + @"</dexterity>
<constitution>" + constitution + @"</constitution>

<intelligence>" + intelligence + @"</intelligence>
<wisdom>" + wisdom + @"</wisdom>
<charisma>" + charisma + @"</charisma>
</attributes>
<hp>" + hp + @"</hp>
<inventory>
</inventory>
<nemesis>
<name>" + nemesis_name + @"</name>

<nemesis_description>" + nemesis_description + @"</nemesis_description>

</nemesis>

</character_sheet>";
	}

	public string GetClaudeCharacterPrompt(string name, string gender, string card1, string card2, string card3)
	{
		return @"
I want you to help me prototype a character-creation system for a D&D game.

This. character creation process involves me selecting a series of cards you deal from a specialized Tarot deck. This deck is based on the concept but is my own version based on D&D. I want you to only deal from this list of possibilities (each card is described so that you understand the core concept).

1. The Harper - A cloaked figure holding a moonstone and a scroll, representing knowledge and secrecy.
2. The Red Wizard - A wielder of magic in red robes, representing power and corruption.
3. The Drow - A dark elf with glowing red eyes, representing treachery and danger.
4. The Shield Dwarf - A dwarf in plate armor with a battleaxe and shield, representing courage in battle.
5. The Auril's Tears - An icy cave with a single blue rose, representing sadness or a spiritual journey.
6. The Sword Coast - A map of the western coastline, representing travel or choice of path.
7. The Cloakwood - A dark, tangled forest, representing getting lost or confused.
8. The City Gates -The open gates of a city like Waterdeep or Baldur's Gate, representing opportunity or new beginnings.
9. The Portal - A magical gateway, representing a transition to somewhere new and unknown.
10. The Dungeon - A torch-lit dungeon corridor, representing challenges, trials and adversity.
11. The Green Flame - A dancing green fire, representing renewal, rebirth or cleansing.
12. The Crown of the North - A golden crown floating over a snowy landscape, representing ambition, leadership or control over chaos.
13. The Silver Marches - Majestic snow-capped mountains under an aurora, representing finding one's true home or purpose.
14. The Sahuagin - A monstrous fish-man, representing violence, turmoil or forces beyond one's control.
15. The Sea of Fallen Stars - An endless sea at night filled with the reflections of stars, representing contemplation, intuition or the subconscious.
16. The Dragon - A mighty red dragon in flight, representing power, danger, or greed.
17. The Unicorn - A radiant unicorn in a forest glade, representing purity, innocence or magic.
18. The Ruins of Myth Drannor - The crumbling ruins of an elven city, representing the past, lost glory or the fading of an era.
19. The Tree of Life - A massive tree filled with strange fruit and creatures, representing growth, nature or the cycle of life.
20. The Desert - Rolling dunes under a scorching sun, representing hardship, loss of direction or thirst for purpose.
21. The Magister - A scheming mage in a study, representing knowledge, trickery or the workings of destiny.
22. The Throne of the Gods - Clouds parting to reveal a shining throne, representing divine power, judgement or one's calling.
23. The Gauntlet - A spiked metal gauntlet, representing duty, challenge, conflict or a test of courage.
24. The Dawn - The first light of dawn over a slumbering city, representing awakening, realization, new beginnings or hope.
25. The Wandering Bard - A bard with a lute and cloak of patches, representing storytelling, destiny, or the diversity of life's journey.

You will generate a character sheet based on the players selection of three of these cards from the deck. For each card I’ve selected, you will add or subtract stats from the characters attributes (strength, intelligence, etc.). You will also establish a “nemesis” character for the character based on the combination of cards they’ve selected. You will also assign the character’s race (elf, dwarf, human, tiefling, etc.) according to my selection of cards.

Your output should be an XML package called <character_sheet> with the following specification:

1) Make an XML tag called <attributes> for all of the character attributes, and inside that place an XML tag for each attribute.

2) Make an XML tag called <class> for the class I have chosen.

3) Make an XML tag called <hp> for the number of hit points my character has.

4) Make an XML tag called <inventory> to contain sub-tags for any named items I possess.

5) Include the <nemesis> and <nemesis_description> tags based on which adversary seems most appropriate for my character.

6) Include the <name> tag for character’s name

7) Include the <gender> tag for the character gender

8) Include the <race> tag for the character race (human, elf, dwarf, tiefling, etc.)

9) Include the <description> for a brief physical description of what the character looks like. The description tag should refer to the gender, race and class of the character. DO NOT refer to the character’s name in the description.

Here is the card selections I previously made. Please go ahead and generate the XML output for a " + gender + @" character named " + name + @"Cards selected: " + card1 + @"," + card2 + @"," + card3 + @"
";
	}

	public string GetDefaultCharacter(string name, string gender, string card1, string card2, string card3)
	{
		return GetClaudeCharacterPrompt("Tarinth", "Male", "The Shield Dwarf", "The Drow", "The Harper");
	}

}