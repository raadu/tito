// The AppHost describes every piece of the app for local development.
// `aspire run` (or F5 on this project) starts them all and opens the Aspire dashboard.
var builder = DistributedApplication.CreateBuilder(args);

// SQL Server 2022 in Docker. Aspire generates the sa password and stores it in user secrets.
var sql = builder.AddSqlServer("sql")
    .WithDataVolume()                               // keep data when the container restarts
    .WithLifetime(ContainerLifetime.Persistent);    // keep container running between runs (faster start)

var db = sql.AddDatabase("tito");                   // the database our API uses

// Run the API project; the path is relative to this AppHost folder.
builder.AddDotnetProject("api", "../Tito.Api/Tito.Api.csproj")
    .WithReference(db)                              // injects ConnectionStrings__tito into the API
    .WaitFor(db);                                   // start the API only when SQL is healthy

builder.Build().Run();
