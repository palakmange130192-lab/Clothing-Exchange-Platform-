# ReWear - Clothing Exchange Platform

A web application where users exchange unused clothes instead of throwing them away.
Made for the .NET Technologies course (01CE1523).

Built with ASP.NET Core MVC (C#), ADO.NET and SQL Server LocalDB.

## Features
- Register and login (session + cookie)
- Add, edit, delete your own clothes (with photo upload)
- Browse and search clothes by title or category
- Request an exchange by offering one of your own clothes
- Accept or reject requests you receive
- Small REST API: `GET /api/clothes` and `GET /api/clothes/{id}`

## How to run
1. Open `ClothExchange.sln` in Visual Studio 2022 (.NET 9 SDK).
2. Open `Database/setup.sql` in Visual Studio (SQL Server Object Explorer -> `(localdb)\MSSQLLocalDB`) or SSMS and run it once.
3. Press F5.

Demo logins (password `demo123`): `sarah@example.com`, `alex@example.com`

## Folders
| Folder | What is inside |
| --- | --- |
| Controllers | Handle requests (Account, Clothes, Exchange, Home, API) |
| Models / ViewModels | Classes for data and forms |
| Views | Razor pages (.cshtml) |
| Data | Database connection helper and row mapper |
| Filters / Middleware | Login filter and request logger |
| Database | SQL script |
