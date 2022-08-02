using System.Text.Json;
using FireflyXBot.Data;
using FireflyXBot.Entity;
using FireflyXBot.Function;
using FireflyXBot.Task;
using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;

namespace FireflyXBot;

public static class Program
{
    private static Bot _bot = null!;

    public static async System.Threading.Tasks.Task Main()
    {
        Console.WriteLine(System.IO.Directory.GetCurrentDirectory());
        _bot = BotFather.Create(GetConfig(),
            GetDevice(), GetKeyStore());
        {
            _bot.OnLog += (_, e) =>
            {
                if (e.Tag != "PacketComponent")
                {
                    Info(e.EventMessage);
                }
            };

            _bot.OnCaptcha += (s, e) =>
            {
                switch (e.Type)
                {
                    case CaptchaEvent.CaptchaType.Sms:
                        Console.WriteLine(e.Phone);
                        s.SubmitSmsCode(Console.ReadLine());
                        break;

                    case CaptchaEvent.CaptchaType.Slider:
                        Console.WriteLine(e.SliderUrl);
                        s.SubmitSliderTicket(Console.ReadLine());
                        break;

                    default:
                    case CaptchaEvent.CaptchaType.Unknown:
                        break;
                }
            };

            
           // _bot.OnGroupPoke += Poke.OnGroupPoke;

            _bot.OnGroupMessage += Command.OnGroupMessage;
        }

        var result = await _bot.Login();
        {
            if (result) UpdateKeystore(_bot.KeyStore);
        }

        SetupLottery();

        new AutoRefresher();

        while (true)
        {
            switch (Console.ReadLine())
            {
                case "/stop":
                    await _bot.Logout();
                    _bot.Dispose();
                    return;
            }
        }
    }

    public static void Info(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("FireflyX >> ");
        Console.ResetColor();
        DateTime now = DateTime.Now;
        Console.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss"));
        Console.WriteLine(message);
    }

    private static BotConfig GetConfig()
    {
        return new BotConfig
        {
            EnableAudio = false,
            TryReconnect = true,
            HighwayChunkSize = 8192
        };
    }

    private static void SetupLottery()
    {
        string? line = "";
        using StreamReader reader = new StreamReader("lottery.txt");
        while ((line = reader.ReadLine()) is not null)
        {
            TempStorage.lotteries.Add(new Lottery(line, reader.ReadLine()!));
        }
    }

    private static BotDevice? GetDevice()
    {
        if (File.Exists("device.json"))
        {
            return JsonSerializer.Deserialize
                <BotDevice>(File.ReadAllText("device.json"));
        }

        var device = BotDevice.Default();
        {
            var deviceJson = JsonSerializer.Serialize(device,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("device.json", deviceJson);
        }

        return device;
    }

    private static BotKeyStore? GetKeyStore()
    {
        if (File.Exists("keystore.json"))
        {
            return JsonSerializer.Deserialize
                <BotKeyStore>(File.ReadAllText("keystore.json"));
        }

        Console.WriteLine("你是第一次启动机器人, 请输入你的帐号和密码.");

        Console.Write("账号: ");
        var account = Console.ReadLine();

        Console.Write("密码: ");
        var password = Console.ReadLine();

        Console.WriteLine("已创建机器人.");
        return UpdateKeystore(new BotKeyStore(account, password));
    }

    private static BotKeyStore UpdateKeystore(BotKeyStore keystore)
    {
        var deviceJson = JsonSerializer.Serialize(keystore,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("keystore.json", deviceJson);
        return keystore;
    }
}