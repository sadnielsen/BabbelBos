var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with a persistent data volume; the API gets the connection string "babbelbos".
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("babbelbos-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("babbelbos");

builder.AddProject<Projects.Babbelbos_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health/ready");

await builder.Build().RunAsync();
