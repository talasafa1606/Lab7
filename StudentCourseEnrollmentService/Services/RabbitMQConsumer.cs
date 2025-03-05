using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StudentCourseEnrollmentService.Data;
using StudentCourseEnrollmentService.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IModel = Microsoft.EntityFrameworkCore.Metadata.IModel;

namespace StudentCourseEnrollmentService.Services
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;
        private IConnection _connection;
        private RabbitMQ.Client.IModel _channel;

        public RabbitMQConsumer(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<RabbitMQConsumer> logger)
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
                
                _channel.ExchangeDeclare(
                    exchange: "course_exchange", 
                    type: ExchangeType.Direct);
                    
                _channel.QueueDeclare(
                    queue: "course_queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                    
                _channel.QueueBind(
                    queue: "course_queue",
                    exchange: "course_exchange",
                    routingKey: "course_created");
                
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

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (sender, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation($"Message received: {message}");
                    
                    ProcessCourseMessage(message);
                    
                    _channel.BasicAck(eventArgs.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    _channel.BasicNack(eventArgs.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: "course_queue",
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
        }

        private void ProcessCourseMessage(string message)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EnrollmentDbContext>();
            
            try
            {
                var courseData = JsonSerializer.Deserialize<Course>(message, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (courseData != null)
                {
                    var existingCourse = dbContext.Courses
                        .FirstOrDefault(c => c.Code == courseData.Code);
                        
                    if (existingCourse == null)
                    {
                        dbContext.Courses.Add(courseData);
                        dbContext.SaveChanges();
                        _logger.LogInformation($"Course added: {courseData.Name}");
                    }
                    else
                    {
                        existingCourse.Name = courseData.Name;
                        existingCourse.Description = courseData.Description;
                        existingCourse.Credits = courseData.Credits;
                        dbContext.SaveChanges();
                        _logger.LogInformation($"Course updated: {courseData.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing course message");
                throw;
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}