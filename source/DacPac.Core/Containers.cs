namespace DacPac.Core;

public record Containers(
    string Command,
    string CreatedAt,
    string HealthStatus,
    string ID,
    string Image,
    string Labels,
    string LocalVolumes,
    string Mounts,
    string Names,
    string Networks,
    object Platform,
    string Ports,
    string RunningFor,
    string Size,
    string State,
    string Status
);