using System.Diagnostics;
using Nexus.Store;
using Nexus.Store.BenchMark.Project.Entities;
using Spectre.Console;

// --- UI Header ---
// We avoid the .LeftAligned() extension if the compiler complains about IAlignable
// and instead use the Justification property or a Layout container.
var logo = new FigletText("Nexus.Store")
    .Color(Color.Cyan1);

AnsiConsole.Write(logo);
AnsiConsole.MarkupLine("[bold white]Core Engine:[/] [cyan1].NET 10 / C# 14[/]");
AnsiConsole.MarkupLine("[bold white]Status:[/] [green]Ready to accept connections[/]\n");

// 1. Engine Configuration
var options = new NexusOptions
{
    Name = "BenchmarkEngine",
    Port = 7005,
    MemoryLimit = "2g", 
    StoragePath = Path.Combine(AppContext.BaseDirectory, "bench-data")
};

using var nexus = new NexusEngine(options);

// 2. Data Preparation
int totalRecords = 500_000; 
AnsiConsole.MarkupLine($"[blue]📦 Generating {totalRecords:N0} records in memory...[/]");

var dataBatch = new Dictionary<string, DevicePayload>();
for (int i = 0; i < totalRecords; i++)
{
    // C# 14 Primary Constructor / Collection expression style
    dataBatch.Add($"dev_id_{i}", new DevicePayload(i, "Sensor-X", i * 1.5, DateTime.UtcNow));
}

// 3. Performance Test: Bulk Write
await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("[bold orange1]🔥 Executing SetBatchAsync...[/]", async ctx =>
    {
        var sw = Stopwatch.StartNew();
        await nexus.SetBatchAsync(dataBatch);
        sw.Stop();

        double seconds = sw.Elapsed.TotalSeconds;
        double opsPerSec = totalRecords / seconds;

        // 4. Results Display
        AnsiConsole.WriteLine();
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[yellow]Metric[/]");
        table.AddColumn("[yellow]Value[/]");

        table.AddRow("Status", "[green]✅ Benchmark Completed[/]");
        table.AddRow("Total Records", $"{totalRecords:N0}");
        table.AddRow("Total Time", $"{seconds:F2} seconds");
        table.AddRow("Throughput", $"[bold cyan1]{opsPerSec:N0} OPS[/]");

        AnsiConsole.Write(table);
    });

// 5. Random Read Integrity Check
AnsiConsole.MarkupLine("\n[bold white]🔍 Verifying random record integrity...[/]");

var targetId = $"dev_id_{totalRecords / 2}";
var sample = await nexus.GetAsync<DevicePayload>(targetId);

if (sample != null)
{
    var panel = new Panel(new Markup($"[green]🟢 Success:[/] Retrieved [white]{sample.DeviceName}[/] with value [white]{sample.Value}[/]"))
    {
        Border = BoxBorder.Rounded,
        Padding = new Padding(1, 0, 1, 0)
    };
    AnsiConsole.Write(panel);
}
else
{
    AnsiConsole.MarkupLine("[red]🔴 Error: Could not recover the requested data segment.[/]");
}

AnsiConsole.MarkupLine("\n[grey]Press any key to shutdown the engine...[/]");
Console.ReadKey();