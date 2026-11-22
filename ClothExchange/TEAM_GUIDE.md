# Team guide (not part of the submission)

## 1. Setup
1. Open `ClothExchange.sln` in Visual Studio 2022 (needs the ASP.NET workload; LocalDB comes with it).
2. Run `Database/setup.sql` once. In Visual Studio: View > SQL Server Object Explorer > `(localdb)\MSSQLLocalDB` > right-click > New Query > paste > run.
3. Press F5. If the connection fails, check the connection string in `appsettings.json`.
4. Demo logins: `sarah@example.com` and `alex@example.com`, password `demo123`.

## 2. Where each syllabus topic lives
| Syllabus topic | Where to show it |
| --- | --- |
| Class, object, constructor (Unit 1, P1) | `Models/Clothing.cs`, `ExchangeRequest.cs` constructors |
| Abstract class, inheritance (P2) | `Models/BaseEntity.cs` -> inherited by `User`, `Clothing`, `ExchangeRequest` |
| Exception handling (P3) | `try/catch` in `AccountController`, `ClothesController`, `ExchangeController` |
| Session and cookie (Unit 2, P7) | `AccountController.Login` (Session: UserId, Cookie: LastEmail) |
| MVC, routing (Unit 3, P8) | `Program.cs` route pattern, all Controllers and Views |
| Layout view | `Views/Shared/_Layout.cshtml` |
| Partial view | `_ClothingCard.cshtml`, `_ClothingFields.cshtml` |
| HTML helpers | `Html.BeginForm`, `Html.TextBoxFor`, `Html.DropDownListFor` in Create/Edit/Login/Register |
| Model validation | `[Required]`, `[StringLength]`, `[Compare]` in Models and ViewModels |
| Action filter | `Filters/LoginRequiredAttribute.cs` |
| Connected ADO.NET (Unit 4, P9) | `SqlConnection`, `SqlCommand`, `SqlDataReader` in `ClothesController.Details/MyClothes` |
| Disconnected ADO.NET | `SqlDataAdapter` + `DataSet` in `ClothesController.Index` |
| CRUD | Clothes: Create / Index+Details / Edit / Delete |
| File upload | `ClothesController.SavePhoto` (`IFormFile`, saved in `wwwroot/uploads`, path stored in the database) |
| Transaction | `ClothesController.Delete`, `ExchangeController.Accept` |
| ASP.NET Core, DI, middleware (Unit 5) | `Program.cs` (`AddScoped<DbHelper>`), `Middleware/RequestLoggerMiddleware.cs` |
| REST API (P11) | `Controllers/ClothesApiController.cs` -> open `/api/clothes` in the browser |

## 3. Suggested split (so each person can explain their part)
- **Person 1 - Accounts:** `AccountController`, `AccountViewModels`, `PasswordHelper`, Login/Register views, `LoginRequiredAttribute`. Topics: session, cookie, validation, filter.
- **Person 2 - Clothes:** `ClothesController`, `Clothing`, `ClothingMapper`, Clothes views, partials. Topics: CRUD, DataReader vs DataAdapter/DataSet, partial views, HTML helpers.
- **Person 3 - Exchange + API:** `ExchangeController`, `ExchangeRequest`, `ClothesApiController`, `Program.cs`, middleware, `setup.sql`. Topics: transactions, REST API, DI, middleware, pipeline.

Everyone should still be able to describe the whole flow (see section 4).

## 4. The flow in one minute (practise saying this)
Browser sends a request -> middleware pipeline (static files, session, our logger) -> routing picks a controller and action -> the action filter checks login if needed -> the controller opens a `SqlConnection` (given by DI through `DbHelper`), runs a parameterised `SqlCommand`, reads the result -> fills a model -> returns a Razor view -> the view uses the layout and partials to build the HTML.

## 5. Questions your sir may ask
- **Why `@Email` parameters instead of putting the value in the SQL string?** Prevents SQL injection.
- **DataReader vs DataSet?** DataReader is connected, forward-only, fast, keeps the connection open while reading. DataSet is disconnected: the adapter fills it and closes the connection; we can work with the data after.
- **Why a transaction in Accept?** Three updates must all succeed or none (request accepted, both clothes exchanged, other requests rejected).
- **What does `AddScoped<DbHelper>` mean?** One instance per web request, created by the DI container and given to the controller constructor.
- **Why `using` around connections?** It closes and disposes the connection automatically, even if an exception happens.
- **Session vs cookie?** Session is stored on the server (we keep UserId), cookie is stored in the browser (we keep the last email).
- **Why store a hash?** We never keep the real password. (Be honest: SHA256 without a salt is fine for a class project, real apps use ASP.NET Identity / bcrypt.)
- **What is middleware?** A component in the request pipeline that can handle the request or pass it to the next one.
- **Controller vs API controller?** MVC controller returns a View (HTML), API controller returns data (JSON).

- **How does photo upload work?** The form uses `enctype="multipart/form-data"`. The action receives an `IFormFile`, we check the extension and size, save it in `wwwroot/uploads` with a random name (GUID), and store only the path in the database. Files in `wwwroot` are served as static files, so everyone can see them.
- **Where does `IWebHostEnvironment` come from?** Dependency Injection, same as `DbHelper`. It tells us the physical path of `wwwroot`.

## 6. Before the viva
- Change something small yourselves (a colour, a field, a message) and run it again. It builds real understanding.
- Put a breakpoint in `Login` and step through once.
- Try the API: `http://localhost:5067/api/clothes`.
- Know the limits: no image upload (we use a link), no admin panel, SHA256 without salt. Say them confidently, they are scope decisions.

## 7. Troubleshooting
- **A page shows a red box with an error:** read the message, it comes straight from SQL Server. "Invalid object name" means the tables were not created, run `Database/setup.sql` again. "Execution Timeout Expired" means something is blocking the database, run `Database/fix_timeouts.sql`.
- **Page keeps loading and then a timeout:** stop the app (Shift+F5), run `Database/fix_timeouts.sql`, start again. LocalDB also sleeps when idle, the first request after a break can be slow (the connection string now waits up to 60 seconds and retries).
- **Visual Studio stops on an exception (yellow line):** press F5 once to continue, or untick "Break when this exception type is user-unhandled" in the popup. The page will then show the red error box.
- **"Invalid column name RequestedClothId":** an old wrong `ExchangeRequests` table exists. Run `Database/fix_exchange_table.sql`.
- **Uploaded photos are saved on the computer that runs the app** (`wwwroot/uploads`), not in git. For the demo, run everything on one laptop.
- You can only request OTHER people's clothes. To test: register a second user (or login as sarah@example.com), add clothes, then switch accounts.
- Do not leave a query window open with `BEGIN TRAN` in SQL Server, it locks the tables.
