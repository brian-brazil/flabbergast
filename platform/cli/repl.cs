using Flabbergast;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Terminal;
using NDesk.Options;

public class Completables {

	private readonly string[] keywords = { "args", "value", "As", "Bool", "By", "Container", "Each", "Else", "Error", "False", "Finite", "Float", "FloatMax", "FloatMin", "For", "Frame", "From", "GenerateId", "Id", "If", "In", "Infinity", "Int", "IntMax", "IntMin", "Is", "Length", "Let", "Lookup", "NaN", "Name", "Null", "Order", "Ordinal", "Reduce", "Reverse", "Select", "Str", "Template", "Then", "This", "Through", "To", "True", "Where", "With" };

	public LineEditor.Completion Handler(string text, int pos) {
		var start = pos;
		while (text.Length > 0 && start > 0 && Char.IsLetter(text[start - 1])) {
			start--;
		}
		var candidates = new List<string>();
		foreach(var word in keywords) {
			if (String.Compare(word, 0, text, start, pos - start) == 0) {
				candidates.Add(word.Substring(pos - start));
			}
		}
		return new LineEditor.Completion(text.Substring(start, pos - start), candidates.ToArray());
	}

}

public class REPL {
	private static void HandleResult(object result) {
		var seen = new Dictionary<Frame, int>();
		var counter = 1;
		Print(result, 0, seen, ref counter);
	}
	private static void Print(object result, int indentation, Dictionary<Frame, int> seen, ref int counter) {
		if (result == null) {
			Console.WriteLine("∅");
		} else if (result is Frame) {
			var f = result as Frame;
			if (seen.ContainsKey(f)) {
				Console.WriteLine("{0} # Frame {1}", f.Id, seen[f]);
			} else {
				Console.Write("{ # Frame ");
				Console.WriteLine(counter);
				seen[f] = counter++;
				foreach (var name in f.GetAttributeNames()) {
					for (var it = 0; it <= indentation; it++) {
						Console.Write("\t");
					}
					Console.Write("{0} : ", name);
					Print(f[name], indentation + 1, seen, ref counter);
				}
				for (var it = 0; it < indentation; it++) {
					Console.Write("\t");
				}
				Console.WriteLine("}");
			}
		} else if (result is bool) {
			Console.WriteLine(((bool)result) ? "True" : "False");
		} else if (result is Template) {
			var t = result as Template;
			Console.WriteLine("Template");
			foreach (var name in t.GetAttributeNames()) {
				Console.Write(" ");
				Console.Write(name);
			}
			Console.WriteLine();
			t.SourceReference.Write(Console.Out, indentation + 1, "");
		} else {
			Console.WriteLine(result);
		}
	}

	public static int Main(string[] args) {
		bool show_help = false;
		var options = new OptionSet () {
			{ "h|help",  "show this message and exit", v => show_help = v != null },
		};

		List<string> files;
		try {
			files = options.Parse (args);
		} catch (OptionException e) {
			Console.Error.Write (System.AppDomain.CurrentDomain.FriendlyName + ": ");
			Console.Error.WriteLine (e.Message);
			Console.Error.WriteLine ("Try “" + System.AppDomain.CurrentDomain.FriendlyName + " --help” for more information.");
			return 1;
		}

		if (show_help) {
			Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " input.flbgst");
			Console.WriteLine("Run Flabbergast interactively.");
			Console.WriteLine();
			Console.WriteLine("Options:");
			options.WriteOptionDescriptions (Console.Out);
			return 1;
		}

		if (files.Count > 1) {
			Console.Error.WriteLine ("No more than one Flabbergast script may be given.");
			return 1;
		}

		var assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("Repl"), AssemblyBuilderAccess.Run);
		var module_builder = assembly_builder.DefineDynamicModule("ReplModule");
		var unit = new CompilationUnit("<repl>", module_builder, false);
		var id = 0;

		var task_master = new ConsoleTaskMaster();
		var collector = new ConsoleCollector();
		var line_editor = new LineEditor("flabbergast");
		var completables = new Completables();
		line_editor.AutoCompleteEvent = completables.Handler;
		string s;
		var run = true;
	
		while (run && (s = line_editor.Edit(id + "‽ ", "")) != null) {
			var parser = new Parser("line" + id, s);
 			var run_type = parser.ParseFile(collector, unit, "Test" + id++); // TODO: change this to a REPL-specific element
			if (run_type != null) {
				object result = null;
				var computation = (Computation) Activator.CreateInstance(run_type, task_master);
				computation.Notify((r) => result = r);
				task_master.Slot(computation);
				task_master.Run();
				if (result != null) {
					HandleResult(result);
				}
			}
		}
		line_editor.SaveHistory();
		return 0;
	}
}