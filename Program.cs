using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR(); // ✅ Add SignalR support

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // ✅ Enables serving static files (CSS, JS)
app.UseRouting();
app.UseAuthorization();

// ✅ Map SignalR hub
app.MapHub<TicTacToeHub>("/tictactoehub");

app.MapRazorPages();

app.Run();
