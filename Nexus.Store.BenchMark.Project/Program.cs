using System.Diagnostics;
using Nexus.Store;
using Nexus.Store.BenchMark.Project.Entities;

Console.WriteLine("🚀 Iniciando Nexus.Store Benchmark...");

// 1. Configuración del motor
var options = new NexusOptions
{
    Name = "BenchmarkEngine",
    Port = 7005,
    MemoryLimit = "2g", // Le damos espacio para correr
    StoragePath = Path.Combine(AppContext.BaseDirectory, "bench-data")
};

using var nexus = new NexusEngine(options);

// 2. Preparación de datos (1 millón de registros)
int totalRecords = 50_000;
Console.WriteLine($"📦 Generando {totalRecords:N0} registros en memoria...");

var dataBatch = new Dictionary<string, DevicePayload>();
for (int i = 0; i < totalRecords; i++)
{
    dataBatch.Add($"dev_id_{i}", new DevicePayload(i, "Sensor-X", i * 1.5, DateTime.UtcNow));
}

// 3. Prueba de Fuego: Escritura Masiva (SetBatch)
Console.WriteLine("🔥 Ejecutando SetBatchAsync...");
var sw = Stopwatch.StartNew();

await nexus.SetBatchAsync(dataBatch);

sw.Stop();

// 4. Resultados
double seconds = sw.Elapsed.TotalSeconds;
double opsPerSec = totalRecords / seconds;

Console.WriteLine("\n" + new string('-', 40));
Console.WriteLine($"✅ Benchmark Completado");
Console.WriteLine($"⏱️ Tiempo total: {seconds:F2} segundos");
Console.WriteLine($"🚀 Velocidad: {opsPerSec:N0} OPS (Operaciones por segundo)");
Console.WriteLine(new string('-', 40));

// 5. Prueba de Lectura Aleatoria
Console.WriteLine("\n🔍 Verificando integridad de un registro aleatorio...");
var sample = await nexus.GetAsync<DevicePayload>("dev_id_500000");

if (sample != null)
{
    Console.WriteLine($"🟢 Registro recuperado: {sample.DeviceName} con valor {sample.Value}");
}
else
{
    Console.WriteLine("🔴 Error: No se pudo recuperar el dato.");
}

Console.WriteLine("\nPresiona cualquier tecla para cerrar el motor...");
Console.ReadKey();