using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Lab7.Entities;

namespace Lab7.Services;

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
                exchange: "student_exchange", 
                type: ExchangeType.Direct);
                
            _channel.QueueDeclare(
                queue: "student_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
                
            _channel.QueueBind(
                queue: "student_queue",
                exchange: "student_exchange",
                routingKey: "student_created");
            
            _channel.ExchangeDeclare(
                exchange: "teacher_exchange", 
                type: ExchangeType.Direct);
                
            _channel.QueueDeclare(
                queue: "teacher_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
                
            _channel.QueueBind(
                queue: "teacher_queue",
                exchange: "teacher_exchange",
                routingKey: "teacher_created");
                
            _logger.LogInformation("RabbitMQ publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ publisher");
            throw;
        }
    }

    public void PublishStudent(Student student)
    {
        try
        {
            var message = JsonSerializer.Serialize(student);
            var body = Encoding.UTF8.GetBytes(message);
            
            _channel.BasicPublish(
                exchange: "student_exchange",
                routingKey: "student_created",
                basicProperties: null,
                body: body);
                
            _logger.LogInformation($"Student published: {student.FirstName} {student.LastName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing student: {student.FirstName} {student.LastName}");
            throw;
        }
    }

    public void PublishTeacher(Teacher teacher)
    {
        try
        {
            var message = JsonSerializer.Serialize(teacher);
            var body = Encoding.UTF8.GetBytes(message);
            
            _channel.BasicPublish(
                exchange: "teacher_exchange",
                routingKey: "teacher_created",
                basicProperties: null,
                body: body);
                
            _logger.LogInformation($"Teacher published: {teacher.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing teacher: {teacher.Name}");
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
