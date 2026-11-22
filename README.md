# ReWear - Clothing Exchange Platform

A .NET web application where users exchange clothes they no longer wear instead of throwing them away.
Made for the **.NET Technologies (01CE1523)** course.

**Built with:** ASP.NET Core MVC (C#), ADO.NET, SQL Server LocalDB, Bootstrap

## Features
- Register and login (Session + Cookie)
- Add, edit, delete your own clothes (CRUD) and search/browse everyone's clothes
- Request an exchange by offering one of your own items
- Accept or reject incoming requests
- REST API: `GET / POST / PUT / DELETE  /api/clothes`

## Requirements (on any laptop)
- **Visual Studio 2022 version 17.12 or newer**, with the **ASP.NET and web development** workload (this installs .NET 9 and SQL Server LocalDB)
- Internet connection on the first run (NuGet downloads one package)

> Older Visual Studio? Open `ClothExchange/ClothExchange.csproj` and change `net9.0` to `net8.0`.

## How to run
1. In Visual Studio: **Git -> Clone Repository**, paste this repo's URL, and click **Clone**.
2. Open `Clothing-Exchange-Platform.sln`.
3. Press **F5** (the green **http** button).
4. The database `ClothExchangeDb` and its tables are created automatically on the first run. Register two users to try the exchange flow.

Each laptop has its own local database, so users and items are not shared between laptops.

## If the app shows "network-related or instance-specific error"
LocalDB is not running. Open a Command Prompt and run:
```
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```
Then press F5 again.

## Project structure
```
ClothExchange/
  Controllers/   Account, Clothes, Exchange, Home, ClothesApi (REST)
  Models/        BaseEntity (abstract), IDisplayable (interface), Clothing, ExchangeRequest, view models
  Views/         Razor pages, layout, partial views
  Data/          DbHelper (all ADO.NET code)
  Filters/       LoginRequired action filter
  Middleware/    RequestLoggerMiddleware
  Program.cs     Services (DI, Session) and request pipeline
```
More details (syllabus mapping, demo steps, viva questions): see `ClothExchange/README.md`.
