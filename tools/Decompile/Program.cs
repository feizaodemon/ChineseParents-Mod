// Decompile a game assembly into a project folder for local API reference only (not for distribution).
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

var input = args[0];
var output = args[1];
Directory.CreateDirectory(output);
var module = new PEFile(input);
var resolver = new UniversalAssemblyResolver(input, false, module.DetectTargetFrameworkId());
resolver.AddSearchDirectory(Path.GetDirectoryName(input));
var decompiler = new WholeProjectDecompiler(new DecompilerSettings { ThrowOnAssemblyResolveErrors = false }, resolver, null, null, null);
decompiler.DecompileProject(module, output);
Console.WriteLine("done: " + output);
