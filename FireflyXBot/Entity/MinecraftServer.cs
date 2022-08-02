namespace FireflyXBot.Entity;

public class MinecraftServer
{
    public string host { get; set; }
    public string port { get; set; }
    public string status { get; set; }
    private int? _players_max;
    public int? players_max
    {
        get
        {
            return _players_max;
        }
        set
        {
            _players_max = value == null ? 0 : value;
        }
    }
    private int? _players_online;
    public int? players_online
    {
        get
        {
            return _players_online;
        }
        set
        {
            _players_online = value == null ? 0 : value;
        }
    }
    public string version { get; set; }
}

