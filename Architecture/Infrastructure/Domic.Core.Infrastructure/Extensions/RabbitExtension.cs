using System.Text;
using RabbitMQ.Client;

namespace Domic.Core.Infrastructure.Extensions;

public static class RabbitExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="queue"></param>
    public static void QueueDeclare(this IModel Channel, string queue)
        => Channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="queue"></param>
    /// <param name="args"></param>
    public static void QueueDeclare(this IModel Channel, string queue, IDictionary<string, object> args)
        => Channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="exchange"></param>
    public static void DirectExchangeDeclare(this IModel Channel, string exchange)
        => Channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="exchange"></param>
    /// <param name="args"></param>
    public static void DirectExchangeDeclare(this IModel Channel, string exchange, IDictionary<string, object> args)
        => Channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: args);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="exchange"></param>
    public static void FanOutExchangeDeclare(this IModel Channel, string exchange)
        => Channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="exchange"></param>
    /// <param name="queue"></param>
    /// <param name="route"></param>
    public static void BindQueueToDirectExchange(this IModel Channel, string exchange, string queue, string route)
        => Channel.QueueBind(queue: queue, exchange: exchange, routingKey: route, arguments: null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="exchange"></param>
    /// <param name="queue"></param>
    public static void BindQueueToFanOutExchange(this IModel Channel, string exchange, string queue)
        => Channel.QueueBind(queue: queue, exchange: exchange, routingKey: "", arguments: null);
    
    /*ارسال مستقیم پیام به Queue*/
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="message"></param>
    /// <param name="queue"></param>
    public static void PublishMessage(this IModel Channel, string message, string queue)
    {
        //Message => Queue
        
        byte[] Body = Encoding.UTF8.GetBytes(message);

        Channel.BasicPublish(exchange: "", routingKey: queue, basicProperties: null, body: Body);
    }
    
    /*ارسال پیام به مبادله گر ( Exchange ) پیام به روش Direct*/
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="message"></param>
    /// <param name="exchange"></param>
    /// <param name="route"></param>
    public static void PublishMessageToDirectExchange(this IModel Channel, string message, string exchange, string route)
    {
        //Message => Exchange => Queue

        byte[] Body = Encoding.UTF8.GetBytes(message);

        var props = Channel.CreateBasicProperties();

        //If this mode is not active, when there are a lot of messages inside our broker and the broker is down
        //at this time, after the broker is reactivated, the messages are lost .
        props.Persistent = true;
        
        Channel.BasicPublish(exchange: exchange, routingKey: route, basicProperties: props, body: Body);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="message"></param>
    /// <param name="exchange"></param>
    /// <param name="route"></param>
    /// <param name="headers"></param>
    public static void PublishMessageToDirectExchange(this IModel Channel, string message, string exchange, string route, 
        Dictionary<string, object> headers
    )
    {
        //Message => Exchange => Queue

        byte[] Body = Encoding.UTF8.GetBytes(message);

        var props = Channel.CreateBasicProperties();

        //If this mode is not active, when there are a lot of messages inside our broker and the broker is down
        //at this time, after the broker is reactivated, the messages are lost .
        props.Persistent = true;
        props.Headers    = headers;
        
        Channel.BasicPublish(exchange: exchange, routingKey: route, basicProperties: props, body: Body);
    }
    
    /*ارسال پیام به مبادله گر ( Exchange ) پیام به روش Direct | با ارسال Header های اختصاصی*/
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="message"></param>
    /// <param name="exchange"></param>
    /// <param name="route"></param>
    /// <param name="properties"></param>
    public static void PublishMessageToDirectExchange(this IModel Channel, string message, string exchange, 
        string route, IBasicProperties properties
    )
    {
        //Message => Exchange => Queue

        byte[] Body = Encoding.UTF8.GetBytes(message);
        
        Channel.BasicPublish(exchange: exchange, routingKey: route, basicProperties: properties, body: Body);
    }
    
    /*ارسال پیام به مبادله گر ( Exchange ) پیام به روش FanOut*/
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="message"></param>
    /// <param name="exchange"></param>
    public static void PublishMessageToFanOutExchange(this IModel Channel, string message, string exchange)
    {
        //Message => Exchange => Queue
        
        byte[] Body = Encoding.UTF8.GetBytes(message);
        
        var props = Channel.CreateBasicProperties();

        //If this mode is not active, when there are a lot of messages inside our broker and the broker is down
        //at this time, after the broker is reactivated, the messages are lost .
        props.Persistent = true;
        
        Channel.BasicPublish(exchange: exchange, routingKey: "", basicProperties: props, body: Body);
    }
    
    /*ارسال پیام به مبادله گر ( Exchange ) پیام به روش FanOut | با ارسال Header های اختصاصی*/
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="message"></param>
    /// <param name="exchange"></param>
    /// <param name="properties"></param>
    public static void PublishMessageToFanOutExchange(this IModel Channel, string message, string exchange, 
        IBasicProperties properties
    )
    {
        //Message => Exchange => Queue
        
        byte[] Body = Encoding.UTF8.GetBytes(message);
        
        Channel.BasicPublish(exchange: exchange, routingKey: "", basicProperties: properties, body: Body);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Channel"></param>
    /// <param name="queue"></param>
    /// <returns></returns>
    public static uint CountQueue(this IModel Channel, string queue) => Channel.MessageCount(queue);
}