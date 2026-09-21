# StudentPass

Единая платформа студенческих скидок на профессиональный софт и AI-сервисы.

Репозиторий: [github.com/NomosNet/studentp2](https://github.com/NomosNet/studentp2)

- **frontend** — Vue 3 + Vite
- **backend** — ASP.NET Core (ApiGateway, ServiceUsers, ServiceNotify), PostgreSQL, Redis, RabbitMQ

## Быстрый старт

### 1. Бэкенд

Нужен Docker Desktop.

```bash
cd backend
copy .env.example .env
docker compose up --build -d
```

- Gateway: http://localhost
- Swagger: http://localhost:8001/swagger
- RabbitMQ UI: http://localhost:15672 (`guest` / `guest`)

Подробнее: [backend/README.md](backend/README.md).

### 2. Фронтенд

```bash
cd frontend
npm install
npm run dev
```

Сайт: http://localhost:5173  
API в dev: `VITE_API_BASE_URL=http://localhost` (см. `frontend/.env.example`).

Подробнее: [frontend/README.md](frontend/README.md).

## SMTP для регистрации и авторизации

**Чтобы пользователь получил код на почту и смог зарегистрироваться, нужно настроить SMTP.** Без SMTP письмо не уйдёт: код регистрации пишется только в лог контейнера `studentpass-notify`.

Регистрация работает так: пользователь указывает email → сервис кладёт письмо в очередь → `ServiceNotify` отправляет код через SMTP.

Скопируйте `backend/.env.example` в `backend/.env` и заполните:

```env
SMTP_SERVER=smtp.yandex.ru
SMTP_PORT=587
SMTP_LOGIN=your-login@yandex.ru
SMTP_PASSWORD=your-app-password
```

| Переменная | Назначение |
|---|---|
| `SMTP_SERVER` | Адрес SMTP-сервера (например `smtp.yandex.ru`, `smtp.gmail.com`) |
| `SMTP_PORT` | Обычно `587` (STARTTLS) |
| `SMTP_LOGIN` | Почта, с которой отправляются коды |
| `SMTP_PASSWORD` | Пароль или пароль приложения |

После изменения `.env` перезапустите контейнеры:

```bash
cd backend
docker compose up -d
```

Проверка: на сайте нажмите «Регистрация» → «Получить код на почту». Письмо должно прийти на указанный адрес. Если SMTP пустой, смотрите логи:

```bash
docker logs studentpass-notify
```

Вход в уже существующий аккаунт SMTP не требует. Демо без почты:

- админ: `admin` / `admin`
- менеджер: `manager` / `manager`
