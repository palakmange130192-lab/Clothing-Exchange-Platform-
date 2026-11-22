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
