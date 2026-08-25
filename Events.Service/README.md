# Events.Service

### Назначение

Микросервис управляет мероприятиями: создаёт, фильтрует, резервирует и освобождает места. Обеспечивает атомарность операций и защиту от состояний гонки.

### Стек технологий

- ASP.NET Core Web API $.NET 10$
- Entity Framework Core + PostgreSQL $`timestamp with time zone`$
- Транзакции с уровнем изоляции `RepeatableRead`
- Семафоры и блокировки $`UPDLOCK`$ для защиты от состояний гонки
- Confluent.Kafka для событийной коммуникации
- Swagger/OpenAPI
- Clean Architecture

### Основные возможности

- CRUD для мероприятий
- Фильтрация и пагинация списка мероприятий
- Резервирование и освобождение мест с защитой от состояний гонки
- Публикация событий в Kafka при бронировании
- Глобальная обработка ошибок $RFC 7807$

### Архитектура

#### Domain Layer

- Модель `Event`: `Id`, `Title`, `Description`, `TotalSeats`, `AvailableSeats`, `StartAt`, `EndAt`.
- Модель `FilterEvent` для фильтрации.
- Модель `PaginatedResult` для пагинации.
- Доменные исключения: `NoAvailableSeatsException`, `BookingForPastEventException`.

#### Application Layer

- Интерфейсы: `IEventService`, `IEventRepository`, `IEventKafkaPublisher`.
- DTO: `EventCreateDto`, `EventUpdateDto`, `FilterEventDto`, `EventInfoDto`, `FilterRepositoryEventDto`, `EventRepositoryUpdateDto`.
- Сервис: `EventService` с реализацией бизнес-логики.
- Фоновый сервис: `EventBackgroundService` для обработки входящих Kafka-событий.

#### Infrastructure Layer

- Контекст БД: `AppDbContext` с конфигурацией через Fluent API $`EventConfiguration`$.
- Репозиторий: `EventRepository` с методами для CRUD, фильтрации, резервирования и освобождения мест.
- Kafka-клиент: `EventKafkaPublisher` для публикации событий.
- Инициализация топиков Kafka: `KafkaTopicInitializer`.
- Миграции EF Core.
- Настройка DI через `InfrastructureCollectionExtensions`.

#### Presentation Layer

- Контроллер: `EventsController` с эндпоинтами для управления мероприятиями и создания бронирований.
- Middleware: `GlobalExceptionHandlingMiddleware`.
- Swagger UI.
- Конфигурация приложения и DI-контейнера в `Program.cs`.

### API эндпоинты

| Метод | Путь | Описание | Доступ |
|-------|------|----------|--------|
| `GET` | `/events` | Список мероприятий с фильтрацией и пагинацией. Параметры: `title`, `startAt`, `endAt`, `page` $по умолч. 1$, `pageSize` $по умолч. 10$. | User |
| `GET` | `/events/{id}` | Получение мероприятия по ID $`Guid`$. | User |
| `POST` | `/events` | Создание нового мероприятия. Принимает `EventCreateDto`. Возвращает `202 Accepted` с URL ресурса. | Admin |
| `PUT` | `/events/{id}` | Обновление мероприятия. Принимает `EventUpdateDto`. Не позволяет изменить `TotalSeats`. Возвращает `204 No Content`. | Admin |
| `DELETE` | `/events/{id}` | Удаление мероприятия. | Admin |

### Защита от состояний гонки

- Транзакции с уровнем изоляции `RepeatableRead`.
- Применение `UPDLOCK` при чтении записей для блокировки на время обновления.
- Обработка `DbUpdateException` с повторной попыткой или откатом.
- Семафоры для синхронизации параллельных запросов.

### Поток данных через Kafka

- При получении события `booking.response` от `Bookings.Service`:
  - если статус `Rejected` — освобождает занятые места;
  - если статус `Confirmed` — подтверждает резервирование.
- При получении события `booking.cancellation` — освобождает места.

### Настройка

Строка подключения к БД:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=EventsDb;Username=postgres;Password=your_password"
  }
}
```

Параметры Kafka:

```json
{
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092"
  }
}
```

### Запуск

```bash
dotnet run --project Events.Service/Presentation/
```
