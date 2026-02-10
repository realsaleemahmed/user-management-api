using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Error handling FIRST
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal server error."
        });
    }
});

// Authentication
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        await next();
        return;
    }

    if (!context.Request.Headers.TryGetValue("Authorization", out var token) ||
        token != "Bearer my-secret-token")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Unauthorized"
        });
        return;
    }

    await next();
});

// Logging LAST
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;

    await next();

    var statusCode = context.Response.StatusCode;
    Console.WriteLine($"{method} {path} → {statusCode}");
});


//in memory storage
var users = new List<User>
{
    new User  { Id = 4, UserName = "Alice", Age = 35 },
    new User  { Id = 1, UserName = "John", Age = 30 },
    new User  { Id = 2, UserName = "Jane", Age = 25 },
    new User  { Id = 3, UserName = "Bob", Age = 40 }
};
//api endpoint

//Default 

app.MapGet("/", () => "User Management Api is Running!");

//Create User
app.MapPost("/users/create", (User user) => 
{
    //Validation

    /* No need because i automated it below 
    if (users.Any(u => u.Id == user.Id))
    {
        return Results.BadRequest("ID already exists");
    }*/
    try
    {
        if(user == null)
        {
            return Results.BadRequest("User cannot be null");
        }

        if (string.IsNullOrWhiteSpace(user.UserName) || (user.Age <= 0))
        {
            return Results.BadRequest("Username cannot be empty and age must be greater than 0");
        }
        // Auto-generate a unique ID by taking the highest existing ID and adding 1
        user.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
        user.UserName = user.UserName.Trim();

        users.Add(user);
        return Results.Ok(user);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});


//Get all Users
app.MapGet("/users/get",() =>
{
    try
    {
        return Results.Ok(users.OrderBy(u => u.Id));
    }
    catch(Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

//Get user by id
app.MapGet("/users/get/{id}",(int id) =>
{   try
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        return user == null ?
        Results.NotFound() : Results.Ok(user);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    } 
});

//Update User
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    try
    {
        var index = users.FindIndex(u => u.Id == id);
        if (index == -1)
        {
            return Results.NotFound();
        }
        //Validation
        if (string.IsNullOrWhiteSpace(updatedUser.UserName) || (updatedUser.Age <= 0))
        {
            return Results.BadRequest("Username cannot be empty and age must be greater than 0");
        }

        //enforce it from URL
        updatedUser.Id = id;
        //update
        users[index] = updatedUser;
        updatedUser.UserName = updatedUser.UserName.Trim();
        return Results.Ok(updatedUser);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }    
});

//Delete User
app.MapDelete("/users/{id:int}", (int id) =>
{
    try
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return Results.NotFound();
        }
        users.Remove(user);
        return Results.Ok(new { message = $"User with ID {id} has been deleted" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();

public class User
{
    public int Id { get; set; }
    required public string UserName { get; set; }
    public int Age { get; set; }
    
}
