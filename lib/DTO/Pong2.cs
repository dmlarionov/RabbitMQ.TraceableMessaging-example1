using RabbitMQ.TraceableMessaging.Models;

namespace lib.DTO
{
    public class Pong2 : Reply
    {
        public int Result { get; set; }
    }
}