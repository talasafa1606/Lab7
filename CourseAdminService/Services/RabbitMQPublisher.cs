namespace CourseAdminService.Services;

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using CourseAdminService.Entities;

public class RabbitMQPublisher
{ 
    private readonly IConnection _connection; 
    private readonly IModel _channel; 
    private readonly ILogger<RabbitMQPublisher> _logger;
    
    public RabbitMQPublisher(IConfiguration configuration, ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
            
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        try
        {
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
                
            _logger.LogInformation("RabbitMQ publisher initialized successfully");
        }
        catch (Exception ex)
        { 
            _logger.LogError(ex, "Failed to initialize RabbitMQ publisher"); 
            throw;
        }
    }

    public void PublishCourse(Course course)
    {
        try
        {
            var message = JsonSerializer.Serialize(course);
            var body = Encoding.UTF8.GetBytes(message);
            
            _channel.BasicPublish(
                exchange: "course_exchange",
                routingKey: "course_created",
                basicProperties: null,
                body: body);
                
            _logger.LogInformation($"Course published: {course.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing course: {course.Name}");
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}