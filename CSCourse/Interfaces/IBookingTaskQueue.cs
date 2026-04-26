using CSCourse.Models;
using System.Collections.Concurrent;

namespace CSCourse.Interfaces
{
    /// <summary>
    /// Интерфейс очереди задач бронирования. Определяет базовый контракт для добавления и извлечения задач бронирования.
    /// </summary>
    public interface IBookingTaskQueue
    {
        /// <summary>
        /// Добавляет задачу бронирования в очередь.
        /// </summary>
        /// <param name="task">Объект <see cref="Booking"/>, представляющий задачу бронирования, которую необходимо добавить в очередь.</param>
        /// <remarks>
        /// Операция является потокобезопасной в реализациях, предназначенных для многопоточной среды.
        /// </remarks>
        void Enqueue(Booking task);

        /// <summary>
        /// Пытается извлечь задачу бронирования из очереди без блокировки потока.
        /// </summary>
        /// <param name="task">При успешном извлечении содержит объект <see cref="Booking"/>;
        /// при отсутствии элементов в очереди устанавливается в <c>null</c>.</param>
        /// <returns>
        /// Возвращает <c>true</c>, если задача была успешно извлечена из очереди;
        /// <c>false</c> — если очередь пуста или произошла ошибка извлечения.
        /// </returns>
        /// <remarks>
        /// Метод не блокирует поток выполнения: сразу возвращает результат,
        /// даже если в очереди нет элементов.
        /// </remarks>
        bool TryDequeue(out Booking? task);
    }

    /// <summary>
    /// Реализация очереди задач бронирования в памяти на основе <see cref="ConcurrentQueue{T}"/>.
    /// Обеспечивает потокобезопасную работу с задачами бронирования.
    /// </summary>
    public class InMemoryBookingTaskQueue : IBookingTaskQueue
    {
        private readonly ConcurrentQueue<Booking> _queue = new();

        /// <summary>
        /// Добавляет задачу бронирования в конец очереди.
        /// Использует потокобезопасную коллекцию <see cref="ConcurrentQueue{T}"/> для хранения задач.
        /// </summary>
        /// <param name="task">Объект <see cref="Booking"/>, который необходимо добавить в очередь задач.</param>
        public void Enqueue(Booking task)
        {
            _queue.Enqueue(task);
        }

        /// <summary>
        /// Пытается асинхронно извлечь первую задачу из начала очереди.
        /// Если очередь пуста, метод сразу возвращает <c>false</c> без ожидания.
        /// </summary>
        /// <param name="task">Выходной параметр: при успешном извлечении получает объект <see cref="Booking"/>;
        /// в противном случае устанавливается в <c>null</c>.</param>
        /// <returns>
        /// <c>true</c>, если задача успешно извлечена; <c>false</c>, если очередь пуста.
        /// </returns>
        public bool TryDequeue(out Booking? task)
        {
            return _queue.TryDequeue(out task);
        }
    }
}
