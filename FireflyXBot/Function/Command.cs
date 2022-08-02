using FireflyXBot.Data;
using FireflyXBot.Entity;
using FireflyXBot.Utils;
using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedParameter.Local

namespace FireflyXBot.Function;

public static class Command
{
    private static uint _messageCounter;

    internal static async void OnGroupMessage(Bot bot, GroupMessageEvent group)
    {
        Program.Info($"[{group.GroupUin}] [{group.MemberUin}({group.MemberCard})] >> {group.Chain.GetChain<TextChain>()}");
        // Increase
        ++_messageCounter;

        if (group.MemberUin == bot.Uin) return;

        var textChain = group.Chain.GetChain<TextChain>();
        if (textChain is null) return;

        var atChain = group.Chain.GetChain<AtChain>();

        try
        {
            MessageBuilder? reply = null;
            {
                if (textChain.Content.StartsWith("/status"))
                    reply = OnCommandStatus(textChain);
                else if (textChain.Content.StartsWith("https://github.com/"))
                    reply = await OnCommandGithubParser(textChain);
                else if (textChain.Content.StartsWith("/ping"))
                    reply = await OnCommandPing(textChain);
                else if (textChain.Content.StartsWith(".jrrp"))
                    reply = OnCommandJrrp(group, textChain);
                else if (textChain.Content.StartsWith(".抽签"))
                    reply = OnCommandGetPoem(group, textChain);
                else if (textChain.Content.StartsWith(".解签"))
                    reply = OnCommandGetValue(group, textChain);
                else if (atChain is not null && atChain.AtUin == bot.Uin && textChain.Content.Contains("还是"))
                    reply = OnActionSelect(group, textChain);
                else if (textChain.Content.StartsWith(".getNews"))
                    reply = await OnCommandGetNews();
            }
            if (reply is not null)
                await bot.SendGroupMessage(group.GroupUin, reply);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);

            // Send error print
            await bot.SendGroupMessage(group.GroupUin,
                Text($"{e.Message}\n{e.StackTrace}"));
        }
    }

    public static MessageBuilder? OnActionSelect(GroupMessageEvent group, TextChain chain)
    {
        var content = chain.Content.Replace(" ", "");
        if (content == "还是") return null;
        if (content.StartsWith("还是") || content.EndsWith("还是")) return null;
        var select = new Random().Next(101) <= 50 ? "前者" : "后者";
        return new MessageBuilder().Text($"@{group.MemberCard} 选{select}!");
    }

    public static MessageBuilder OnCommandGetPoem(GroupMessageEvent group, TextChain chain)
    {
        int random;
        if (TempStorage.lotteriesCode.ContainsKey(group.MemberUin))
        {
            random = TempStorage.lotteriesCode[group.MemberUin];
        }
        else
        {
            random = new Random().Next(384);
            TempStorage.lotteriesCode.Add(group.MemberUin, random);
        }
        return new MessageBuilder()
            .Text($"@{group.MemberCard}\n")
            .Text($"第[{random}]签: \n")
            .Text(TempStorage.lotteries[random].poem);
    }

    public static MessageBuilder? OnCommandGetValue(GroupMessageEvent group, TextChain chain)
    {
        if (TempStorage.lotteriesCode.ContainsKey(group.MemberUin))
        {
            var code = TempStorage.lotteriesCode[group.MemberUin];
            return new MessageBuilder()
                .Text($"{group.MemberCard}的解签:\n")
                .Text(TempStorage.lotteries[code].value);
        }
        return null;
    }

    public static MessageBuilder OnCommandJrrp(GroupMessageEvent group, TextChain chain)
    {
        int random;
        if (TempStorage.jrrp.ContainsKey(group.MemberUin))
        {
            random = TempStorage.jrrp[group.MemberUin];
        }
        else
        {
            random = new Random().Next(101);
            TempStorage.jrrp.Add(group.MemberUin, random);
        }
        return new MessageBuilder().Text($"{group.MemberCard}今天的人品值是:{random}");
    }

    public static MessageBuilder OnCommandHelp(TextChain chain)
        => new MessageBuilder()
            .Text("[Kagami Help]\n")
            .Text("/help\n Print this message\n\n")
            .Text("/ping\n Pong!\n\n")
            .Text("/status\n Show bot status\n\n")
            .Text("/echo\n Send a message");

    public static MessageBuilder OnCommandStatus(TextChain chain)
        => new MessageBuilder()
            // Core descriptions
            .Text($"[FireflyX - Bot]\n")

            // System status
            .Text($"Processed {_messageCounter} message(s)\n")
            .Text($"GC Memory {GC.GetTotalAllocatedBytes().Bytes2MiB(2)} MiB " +
                  $"({Math.Round((double)GC.GetTotalAllocatedBytes() / GC.GetTotalMemory(false) * 100, 2)}%)\n")
            .Text($"Total Memory {Process.GetCurrentProcess().WorkingSet64.Bytes2MiB(2)} MiB\n\n")

            // Copyrights
            .Text("MCStarrySky (C) 2022");

    public static async Task<MessageBuilder?> OnCommandGithubParser(TextChain chain)
    {
        // UrlDownload the page
        try
        {
            var bytes = await $"{chain.Content.TrimEnd('/')}.git".UrlDownload();

            var html = Encoding.UTF8.GetString(bytes);

            // Get meta data
            var metaData = html.GetMetaData("property");
            var imageMeta = metaData["og:image"];

            // Build message
            var image = await imageMeta.UrlDownload();
            return new MessageBuilder().Image(image);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Not a repository link. \n" +
                              $"{e.Message}");
            return null;
        }
    }

    public static async Task<MessageBuilder> OnCommandGetNews()
    {
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("https://api.03c3.cn/zb");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsByteArrayAsync();
        return new MessageBuilder().Image(result);
    }

    public static async Task<MessageBuilder> OnCommandPing(TextChain chain)
    {
        var args = chain.Content.Split(" ");
        if (args.Length == 3)
        {
            var address = args[1];
            var portInput = args[2];
            try
            {
                var port = int.Parse(portInput);
                var url = "https://api.imlazy.ink/mcapi/?host=" + address + "&port=" + port + "&type=json";
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                var minecraftServer = JsonSerializer.Deserialize<MinecraftServer>(result)!;
                if (minecraftServer.status == "离线")
                {
                    return new MessageBuilder().Text("FireflyX >> 服务器不在线!");
                }
                return new MessageBuilder()
                    .Text($"地址: {minecraftServer.host}:{minecraftServer.port}\n")
                    .Text($"状态: {minecraftServer.status}\n")
                    .Text($"在线: {minecraftServer.players_online}/{minecraftServer.players_max}\n")
                    .Text($"版本: {minecraftServer.version}");
            }
            catch (Exception ignored)
            {
                Console.WriteLine(ignored);
                throw;
                return new MessageBuilder().Text("FireflyX >> 你输入的端口有误!");
            }
        }
        else
        {
            return new MessageBuilder().Text("FireflyX >> 参数长度有误, 请检查您输入的内容.\n").Text("命令帮助: /ping <地址> <端口, SRV填写25565>");
        }
    }

    public static MessageBuilder OnRepeat(MessageChain message)
        => new(message);

    private static MessageBuilder Text(string text)
        => new MessageBuilder().Text(text);
}