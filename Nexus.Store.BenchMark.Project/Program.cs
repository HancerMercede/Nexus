using BenchmarkDotNet.Running;
using Nexus.Store.BenchMark.Project;
using Spectre.Console;

AnsiConsole.Write(new FigletText("Nexus.Store").Color(Color.Cyan1));
AnsiConsole.MarkupLine("[bold white]Engine:[/] .NET 10 | [bold white]Status:[/] [green]Production Ready[/]");
try 
{
    var summary = BenchmarkRunner.Run<NexusStoreBenchmark>();
    Console.WriteLine(summary);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
}

