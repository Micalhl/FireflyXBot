using System;
using Cron;
using FireflyXBot.Data;

namespace FireflyXBot.Task;

public class AutoRefresher
{
    public AutoRefresher()
    {
        var daemon = new CronDaemon();

        daemon.Add("0 4 * * ?", () =>
        {
            TempStorage.jrrp.Clear();
            TempStorage.lotteriesCode.Clear();
        });

        daemon.Start();
    }
}

