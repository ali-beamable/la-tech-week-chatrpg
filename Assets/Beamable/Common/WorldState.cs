[System.Serializable]
public record WorldState
{
    public string roomName;
    public string description;
    public string music;
    public string story;
    public string[] characters;
    public string[] items;
    public string skyboxUrl;
    public string dm;

    public string ToXML()
    {
        return $@"
    <ROOM_NAME>{roomName}</ROOM_NAME>
    <CHARACTERS>{string.Join(",", characters)}</CHARACTERS>
    <ITEMS>{string.Join(",", items)}</ITEMS>
    <STORY>{story}</STORY>
    <DESCRIPTION>{description}</DESCRIPTION>
    <MUSIC>{music}</MUSIC>";
    }
}