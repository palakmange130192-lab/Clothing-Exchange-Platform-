# ReWear - Clothes Exchange Platform
.NET Technologies (01CE1523) mini project - ASP.NET Core MVC + ADO.NET + SQL Server

## How to run
1. Install Visual Studio 2022 (it includes SQL Server LocalDB) and the .NET 9 SDK.
2. Open `ClothExchange.csproj` in Visual Studio and press F5.
   (or in a terminal inside this folder: `dotnet run`, then open http://localhost:5067)
3. The database `ClothExchangeDb` and all tables are created automatically on the first run.
4. If you use full SQL Server instead of LocalDB, change the connection string in `appsettings.json`.

## Demo flow (do this in the viva)
1. Register user A and user B (use two browsers / one normal + one incognito).
2. Login as A -> My Items -> Add Item (add 1-2 items).
3. Login as B -> add an item -> Browse -> open A's item -> Request Exchange -> choose your item.
4. Login as A -> Incoming Requests -> Accept.
5. Both items now show status "Exchanged". Show REST API: /api/clothes

## Project structure (MVC)
| Folder | What it contains |
|---|---|
| Models | Clothing, ExchangeRequest, Register/Login view models (data + validation rules) |
| Views | Razor pages (.cshtml), Layout, Partial views |
| Controllers | Account (login/register), Clothes (CRUD), Exchange (requests), Home, ClothesApi (REST) |
| Data/DbHelper.cs | All ADO.NET database code |
| Middleware | Our own request logger |
| Filters | LoginRequired action filter |
| Program.cs | Services (DI, Session) + middleware pipeline + routing |

## Syllabus mapping
| Syllabus topic | Where in the project |
|---|---|
| OOP: classes, constructors, access modifiers | Models, DbHelper (constructor), controllers |
| Abstract class + inheritance | Models/BaseEntity.cs -> inherited by Clothing and ExchangeRequest |
| Interface | Models/IDisplayable.cs (implemented by BaseEntity) |
| Polymorphism (method overriding) | `GetSummary()` is abstract in BaseEntity; Clothing and ExchangeRequest each override it. Shown on the Details page and as a tooltip on request rows |
| Exception handling | try/catch in AccountController, ClothesController, ExchangeController |
| Session | AccountController.Login (SetInt32 / SetString), Logout (Clear) |
| Cookie | AccountController.Login (Response.Cookies.Append "LastEmail", 7 days) and Login GET (Request.Cookies) |
| MVC: controllers, Razor views | Controllers/ and Views/ folders |
| Layout view | Views/Shared/_Layout.cshtml |
| Partial view | Shared/_ClothCard.cshtml, Clothes/_Form.cshtml |
| Model validation | [Required], [StringLength], [EmailAddress], [Compare] in Models |
| Action filter | Filters/LoginRequiredAttribute.cs |
| Routing | Program.cs -> MapControllerRoute ({controller}/{action}/{id?}) |
| ADO.NET (connected) | SqlConnection, SqlCommand, SqlDataReader in DbHelper and controllers |
| ADO.NET (disconnected) | SqlDataAdapter + DataSet + DataTable in DbHelper.SearchClothes (used by the Browse page) |
| HTML helpers | Html.BeginForm, Html.TextBoxFor, Html.PasswordFor, Html.LabelFor in Login and Register views |
| CRUD | ClothesController (Create, Read, Update, Delete) |
| Transaction | ExchangeController.Accept (BeginTransaction / Commit / Rollback) |
| Dependency Injection | `AddSingleton<DbHelper>()` in Program.cs, constructor injection in controllers |
| Middleware pipeline | UseStaticFiles -> UseRouting -> UseSession -> RequestLoggerMiddleware -> endpoints |
| Custom middleware | Middleware/RequestLoggerMiddleware.cs (prints every request in the console) |
| REST API | Controllers/ClothesApiController.cs: GET, POST, PUT, DELETE on /api/clothes (POST/PUT/DELETE need login) |

## Likely viva questions
- **What is MVC?** Model = data classes, View = Razor pages (UI), Controller = handles requests and connects the two.
- **Why parameters like @id in SQL?** To prevent SQL injection.
- **How does login work?** On login we check email + hashed password in the Users table, then store UserId in Session. The LoginRequired filter checks Session before protected actions.
- **Why hash passwords?** So plain passwords are never stored in the database (SHA256 in DbHelper.HashPassword).
- **Connected vs disconnected ADO.NET?** Connected: SqlConnection stays open while SqlDataReader reads rows (used in most pages). Disconnected: SqlDataAdapter fills a DataSet, the connection closes, and we work on the data in memory (used on the Browse page).
- **Session vs cookie?** Session is stored on the server (we keep UserId). Cookie is stored in the browser (we keep the last email for 7 days).
- **What is an abstract class?** A class that cannot be created with new; BaseEntity holds Id and CreatedAt, and Clothing and ExchangeRequest inherit them.
- **What is middleware?** A component in the request pipeline that can do work and pass the request to the next one. Ours prints each request in the console.
- **Why try/catch?** So a database error shows a message on the page instead of crashing the app.
- **Why a transaction in Accept?** Updating the request and both items must all succeed together, otherwise rollback.
- **What is DI?** Instead of creating DbHelper with `new` everywhere, the framework creates it once and gives it to controllers through their constructor.
- **What does TempData do?** Holds a message (like "Item added") for one redirect.

## Testing the REST API (POST / PUT / DELETE)
GET works in the address bar: http://localhost:5067/api/clothes
POST/PUT/DELETE need you to be logged in on the website first. Then press F12 -> Console in the same browser tab and paste:

```js
// POST: add an item
fetch('/api/clothes', {method:'POST', headers:{'Content-Type':'application/json'},
  body: JSON.stringify({title:'API Jacket', description:'added by API', size:'M', category:'Men', condition:'Good'})
}).then(r => r.json()).then(console.log)

// PUT: update item 1 (replace 1 with your own item's id)
fetch('/api/clothes/1', {method:'PUT', headers:{'Content-Type':'application/json'},
  body: JSON.stringify({title:'Updated title', description:'changed', size:'L', category:'Men', condition:'Good'})
}).then(r => r.json()).then(console.log)

// DELETE: remove item 1
fetch('/api/clothes/1', {method:'DELETE'}).then(r => console.log(r.status))   // 204 = deleted
```

## More viva answers
- **Interface vs abstract class?** An interface is only a contract (what methods must exist). An abstract class can also hold real code and data (Id, CreatedAt). Our BaseEntity does both: it implements IDisplayable and shares Id/CreatedAt.
- **What is polymorphism here?** GetSummary() is declared once in BaseEntity, and Clothing and ExchangeRequest each override it with their own text. Calling it on a BaseEntity runs the correct child version.
- **HTTP status codes in the API?** 200 OK, 201 Created, 204 No Content, 400 bad data, 401 not logged in, 403 not your item, 404 not found.
