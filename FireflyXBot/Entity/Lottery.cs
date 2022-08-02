using System;
namespace FireflyXBot.Entity;

public class Lottery
{
    public string poem { get; set; }
    public string value { get; set; }

    public Lottery(string poem, string value)
    {
        this.poem = poem;
        this.value = value;
    }
}

