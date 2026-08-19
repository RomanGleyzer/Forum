# Forum

**Forum** — веб-приложение форума с backend на **ASP.NET Core Web API** и простым клиентским интерфейсом.

Приложение позволяет пользователям регистрироваться и авторизовываться, работать со своим профилем, публиковать записи, просматривать ленту и загружать аватар. Backend предоставляет REST API, а демонстрационный web-интерфейс на HTML, CSS и JavaScript обслуживается непосредственно ASP.NET Core приложением.

Проект разработан как практический пример построения backend-приложения с разделением ответственности между слоями, JWT-аутентификацией, PostgreSQL, Redis, валидацией запросов, централизованной обработкой ошибок, структурированным логированием и контейнеризацией.

## Возможности

На текущий момент реализованы:

* регистрация пользователей;
* авторизация с использованием JWT;
* получение информации о текущем пользователе;
* получение и изменение профиля;
* создание публикаций;
* получение публикации по идентификатору;
* получение ленты публикаций с cursor-based пагинацией;
* получение публикаций конкретного пользователя;
* загрузка пользовательского аватара;
* преобразование загружаемых изображений в WebP;
* выдача и HTTP-кэширование пользовательских аватаров;
* Redis-кэширование;
* ограничение частоты запросов для чувствительных операций;
* централизованная обработка исключений;
* автоматическое применение EF Core migrations в режиме `Development`;
* health check приложения и базы данных;
* Swagger/OpenAPI-документация;
* структурированное логирование;
* интеграция с OpenTelemetry;
* запуск всего окружения через Docker Compose;
* простой web-интерфейс для взаимодействия с приложением.

## Технологический стек

### Backend

* **C#**
* **.NET 9**
* **ASP.NET Core Web API**
* **Entity Framework Core**
* **ASP.NET Core Identity**
* **JWT Bearer Authentication**
* **MediatR**
* **FluentValidation**
* **AutoMapper**

### Работа с данными

* **PostgreSQL 18**
* **Npgsql**
* **Redis 8**

### Web UI

* **HTML**
* **CSS**
* **JavaScript**
* ASP.NET Core Static Files

Web-интерфейс предназначен прежде всего для демонстрации возможностей API и позволяет взаимодействовать с основными функциями приложения без отдельного frontend-проекта.

### Infrastructure

* **Docker**
* **Docker Compose**
* **Serilog**
* **OpenTelemetry**
* **Swagger / OpenAPI**
* **ASP.NET Core Rate Limiting**
* **ASP.NET Core Health Checks**
* **ImageSharp**

## Архитектура

Приложение разделено на четыре основных слоя:

```text
src/
├── Domain/
├── Application/
├── Infrastructure/
│   └── Migrations/
├── SocialNetworkAPI/
│   ├── Controllers/
│   ├── Extensions/
│   ├── Middleware/
│   ├── Services/
│   ├── wwwroot/
│   ├── Dockerfile
│   └── Program.cs
├── docker-compose.yml
└── SocialNetworkAPI.sln
```

Зависимости между слоями организованы таким образом, чтобы прикладная и доменная логика не зависели от конкретных инфраструктурных деталей.

### Domain

Содержит доменные сущности и основные модели предметной области.

Слой не зависит от HTTP, базы данных, Redis и других инфраструктурных механизмов.

### Application

Содержит прикладную логику и сценарии использования системы:

* команды;
* запросы;
* DTO;
* MediatR handlers;
* валидацию;
* абстракции;
* pipeline behaviors;
* прикладные исключения.

Для разделения операций чтения и изменения данных используются элементы подхода **CQRS**.

### Infrastructure

Содержит реализации инфраструктурных компонентов:

* доступ к PostgreSQL через Entity Framework Core;
* EF Core migrations;
* ASP.NET Core Identity;
* JWT;
* Redis;
* репозитории;
* хранение пользовательских файлов;
* работу с кэшированием;
* логирование;
* конфигурацию инфраструктурных сервисов.

### SocialNetworkAPI

Представляет входную точку приложения и отвечает за HTTP-уровень:

* REST API;
* контроллеры;
* middleware;
* authentication и authorization;
* Swagger;
* rate limiting;
* health checks;
* CORS;
* статические файлы;
* конфигурацию HTTP pipeline.

## REST API

Большинство операций приложения доступны через REST API.

