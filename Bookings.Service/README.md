# Bookings.Service

### Назначение

Микросервис обрабатывает бронирования: создаёт записи, публикует события в Kafka, потребляет ответы от `Events.Service` и управляет жизненным циклом бронирования. Реализует асинхронную событийную модель взаимодействия — решение о подтверждении или отказе принимается не в момент запроса, а после обработки в `Events.Service`.

### Стек технологий

- ASP.NET Core Web API $.NET 10$
- Entity Framework Core + PostgreSQL
- Confluent.Kafka $продюсер и консьюмер$
- BackgroundService для потребления Kafka-сообщений
- Swagger/OpenAPI
- Clean Architecture

### Основные возможности

- Инициирование бронирования: создание записи со статусом `Pending` и публикация события `booking.created` в Kafka
- Получение информации о бронировании по ID $с проверкой владельца$
- Отмена бронирования с публикацией события `booking.cancellation` в Kafka
- Фоновая обработка ответов от `Events.Service` через `BookingBackgroundService` — потребление топика `booking.response`
- Обновление статуса бронирования $`Confirmed` / `Rejected`$ на основе Kafka-сообщений
- Автоматическая отмена при ошибках обновления статуса

### Архитектура

#### Domain Layer

- Модель `Booking`: `Id`, `EventId`, `Status`, `CreatedAt`, `ProcessedAt`.
- Перечисление `BookingStatus`: `Pending`, `Confirmed`, `Rejected`.
- Доменные исключения: `BookingAlreadyCancelledException`.

#### Application Layer

- Интерфейсы: `IBookingService`, `IBookingRepository`, `IBookingKafkaPublisher`.
- DTO: `BookingResponseDto`, `BookingProcessedDto`, `BookingRepositoryCreateDto`, `BookingRepositoryUpdateDto`.
- Сервис: `BookingService` с реализацией бизнес-логики.
  - `InitiateBookingAsync` — создаёт запись `Pending` и инициирует публикацию `booking.created`.
  - `GetBookingByIdAsync` — возвращает бронирование с проверкой прав $владелец или Admin$.
  - `CancelBookingAsync` — инициирует отмену и публикацию `booking.cancellation`.
  - `UpdateBookingStatusAsync` — обновляет статус по ответу из Kafka.

#### Infrastructure Layer

- Контекст БД: `AppDbContext` с конфигурацией через Fluent API $`BookingConfiguration`$.
- Репозиторий: `BookingRepository` с методами для CRUD и специализированных запросов.
- Kafka-продюсер: `BookingKafkaPublisher` — публикует события `booking.created` и `booking.cancellation`.
- Kafka-консьюмер: `BookingBackgroundService` — потребляет топик `booking.response` и обновляет статусы.
- Инициализация топиков Kafka: `KafkaTopicInitializer`.
- Миграции EF Core.
- Настройка DI через `InfrastructureCollectionExtensions`.

#### Presentation Layer

- Контроллер: `BookingsController` с эндпоинтами для создания, получения и отмены бронирований.
- Middleware: `GlobalExceptionHandlingMiddleware`.
- Swagger UI.
- Конфигурация приложения и DI-контейнера в `Program.cs`.

### API эндпоинты

Все эндпоинты требуют аутентификации $`[Authorize]`$. При отсутствии токена — `401 Unauthorized`.

| Метод | Путь | Описание | Доступ | Возможные коды ответа |
|-------|------|----------|--------|----------------------|
| `POST` | `/bookings/{eventId}` | Инициирует бронирование. НЕ проверяет существование мероприятия — создаёт запись `Pending` и публикует `booking.created` в Kafka. Решение принимается асинхронно в `Events.Service`. | Аутентифицированный пользователь | `202 Accepted` — бронирование принято в обработку; `400 Bad Request` — `userId` или `role` не найдены в claims; `403 Forbidden` — недостаточно прав; `500` — внутренняя ошибка |
| `GET` | `/bookings/{id}` | Получение информации о бронировании по ID. Обычный пользователь может запрашивать только свои брони; администратор — любые. | Аутентифицированный пользователь | `200 OK` — объект `BookingResponseDto`; `400 Bad Request` — `userId`/`role` не найдены; `403 Forbidden` — чужая бронь; `404 Not Found` — бронирование не найдено; `500` — внутренняя ошибка |
| `DELETE` | `/bookings/{id}` | Отмена бронирования. Инициирует отмену: публикует `booking.cancellation` в Kafka. Обычный пользователь может отменить только свою бронь; администратор — любую. | Аутентифицированный пользователь | `202 Accepted` — запрос на отмену отправлен; `400 Bad Request` — `userId`/`role` не найдены; `403 Forbidden` — чужая бронь; `404 Not Found` — бронирование не найдено; `409 Conflict` — бронирование уже отменено или в недопустимом статусе; `500` — внутренняя ошибка |

#### Ответ `POST /bookings/{eventId}`

При успехе возвращается `202 Accepted` с объектом `BookingResponseDto` и заголовком `Location`, указывающим URL для получения информации о бронировании $`GET /bookings/{id}`$:

