using UsersCRUD.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.MapControllers();

// app.UseHttpsRedirection();
app.Run();