Защищенные endpoint'ы требуют JWT Bearer token:

```http
Authorization: Bearer <token>
```

### Аутентификация

Регистрация пользователя:

```http
POST /api/auth/register
```

Авторизация:

```http
POST /api/auth/login
```

При успешной авторизации API возвращает JWT, который используется для обращения к защищенным ресурсам.

Для endpoint авторизации дополнительно настроен rate limiting.

### Пользователь

Получение данных текущего пользователя:

```http
GET /api/users/me
```

Получение профиля:

```http
GET /api/users/me/profile
```

Изменение профиля:

```http
PUT /api/users
```

Все перечисленные операции требуют авторизации.

### Публикации

Получение ленты:

```http
GET /api/posts
```

Поддерживаются параметры cursor-based пагинации:

```text
cursorCreatedAt
cursorId
take
```

Получение конкретной публикации:

```http
GET /api/posts/{id}
```

Создание публикации:

```http
POST /api/posts
```

Получение публикаций определенного пользователя:

```http
GET /api/users/{userId}/posts
```

Для получения общей ленты используется **cursor-based pagination** по дате создания и идентификатору публикации.

На уровне PostgreSQL для этого создан составной индекс:

```text
(CreationDate, Id)
```

Это позволяет эффективнее получать следующие страницы ленты без использования обычной offset-пагинации.

### Аватар пользователя

Загрузка или изменение аватара:

```http
POST /api/users/me/avatar
```

Endpoint принимает изображение через `multipart/form-data`.

Для загрузки настроены:

* ограничение размера HTTP-запроса;
* ограничение допустимого размера изображения;
* проверка MIME-типа;
* rate limiting;
* обработка изображения;
* сохранение результата в формате WebP.

Получение сохраненного аватара:

```http
GET /api/files/avatars/{userId}/{avatarId}
```

Endpoint получения изображения является публичным.

Для HTTP-кэширования используются:

* `ETag`;
* `If-None-Match`;
* `Last-Modified`;
* `Cache-Control`;
* ответ `304 Not Modified`.

## Безопасность

В приложении используются несколько механизмов защиты.

### Authentication и Authorization

Аутентификация реализована с помощью:

* ASP.NET Core Identity;
* JWT Bearer Authentication.

Защищенные endpoint'ы требуют действительный JWT.

### Rate Limiting

Используются отдельные политики для различных типов запросов.

Для авторизации применяется **Fixed Window Rate Limiter**.

Для загрузки файлов используется **Token Bucket Rate Limiter**.

Это ограничивает количество чувствительных операций, которые клиент может выполнить за короткий промежуток времени.

### Работа с файлами

При загрузке и выдаче пользовательских изображений предусмотрены:

* ограничение размера запроса;
* проверка входных данных;
* проверка MIME-типа;
* защита от path traversal;
* выдача фиксированного типа содержимого;
* HTTP-кэширование.

### CORS

Для API используется отдельная CORS-политика.

Разрешенные origins задаются через конфигурацию приложения и могут быть переопределены через переменные окружения.

## PostgreSQL и Entity Framework Core

Для основной базы данных используется **PostgreSQL**.

Доступ к данным осуществляется через **Entity Framework Core** и провайдер **Npgsql**.

Схема базы данных управляется с помощью EF Core migrations.

### Автоматические миграции

В режиме:

```text
Development
```

приложение при запуске автоматически выполняет:

```csharp
Database.MigrateAsync()
```

Поэтому при запуске через Docker Compose существующие миграции автоматически применяются к базе данных.

Отдельно выполнять:

```bash
dotnet ef database update
```

для обычного Development-запуска не требуется.

> Автоматическое применение migrations при старте используется здесь для упрощения локальной разработки. Для production-окружения предпочтительнее применять миграции как отдельный этап развертывания.

### Создание новой migration

После изменения EF Core-модели необходимо создать новую migration.

Из корня репозитория:

```bash
dotnet ef migrations add MigrationName \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/SocialNetworkAPI/SocialNetworkAPI.csproj
```

Перед применением рекомендуется проверить содержимое созданной migration.

### Проверка модели

