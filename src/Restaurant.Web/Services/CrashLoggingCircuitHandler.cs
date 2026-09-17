using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Restaurant.Web.Services;

public class CrashLoggingCircuitHandler : CircuitHandler
{
    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        System.IO.File.AppendAllText("circuit_crash.txt", $"[{DateTime.UtcNow}] Circuit {circuit.Id} went down.\n");
        return base.OnConnectionDownAsync(circuit, cancellationToken);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        System.IO.File.AppendAllText("circuit_crash.txt", $"[{DateTime.UtcNow}] Circuit {circuit.Id} closed.\n");
        return base.OnCircuitClosedAsync(circuit, cancellationToken);
    }
}
