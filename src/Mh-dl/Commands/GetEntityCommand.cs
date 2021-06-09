using Anf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Web;

namespace Mh_dl.Commands
{
    public class GetEntityCommand : AsyncCommand<GetEntityCommand.GetEntityCommandSettings>
    {
        public override ValidationResult Validate(CommandContext context, GetEntityCommandSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Url)||
                !UrlHelper.IsWebsite(settings.Url))
            {
                return ValidationResult.Error("输入的不是合法地址");
            }
            return ValidationResult.Success();
        }
        public override async Task<int> ExecuteAsync(CommandContext context, GetEntityCommandSettings settings)
        {
            AnsiConsole.MarkupLine("正在从远程服务器获取实体");
            var eng = AppEngine.GetRequiredService<ComicEngine>();
            var type = eng.GetComicSourceProviderType(settings.Url);
            if (type == null)
            {
                AnsiConsole.MarkupLine("[red]当前不支持路径{0}[/]", settings.Url);
            }
            else
            {
                var factory = AppEngine.GetRequiredService<IServiceScopeFactory>();
                using (var scope = factory.CreateScope())
                {
                    var targetEng = (IComicSourceProvider)scope.ServiceProvider.GetRequiredService(type.ProviderType);
                    try
                    {
                        var entity = await targetEng.GetChaptersAsync(settings.Url);
                        if (entity == null)
                        {
                            AnsiConsole.MarkupLine("[red]无法获取该地址实体[/]");
                            return -1;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine(string.Empty);
                            AnsiConsole.Render(
                              new FigletText(entity.Name)
                                  .Centered()
                                  .Color(Color.Green));
                            AnsiConsole.Render(new Markup($"目标: {entity.ComicUrl}\n", new Style(link: entity.ComicUrl)));
                            AnsiConsole.Render(new Markup($"封面: {entity.ImageUrl}\n", new Style(link: entity.ImageUrl)));
                            AnsiConsole.MarkupLine($"描述: {entity.Descript}");
                            AnsiConsole.MarkupLine(string.Empty);
                            AnsiConsole.MarkupLine("一共{0}个章节", entity.Chapters.Length);
                            var table = new Table();
                            table.AddColumns("名字", "地址");
                            foreach (var item in entity.Chapters)
                            {
                                table.AddRow(
                                    new Markup(item.Title),
                                    new Markup(item.TargetUrl, new Style(link: item.TargetUrl)));
                            }
                            AnsiConsole.Render(table);
                            if (settings.Save)
                            {
                                var folder = settings.Path;
                                if (string.IsNullOrEmpty(folder))
                                {
                                    folder = Environment.CurrentDirectory;
                                }
                                if (!Directory.Exists(folder))
                                {
                                    Directory.CreateDirectory(folder);
                                    AnsiConsole.MarkupLine("目录[green]{0}[/]缺失, 已创建该目录", folder);
                                }
                                var invalidChart = new HashSet<char>(Path.GetInvalidPathChars());
                                var newName = new string(entity.Name.Select(x => invalidChart.Contains(x) ? '_' : x).ToArray());
                                folder = Path.Combine(folder, newName);
                                AnsiConsole.MarkupLine("目标漫画目录[green]{0}[/]", folder);
                                if (!Directory.Exists(folder))
                                {
                                    Directory.CreateDirectory(folder);
                                    AnsiConsole.MarkupLine("目录[green]{0}[/]缺失, 已创建该目录", folder);
                                }
                                AnsiConsole.MarkupLine("开始下载[green]{0}[/]个章节的资源", entity.Chapters.Length);
                                await AnsiConsole.Progress()
                                    .StartAsync(async ctx =>
                                    {
                                        var invalidPathChart = new HashSet<char>(Path.GetInvalidFileNameChars());
                                        foreach (var item in entity.Chapters)
                                        {
                                            var pgs = await targetEng.GetPagesAsync(item.TargetUrl);
                                            if (pgs == null)
                                            {
                                                AnsiConsole.Render(
                                                    new Markup($"[yellow]无法加载章节{item.Title}[/]", new Style(link: item.TargetUrl)));
                                                continue;
                                            }
                                            var cname = new string(item.Title.Select(x => invalidPathChart.Contains(x) ? '_' : x).ToArray());
                                            var chFolder = Path.Combine(folder, cname);
                                            if (!Directory.Exists(chFolder))
                                            {
                                                Directory.CreateDirectory(chFolder);
                                                AnsiConsole.MarkupLine("目录[green]{0}[/]缺失, 已创建该目录", chFolder);
                                            }
                                            var task = ctx.AddTask(item.TargetUrl, true, pgs.Length);
                                            for (int i = 0; i < pgs.Length; i++)
                                            {
                                                var p = pgs[i];
                                                try
                                                {
                                                    var fi = Path.Combine(chFolder, i.ToString());
                                                    if (!File.Exists(fi) || settings.Force)
                                                    {
                                                        using (var stream = await targetEng.GetImageStreamAsync(p.TargetUrl))
                                                        {
                                                            using (var fs = File.Open(fi, FileMode.Create))
                                                            {
                                                                await stream.CopyToAsync(fs);
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    AnsiConsole.WriteException(ex);
                                                }
                                                finally
                                                {
                                                    task.Increment(1);
                                                }
                                            }
                                            task.StopTask();
                                        }
                                    });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.WriteException(ex);
                    }
                }
            }
            return 0;
        }

        public class GetEntityCommandSettings:CommandSettings
        {
            [CommandArgument(0,"[Url]")]
            public string Url { get; set; }

            [CommandOption("-p|--path <PATH>")]
            public string Path { get; set; }

            [CommandOption("-s|--save <SAVE>")]
            public bool Save { get; set; }

            [CommandOption("-f|--force <FORCE>")]
            public bool Force { get; set; }
        }
    }
}
