Utilities.ResetLogFile();

var configuration = Configurator.Read(args);
if (configuration == null) return;

var sim = new Simulator();
sim.Run(configuration);
