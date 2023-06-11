using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterView
{
    public string imageUrl;
    public string skyboxUrl;
    
    public string characterName;
    public string characterClass;
    public string characterLevel;
    public string characterDescription;
    public string characterRace;
    public string characterGender;
    
    public string nemesisName;
    public string nemesisDescription;

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
    <health_points>{currentHp}</health_points>
    <mana_points>{currentMana}</mana_points>
    <nemesis>
        <name>{nemesisName}</name>
        <description>{nemesisDescription}</description>
    </nemesis>
</character_sheet>";
    }
}
