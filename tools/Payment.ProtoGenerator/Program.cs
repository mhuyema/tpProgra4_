using Payment.Contracts;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;
using Google.Protobuf.Reflection;

var outputDirectory = Path.GetFullPath(args.Length > 0 ? args[0] : "src/Payment/Protos");
Directory.CreateDirectory(outputDirectory);

var generator = new SchemaGenerator { ProtoSyntax = ProtoSyntax.Proto3 };
var schema = generator.GetSchema<IPaymentGrpcService>();
var outputPath = Path.Combine(outputDirectory, "payment.proto");
await File.WriteAllTextAsync(outputPath, schema);
Console.WriteLine($"Generado: {outputPath}");

// Exportar la definición oficial incluida en la dependencia, sin mantener otra copia a mano.
var bclPath = Path.Combine(outputDirectory, "protobuf-net", "bcl.proto");
Directory.CreateDirectory(Path.GetDirectoryName(bclPath)!);
await using (var source = typeof(FileDescriptorSet).Assembly
    .GetManifestResourceStream("ProtoBuf.protobuf_net.bcl.proto")
    ?? throw new InvalidOperationException("No se encontró el esquema BCL de protobuf-net."))
await using (var target = File.Create(bclPath))
{
    await source.CopyToAsync(target);
}
Console.WriteLine($"Exportado: {bclPath}");

// Comprobar el esquema y sus imports antes de dar por terminada la exportación.
var descriptors = new FileDescriptorSet();
descriptors.AddImportPath(outputDirectory);
descriptors.Add("payment.proto", true);
descriptors.Process();
var errors = descriptors.GetErrors().Where(error => error.IsError).ToArray();
if (errors.Length > 0)
    throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
Console.WriteLine("Esquema e imports válidos.");
