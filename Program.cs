using System;
using System.IO;
using System.Text;


string input;

// cmd loop
while (true)
{

	// take input
	PrintNDisplay.Green($"[{General.current_dir}] >>> ");
	PrintNDisplay.Yellow("");
	input = Console.ReadLine();
	Console.ForegroundColor = ConsoleColor.White;

	// validation
	if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input)) { continue; }

	// separate cmds from kw via split(" ")
	string[] cmds = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
	string cmd_kw = cmds[0];
	string[] cmd_args = new string[cmds.Length-1]; Array.Copy(cmds, 1, cmd_args, 0, cmds.Length-1);  // copies cmds but removes the kw

	Debuger.Print(cmds);
	Debuger.Print(cmd_kw);
	Debuger.Print(cmd_args);

	// cmd n kw validation
	if (!Validate.KWord(cmd_kw)) { continue; }
	// if (!Validate.Input.Array(cmd_args, $"Invalid arguments {cmd_args.ToString()} for keyword {cmd_kw}.")) { continue; }

	// operation
	string result = KeyWord.map[cmd_kw].operate(cmd_args);
	Console.Write(!string.IsNullOrEmpty(result) ? result+'\n': "");

}


static class General
{
	static public readonly string start_dir = Environment.CurrentDirectory;  // permanent
	static public string current_dir = start_dir;  // changed through out the cmd loop
	static public readonly char path_separator = Path.DirectorySeparatorChar;
	static public string root = Path.GetPathRoot(current_dir);
	static public string user = root + string.Join(path_separator, current_dir.Split(path_separator)[1..3]);

	static private Dictionary<string, string> special_strings = new Dictionary<string, string>
	{
		{ "@start" , start_dir},
		{ "@self", current_dir},
		{ "@root", root },
		{ "@user",  user}
	};
	static public void ChangeCurrentDir(string new_dir)
	{
		current_dir = new_dir;
		special_strings["@self"] = current_dir;
	}

	static public void FilterSpStr(ref string argument)
	{
		static string Filter(string str)
		{
			try { return special_strings[str.Trim()]; }
			catch (KeyNotFoundException) { return str; }
		}
		argument = Filter(argument);
	}
}

static class Debuger
{
	private const bool DEBUGGING = false;

	static public void Print<T>(T content)
	{ if (DEBUGGING) { Console.WriteLine(content); } }

	static public void Print<T>(T[] content)
	{ 
		if (DEBUGGING) 
		{ 
			foreach (T item in content)
			{ Console.WriteLine(item); }
		} 
	}

}


static class PrintNDisplay
{
	static public void Red(string content)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Write(content);
		Console.ResetColor();
	}

	static public void Green(string content)
	{
		Console.ForegroundColor = ConsoleColor.Green;
		Console.Write(content);
	}

	static public void Yellow(string content)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.Write(content);
	}

	static public void Blue(string content)
	{
		Console.ForegroundColor = ConsoleColor.Blue;
		Console.Write(content);
		Console.ResetColor();
	}

	static public void White(string content)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write(content);
	}

	static public string File(string filename)
	{ return System.IO.File.ReadAllText(PrintNDisplay.ToAbsolutePath(filename)); }

	static public string Dir(string path, string indent = "", bool isLast = true)
	{
		if (!Directory.Exists(path)) return "";

		string result = indent + (isLast ? "└── " : "├── ") + Path.GetFileName(path) + "\n";
		indent += isLast ? "    " : "│   ";

		try
		{
			PrintNDisplay.Blue($"\t[loading directory: {path}]\n");
			PrintNDisplay.White("");
			string[] entries = Directory.GetFileSystemEntries(path);
			for (int i = 0; i < entries.Length; i++)
			{
				Debuger.Print("Loading...");
				string entry = entries[i];
				bool last = i == entries.Length - 1;
				if (Directory.Exists(entry))
					result += Dir(entry, indent, last);
				else
					result += indent + (last ? "└── " : "├── ") + Path.GetFileName(entry) + "\n";
			}
		}
		catch (UnauthorizedAccessException) { return "[access denied]"; }

		return result;
	}

	static public string ToAbsolutePath(string path) =>
	Path.IsPathRooted(path) ? path : Path.GetFullPath(path);


}

static class Validate
{

	static private bool Base(string invalid_message, bool cond)
	{
		if (cond) 
		{ PrintNDisplay.Red(invalid_message+'\n'); 
			return false; } 
		return true;
	}

	static public class Input
	{

		static public bool Basic(string message, bool condition)
		{ return Base($"Invalid input; {message}.", condition); }

		static public bool Array<T>(T[] array, string message)
		{ return Base($"Invalid input; {message}.", array.Length == 0); }

	}

	static public bool KWord(string kw)
	{ return Base($"Invalid keyword: {kw}", !KeyWord.all.Contains(kw)); }

}

class KeyWord
{

	private Delegate Method { get; }
	private string ID { get; }

	static public List<string> all = new List<string>();
	static public Dictionary<string, KeyWord> map = new Dictionary<string, KeyWord>();

	private KeyWord(Delegate method)
	{
		Method = method;
		ID = method.Method.Name.ToLower();
		all.Add(ID.ToLower());
		map[ID.ToLower()] = this;
	}

