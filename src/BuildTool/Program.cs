using BuildTool;
using System.CommandLine;



var rootCommand = new RootCommand("Starward build tool.");


var packCommand = new PackCommand();
rootCommand.Subcommands.Add(packCommand.Command);

var diffCommand = new DiffCommand();
rootCommand.Subcommands.Add(diffCommand.Command);

var releaseCommand = new ReleaseCommand();
rootCommand.Subcommands.Add(releaseCommand.Command);



return rootCommand.Parse(args).Invoke();