```json
{
  "id": "2fad1bc1-bece-4c6a-8d84-d99eaf53d5bd",
  "eventId": "308dd020-a855-4e80-b29e-b3582b6de65c",
  "status": "Pending",
  "createdAt": "2026-08-26T10:00:00",
  "processedAt": null
}
```

#### Ответ `DELETE /bookings/{id}`

Возвращает `202 Accepted` с телом:

```json
{
  "id": "2fad1bc1-bece-4c6a-8d84-d99eaf53d5bd",
  "status": "Cancelling",
  "message": "Cancel request sent to queue."
}
```

### Жизненный цикл бронирования

1. **Инициирование** — пользователь вызывает `POST /bookings/{eventId}/book`. `Bookings.Service` создаёт запись со статусом `Pending`, публикует событие `booking.created` в Kafka и возвращает `202 Accepted`.
2. **Обработка в Events.Service** — `Events.Service` потребляет `booking.created`, проверяет существование мероприятия и наличие свободных мест, резервирует место и публикует `booking.response` со статусом `Confirmed` / `Rejected` / `Error`.
3. **Обновление статуса** — `BookingBackgroundService` в `Bookings.Service` потребляет `booking.response`:
   - `Confirmed` — обновляет статус бронирования на `Confirmed`, фиксирует `ProcessedAt`.
   - `Rejected` — обновляет статус на `Rejected`.
   - `Error` — обновляет статус на `Rejected`.
   - При ошибке обновления $`NotFoundException`$ публикует `booking.cancellation` для отката резервирования в `Events.Service`.
4. **Отмена** — пользователь или администратор вызывает `DELETE /bookings/{id}`. `Bookings.Service` публикует `booking.cancellation` в Kafka. `Events.Service` потребляет событие и освобождает места.

### Фоновая обработка: BookingBackgroundService

`BookingBackgroundService` — это Kafka-консьюмер, работающий на фоне при старте приложения.

**Конфигурация консьюмера:**
- `BootstrapServers` — из `KafkaSettings`.
- `GroupId` — `booking-consumer-group`.
- `AutoOffsetReset` — `Earliest`.
- `EnableAutoCommit` — `false` $ручной коммит после обработки$.
- `EnableAutoOffsetStore` — `false`.
- Топик подписки — `KafkaTopics.BookingResponse`.

**Логика работы:**

1. Подписывается на топик `booking.response` и ожидает сообщения.
2. Десериализует сообщение в `BookingResponse`, содержащий `Id`, `ProcessedAt`, `Status` $`Confirmed` / `Rejected` / `Error`$, `Message`.
3. В зависимости от статуса:
   - **`Confirmed`** — вызывает `UpdateBookingStatusAsync(id, Confirmed)`. При `NotFoundException` публикует `booking.cancellation` для отката.
   - **`Rejected`** — вызывает `UpdateBookingStatusAsync(id, Rejected)`.
   - **`Error`** — вызывает `UpdateBookingStatusAsync(id, Rejected)`.
   - Неизвестный статус — логирует предупреждение, пропускает.
4. Коммитит смещение $`consumer.Commit`$ после обработки каждого сообщения — даже при ошибке десериализации.
5. Корректно обрабатывает `OperationCanceledException` при остановке приложения.
6. Создаёт изолированные DI-области $`IServiceScopeFactory`$ для каждого сообщения.

### Kafka-продюсер: BookingKafkaPublisher

`BookingKafkaPublisher` реализует `IBookingKafkaPublisher` и публикует два типа событий:

| Метод | Топик | Назначение | Ключ сообщения |
|-------|-------|------------|---------------|
| `PublishBookingCreatedAsync` | `KafkaTopics.BookingCreated` | Создано новое бронирование — `Events.Service` должен проверить и зарезервировать место | `Booking.Id` |
| `PublishBookingCancellationAsync` | `KafkaTopics.BookingCancellation` | Бронирование отменено — `Events.Service` должен освободить место | `Booking.Id` |

**Конфигурация продюсера:**
- `BootstrapServers` — из `KafkaSettings`.
- `Acks` — `All` $гарантия доставки на все реплики$.
- Сообщения сериализуются через `System.Text.Json`.

### Поток данных через Kafka

| Топик | Кто публикует | Кто потребляет | Назначение |
|-------|---------------|----------------|-----------|
| `booking.created` | `Bookings.Service` | `Events.Service` | Новое бронирование — проверить мероприятие и зарезервировать место |
| `booking.response` | `Events.Service` | `Bookings.Service` | Результат обработки: `Confirmed`, `Rejected` или `Error` |
| `booking.cancellation` | `Bookings.Service` | `Events.Service` | Бронирование отменено — освободить место |

Топики создаются автоматически при старте приложения через `KafkaTopicInitializer`.

### Настройка

Строка подключения к БД:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=BookingsDb;Username=postgres;Password=your_password"
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
dotnet run --project Bookings.Service/Presentation/
```