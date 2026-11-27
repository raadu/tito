// The AppHost describes every piece of the app for local development.
// `aspire run` (or F5 on this project) starts them all and opens the Aspire dashboard.
var builder = DistributedApplication.CreateBuilder(args);

// SQL Server 2022 in Docker. Aspire generates the sa password and stores it in user secrets.
var sql = builder.AddSqlServer("sql")
    .WithDataVolume()                               // keep data when the container restarts
    .WithLifetime(ContainerLifetime.Persistent);    // keep container running between runs (faster start)

var db = sql.AddDatabase("tito");                   // the database our API uses

// Run the API project; the path is relative to this AppHost folder.
var api = builder.AddDotnetProject("api", "../Tito.Api/Tito.Api.csproj")
    .WithReference(db)                              // injects ConnectionStrings__tito into the API
    .WaitFor(db);                                   // start the API only when SQL is healthy

// The Angular app: runs `npm install`, then `npm run start:aspire` (ng serve).
builder.AddJavaScriptApp("web", "../../web/tito-web", "start:aspire")
    .WithReference(api)                             // injects services__api__https__0 for proxy.conf.mjs
    .WaitFor(api)
    .WithHttpEndpoint(port: 4200, env: "PORT")      // browser uses :4200; Aspire tells ng serve its real port via PORT
    .WithExternalHttpEndpoints();

builder.Build().Run();
