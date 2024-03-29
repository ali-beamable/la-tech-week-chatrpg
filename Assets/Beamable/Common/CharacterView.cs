using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public record CharacterView
{
    public string imageUrl;
    public string skyboxUrl;
    
    public string characterName;
    public string characterClass;
    public string characterLevel;
    public string characterDescription;
    public string characterBackground;
    public string characterRace;
    public string characterGender;
    
    public string nemesisName;
    public string nemesisDescription;

    public int strength;
    public int dexterity;
    public int constitution;
    public int intelligence;
    public int charisma;
    public int wisdom;

    public int currentHp;
    public int maxHp;

    public int currentMana;
    public int maxMana;

    public float PercentHp => ((float) currentHp) / ((float) maxHp);
    public float PercentMana => ((float) currentMana) / ((float) maxMana);

    public string ToXML()
    {
        return $@"
<character_sheet>
    <name>{characterName}</name>
    <class>{characterClass}</class>
    <level>{characterLevel}</level>
    <gender>{characterGender}</gender>
    <race>{characterRace}</race>
    <description>{characterDescription}</description>
    <background>{characterBackground}</background>
    <attributes>
        <strength>{strength}</strength>
        <dexterity>{dexterity}</dexterity>
        <constitution>{constitution}</constitution>
        <intelligence>{intelligence}</intelligence>
        <wisdom>{wisdom}</wisdom>
        <charisma>{charisma}</charisma>
    </attributes>
    <health_points>{currentHp}</health_points>
    <mana_points>{currentMana}</mana_points>
    <nemesis>
        <name>{nemesisName}</name>
        <description>{nemesisDescription}</description>
    </nemesis>
</character_sheet>";
    }
}
