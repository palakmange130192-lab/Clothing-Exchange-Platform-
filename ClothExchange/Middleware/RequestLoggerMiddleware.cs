namespace ClothExchange.Middleware
{
    // Our own middleware: prints every request in the console window.
    public class RequestLoggerMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {context.Request.Method} {context.Request.Path}");
            await _next(context);   // pass the request to the next middleware
        }
    }
}
