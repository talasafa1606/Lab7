using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StudentCourseEnrollmentService.Data;
using StudentCourseEnrollmentService.Entities;

namespace StudentCourseEnrollmentService.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMQConsumer(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<RabbitMQConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:HostName"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:UserName"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(exchange: "course_exchange", type: ExchangeType.Direct);

                _channel.QueueDeclare(queue: "course_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                _channel.QueueBind(queue: "course_queue", exchange: "course_exchange", routingKey: "course_created");

                _logger.LogInformation("RabbitMQ initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, eventArgs) => await ProcessCourseMessageAsync(eventArgs);

            _channel.BasicConsume(queue: "course_queue", autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessCourseMessageAsync(BasicDeliverEventArgs eventArgs)
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"Message received: {message}");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();

            try
            {
                var courseData = JsonSerializer.Deserialize<Course>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (courseData != null)
                {
                    var existingCourse = await dbContext.Courses.FirstOrDefaultAsync(c => c.Code == courseData.Code);

                    if (existingCourse == null)
                    {
                        await dbContext.Courses.AddAsync(courseData);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation($"Course added: {courseData.Name}");
                    }
                    else
                    {
                        existingCourse.Name = courseData.Name;
                        existingCourse.Description = courseData.Description;
                        existingCourse.Credits = courseData.Credits;
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation($"Course updated: {courseData.Name}");
                    }
                }

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing course message");
                _channel.BasicNack(eventArgs.DeliveryTag, false, true);
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
