# Event Manager: распределённая система управления мероприятиями и бронированиями

## О проекте

Event Manager — распределённая система на стеке .NET 10 + PostgreSQL + Kafka для управления мероприятиями, учётными записями и обработкой бронирований. Архитектура построена по принципам Clean Architecture в каждом микросервисе с разделением на слои Domain, Application, Infrastructure, Presentation.

Система реализует:
- RESTful API с авторизацией через JWT и документацией Swagger;
- управление мероприятиями (создание, фильтрация, пагинация, резервирование мест);
- обработку бронирований с асинхронной фоновой обработкой и защитой от состояний гонки;
- разграничение прав (Admin/User) и глобальную обработку ошибок по RFC 7807;
- асинхронную коммуникацию между сервисами через Kafka (событийная модель).

---

## Состав системы

| Сервис | Назначение |
| --- | --- |
| `Identity.Service` | Управление учётными записями, авторизация, генерация JWT-токенов |
| `Events.Service` | Управление мероприятиями: создание, фильтрация, резервирование/освобождение мест |
| `Bookings.Service` | Обработка бронирований: создание, фоновая обработка, публикация событий в Kafka |

Все сервисы используют Kafka как шину событий. Конфигурация Kafka вынесена в `docker-compose.yaml`.

---

## Поток данных и взаимодействие сервисов

### Основные события Kafka

Определены в контрактах (`CSCourse.Contracts.Kafka`):

- `booking.created` — публикуется `Bookings.Service` при создании бронирования со статусом `Pending`.
- `booking.response` — публикуется `Bookings.Service` после завершения обработки (статус `Confirmed`/`Rejected`).
- `booking.cancellation` — публикуется при отмене бронирования.

### Сценарии взаимодействия

1. **Создание бронирования**
   - Пользователь вызывает `POST /events/{eventId}/book` в `Events.Service`.
   - `Events.Service` проверяет наличие мероприятия и доступные места.
   - При успехе `Events.Service` отправляет запрос в `Bookings.Service` (через HTTP или внутреннюю шину, в зависимости от реализации).
   - `Bookings.Service`:
     - создаёт запись бронирования со статусом `Pending`;
     - публикует событие `booking.created` в Kafka;
     - возвращает ответ `202 Accepted`.
   - Фоновый сервис `BookingBackgroundService` периодически опрашивает ожидающие брони (`Pending`) и имитирует обработку (задержка 2 с).
   - После обработки статус меняется на `Confirmed` или `Rejected`, и публикуется событие `booking.response`.

2. **Отмена бронирования**
   - Пользователь или администратор вызывает отмену.
   - `Bookings.Service` обновляет статус, публикует `booking.cancellation`.
   - Другие сервисы могут реагировать на отмену (например, освободить места в `Events.Service`).

3. **Реакция на события**
   - В текущей реализации `Events.Service` и `Identity.Service` подписаны на соответствующие топики Kafka (или будут подписаны в будущем).
   - При получении `booking.response`:
     - `Events.Service` может обновить счётчики занятых мест, если это не сделано атомарно при обработке.
   - При получении `booking.cancellation`:
     - `Events.Service` освобождает места, если они были зарезервированы.

4. **Обработка ошибок и отмена**
   - Если обработка бронирования завершается ошибкой, статус устанавливается в `Rejected`, публикуется `booking.response` с признаком ошибки.
   - Все операции выполняются в транзакциях с уровнем изоляции `RepeatableRead`, используются семафоры и блокировки (`UPDLOCK`) для защиты от состояний гонки.

---

## Структура проекта

```txt
├── Bookings.Service # Микросервис бронирований
│ ├── Application # Интерфейсы, DTO, сервисы
│ ├── Domain # Модели и исключения домена
│ ├── Infrastructure # EF Core, репозитории, миграции, Kafka-клиенты
│ └── Presentation # Контроллеры, middleware, Program.cs
├── Contracts # Общие контракты: исключения, DTO, Kafka-топики
├── docker-compose.yaml # Оркестрация контейнеров (PostgreSQL, Kafka, сервисы)
├── Events.Service # Микросервис мероприятий
│ ├── Application
│ ├── Domain
│ ├── Infrastructure
│ └── Presentation
├── Identity.Service # Микросервис идентификации
│ ├── Application
│ ├── Domain
│ ├── Infrastructure
│ └── Presentation
└── README.md # Эта документация
```

---

## Запуск системы

```bash
docker compose up
dotnet run --project Bookings.Service/Presentation/
dotnet run --project Events.Service/Presentation/
dotnet run --project Identity.Service/Presentation/
```

Swagger UI доступен по адресу /swagger для каждого сервиса.