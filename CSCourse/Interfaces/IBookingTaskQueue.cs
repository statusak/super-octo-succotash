using CSCourse.Models;
using System.Collections.Concurrent;

namespace CSCourse.Interfaces
{
    public interface IBookingTaskQueue
    {
        void Enqueue(Booking task);
        bool TryDequeue(out Booking task);
    }

    public class InMemoryBookingTaskQueue : IBookingTaskQueue
    {
        private readonly ConcurrentQueue<Booking> _queue = new();

        public void Enqueue(Booking task)
        {
            _queue.Enqueue(task);
        }

        public bool TryDequeue(out Booking task)
        {
            return _queue.TryDequeue(out task);
        }
    }
}
