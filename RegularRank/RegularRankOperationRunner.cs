using System;
using System.IO;
using System.Reflection;

class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string library = Path.GetFullPath(Path.Combine(baseDir, @"..\..\Library"));
        AppDomain.CurrentDomain.AssemblyResolve += delegate (object sender, ResolveEventArgs e)
        {
            string name = new AssemblyName(e.Name).Name;
            string[] candidates = new string[]
            {
                Path.Combine(baseDir, name + ".dll"),
                Path.Combine(library, name + ".dll"),
                Path.Combine(library, name + "_201402.dll")
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return Assembly.LoadFrom(candidate);
            }
            return null;
        };
        if (args != null && args.Length > 0 && args[0].StartsWith("--check=", StringComparison.Ordinal))
        {
            string name = args[0].Substring("--check=".Length);
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^RegularRank[A-Za-z0-9]+Check$"))
                throw new ArgumentException("Unknown check name");
            Type check = typeof(SHEvaluation.Rank.RegularRank.RegularRankOperationCheck).Assembly.GetType("SHEvaluation.Rank.RegularRank." + name, true);
            string[] remaining = new string[args.Length - 1];
            Array.Copy(args, 1, remaining, 0, remaining.Length);
            return (int)check.GetMethod("Main").Invoke(null, new object[] { remaining });
        }
        return SHEvaluation.Rank.RegularRank.RegularRankOperationCheck.Main(args);
    }
}