Проверить, существуют ли изменения модели, для которых еще не создана migration:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/SocialNetworkAPI/SocialNetworkAPI.csproj
```

При отсутствии несохраненных изменений EF Core сообщит:

```text
No changes have been made to the model since the last migration.
```

## Redis

Redis используется в качестве внешнего in-memory хранилища для кэширования.

Подключение осуществляется через `StackExchange.Redis`.

В Docker Compose Redis запускается отдельным контейнером и доступен API внутри Docker-сети по адресу:

```text
redis:6379
```

Для Redis используется persistent volume, поэтому данные могут сохраняться между перезапусками контейнера.

## Работа с изображениями

Приложение поддерживает загрузку пользовательских аватаров.

Для обработки изображений используется **ImageSharp**.

После проверки загруженное изображение обрабатывается и сохраняется в формате:

```text
WebP
```

Файлы хранятся отдельно от основной базы данных.

При запуске через Docker Compose каталог пользовательских изображений подключен к отдельному Docker volume, поэтому загруженные аватары не теряются при пересоздании контейнера API.

## Логирование и наблюдаемость

### Serilog

Для структурированного логирования используется **Serilog**.

Логи выводятся:

* в консоль;
* в файлы.

При Docker-запуске файловые логи сохраняются в отдельный volume.

### OpenTelemetry

Проект содержит интеграцию с **OpenTelemetry**, позволяющую расширить наблюдаемость приложения и подключить внешние системы сбора telemetry и distributed tracing.

## Health Check

Состояние приложения можно проверить через:

```http
GET /health
```

Health check также проверяет доступность `SocialNetworkDbContext`.

При запуске окружения Docker Compose PostgreSQL и Redis имеют собственные health checks.

API запускается после того, как необходимые инфраструктурные сервисы становятся готовы к работе.

## Swagger / OpenAPI

В режиме `Development` доступен Swagger UI.

При Docker-запуске:

```text
http://localhost:8080/swagger
```

Swagger позволяет:

* просматривать доступные endpoint'ы;
* изучать параметры запросов;
* видеть возможные HTTP-ответы;
* отправлять запросы к API непосредственно из браузера.

## Web UI

Помимо REST API, приложение содержит небольшой клиентский интерфейс.

Он находится в:

```text
src/SocialNetworkAPI/wwwroot/
```

В частности, реализованы страницы:

```text
index.html
login.html
register.html
profile.html
```

ASP.NET Core обслуживает эти файлы через Static Files middleware.

При Docker-запуске главная страница доступна по адресу:

```text
http://localhost:8080
```

Web UI является демонстрационным клиентом проекта и использует REST API приложения.

---

# Запуск проекта

Проект можно запустить двумя способами:

1. через Docker Compose — рекомендуемый и самый простой вариант;
2. локально через .NET CLI.

## Вариант 1 — Docker Compose

Docker Compose запускает все необходимые компоненты приложения:

```text
                   ┌─────────────────────┐
                   │        API          │
                   │ ASP.NET Core/.NET 9 │
                   │       :8080         │
                   └─────────┬───────────┘
                             │
                  ┌──────────┴──────────┐
                  │                     │
                  ▼                     ▼
        ┌─────────────────┐   ┌─────────────────┐
        │   PostgreSQL    │   │      Redis      │
        │       18        │   │        8        │
        │      :5432      │   │      :6379      │
        └─────────────────┘   └─────────────────┘
```

### Требования

Для этого варианта требуется:

* **Docker**
* **Docker Compose**

Устанавливать .NET SDK, PostgreSQL и Redis непосредственно в систему для запуска приложения не требуется.

### 1. Клонировать репозиторий

```bash
git clone https://github.com/RomanGleyzer/Forum.git
cd Forum
```

### 2. Создать `.env`

Создайте файл:

```text
src/.env
```

Например:

```env
POSTGRES_DB=socialnet
POSTGRES_USER=postgres
POSTGRES_PASSWORD=change_this_password

JWT_KEY=replace_this_with_a_long_random_secret_key_at_least_32_bytes

CORS_ALLOWED_ORIGIN=http://localhost:8080
```

Не рекомендуется добавлять настоящий `.env` с паролями и секретами в Git.

JWT-ключ должен содержать не менее 32 байт.

### 3. Перейти в каталог `src`

```bash
cd src
```

### 4. Запустить приложение

```bash
docker compose up --build
```

Docker Compose:

1. создаст необходимые volumes;
2. запустит PostgreSQL;
3. запустит Redis;
4. дождется прохождения health checks;
5. соберет Docker-образ API;
6. запустит ASP.NET Core приложение;
7. применит существующие EF Core migrations в режиме `Development`.

После успешного запуска доступны:

| Компонент    | Адрес                           |
| ------------ | ------------------------------- |
| Web UI       | `http://localhost:8080`         |
| REST API     | `http://localhost:8080/api/...` |
| Swagger      | `http://localhost:8080/swagger` |
| Health Check | `http://localhost:8080/health`  |
| PostgreSQL   | `localhost:5431`                |
| Redis        | `localhost:6379`                |