	public string operate(params string[] parameters)
	{
		try
		{
			var result = Method.DynamicInvoke(parameters);
			return result?.ToString() ?? "";
		}
		catch (Exception e)
		{
			var ex = e.InnerException ?? e;
			Console.ForegroundColor = ConsoleColor.Red;
			return $"Something went wrong with the operation \"{ID}\":\n\t{ex.ToString().Replace("\n", "\t\n")}";
		}
	}

	private static class KWFuncs
	{
		static public string Clear()
		{
			Console.Clear();
			return "";
		}

		static public string Reset()
		{
			General.current_dir = General.start_dir;
			Console.Clear();
			return "";
		}

		static public string Open(string file)
		{
			General.FilterSpStr(ref file);
			System.Diagnostics.Process.Start("explorer.exe", file);
			return $"\"{file}\" successfully opened on File Explorer.";
		}

		static public string Enter(string dir)
		{
			string raw_dir = dir;
			General.FilterSpStr(ref dir);

			string full_path = Path.IsPathRooted(dir) ? dir : Path.Combine(General.current_dir, dir);

			if (!Directory.Exists(full_path))
				throw new Exception($"Cannot enter file \"{full_path}\".");

			General.ChangeCurrentDir(full_path);
			return $"Directory \"{General.current_dir}\" successfully entered.";
		}

		static public string Back(string times_str) 
		{
			if (times_str.ToLower().Trim() == "all") { return Root(); }
			string[] parts = General.current_dir.Split(General.path_separator, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length <= 1)
			{
				General.current_dir = Path.GetPathRoot(General.current_dir);
				return $"Went back to root: \"{General.current_dir}\".";
			}

			int times = Convert.ToInt32(times_str);
			for (int i = 0; i < times; i++)
			{
				parts = General.current_dir.Split(General.path_separator, StringSplitOptions.RemoveEmptyEntries);
				if (parts == Array.Empty<string>()) { General.current_dir = General.root; break; }
				General.ChangeCurrentDir(string.Join(General.path_separator.ToString(), parts[..^1])); 
			}
			return $"Went back to \"{General.current_dir}\" successfully.";
		}
		
		static public string Read(string file)
		{
			string fullPath = Path.Combine(General.current_dir, file);
			return PrintNDisplay.File(fullPath);
		}

		static public string Display(string path, string file)
		{
			bool save_file = file != "@null";
			string result;
			General.FilterSpStr(ref path);
			if (File.Exists(path)) 
			{ 
				result = Read(path); 
				if (save_file) { File.WriteAllText(file, result); }
				return result;
			}
			else if (Directory.Exists(path)) 
			{ 
				result = PrintNDisplay.Dir(path); 
				if (save_file) { File.WriteAllText(file, result); }
				return result;
			}
			throw new Exception($"Unable to access the path \"{PrintNDisplay.ToAbsolutePath(path)}\".");
		}

		static public string List(string path, string file)
		{
			bool save_file = file != "@null";
			General.FilterSpStr(ref path);
			if (!Directory.Exists(path)) 
			{ throw new Exception("The kew word \"list\" must take a directory as an argument."); }
			string result = PrintNDisplay.Dir(path, isLast: false);
			if (save_file) { File.WriteAllText(file, result); }
			return result;
		}

		static public string Creat(string path)
		{
			if (File.Exists(path) || Directory.Exists(path))
			{ throw new Exception($"The path \"{path}\" already exists."); }

			string[] parts = path.Split(General.path_separator);

			if (parts.Last().Split(".").Length == 1)  // dir
			{ Directory.CreateDirectory(path); }
			else /* file */ { File.Create(path); }

			return $"\"{path}\" successfully created.";
		}

		static public string Delete(string path)
		{
			string abs_path = Path.Combine(General.current_dir, path);

			if (File.Exists(abs_path))
			{
				File.Delete(abs_path);
				return $"File \"{abs_path}\" successfully deleted.";
			}
			else if (Directory.Exists(abs_path))
			{
				Directory.Delete(abs_path, true);
				return $"Directory \"{abs_path}\" successfully deleted.";
			}
			else
			{
				throw new Exception($"Delete failed: \"{abs_path}\" does not exist.");
			}
		}

		static public string Move(string source, string detination)
		{
			File.Move(source, detination);
			return $"File \"{PrintNDisplay.ToAbsolutePath(source)}\" " +
				$"successfully moved to " +
				$"\"{PrintNDisplay.ToAbsolutePath(detination)}{General.path_separator}{source.Split(General.path_separator).Last()}\".";
		}

		static public string Root()
		{
			General.current_dir = General.root;
			return $"Current directory set to root: \"{General.current_dir}\".";
		}

		static public void Exit()
		{ Environment.Exit(0); }

	}

	static public KeyWord clear = new KeyWord(KWFuncs.Clear);
	static public KeyWord open = new KeyWord(KWFuncs.Open);
	static public KeyWord enter = new KeyWord(KWFuncs.Enter);
	static public KeyWord back = new KeyWord(KWFuncs.Back);
	static public KeyWord read = new KeyWord(KWFuncs.Read);
	static public KeyWord display = new KeyWord(KWFuncs.Display);
	static public KeyWord list = new KeyWord(KWFuncs.List);
	static public KeyWord creat = new KeyWord(KWFuncs.Creat);
	static public KeyWord delete = new KeyWord(KWFuncs.Delete);
	static public KeyWord move = new KeyWord(KWFuncs.Move);
	static public KeyWord exit = new KeyWord(KWFuncs.Exit);
	static public KeyWord root = new KeyWord(KWFuncs.Root);

}
