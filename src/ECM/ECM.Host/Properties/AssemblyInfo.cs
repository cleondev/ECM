using System.Runtime.CompilerServices;

// Expose internals to the ECM.Host.Tests project so middleware and helpers can be validated without
// weakening the public surface of the host.
[assembly: InternalsVisibleTo("ECM.Host.Tests")]