PostgreSQL внутри Docker-сети работает на стандартном порту `5432`, а для подключения с хост-машины опубликован порт `5431`.

### Запуск в фоне

```bash
docker compose up --build -d
```

### Просмотр контейнеров

```bash
docker compose ps
```

### Просмотр логов

Все сервисы:

```bash
docker compose logs
```

Только API:

```bash
docker compose logs api
```

Наблюдение за логами API в реальном времени:

```bash
docker compose logs -f api
```

### Остановка

```bash
docker compose down
```

При обычном `docker compose down` persistent volumes сохраняются.

Чтобы полностью удалить контейнеры вместе с данными PostgreSQL, Redis, пользовательскими файлами и логами:

```bash
docker compose down -v
```

> Команда с `-v` удаляет persistent volumes и предназначена только для случаев, когда сохраненные данные больше не нужны.

---

## Вариант 2 — локальный запуск через .NET CLI

Этот вариант удобен во время разработки, когда API запускается непосредственно через `dotnet run`.

### Требования

Потребуются:

* **.NET 9 SDK**;
* доступный **PostgreSQL**;
* доступный **Redis**.

PostgreSQL и Redis при желании можно продолжать запускать через Docker Compose.

### Запуск только инфраструктуры

Из каталога `src`:

```bash
docker compose up -d postgres redis
```

В этом случае:

```text
PostgreSQL → localhost:5431
Redis      → localhost:6379
```

### Перейти в API-проект

```bash
cd SocialNetworkAPI
```

Если команда выполняется из корня репозитория:

```bash
cd src/SocialNetworkAPI
```

### Настроить PostgreSQL

Для локальной разработки секретные параметры рекомендуется хранить через .NET User Secrets:

```bash
dotnet user-secrets set \
  "ConnectionStrings:PostgreSQLConnection" \
  "Host=localhost;Port=5431;Database=socialnet;Username=postgres;Password=YOUR_PASSWORD"
```

Если PostgreSQL установлен непосредственно на компьютере и работает на стандартном порту, вместо `5431` обычно будет использоваться `5432`.

### Настроить Redis

```bash
dotnet user-secrets set \
  "ConnectionStrings:Redis" \
  "localhost:6379"
```

### Настроить JWT

```bash
dotnet user-secrets set \
  "Jwt:Key" \
  "YOUR_LONG_SECRET_KEY_AT_LEAST_32_BYTES"
```

### Восстановить зависимости

```bash
dotnet restore
```

### Запустить

```bash
dotnet run
```

ASP.NET Core выведет адреса приложения в консоль после запуска.

В режиме `Development` существующие EF Core migrations будут автоматически применены к базе данных.

## Docker

API собирается с помощью многоэтапного `Dockerfile` на базе официальных образов .NET 9.

Сборка разделена на несколько этапов:

```text
restore
   ↓
build
   ↓
publish
   ↓
ASP.NET Core Runtime
```

Build context включает все проекты решения:

```text
Domain
Application
Infrastructure
SocialNetworkAPI
```

Это позволяет корректно восстанавливать и собирать межпроектные зависимости внутри Docker.

В Docker Compose используются отдельные persistent volumes:

```text
postgres_data
redis_data
media_data
logs_data
```

Они отвечают соответственно за:

* данные PostgreSQL;
* данные Redis;
* пользовательские изображения;
* файловые логи приложения.

## Статус проекта

Проект находится в разработке и используется для практической реализации и демонстрации подходов к созданию backend-приложений на платформе .NET.

В дальнейшем функциональность может быть расширена, например:

* полноценной системой комментариев;
* дополнительными возможностями взаимодействия между пользователями;
* автоматизированными тестами;
* CI/CD;
* расширенной observability-инфраструктурой.

## Автор

**Roman Gleyzer**

GitHub: [RomanGleyzer](https://github.com/RomanGleyzer)
