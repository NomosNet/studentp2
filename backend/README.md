# StudentPass Backend (ASP.NET Core)

Микросервисы платформы студенческих скидок:

- **ApiGateway** (порт 80) — JWT, CORS, rate limit, прокси `/api/v1/*`
- **ServiceUsers** (порт 8001) — пользователи, объявления, партнёры, админка, Swagger
- **ServiceNotify** — письма из очереди `email_queue`
- PostgreSQL, Redis, RabbitMQ

## Docker Desktop

1. Запустите Docker Desktop.
2. В нём: **Containers** → кнопка **Compose** / **Open compose** → выберите файл `backend/docker-compose.yml` (или корневой `compose.yaml`).
3. Нажмите **Start** / **Run**.

Первый запуск соберёт образы. Дальше в списке появится группа **studentpass**: Start / Stop / Restart без консоли.

Через терминал:

```bash
cd backend
docker compose up --build -d
```

Gateway: `http://localhost`  
Swagger: `http://localhost:8001/swagger`  
RabbitMQ UI: `http://localhost:15672` (guest/guest)

Фронт (`frontend`) в dev ходит на `VITE_API_BASE_URL=http://localhost`.

Если Postgres уже поднимался со старым Python-бэкендом и нет базы `users_db`:

```bash
cd backend
docker compose down -v
docker compose up --build -d
```

Это сотрёт данные БД.

Переменные — `.env.example`. Если задан `MAILTRAP_API_TOKEN`, `ServiceNotify` отправляет письма через Mailtrap. Без Mailtrap и без SMTP коды регистрации пишутся в лог контейнера `studentpass-notify`.

## Локально без Docker

Нужны PostgreSQL (`users_db`), Redis и RabbitMQ. Затем:

```bash
cd backend
dotnet run --project src/ApiGateway
dotnet run --project src/ServiceUsers
dotnet run --project src/ServiceNotify
```

Gateway по умолчанию проксирует на `http://localhost:8001`.
