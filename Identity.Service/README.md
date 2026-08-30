# Identity.Service

### Назначение

Микросервис отвечает за управление учётными записями пользователей, аутентификацию и авторизацию. Реализует регистрацию, вход, выдачу JWT-токенов и проверку прав доступа.

### Стек технологий

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + PostgreSQL
- JWT (HS256) для авторизации
- Swagger/OpenAPI для документации API
- Clean Architecture (Domain, Application, Infrastructure, Presentation)

### Основные возможности

- Регистрация пользователей $`POST /auth/register`$
- Аутентификация и получение JWT-токена $`POST /auth/login`$
- Разграничение прав: роли `Admin` и `User`
- Глобальная обработка ошибок по стандарту RFC 7807

### Архитектура

#### Domain Layer

- Модель `Account`: `Id`, `Login`, `PasswordHash`, `Role` $перечисление `AccountRole`$.
- Доменные исключения: `UserAlreadyExistsException`.

#### Application Layer

- Интерфейсы: `IIdentityService`, `ISecurityService`.
- DTO: `AccountRegisterDto`, `AccountSignInDto`, `AccountJWTInfoDto`.
- Сервисы: `IdentityService` $управление учётными записями$, `SecurityService` $хеширование паролей, генерация JWT$.

#### Infrastructure Layer

- Контекст БД: `AppDbContext` с конфигурацией через Fluent API $`AccountConfiguration`$.
- Миграции EF Core в папке `Migrations`.
- Реализация репозитория учётных записей.
- Настройка DI через `InfrastructureCollectionExtensions`.

#### Presentation Layer

- Контроллер: `AuthController` с эндпоинтами `/auth/register`, `/auth/login`.
- Middleware: `GlobalExceptionHandlingMiddleware`.
- Swagger с кнопкой Authorize для передачи JWT-токена.
- Конфигурация приложения и DI-контейнера в `Program.cs`.

### API эндпоинты

| Метод | Путь | Описание |
|-------|------|----------|
| `POST` | `/auth/register` | Регистрация нового пользователя. Возвращает `204 No Content` при успехе, `400 Bad Request` при ошибке валидации, `409 Conflict` если логин уже занят. |
| `POST` | `/auth/login` | Аутентификация пользователя. Возвращает JWT-токен в теле ответа $`200 OK`$. При неверных учётных данных — `404 Not Found`. |

### Ролевая модель и разграничение прав

- Роли: `Admin` $полный доступ$, `User` $ограниченный доступ$.
- При отсутствии токена — `401 Unauthorized`.
- При наличии токена, но недостаточных правах — `403 Forbidden`.

### Настройка

Параметры JWT задаются в `appsettings.json` в секции `JwtSettings`:

```json
{
  "JwtSettings": {
    "Secret": "1234567890123456789012",
    "Issuer": "https://example.com",
    "Audience": "https://example.com",
    "ExpirationMinutes": 10
  }
}
```

Строка подключения к БД:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=IdentityDb;Username=postgres;Password=your_password"
  }
}
```

> В продакшн-среде используйте криптографически стойкий секрет $не менее 32 символов$ и храните его в переменных окружения или специализированных хранилищах секретов.

### Запуск

```bash
dotnet run --project Identity.Service/Presentation/
```