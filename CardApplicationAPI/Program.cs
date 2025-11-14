
using CardApplicationAPI.Data.DBContext;
using CardApplicationAPI.Repositories.Implementations;
using CardApplicationAPI.Repositories.Interfaces;
using CardApplicationAPI.Services.Implementations;
using CardApplicationAPI.Services.Interfaces;
using CardApplicationAPI.Data.Entities;
using Microsoft.EntityFrameworkCore;
using CardApplicationAPI.Middleware;
namespace CardApplicationAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            // dependecy injection
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICardInfoRepository, CardInfoRepository>();
            builder.Services.AddScoped<ICardInfoService, CardInfoService>();

            builder.Services.AddDbContext<AppDbContext>(options =>
           options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("all", builder =>
                {
                    builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
           // }

            app.UseHttpsRedirection();
            app.UseCors("all");

            app.UseAuthorization();

            app.UseGlobalExceptionHandler();


            app.MapControllers();

            app.Run();
        }
    }
}
