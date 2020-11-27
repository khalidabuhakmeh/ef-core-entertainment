# Entity Framework Core Entertainment Demo

## Step 1. Packages

- `dotnet-ef` cli tools

Enables these commonly used dotnet-ef commands:
- dotnet ef migrations add
- dotnet ef migrations list
- dotnet ef migrations script
- dotnet ef dbcontext info
- dotnet ef dbcontext scaffold
- dotnet ef database drop
- dotnet ef database update

```console
dotnet tool install dotnet-ef --version 5.0.0
```

The design package used for modeling and keeping track of schema changes.

```console
dotnet add package Microsoft.EntityFrameworkCore.Design
```

