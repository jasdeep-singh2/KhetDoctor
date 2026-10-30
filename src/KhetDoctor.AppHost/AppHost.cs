var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17")   // Postgres + vector search
    .WithDataVolume()                          // data survives restarts
    .WithPgAdmin();                            // DB browser UI

var db = postgres.AddDatabase("khetdoctor");

builder.AddProject<Projects.KhetDoctor_Api>("api")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();