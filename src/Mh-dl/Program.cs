using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Anf.KnowEngines;
using Anf.Engine;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Jint;
using Anf.Networks;
using Anf;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Mh_dl.Commands;
using System.Diagnostics;

namespace Mh_dl
{
    class Program
    {
        static int Main(string[] args)
        {
            var sw = Stopwatch.GetTimestamp();
            AnsiConsole.Render(
                    new FigletText("Anf-Mhdl")
                        .Centered()
                        .Color(Color.Green));
            Add();
            var app = new CommandApp();
            app.Configure(x => 
            {
#if DEBUG
                x.PropagateExceptions();
                x.ValidateExamples();
#endif
                x.AddCommand<SearchCommand>("s")
                    .WithAlias("search")
                    .WithDescription("从远程服务器使用关键字搜索漫画")
                    .WithExample(new string[] { "s", "魔女" });
                x.AddCommand<GetEntityCommand>("e")
                    .WithAlias("entity")
                    .WithDescription("从远程服务器获取漫画实体或下载漫画")
                    .WithExample(new string[] { "e", "http://www.dm5.com/manhua-putianmonv/", "--save", "true" });
                x.AddCommand<GetPagesCommand>("p")
                    .WithAlias("pages")
                    .WithDescription("从远程服务器获取漫画章节的图片");
            });
            var res= app.Run(args);
            var ed = Stopwatch.GetTimestamp();
            AnsiConsole.MarkupLine($"[green]已完成操作耗时{new TimeSpan(ed-sw)}[/]");
            return res;
        }
        private static void Add()
        {
            AppEngine.Services.AddScoped<IJsEngine, JintJsEngine>();
            AppEngine.Services.AddScoped<INetworkAdapter, WebRequestAdapter>();
            AppEngine.Services.AddSingleton(x =>
            {
                var factory = x.GetRequiredService<IServiceScopeFactory>();
                var eng = new SearchEngine(factory);
                eng.AddSearchProvider();
                return eng;
            });
            var comicEng = new ComicEngine();
            comicEng.AddComicSource();
            AppEngine.Services.AddSingleton(comicEng);
            AppEngine.Services.AddSingleton(x =>
            {
                var factory = x.GetRequiredService<IServiceScopeFactory>();
                var eng = new ProposalEngine(factory);
                eng.AddProposalEngine();
                return eng;
            });
            AppEngine.Services.AddKnowEngines();
        }
    }
}
