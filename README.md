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

## Деплой на VDS

Нужны Ubuntu, Docker и Docker Compose 2.24+ (для `ports: !override` в prod-файле). Сайт и API открываются с одного адреса: nginx отдаёт фронт и проксирует `/api/` на gateway. Postgres, Redis, RabbitMQ и Swagger снаружи не публикуются. Код на почту сейчас отключён, SMTP для запуска не нужен.

```bash
sudo apt update
sudo apt install -y git docker.io docker-compose-v2
sudo usermod -aG docker $USER
# перелогиньтесь, чтобы группа docker применилась

git clone https://github.com/NomosNet/studentp2.git
cd studentp2/backend
cp .env.example .env
```

В `backend/.env` замените `POSTGRES_PASSWORD` и `JWT_SECRET_KEY` (длинная случайная строка, не короче 32 символов). `SMTP_*` и `MAILTRAP_API_TOKEN` можно оставить пустыми.

Откройте порт 80 в файрволе и у хостера. Затем:

```bash
cd backend
docker compose -f docker-compose.yml -f docker-compose.vds.yml up --build -d
```

Сайт на VDS: `https://student-pass.ru`. Caddy сам получает сертификат Let's Encrypt. Локальный `docker compose up` этот файл не использует: gateway по-прежнему на порту 80, Swagger на `8001`.

Перед первым запуском с доменом направьте `student-pass.ru` на IP сервера (A-запись) и откройте порт 443:

```bash
sudo ufw allow 443/tcp
```

Сборка `ServiceNotify` качает пакет Mailtrap из GitHub Packages. В `backend/.env` на сервере заполните `GITHUB_USERNAME` и `GITHUB_PAT` (scope `read:packages`), иначе `docker compose up --build` остановится на этом образе. SMTP при отключённом коде на почту можно не задавать.

### CI/CD

Дальше сайт обновляется сам. Проверка сборки (фронт, ApiGateway, ServiceUsers) идёт на каждый push и pull request в `develop` и `main`. Выкладка на VDS — только push в `main` и ручной запуск workflow **Deploy**. Ветка `develop` на сервер не выкладывается: чтобы опубликовать работу, влейте её в `main`.

Один раз на сервере, пользователь с доступом к Docker (не обязательно root):

```bash
sudo mkdir -p /opt/studentpass
sudo chown "$USER":"$USER" /opt/studentpass
git clone https://github.com/NomosNet/studentp2.git /opt/studentpass
cd /opt/studentpass/backend
cp .env.example .env
# пароль Postgres, JWT_SECRET_KEY, GITHUB_USERNAME, GITHUB_PAT
```

Ключ для GitHub Actions, на своём компьютере:

```bash
ssh-keygen -t ed25519 -f studentpass-deploy -C "github-actions"
```

Публичную часть (`studentpass-deploy.pub`) добавьте в `~/.ssh/authorized_keys` этого пользователя на VDS. Приватный файл `studentpass-deploy` целиком — в секрет репозитория, пароль от сервера в GitHub не кладётся.

Секреты репозитория (Settings → Secrets and variables → Actions):

| Секрет | Значение |
|---|---|
| `VDS_HOST` | IP или домен сервера |
| `VDS_USER` | SSH-пользователь, который владеет `/opt/studentpass` и входит в группу `docker` |
| `VDS_SSH_KEY` | Приватный ключ целиком, вместе со строками `BEGIN` и `END` |
| `VDS_SSH_PORT` | Необязательно. Если пусто, используется `22` |
| `GITHUB_USERNAME` | Необязательно, только чтобы CI собирал `ServiceNotify` |
| `GITHUB_PAT` | Необязательно, PAT со scope `read:packages` для CI |

После того как секреты заданы и каталог `/opt/studentpass` уже склонирован, пуш в `main` запускает на сервере `deploy/vds-update.sh`: `git reset` на `origin/main` и `docker compose up --build -d`. Файл `backend/.env` в git не входит и при обновлении не затирается.

## SMTP для регистрации и авторизации

Проверка кода с почты временно закомментирована: регистрация проходит без письма. Блок ниже нужен, когда проверку снова включат.

**Чтобы пользователь получил код на почту, задайте `MAILTRAP_API_TOKEN` или SMTP.** Без этого письмо не уйдёт: код регистрации пишется только в лог контейнера `studentpass-notify`.

Регистрация работает так: пользователь указывает email → сервис кладёт письмо в очередь → `ServiceNotify` отправляет код. Если задан `MAILTRAP_API_TOKEN`, письмо уходит через Mailtrap Email API. Иначе используется SMTP ниже. Без обоих вариантов код пишется только в лог контейнера `studentpass-notify`.

Скопируйте `backend/.env.example` в `backend/.env` и заполните:

```env
SMTP_SERVER=smtp.yandex.ru
SMTP_PORT=587
SMTP_LOGIN=your-login@yandex.ru
SMTP_PASSWORD=your-app-password
MAILTRAP_API_TOKEN=
GITHUB_USERNAME=
GITHUB_PAT=
```

| Переменная | Назначение |
|---|---|
| `SMTP_SERVER` | Адрес SMTP-сервера (например `smtp.yandex.ru`, `smtp.gmail.com`) |
| `SMTP_PORT` | Обычно `587` (STARTTLS) |
| `SMTP_LOGIN` | Почта, с которой отправляются коды |
| `SMTP_PASSWORD` | Пароль или пароль приложения |
| `MAILTRAP_API_TOKEN` | API-токен Mailtrap. Если задан, SMTP не используется |
| `GITHUB_USERNAME` | GitHub-логин, нужен только чтобы скачать пакет `Mailtrap` при сборке |
| `GITHUB_PAT` | GitHub PAT со scope `read:packages`, нужен только при сборке |

После изменения `.env` перезапустите контейнеры:

```bash
cd backend
docker compose up -d
```

Когда проверку кода снова включат, на сайте нажмите «Регистрация» → «Получить код на почту». Письмо должно прийти на указанный адрес. Если SMTP пустой, смотрите логи:

```bash
docker logs studentpass-notify
```

Вход в уже существующий аккаунт SMTP не требует. Демо без почты:

- админ: `admin` / `admin`
- менеджер: `manager` / `manager`
